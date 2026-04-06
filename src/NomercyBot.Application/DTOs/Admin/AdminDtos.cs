// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

namespace NoMercyBot.Application.DTOs.Admin;

public sealed record AdminStatsDto(
    int TotalChannels,
    int ActiveChannels,
    int TotalUsers,
    string SystemStatus,
    long BotUptimeSeconds,
    int EventsProcessedToday
);

public sealed record AdminChannelDto(
    string Id,
    string DisplayName,
    string Login,
    bool IsLive,
    bool IsActive,
    int ViewerCount,
    string Plan,
    DateTime CreatedAt
);

public sealed record ServiceHealthDto(string Name, string Status, int? LatencyMs);

public sealed record AdminUserDto(
    string Id,
    string DisplayName,
    string Login,
    string? Email,
    string Role,
    int ChannelCount,
    DateTime CreatedAt,
    DateTime? LastActive
);

public sealed record AdminSystemDto(
    string Overall,
    List<ServiceHealthDto> Services,
    string BotVersion,
    long MemoryUsageMb,
    double CpuPercent
);
