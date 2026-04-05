// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Tts;

/// <summary>
/// Azure Cognitive Services Text-to-Speech provider (BYOK).
/// Requires AZURE_TTS_KEY and AZURE_TTS_REGION configuration.
/// Stored in Configuration table for runtime updates.
/// </summary>
public sealed class AzureTtsProvider : ITtsProvider
{
    private const string ProviderName = "azure";

    private readonly HttpClient _http;
    private readonly ILogger<AzureTtsProvider> _logger;
    private readonly string? _apiKey;
    private readonly string _region;

    public AzureTtsProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<AzureTtsProvider> logger,
        string? apiKey,
        string region = "westeurope"
    )
    {
        _http = httpClientFactory.CreateClient("azure-tts");
        _logger = logger;
        _apiKey = apiKey;
        _region = region;
    }

    public async Task<TtsSynthesisResult> SynthesizeAsync(
        string text,
        string voiceId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogDebug("Azure TTS: No API key configured, returning empty result");
            return EmptyResult(voiceId);
        }

        var ssml = $"""
            <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
              <voice name='{voiceId}'>{System.Security.SecurityElement.Escape(text)}</voice>
            </speak>
            """;

        var url = $"https://{_region}.tts.speech.microsoft.com/cognitiveservices/v1";

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
        request.Headers.Add("X-Microsoft-OutputFormat", "audio-24khz-48kbitrate-mono-mp3");
        request.Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

        try
        {
            var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure TTS: Request failed {Status}", response.StatusCode);
                return EmptyResult(voiceId);
            }

            var audioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            int durationMs = (int)(audioData.Length / 16.0 * 1000.0 / 1024.0); // estimate

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text + voiceId)))[
                ..16
            ];

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
            _logger.LogError(ex, "Azure TTS: Synthesis failed");
            return EmptyResult(voiceId);
        }
    }

    public async Task<IReadOnlyList<TtsVoiceInfo>> GetVoicesAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(_apiKey))
            return [];

        var url = $"https://{_region}.tts.speech.microsoft.com/cognitiveservices/voices/list";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

        try
        {
            var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            var voices = await response.Content.ReadFromJsonAsync<List<AzureVoice>>(
                cancellationToken: cancellationToken
            );
            if (voices is null)
                return [];

            return voices
                .Select(v => new TtsVoiceInfo
                {
                    Id = v.ShortName,
                    Name = v.ShortName,
                    DisplayName = v.DisplayName,
                    Locale = v.Locale,
                    Gender = v.Gender,
                    Provider = ProviderName,
                })
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Azure TTS: Failed to fetch voice list");
            return [];
        }
    }

    private static TtsSynthesisResult EmptyResult(string voiceId) =>
        new()
        {
            AudioData = [],
            DurationMs = 0,
            Provider = ProviderName,
            VoiceId = voiceId,
            ContentHash = string.Empty,
        };

    private sealed class AzureVoice
    {
        public string ShortName { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Locale { get; set; } = null!;
        public string Gender { get; set; } = null!;
    }
}
