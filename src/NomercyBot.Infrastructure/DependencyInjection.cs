using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Domain.Interfaces;
using NoMercyBot.Infrastructure.EventBus;
using NoMercyBot.Infrastructure.Persistence;
using NoMercyBot.Infrastructure.Persistence.Interceptors;
using NoMercyBot.Infrastructure.Services.Caching;
using NoMercyBot.Infrastructure.Services.General;
using NoMercyBot.Infrastructure.Services.Identity;
using NoMercyBot.Infrastructure.Services.Security;

namespace NoMercyBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Interceptors (scoped so they can resolve scoped services like ICurrentTenantService)
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<TenantStampInterceptor>();

        // DbContext with Npgsql and interceptors
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetSection("Database:ConnectionString").Value;

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });

            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<SoftDeleteInterceptor>(),
                serviceProvider.GetRequiredService<TenantStampInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // EventBus (singleton -- resolves scoped handlers internally via IServiceProvider)
        services.AddSingleton<EventLogger>();
        services.AddSingleton<NoMercyBot.Domain.Interfaces.IEventBus, EventBus.EventBus>();

        // Auto-register IEventHandler<T> implementations from Infrastructure assembly
        RegisterEventHandlers(services, typeof(DependencyInjection).Assembly);

        // Security
        services.AddDataProtection();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // Caching
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // General services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ICooldownManager, CooldownManager>();
        services.AddSingleton<ITemplateEngine, TemplateEngine>();

        // Identity / tenant
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Database migrator (development utility)
        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();

        return services;
    }

    /// <summary>
    /// Registers additional IEventHandler implementations from external assemblies.
    /// Call this from Application or other layers to register their handlers.
    /// </summary>
    public static IServiceCollection AddEventHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        RegisterEventHandlers(services, assembly);
        return services;
    }

    /// <summary>
    /// Scans assemblies for IEventHandler implementations and registers them as transient.
    /// </summary>
    private static void RegisterEventHandlers(IServiceCollection services, params Assembly[] assemblies)
    {
        var handlerInterfaceType = typeof(IEventHandler<>);

        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType));

            foreach (var handlerType in handlerTypes)
            {
                var handlerInterfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType
                        && i.GetGenericTypeDefinition() == handlerInterfaceType);

                foreach (var @interface in handlerInterfaces)
                {
                    services.AddTransient(@interface, handlerType);
                }
            }
        }
    }
}

/// <summary>
/// Service for running EF Core migrations at startup (development only).
/// </summary>
public interface IDatabaseMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken);
}

public sealed class DatabaseMigrator(AppDbContext dbContext) : IDatabaseMigrator
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
