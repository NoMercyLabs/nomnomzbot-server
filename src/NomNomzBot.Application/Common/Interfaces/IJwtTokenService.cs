using System.Security.Claims;

namespace NoMercyBot.Application.Common.Interfaces;

/// <summary>
/// Generates and validates JWT tokens for API authentication.
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(string userId, string username, IEnumerable<string>? roles = null);
    string GenerateRefreshToken(string userId, string username);
    ClaimsPrincipal? ValidateToken(string token);
}
