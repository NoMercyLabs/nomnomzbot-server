using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NoMercyBot.Application.Common.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Identity;

/// <summary>
/// ICurrentUserService implementation that reads the current user
/// from HttpContext claims (populated by JWT authentication).
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? DisplayName =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("display_name")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.GivenName);

    public string? Username =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];
}
