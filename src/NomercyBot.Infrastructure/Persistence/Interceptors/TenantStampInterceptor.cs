using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps the BroadcasterId on new ITenantScoped entities when they are added,
/// using the current tenant from ICurrentTenantService.
/// </summary>
public sealed class TenantStampInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenantService _currentTenantService;

    public TenantStampInterceptor(ICurrentTenantService currentTenantService)
    {
        _currentTenantService = currentTenantService;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is not null)
        {
            StampTenant(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        if (eventData.Context is not null)
        {
            StampTenant(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void StampTenant(DbContext context)
    {
        string? broadcasterId = _currentTenantService.BroadcasterId;

        if (string.IsNullOrEmpty(broadcasterId))
        {
            return;
        }

        foreach (EntityEntry<ITenantScoped> entry in context.ChangeTracker.Entries<ITenantScoped>())
        {
            if (
                entry.State == EntityState.Added
                && string.IsNullOrEmpty(entry.Entity.BroadcasterId)
            )
            {
                entry.Entity.BroadcasterId = broadcasterId;
            }
        }
    }
}
