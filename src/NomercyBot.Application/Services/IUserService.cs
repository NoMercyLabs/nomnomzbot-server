using NoMercyBot.Application.Common.Models;
using NoMercyBot.Application.DTOs.Users;

namespace NoMercyBot.Application.Services;

/// <summary>
/// Application service for user management.
/// </summary>
public interface IUserService
{
    /// <summary>Get or create a user by their platform ID. Used when a user is first seen in chat.</summary>
    Task<Result<UserDto>> GetOrCreateAsync(
        string platformUserId,
        string username,
        string displayName,
        CancellationToken cancellationToken = default
    );

    /// <summary>Update a user's profile information.</summary>
    Task<Result<UserProfileDto>> UpdateProfileAsync(
        string userId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Search users by name or display name.</summary>
    Task<Result<PagedList<UserSearchResult>>> SearchAsync(
        string query,
        PaginationParams pagination,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get a user by their ID.</summary>
    Task<Result<UserDto>> GetAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Get a user's full profile.</summary>
    Task<Result<UserProfileDto>> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get a GDPR data summary for a user.</summary>
    Task<Result<UserStatsDto>> GetStatsAsync(
        string userId,
        CancellationToken cancellationToken = default
    );
}
