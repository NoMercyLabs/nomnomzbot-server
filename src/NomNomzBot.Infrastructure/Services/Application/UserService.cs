// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Users;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Entities;

namespace NoMercyBot.Infrastructure.Services.Application;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _db;

    public UserService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<UserDto>> GetOrCreateAsync(
        string platformUserId,
        string username,
        string displayName,
        CancellationToken cancellationToken = default
    )
    {
        User? user = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == platformUserId,
            cancellationToken
        );

        if (user is null)
        {
            user = new()
            {
                Id = platformUserId,
                Username = username,
                DisplayName = displayName,
                Enabled = true,
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Update username/displayName in case they changed
            user.Username = username;
            user.DisplayName = displayName;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(ToDto(user));
    }

    public async Task<Result<UserProfileDto>> UpdateProfileAsync(
        string userId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default
    )
    {
        User? user = await _db
            .Users.Include(u => u.Pronoun)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Errors.NotFound<UserProfileDto>("User", userId);

        if (request.DisplayName is not null)
            user.DisplayName = request.DisplayName;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(ToProfileDto(user));
    }

    public async Task<Result<PagedList<UserSearchResult>>> SearchAsync(
        string query,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<User> dbQuery = _db.Users.Where(u =>
            u.Username.Contains(query) || u.DisplayName.Contains(query)
        );

        int total = await dbQuery.CountAsync(cancellationToken);

        List<UserSearchResult> items = await dbQuery
            .OrderBy(u => u.Username)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(u => new UserSearchResult(u.Id, u.Username, u.DisplayName, u.ProfileImageUrl))
            .ToListAsync(cancellationToken);

        return Result.Success(
            new PagedList<UserSearchResult>(items, total, pagination.Page, pagination.PageSize)
        );
    }

    public async Task<Result<UserDto>> GetAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Errors.NotFound<UserDto>("User", userId);

        return Result.Success(ToDto(user));
    }

    public async Task<Result<UserProfileDto>> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        User? user = await _db
            .Users.Include(u => u.Pronoun)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Errors.NotFound<UserProfileDto>("User", userId);

        return Result.Success(ToProfileDto(user));
    }

    public async Task<Result<UserStatsDto>> GetStatsAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Errors.NotFound<UserStatsDto>("User", userId);

        int messageCount = await _db.ChatMessages.CountAsync(
            m => m.UserId == userId,
            cancellationToken
        );

        int watchStreakCount = await _db.WatchStreaks.CountAsync(
            w => w.UserId == userId,
            cancellationToken
        );

        int moderatorChannels = await _db.ChannelModerators.CountAsync(
            m => m.UserId == userId,
            cancellationToken
        );

        bool ownsChannel = await _db.Channels.AnyAsync(c => c.Id == userId, cancellationToken);

        int channelsCount = moderatorChannels + (ownsChannel ? 1 : 0);

        UserStatsDto dto = new(
            messageCount,
            0,
            channelsCount,
            0,
            user.CreatedAt,
            user.UpdatedAt,
            true
        );

        return Result.Success(dto);
    }

    public async Task<Result<List<UserChannelAppearanceDto>>> GetUserChannelsAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        // Channels the user has sent messages in
        var messageData = await _db.ChatMessages
            .Where(m => m.UserId == userId)
            .GroupBy(m => m.BroadcasterId)
            .Select(g => new
            {
                ChannelId = g.Key,
                MessageCount = g.Count(),
                FirstSeen = g.Min(m => m.CreatedAt),
            })
            .ToListAsync(cancellationToken);

        // Channels where the user is a moderator
        List<string> modChannelIds = await _db.ChannelModerators
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.ChannelId)
            .ToListAsync(cancellationToken);

        List<string> allChannelIds = messageData.Select(c => c.ChannelId)
            .Union(modChannelIds)
            .Distinct()
            .ToList();

        if (allChannelIds.Count == 0)
            return Result.Success(new List<UserChannelAppearanceDto>());

        var channelNames = await _db.Channels
            .Where(c => allChannelIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        var watchStreaks = await _db.WatchStreaks
            .Where(w => w.UserId == userId && allChannelIds.Contains(w.BroadcasterId))
            .Select(w => new { w.BroadcasterId, w.CurrentStreak })
            .ToListAsync(cancellationToken);

        List<UserChannelAppearanceDto> result = channelNames.Select(ch =>
        {
            var msgs = messageData.FirstOrDefault(m => m.ChannelId == ch.Id);
            var streak = watchStreaks.FirstOrDefault(w => w.BroadcasterId == ch.Id);

            string followDate = msgs is not null
                ? msgs.FirstSeen.ToString("yyyy-MM-dd")
                : "N/A";

            string watchTime = streak is not null
                ? $"{streak.CurrentStreak} day streak"
                : "N/A";

            return new UserChannelAppearanceDto(
                ch.Name,
                followDate,
                msgs is not null ? msgs.MessageCount : 0,
                watchTime
            );
        }).ToList();

        return Result.Success(result);
    }

    public async Task<Result<PagedList<AdminUserDto>>> ListAdminUsersAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<User> query = _db.Users.Include(u => u.Channel);

        int total = await query.CountAsync(cancellationToken);

        List<AdminUserDto> items = await query
            .OrderBy(u => u.Username)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(u => new AdminUserDto(
                u.Id,
                u.DisplayName,
                u.Username,
                null,
                u.Channel != null ? "moderator" : "user",
                u.Channel != null ? 1 : 0,
                u.CreatedAt,
                (DateTime?)u.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(
            new PagedList<AdminUserDto>(items, total, pagination.Page, pagination.PageSize)
        );
    }

    private static UserDto ToDto(User u) =>
        new(u.Id, u.Username, u.DisplayName, u.ProfileImageUrl, null, u.CreatedAt, u.UpdatedAt);

    private static UserProfileDto ToProfileDto(User u) =>
        new(
            u.Id,
            u.Username,
            u.DisplayName,
            u.ProfileImageUrl,
            null,
            u.Pronoun?.Name,
            u.CreatedAt,
            u.UpdatedAt
        );
}
