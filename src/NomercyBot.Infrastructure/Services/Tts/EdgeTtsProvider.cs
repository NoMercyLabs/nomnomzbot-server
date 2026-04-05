// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Tts;

/// <summary>
/// Free TTS provider using the Microsoft Edge Read Aloud WebSocket API.
/// No API key required — works immediately for all channels.
///
/// Uses the unofficial wss://speech.platform.bing.com endpoint that powers
/// Edge's built-in read-aloud feature.
/// </summary>
public sealed class EdgeTtsProvider : ITtsProvider
{
    private const string ProviderName = "edge";
    private const string WssUrl = "wss://speech.platform.bing.com/consumer/speech/synthesize/realtimetts/edge/v1?TrustedClientToken=6A5AA1D4EAFF4E9FB37E23D68491D6F4&ConnectionId=";
    private const string TrustedToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";

    private readonly ILogger<EdgeTtsProvider> _logger;

    public EdgeTtsProvider(ILogger<EdgeTtsProvider> logger)
    {
        _logger = logger;
    }

    public async Task<TtsSynthesisResult> SynthesizeAsync(
        string text,
        string voiceId,
        CancellationToken cancellationToken = default)
    {
        var connectionId = Guid.NewGuid().ToString("N");
        var url = $"wss://speech.platform.bing.com/consumer/speech/synthesize/realtimetts/edge/v1?TrustedClientToken={TrustedToken}&ConnectionId={connectionId}";

        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
        ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        try
        {
            await ws.ConnectAsync(new Uri(url), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Edge TTS: Failed to connect");
            return EmptyResult(voiceId);
        }

        // Send configuration
        var configMsg = BuildConfigMessage(connectionId);
        await SendTextAsync(ws, configMsg, cancellationToken);

        // Send synthesis request
        var requestId = Guid.NewGuid().ToString("N");
        var ssml = BuildSsml(text, voiceId);
        var synthesisMsg = BuildSynthesisMessage(requestId, ssml);
        await SendTextAsync(ws, synthesisMsg, cancellationToken);

        // Collect audio chunks
        using var audioStream = new MemoryStream();
        var buffer = new byte[32768];

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                using var msgStream = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await ws.ReceiveAsync(buffer, cancellationToken);
                    msgStream.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                var msgBytes = msgStream.ToArray();

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Binary message: header\r\n\r\n<audio bytes>
                    var headerEnd = FindHeaderEnd(msgBytes);
                    if (headerEnd >= 0)
                    {
                        var audioStart = headerEnd + 4;
                        if (audioStart < msgBytes.Length)
                            audioStream.Write(msgBytes, audioStart, msgBytes.Length - audioStart);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var text2 = Encoding.UTF8.GetString(msgBytes);
                    if (text2.Contains("turn.end"))
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Edge TTS: Error receiving audio for voice {VoiceId}", voiceId);
        }
        finally
        {
            if (ws.State == WebSocketState.Open)
            {
                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None); }
                catch { /* ignore close errors */ }
            }
        }

        var audioData = audioStream.ToArray();
        if (audioData.Length == 0)
        {
            _logger.LogWarning("Edge TTS: No audio received for voice {VoiceId}", voiceId);
            return EmptyResult(voiceId);
        }

