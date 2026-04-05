// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Tts;

/// <summary>
/// ElevenLabs TTS provider stub (BYOK).
/// Requires ELEVENLABS_API_KEY configuration.
/// </summary>
public sealed class ElevenLabsTtsProvider : ITtsProvider
{
    private const string ProviderName = "elevenlabs";
    private const string ApiBase = "https://api.elevenlabs.io/v1";

    private readonly HttpClient _http;
    private readonly ILogger<ElevenLabsTtsProvider> _logger;
    private readonly string? _apiKey;

    public ElevenLabsTtsProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<ElevenLabsTtsProvider> logger,
        string? apiKey)
    {
        _http = httpClientFactory.CreateClient("elevenlabs-tts");
        _logger = logger;
        _apiKey = apiKey;
    }

    public async Task<TtsSynthesisResult> SynthesizeAsync(
        string text,
        string voiceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogDebug("ElevenLabs TTS: No API key configured");
            return EmptyResult(voiceId);
        }

        var url = $"{ApiBase}/text-to-speech/{voiceId}";
        var body = JsonSerializer.Serialize(new
        {
            text,
            model_id = "eleven_multilingual_v2",
            voice_settings = new { stability = 0.5, similarity_boost = 0.75 },
        });

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("xi-api-key", _apiKey);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ElevenLabs TTS: Request failed {Status}", response.StatusCode);
                return EmptyResult(voiceId);
            }

            var audioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "ElevenLabs TTS: Synthesis failed");
            return EmptyResult(voiceId);
        }
    }

    public async Task<IReadOnlyList<TtsVoiceInfo>> GetVoicesAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey)) return [];

        var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/voices");
        request.Headers.Add("xi-api-key", _apiKey);

        try
        {
            var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return [];

            var data = await response.Content.ReadFromJsonAsync<ElevenLabsVoicesResponse>(cancellationToken: cancellationToken);

            return data?.Voices?.Select(v => new TtsVoiceInfo
            {
                Id = v.VoiceId,
                Name = v.Name,
                DisplayName = v.Name,
                Locale = "en-US",
                Gender = v.Labels?.GetValueOrDefault("gender") ?? "unknown",
                Provider = ProviderName,
            }).ToList() ?? [];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "ElevenLabs TTS: Failed to fetch voices");
            return [];
        }
    }

    private static TtsSynthesisResult EmptyResult(string voiceId) => new()
    {
        AudioData = [],
        DurationMs = 0,
        Provider = ProviderName,
        VoiceId = voiceId,
        ContentHash = string.Empty,
    };

    private sealed class ElevenLabsVoicesResponse
    {
        [JsonPropertyName("voices")]
        public List<ElevenLabsVoice>? Voices { get; set; }
    }

    private sealed class ElevenLabsVoice
    {
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("labels")]
        public Dictionary<string, string>? Labels { get; set; }
    }
}
