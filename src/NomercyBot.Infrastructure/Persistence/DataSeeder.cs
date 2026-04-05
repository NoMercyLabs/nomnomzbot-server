// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Persistence;

public sealed class DataSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(AppDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Seeding reference data...");

        await SeedPronounsAsync(ct);
        await SeedTtsVoicesAsync(ct);
        await SeedGlobalConfigurationAsync(ct);

        _logger.LogInformation("Seed complete.");
    }

    private async Task SeedPronounsAsync(CancellationToken ct)
    {
        if (!await _context.Pronouns.AnyAsync(ct))
        {
            _context.Pronouns.AddRange(
                new[]
                {
                    new Pronoun
                    {
                        Name = "they/them",
                        Subject = "they",
                        Object = "them",
                        Singular = false,
                    },
                    new Pronoun
                    {
                        Name = "she/her",
                        Subject = "she",
                        Object = "her",
                        Singular = true,
                    },
                    new Pronoun
                    {
                        Name = "he/him",
                        Subject = "he",
                        Object = "him",
                        Singular = true,
                    },
                    new Pronoun
                    {
                        Name = "she/they",
                        Subject = "she",
                        Object = "them",
                        Singular = false,
                    },
                    new Pronoun
                    {
                        Name = "he/they",
                        Subject = "he",
                        Object = "them",
                        Singular = false,
                    },
                    new Pronoun
                    {
                        Name = "any/all",
                        Subject = "any",
                        Object = "all",
                        Singular = false,
                    },
                    new Pronoun
                    {
                        Name = "other/ask",
                        Subject = "other",
                        Object = "ask",
                        Singular = false,
                    },
                }
            );
            await _context.SaveChangesAsync(ct);
        }
    }

    private async Task SeedTtsVoicesAsync(CancellationToken ct)
    {
        if (!await _context.TtsVoices.AnyAsync(ct))
        {
            _context.TtsVoices.AddRange(
                new[]
                {
                    new TtsVoice
                    {
                        Id = "en-US-AriaNeural",
                        Name = "AriaNeural",
                        DisplayName = "Aria (US)",
                        Locale = "en-US",
                        Gender = "Female",
                        Provider = "edge",
                        IsDefault = true,
                    },
                    new TtsVoice
                    {
                        Id = "en-US-GuyNeural",
                        Name = "GuyNeural",
                        DisplayName = "Guy (US)",
                        Locale = "en-US",
                        Gender = "Male",
                        Provider = "edge",
                        IsDefault = false,
                    },
                    new TtsVoice
                    {
                        Id = "en-GB-SoniaNeural",
                        Name = "SoniaNeural",
                        DisplayName = "Sonia (GB)",
                        Locale = "en-GB",
                        Gender = "Female",
                        Provider = "edge",
                        IsDefault = false,
                    },
                    new TtsVoice
                    {
                        Id = "en-AU-NatashaNeural",
                        Name = "NatashaNeural",
                        DisplayName = "Natasha (AU)",
                        Locale = "en-AU",
                        Gender = "Female",
                        Provider = "edge",
                        IsDefault = false,
                    },
                    new TtsVoice
                    {
                        Id = "de-DE-KatjaNeural",
                        Name = "KatjaNeural",
                        DisplayName = "Katja (DE)",
                        Locale = "de-DE",
                        Gender = "Female",
                        Provider = "edge",
                        IsDefault = false,
                    },
                    new TtsVoice
                    {
                        Id = "fr-FR-DeniseNeural",
                        Name = "DeniseNeural",
                        DisplayName = "Denise (FR)",
                        Locale = "fr-FR",
                        Gender = "Female",
                        Provider = "edge",
                        IsDefault = false,
                    },
                    new TtsVoice
                    {
                        Id = "es-ES-ElviraNeural",
                        Name = "ElviraNeural",
                        DisplayName = "Elvira (ES)",
                        Locale = "es-ES",
                        Gender = "Female",
                        Provider = "edge",
                        IsDefault = false,
                    },
                    new TtsVoice
                    {
                        Id = "ja-JP-NanamiNeural",
                        Name = "NanamiNeural",
                        DisplayName = "Nanami (JP)",
                        Locale = "ja-JP",
                        Gender = "Female",
                        Provider = "edge",
                        IsDefault = false,
                    },
                    new TtsVoice
                    {
                        Id = "ko-KR-SunHiNeural",
                        Name = "SunHiNeural",
                        DisplayName = "Sun-Hi (KR)",
                        Locale = "ko-KR",
                        Gender = "Female",
                        Provider = "edge",
                        IsDefault = false,
                    },
                    new TtsVoice
                    {
                        Id = "pt-BR-FranciscaNeural",
                        Name = "FranciscaNeural",
                        DisplayName = "Francisca (BR)",
                        Locale = "pt-BR",
                        Gender = "Female",
                        Provider = "edge",
                        IsDefault = false,
                    },
                }
            );
            await _context.SaveChangesAsync(ct);
        }
    }

    private async Task SeedGlobalConfigurationAsync(CancellationToken ct)
    {
        var globalConfigKeys = new Dictionary<string, string>
        {
            ["system:version"] = "1.0.0",
            ["system:tts:providers"] = "edge,azure,elevenlabs",
            ["system:tts:maxDurationSeconds"] = "30",
            ["system:moderation:defaultSpamThreshold"] = "5",
        };

        foreach (var (key, value) in globalConfigKeys)
        {
            if (
                !await _context.Configurations.AnyAsync(
                    c => c.BroadcasterId == null && c.Key == key,
                    ct
                )
            )
            {
                _context.Configurations.Add(
                    new Domain.Entities.Configuration
                    {
                        BroadcasterId = null,
                        Key = key,
                        Value = value,
                    }
                );
            }
        }
        await _context.SaveChangesAsync(ct);
    }
}