        // Estimate duration: MP3 ~128kbps = 16 KB/s
        int durationMs = (int)(audioData.Length / 16.0 * 1000.0 / 1024.0);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text + voiceId)))[..16];

        return new TtsSynthesisResult
        {
            AudioData = audioData,
            DurationMs = durationMs,
            Provider = ProviderName,
            VoiceId = voiceId,
            ContentHash = hash,
        };
    }

    public Task<IReadOnlyList<TtsVoiceInfo>> GetVoicesAsync(CancellationToken cancellationToken = default)
    {
        // Curated subset of popular Edge TTS voices
        IReadOnlyList<TtsVoiceInfo> voices =
        [
            new TtsVoiceInfo { Id = "en-US-AriaNeural",        Name = "Aria",        DisplayName = "Aria (US)",         Locale = "en-US", Gender = "Female", Provider = ProviderName },
            new TtsVoiceInfo { Id = "en-US-GuyNeural",         Name = "Guy",         DisplayName = "Guy (US)",          Locale = "en-US", Gender = "Male",   Provider = ProviderName },
            new TtsVoiceInfo { Id = "en-US-JennyNeural",       Name = "Jenny",       DisplayName = "Jenny (US)",        Locale = "en-US", Gender = "Female", Provider = ProviderName },
            new TtsVoiceInfo { Id = "en-US-DavisNeural",       Name = "Davis",       DisplayName = "Davis (US)",        Locale = "en-US", Gender = "Male",   Provider = ProviderName },
            new TtsVoiceInfo { Id = "en-GB-SoniaNeural",       Name = "Sonia",       DisplayName = "Sonia (UK)",        Locale = "en-GB", Gender = "Female", Provider = ProviderName },
            new TtsVoiceInfo { Id = "en-GB-RyanNeural",        Name = "Ryan",        DisplayName = "Ryan (UK)",         Locale = "en-GB", Gender = "Male",   Provider = ProviderName },
            new TtsVoiceInfo { Id = "en-AU-NatashaNeural",     Name = "Natasha",     DisplayName = "Natasha (AU)",      Locale = "en-AU", Gender = "Female", Provider = ProviderName },
            new TtsVoiceInfo { Id = "en-CA-ClaraNeural",       Name = "Clara",       DisplayName = "Clara (CA)",        Locale = "en-CA", Gender = "Female", Provider = ProviderName },
            new TtsVoiceInfo { Id = "de-DE-KatjaNeural",       Name = "Katja",       DisplayName = "Katja (DE)",        Locale = "de-DE", Gender = "Female", Provider = ProviderName },
            new TtsVoiceInfo { Id = "de-DE-ConradNeural",      Name = "Conrad",      DisplayName = "Conrad (DE)",       Locale = "de-DE", Gender = "Male",   Provider = ProviderName },
            new TtsVoiceInfo { Id = "fr-FR-DeniseNeural",      Name = "Denise",      DisplayName = "Denise (FR)",       Locale = "fr-FR", Gender = "Female", Provider = ProviderName },
            new TtsVoiceInfo { Id = "es-ES-ElviraNeural",      Name = "Elvira",      DisplayName = "Elvira (ES)",       Locale = "es-ES", Gender = "Female", Provider = ProviderName },
            new TtsVoiceInfo { Id = "ja-JP-NanamiNeural",      Name = "Nanami",      DisplayName = "Nanami (JP)",       Locale = "ja-JP", Gender = "Female", Provider = ProviderName },
            new TtsVoiceInfo { Id = "ko-KR-SunHiNeural",       Name = "SunHi",       DisplayName = "SunHi (KR)",        Locale = "ko-KR", Gender = "Female", Provider = ProviderName },
            new TtsVoiceInfo { Id = "pt-BR-FranciscaNeural",   Name = "Francisca",   DisplayName = "Francisca (BR)",    Locale = "pt-BR", Gender = "Female", Provider = ProviderName },
        ];

        return Task.FromResult(voices);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static string BuildConfigMessage(string connectionId)
    {
        var timestamp = DateTime.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss 'GMT+0000 (Coordinated Universal Time)'");
        return $"X-Timestamp:{timestamp}\r\nContent-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n"
            + JsonSerializer.Serialize(new
            {
                context = new
                {
                    synthesis = new
                    {
                        audio = new
                        {
                            metadataoptions = new { sentenceBoundaryEnabled = false, wordBoundaryEnabled = false },
                            outputFormat = "audio-24khz-48kbitrate-mono-mp3",
                        },
                    },
                },
            });
    }

    private static string BuildSynthesisMessage(string requestId, string ssml)
    {
        var timestamp = DateTime.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss 'GMT+0000 (Coordinated Universal Time)'");
        return $"X-RequestId:{requestId}\r\nContent-Type:application/ssml+xml\r\nX-Timestamp:{timestamp}\r\nPath:ssml\r\n\r\n{ssml}";
    }

    private static string BuildSsml(string text, string voiceId)
    {
        var escaped = System.Security.SecurityElement.Escape(text) ?? text;
        return $"""
                <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
                  <voice name='{voiceId}'>
                    <prosody rate='+0%' pitch='+0Hz'>{escaped}</prosody>
                  </voice>
                </speak>
                """;
    }

    private static async Task SendTextAsync(ClientWebSocket ws, string message, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    private static int FindHeaderEnd(byte[] data)
    {
        for (int i = 0; i < data.Length - 3; i++)
        {
            if (data[i] == '\r' && data[i + 1] == '\n' && data[i + 2] == '\r' && data[i + 3] == '\n')
                return i;
        }
        return -1;
    }

    private static TtsSynthesisResult EmptyResult(string voiceId) => new()
    {
        AudioData = [],
        DurationMs = 0,
        Provider = ProviderName,
        VoiceId = voiceId,
        ContentHash = string.Empty,
    };
}
