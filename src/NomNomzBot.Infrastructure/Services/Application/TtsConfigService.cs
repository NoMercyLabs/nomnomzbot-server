// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.Contracts.Tts;
using NoMercyBot.Application.DTOs.Tts;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Entities;
using NoMercyBot.Domain.Interfaces;
using ChannelConfiguration = NoMercyBot.Domain.Entities.Configuration;

namespace NoMercyBot.Infrastructure.Services.Application;

public class TtsConfigService : ITtsConfigService
{
    private const string ConfigKey = "tts:config";

    private readonly IApplicationDbContext _db;
    private readonly ITtsService _ttsService;

    public TtsConfigService(IApplicationDbContext db, ITtsService ttsService)
    {
        _db = db;
        _ttsService = ttsService;
    }

    public async Task<Result<TtsConfigDto>> GetConfigAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        TtsConfigDto config = await LoadConfigAsync(broadcasterId, cancellationToken);
        return Result.Success(config);
    }

    public async Task<Result<TtsConfigDto>> UpdateConfigAsync(
        string broadcasterId,
        UpdateTtsConfigDto request,
        CancellationToken cancellationToken = default
    )
    {
        ChannelConfiguration? existing = await _db.Configurations.FirstOrDefaultAsync(
            c => c.BroadcasterId == broadcasterId && c.Key == ConfigKey,
            cancellationToken
        );

        TtsConfigData current = existing is not null
            ? JsonSerializer.Deserialize<TtsConfigData>(existing.Value ?? "{}")
                ?? new TtsConfigData()
            : new();

        if (request.IsEnabled.HasValue)
            current.IsEnabled = request.IsEnabled.Value;
        if (request.DefaultVoiceId is not null)
            current.DefaultVoiceId = request.DefaultVoiceId;
        if (request.MaxLength.HasValue)
            current.MaxLength = request.MaxLength.Value;
        if (request.MinPermission is not null)
            current.MinPermission = request.MinPermission;
        if (request.SkipBotMessages.HasValue)
            current.SkipBotMessages = request.SkipBotMessages.Value;
        if (request.ReadUsernames.HasValue)
            current.ReadUsernames = request.ReadUsernames.Value;

        string json = JsonSerializer.Serialize(current);

        if (existing is not null)
        {
            existing.Value = json;
        }
        else
        {
            _db.Configurations.Add(
                new()
                {
                    BroadcasterId = broadcasterId,
                    Key = ConfigKey,
                    Value = json,
                }
            );
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(current));
    }

    public async Task<Result<IReadOnlyList<TtsVoiceDto>>> GetVoicesAsync(
        CancellationToken cancellationToken = default
    )
    {
        List<TtsVoice> dbVoices = await _db
            .TtsVoices.OrderBy(v => v.Provider)
            .ThenBy(v => v.Locale)
            .ThenBy(v => v.Name)
            .ToListAsync(cancellationToken);

        if (dbVoices.Count > 0)
        {
            IReadOnlyList<TtsVoiceDto> dbDtos = dbVoices
                .Select(v => new TtsVoiceDto(
                    v.Id,
                    v.Name,
                    v.DisplayName,
                    v.Locale,
                    v.Gender,
                    v.Provider,
                    v.IsDefault
                ))
                .ToList();
            return Result.Success(dbDtos);
        }

        // Fallback: enumerate directly from providers
        IReadOnlyList<TtsVoiceInfo> providerVoices = await _ttsService.GetAvailableVoicesAsync(cancellationToken);
        IReadOnlyList<TtsVoiceDto> dtos = providerVoices
            .Select(v => new TtsVoiceDto(
                v.Id,
                v.Name,
                v.DisplayName,
                v.Locale,
                v.Gender,
                v.Provider,
                IsDefault: false
            ))
            .ToList();
        return Result.Success(dtos);
    }

    public async Task<Result<TtsTestResultDto>> TestVoiceAsync(
        string broadcasterId,
        TtsTestRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            TtsResult result = await _ttsService.SynthesizeAsync(
                request.Text,
                request.VoiceId,
                cancellationToken
            );
            string base64 = Convert.ToBase64String(result.AudioData);
            return Result.Success(
                new TtsTestResultDto(result.VoiceId, result.Provider, result.DurationMs, base64)
            );
        }
        catch (Exception)
        {
            return Errors.ExternalServiceUnavailable("TTS").ToTyped<TtsTestResultDto>();
        }
    }

    private async Task<TtsConfigDto> LoadConfigAsync(
        string broadcasterId,
        CancellationToken cancellationToken
    )
    {
        ChannelConfiguration? entry = await _db.Configurations.FirstOrDefaultAsync(
            c => c.BroadcasterId == broadcasterId && c.Key == ConfigKey,
            cancellationToken
        );

        if (entry?.Value is null)
            return ToDto(new());

        TtsConfigData data = JsonSerializer.Deserialize<TtsConfigData>(entry.Value) ?? new TtsConfigData();
        return ToDto(data);
    }

    private static TtsConfigDto ToDto(TtsConfigData d) =>
        new(
            d.IsEnabled,
            d.DefaultVoiceId,
            d.MaxLength,
            d.MinPermission,
            d.SkipBotMessages,
            d.ReadUsernames
        );

    private sealed class TtsConfigData
    {
        public bool IsEnabled { get; set; } = true;
        public string DefaultVoiceId { get; set; } = "en-US-AriaNeural";
        public int MaxLength { get; set; } = 200;
        public string MinPermission { get; set; } = "everyone";
        public bool SkipBotMessages { get; set; } = true;
        public bool ReadUsernames { get; set; } = true;
    }
}
