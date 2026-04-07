using NoMercyBot.Application.Common.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Identity;

/// <summary>
/// ICurrentTenantService implementation. Scoped service that stores the current
/// BroadcasterId for the request/scope. Set by middleware or manually in background services.
/// </summary>
public sealed class CurrentTenantService : ICurrentTenantService
{
    public string? BroadcasterId { get; set; }

    public void SetTenant(string broadcasterId)
    {
        BroadcasterId = broadcasterId;
    }
}
