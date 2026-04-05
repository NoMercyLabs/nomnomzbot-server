using System.Security.Claims;

namespace NoMercyBot.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? GetBroadcasterId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("broadcaster_id");

    public static string? GetDisplayName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("display_name")
        ?? principal.FindFirstValue(ClaimTypes.Name);

    public static string GetRequiredUserId(this ClaimsPrincipal principal) =>
        principal.GetUserId()
        ?? throw new UnauthorizedAccessException("User ID claim is missing.");

    public static string GetRequiredBroadcasterId(this ClaimsPrincipal principal) =>
        principal.GetBroadcasterId()
        ?? throw new UnauthorizedAccessException("Broadcaster ID claim is missing.");
}
