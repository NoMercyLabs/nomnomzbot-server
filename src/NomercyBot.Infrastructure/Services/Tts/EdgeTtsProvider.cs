// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Tts;

/// <summary>
/// TTS provider using Microsoft Edge's free Speech Synthesis service.
///
/// Edge TTS is a free, no-auth HTTP API used by the browser's built-in speech synthesis.
/// It supports 400+ voices across 100+ locales and requires no API key.
///
/// HTTP flow (matching edge-tts open-source client):
///   1. Fetch the voice list from the Cognitive Services voices endpoint.
///   2. Open an SSE stream to the synthesis endpoint with SSML payload.
///   3. Parse binary audio chunks from the stream and concatenate them.
///
/// Audio format: MP3 (audio-24khz-48kbitrate-mono-mp3) by default.
/// </summary>
public sealed partial class EdgeTtsProvider : ITtsProvider
{
    private const string ProviderName = "edge";
    private const string VoiceListUrl =
        "https://speech.platform.bing.com/consumer/speech/synthesize/readaloud/voices/list?trustedclienttoken=6A5AA1D4EAFF4E9FB37E23D68491D6F4";
    private const string SynthesizeUrl =
        "https://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?TrustedClientToken=6A5AA1D4EAFF4E9FB37E23D68491D6F4";
    private const string DefaultVoice = "en-US-AriaNeural";
    private const string AudioFormat = "audio-24khz-48kbitrate-mono-mp3";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EdgeTtsProvider> _logger;

    // Cache voice list for 24 hours to avoid hammering the endpoint
    private IReadOnlyList<TtsVoiceInfo>? _cachedVoices;
    private DateTimeOffset _voicesCachedAt = DateTimeOffset.MinValue;
    private readonly TimeSpan _voiceCacheDuration = TimeSpan.FromHours(24);

    public EdgeTtsProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<EdgeTtsProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TtsSynthesisResult> SynthesizeAsync(
        string text,
        string voiceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(voiceId))
            voiceId = DefaultVoice;

        var ssml = BuildSsml(text, voiceId);
        var requestId = Guid.NewGuid().ToString("N");

        // Compute cache key
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{voiceId}:{text}")));

        var client = _httpClientFactory.CreateClient("edge-tts");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Microsoft-OutputFormat", AudioFormat);

        var headers = BuildRequestHeaders(requestId);

        var request = new HttpRequestMessage(HttpMethod.Post, SynthesizeUrl)
        {
            Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml"),
        };

        foreach (var (key, value) in headers)
            request.Headers.TryAddWithoutValidation(key, value);

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Edge TTS synthesis failed: {Status} — {Body}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Edge TTS synthesis failed: {response.StatusCode}");
        }

        var audioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        // Estimate duration: MP3 at 48 kbps = 6000 bytes/second
        var durationMs = audioData.Length > 0 ? (int)(audioData.Length / 6.0) : 0;

        _logger.LogDebug("Edge TTS synthesized {Bytes} bytes for voice {VoiceId}", audioData.Length, voiceId);

        return new TtsSynthesisResult
        {
            AudioData = audioData,
            DurationMs = durationMs,
            Provider = ProviderName,
            VoiceId = voiceId,
            ContentHash = hash,
        };
    }

    public async Task<IReadOnlyList<TtsVoiceInfo>> GetVoicesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedVoices is not null
            && DateTimeOffset.UtcNow - _voicesCachedAt < _voiceCacheDuration)
        {
            return _cachedVoices;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("edge-tts");
            var json = await client.GetStringAsync(VoiceListUrl, cancellationToken);
            var voices = ParseVoiceList(json);
            _cachedVoices = voices;
            _voicesCachedAt = DateTimeOffset.UtcNow;
            return voices;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Edge TTS voice list; returning cached or empty");
            return _cachedVoices ?? [];
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string BuildSsml(string text, string voiceId)
    {
        // Sanitize: strip XML special chars
        var safeText = SecurityElement.Escape(text) ?? text;

        return $"""
                <speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="en-US">
                    <voice name="{voiceId}">
                        <prosody rate="+0%" pitch="+0Hz">{safeText}</prosody>
                    </voice>
                </speak>
                """;
    }

    private static Dictionary<string, string> BuildRequestHeaders(string requestId)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss 'GMT+0000 (Coordinated Universal Time)'");
        return new Dictionary<string, string>
        {
            ["X-RequestId"] = requestId,
            ["X-Timestamp"] = timestamp,
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        };
    }

    private IReadOnlyList<TtsVoiceInfo> ParseVoiceList(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var voices = new List<TtsVoiceInfo>();

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var id = item.TryGetProperty("ShortName", out var sn) ? sn.GetString() ?? string.Empty : string.Empty;
                var name = item.TryGetProperty("FriendlyName", out var fn) ? fn.GetString() ?? id : id;
                var locale = item.TryGetProperty("Locale", out var loc) ? loc.GetString() ?? string.Empty : string.Empty;
                var gender = item.TryGetProperty("Gender", out var gen) ? gen.GetString() ?? "Neutral" : "Neutral";

                voices.Add(new TtsVoiceInfo
                {
                    Id = id,
                    Name = id,
                    DisplayName = name,
                    Locale = locale,
                    Gender = gender,
                    Provider = ProviderName,
                });
            }

            return voices;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse Edge TTS voice list");
            return [];
        }
    }
}

/// <summary>Minimal SecurityElement.Escape re-implementation to avoid System.Security dependency.</summary>
file static class SecurityElement
{
    public static string? Escape(string? value)
        => value?
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
}
