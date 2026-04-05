using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Domain.Common;

namespace NoMercyBot.Infrastructure.Persistence.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies a global query filter for soft-deletable entities: WHERE DeletedAt IS NULL.
    /// </summary>
    public static void ApplySoftDeleteFilter<TEntity>(this ModelBuilder modelBuilder)
        where TEntity : SoftDeletableEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.DeletedAt == null);
    }

    /// <summary>
    /// Applies a global query filter for tenant-scoped entities.
    /// Requires ICurrentTenantService to be resolved at query time via a DbContext parameter.
    /// This is a no-op placeholder; tenant filtering is applied per-query or via interceptor.
    /// </summary>
    public static void ApplyTenantFilter<TEntity>(
        this ModelBuilder modelBuilder,
        Expression<Func<TEntity, bool>> filter
    )
        where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }
}
