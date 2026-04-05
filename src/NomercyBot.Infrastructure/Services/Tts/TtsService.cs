// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Contracts.Tts;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Tts;

/// <summary>
/// Application-level TTS service that:
/// - Routes synthesis requests to the appropriate ITtsProvider
/// - Caches synthesized audio by content hash to avoid redundant API calls
/// - Falls back to EdgeTtsProvider if the primary provider fails
/// </summary>
public sealed class TtsService : ITtsService
{
    private readonly IEnumerable<ITtsProvider> _providers;
    private readonly ILogger<TtsService> _logger;

    // In-memory cache: contentHash → audio bytes
    // Production: replace with file cache per spec (TTS_CACHE_PATH)
    private readonly Dictionary<string, byte[]> _cache = new();
    private readonly Lock _cacheLock = new();

    public TtsService(
        IEnumerable<ITtsProvider> providers,
        ILogger<TtsService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<TtsResult> SynthesizeAsync(string text, string voiceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TtsResult([], 0, voiceId, "none");

        var cacheKey = BuildCacheKey(text, voiceId);

        // Check cache
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                _logger.LogDebug("TTS cache hit for voice {VoiceId}", voiceId);
                int cachedDurationMs = (int)(cached.Length / 16.0 * 1000.0 / 1024.0);
                return new TtsResult(cached, cachedDurationMs, voiceId, "edge-cached");
            }
        }

        // Determine provider by voice prefix
        var provider = ResolveProvider(voiceId);

        TtsSynthesisResult result;
        try
        {
            result = await provider.SynthesizeAsync(text, voiceId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "TTS synthesis failed for voice {VoiceId}, falling back to Edge TTS", voiceId);

            // Fall back to Edge TTS
            var edgeProvider = _providers.OfType<EdgeTtsProvider>().FirstOrDefault();
            if (edgeProvider is null)
                return new TtsResult([], 0, voiceId, "error");

            result = await edgeProvider.SynthesizeAsync(text, "en-US-AriaNeural", ct);
        }

        if (result.AudioData.Length > 0)
        {
            lock (_cacheLock)
            {
                _cache[cacheKey] = result.AudioData;

                // Evict if cache exceeds 200 entries
                if (_cache.Count > 200)
                {
                    var oldest = _cache.Keys.First();
                    _cache.Remove(oldest);
                }
            }
        }

        return new TtsResult(result.AudioData, result.DurationMs, voiceId, result.Provider);
    }

    public async Task<IReadOnlyList<TtsVoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default)
    {
        var allVoices = new List<TtsVoiceInfo>();

        foreach (var provider in _providers)
        {
            try
            {
                var voices = await provider.GetVoicesAsync(ct);
                allVoices.AddRange(voices.Select(v => new TtsVoiceInfo(
                    v.Id, v.Name, v.DisplayName, v.Locale, v.Gender, v.Provider)));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "TTS provider {Provider} failed to return voices", provider.GetType().Name);
            }
        }

        return allVoices;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private ITtsProvider ResolveProvider(string voiceId)
    {
        // Voice ID naming convention: provider prefix
        // Azure: "en-US-AriaNeural" (Neural voices)
        // ElevenLabs: UUID-formatted voice IDs

        // ElevenLabs: UUIDs (8-4-4-4-12 format)
        if (Guid.TryParse(voiceId, out _))
        {
            var elevenlabs = _providers.OfType<ElevenLabsTtsProvider>().FirstOrDefault();
            if (elevenlabs is not null) return elevenlabs;
        }

        // Azure configured separately per BYOK
        var azure = _providers.OfType<AzureTtsProvider>().FirstOrDefault();
        if (azure is not null) return azure;

        // Default: Edge TTS (free)
        var edge = _providers.OfType<EdgeTtsProvider>().FirstOrDefault();
        if (edge is not null) return edge;

        return _providers.First();
    }

    private static string BuildCacheKey(string text, string voiceId)
    {
        var bytes = Encoding.UTF8.GetBytes(text + "|" + voiceId);
        return Convert.ToHexString(SHA256.HashData(bytes))[..24];
    }
}
