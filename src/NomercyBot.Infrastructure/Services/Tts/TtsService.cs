// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Tts;
using NoMercyBot.Domain.Entities;
using NoMercyBot.Domain.Interfaces;
using AppTtsVoiceInfo = NoMercyBot.Application.Contracts.Tts.TtsVoiceInfo;
using DomainTtsVoiceInfo = NoMercyBot.Domain.Interfaces.TtsVoiceInfo;

namespace NoMercyBot.Infrastructure.Services.Tts;

/// <summary>
/// TTS service that:
///  1. Routes synthesis to the correct provider based on a voice prefix (e.g. "edge:en-US-AriaNeural")
///  2. Checks the cache (TtsCacheEntry) before synthesizing
///  3. Persists results to the cache
///  4. Falls back to the Edge TTS free provider if no prefix is given
/// </summary>
public sealed class TtsService : ITtsService
{
    private readonly IEnumerable<ITtsProvider> _providers;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TtsService> _logger;

    public TtsService(
        IEnumerable<ITtsProvider> providers,
        IServiceScopeFactory scopeFactory,
        ILogger<TtsService> logger)
    {
        _providers = providers;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<TtsResult> SynthesizeAsync(
        string text,
        string voiceId,
        CancellationToken ct = default)
    {
        // Resolve provider from prefix: "edge:en-US-AriaNeural" or just "en-US-AriaNeural"
        var (provider, resolvedVoiceId) = ResolveProvider(voiceId);

        // Check cache
        var cached = await GetCachedAsync(resolvedVoiceId, text, ct);
        if (cached is not null)
        {
            _logger.LogDebug("TTS cache hit for voice {VoiceId}", resolvedVoiceId);
            return new TtsResult(cached.AudioData, cached.DurationMs, cached.VoiceId, cached.Provider);
        }

        // Synthesize
        var result = await provider.SynthesizeAsync(text, resolvedVoiceId, ct);

        // Persist to cache
        await SaveCacheAsync(result, ct);

        return new TtsResult(result.AudioData, result.DurationMs, result.VoiceId, result.Provider);
    }

    public async Task<IReadOnlyList<AppTtsVoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default)
    {
        var all = new List<AppTtsVoiceInfo>();

        foreach (var provider in _providers)
        {
            try
            {
                var voices = await provider.GetVoicesAsync(ct);
                all.AddRange(voices.Select(v =>
                    new AppTtsVoiceInfo(v.Id, v.Name, v.DisplayName, v.Locale, v.Gender, v.Provider)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get voices from provider {Provider}", provider.GetType().Name);
            }
        }

        return all;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a voice ID like "edge:en-US-AriaNeural" → (EdgeTtsProvider, "en-US-AriaNeural").
    /// Falls back to the first registered provider if no prefix.
    /// </summary>
    private (ITtsProvider provider, string voiceId) ResolveProvider(string voiceId)
    {
        if (voiceId.Contains(':'))
        {
            var separatorIdx = voiceId.IndexOf(':');
            var prefix = voiceId[..separatorIdx];
            var voice = voiceId[(separatorIdx + 1)..];

            var matched = _providers.FirstOrDefault(p =>
                p.GetType().Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (matched is not null)
                return (matched, voice);
        }

        // Default: first provider (Edge TTS)
        return (_providers.First(), voiceId);
    }

    private async Task<TtsCacheEntry?> GetCachedAsync(string voiceId, string text, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            // Use SHA256 hash of voiceId+text as cache key
            var hash = ComputeHash(voiceId, text);

            return await db.TtsCacheEntries
                .FirstOrDefaultAsync(e => e.ContentHash == hash, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "TTS cache lookup failed");
            return null;
        }
    }

    private async Task SaveCacheAsync(Domain.Interfaces.TtsSynthesisResult result, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var exists = await db.TtsCacheEntries
                .AnyAsync(e => e.ContentHash == result.ContentHash, ct);

            if (!exists)
            {
                db.TtsCacheEntries.Add(new TtsCacheEntry
                {
                    ContentHash = result.ContentHash,
                    AudioData = result.AudioData,
                    DurationMs = result.DurationMs,
                    Provider = result.Provider,
                    VoiceId = result.VoiceId,
                });
                await db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to save TTS cache entry");
        }
    }

    private static string ComputeHash(string voiceId, string text)
    {
        var data = System.Text.Encoding.UTF8.GetBytes($"{voiceId}:{text}");
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data));
    }
}
