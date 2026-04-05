// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Channels;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Services.Application;

public class ChannelService : IChannelService
{
    private readonly IApplicationDbContext _db;

    public ChannelService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> JoinAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        Channel? channel = await _db.Channels.FirstOrDefaultAsync(
            c => c.Id == broadcasterId,
            cancellationToken
        );

        if (channel is null)
            return Errors.ChannelNotFound(broadcasterId);

        channel.Enabled = true;
        channel.BotJoinedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> LeaveAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        Channel? channel = await _db.Channels.FirstOrDefaultAsync(
            c => c.Id == broadcasterId,
            cancellationToken
        );

        if (channel is null)
            return Errors.ChannelNotFound(broadcasterId);

        channel.Enabled = false;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<ChannelDto>> GetAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        Channel? channel = await _db
            .Channels.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == broadcasterId, cancellationToken);

        if (channel is null)
            return Errors.ChannelNotFound<ChannelDto>(broadcasterId);

        return Result.Success(ToDto(channel));
    }

    public async Task<Result<IReadOnlyList<ChannelSummaryDto>>> GetAllActiveAsync(
        CancellationToken cancellationToken = default
    )
    {
        List<ChannelSummaryDto> channels = await _db
            .Channels.Include(c => c.User)
            .Where(c => c.Enabled && c.IsOnboarded)
            .OrderBy(c => c.Name)
            .Select(c => new ChannelSummaryDto(
                c.Id,
                c.Name,
                c.User.DisplayName,
                c.User.ProfileImageUrl,
                c.IsLive,
                "broadcaster",
                null
            ))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ChannelSummaryDto>>(channels);
    }

    public async Task<Result<PagedList<ChannelSummaryDto>>> GetChannelsAsync(
        string userId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    )
    {
        // Return channels where the user is the broadcaster or a moderator
        IQueryable<Channel> query = _db
            .Channels.Include(c => c.User)
            .Where(c => c.Id == userId || c.Moderators.Any(m => m.UserId == userId));

        int total = await query.CountAsync(cancellationToken);

        List<ChannelSummaryDto> items = await query
            .OrderBy(c => c.Name)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(c => new ChannelSummaryDto(
                c.Id,
                c.Name,
                c.User.DisplayName,
                c.User.ProfileImageUrl,
                c.IsLive,
                c.Id == userId ? "broadcaster" : "moderator",
                null
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(
            new PagedList<ChannelSummaryDto>(items, total, pagination.Page, pagination.PageSize)
        );
    }

    public async Task<Result<ChannelDto>> UpdateSettingsAsync(
        string broadcasterId,
        UpdateChannelSettingsDto request,
        CancellationToken cancellationToken = default
    )
    {
        Channel? channel = await _db
            .Channels.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == broadcasterId, cancellationToken);

        if (channel is null)
            return Errors.ChannelNotFound<ChannelDto>(broadcasterId);

        if (request.DisplayName is not null)
            channel.User.DisplayName = request.DisplayName;
        if (request.Locale is not null)
            channel.Language = request.Locale;
        if (request.AutoJoin.HasValue)
            channel.Enabled = request.AutoJoin.Value;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDto(channel));
    }

    public async Task<Result<ChannelDto>> OnboardAsync(
        string broadcasterId,
        CreateChannelRequest request,
        CancellationToken cancellationToken = default
    )
    {
        Channel? existing = await _db
            .Channels.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == broadcasterId, cancellationToken);

        if (existing is not null)
        {
            existing.IsOnboarded = true;
            existing.BotJoinedAt ??= DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success(ToDto(existing));
        }

        // Check if user exists
        User? user = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == broadcasterId,
            cancellationToken
        );
        if (user is null)
            return Result.Failure<ChannelDto>(
                "User not found. Cannot onboard channel.",
                "NOT_FOUND"
            );

        Channel channel = new()
        {
            Id = broadcasterId,
            Name = user.Username,
            IsOnboarded = true,
            Enabled = true,
            BotJoinedAt = DateTime.UtcNow,
            User = user,
        };

        _db.Channels.Add(channel);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(channel));
    }

    public async Task<Result> DeleteAsync(
        string broadcasterId,
        CancellationToken cancellationToken = default
    )
    {
        Channel? channel = await _db.Channels.FirstOrDefaultAsync(
            c => c.Id == broadcasterId,
            cancellationToken
        );

        if (channel is null)
            return Errors.ChannelNotFound(broadcasterId);

        _db.Channels.Remove(channel);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<ChannelOverlayInfo?> GetByOverlayTokenAsync(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        return await _db
            .Channels.Include(c => c.User)
            .Where(c => c.OverlayToken == token)
            .Select(c => new ChannelOverlayInfo(c.Id, c.User.DisplayName))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static ChannelDto ToDto(Channel c) =>
        new(
            c.Id,
            c.Name,
            c.User?.DisplayName ?? c.Name,
            c.User?.ProfileImageUrl,
            c.IsLive,
            c.IsOnboarded,
            c.Title,
            c.GameName,
            null,
            c.BotJoinedAt,
            "free",
            c.Language,
            c.CreatedAt
        );
}
