// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using Microsoft.EntityFrameworkCore;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Common.Models;

namespace NoMercyBot.Application.Features.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<CurrentUserDto>> HandleAsync(CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Errors.NotAuthenticated().ToTyped<CurrentUserDto>();

        CurrentUserDto? user = await _db
            .Users.Where(u => u.Id == _currentUser.UserId)
            .Select(u => new CurrentUserDto(
                u.Id,
                u.Username,
                u.DisplayName,
                u.ProfileImageUrl,
                u.Color,
                u.BroadcasterType,
                u.IsAdmin,
                u.CreatedAt
            ))
            .FirstOrDefaultAsync(ct);

        return user is null
            ? Errors.NotFound<CurrentUserDto>("User", _currentUser.UserId)
            : Result.Success(user);
    }
}

public record CurrentUserDto(
    string Id,
    string Username,
    string DisplayName,
    string? ProfileImageUrl,
    string? Color,
    string BroadcasterType,
    bool IsAdmin,
    DateTime CreatedAt
);
