using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Persistence;
using NoMercyBot.Application.Contracts.Twitch;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Interfaces;
using NoMercyBot.Infrastructure.Configuration;
using NoMercyBot.Infrastructure.EventBus;
using NoMercyBot.Infrastructure.Persistence;
using NoMercyBot.Infrastructure.Persistence.Interceptors;
using NoMercyBot.Infrastructure.Persistence.Repositories;
using NoMercyBot.Infrastructure.Services.Application;
using NoMercyBot.Infrastructure.Services.Caching;
using NoMercyBot.Infrastructure.Services.General;
using NoMercyBot.Infrastructure.Services.Identity;
using NoMercyBot.Infrastructure.Services.Registry;
using NoMercyBot.Infrastructure.Services.Security;
using NoMercyBot.Infrastructure.EventHandlers;
using NoMercyBot.Infrastructure.Pipeline;
using NoMercyBot.Infrastructure.Pipeline.Actions;
using NoMercyBot.Infrastructure.Pipeline.Conditions;
using NoMercyBot.Infrastructure.Services.Twitch;

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

        // TemplateResolver: full async resolver with 90+ variables (singleton — all state is scoped internally)
        services.AddSingleton<ITemplateResolver, TemplateResolver>();

        // Pipeline actions (transient — stateless)
        services.AddTransient<ICommandAction, SendMessageAction>();
        services.AddTransient<ICommandAction, SendReplyAction>();
        services.AddTransient<ICommandAction, TimeoutAction>();
        services.AddTransient<ICommandAction, BanAction>();
        services.AddTransient<ICommandAction, WaitAction>();
        services.AddTransient<ICommandAction, SetVariableAction>();
        services.AddTransient<ICommandAction, StopAction>();
        services.AddTransient<ICommandAction, DeleteMessageAction>();

        // Pipeline conditions (transient — stateless)
        services.AddTransient<ICommandCondition, UserRoleCondition>();
        services.AddTransient<ICommandCondition, RandomCondition>();

        // PipelineEngine (singleton — manages per-channel concurrency)
        services.AddSingleton<IPipelineEngine, PipelineEngine>();

        // Identity / tenant
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Database migrator (development utility)
        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();

        // Repositories
        services.AddScoped<ChannelRepository>();
        services.AddScoped<CommandRepository>();
        services.AddScoped<RewardRepository>();
        services.AddScoped<UserRepository>();
        services.AddScoped<WidgetRepository>();

        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application services
        services.AddScoped<ICommandService, CommandService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<IRewardService, RewardService>();
        services.AddScoped<IWidgetService, WidgetService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IModerationService, ModerationService>();

        // ChannelRegistry (singleton + hosted service)
        services.AddSingleton<NoMercyBot.Domain.Interfaces.IChannelRegistry, ChannelRegistry>();
        services.AddHostedService(sp => (ChannelRegistry)sp.GetRequiredService<NoMercyBot.Domain.Interfaces.IChannelRegistry>());

        // TimerService (singleton + hosted service — fires per-channel timers)
        services.AddHostedService<BackgroundServices.TimerService>();

        // Twitch options
        services.Configure<TwitchOptions>(configuration.GetSection(TwitchOptions.SectionName));

        // Twitch HTTP clients
        services.AddHttpClient("twitch-auth");
        services.AddHttpClient("twitch-helix");
        services.AddHttpClient("twitch-eventsub");

        // Spotify HTTP clients
        services.AddHttpClient("spotify");
        services.AddHttpClient("spotify-auth");

        // Music provider + service (singleton — manages per-channel fair queues)
        services.AddSingleton<NoMercyBot.Domain.Interfaces.IMusicProvider, Services.Music.SpotifyMusicProvider>();
        services.AddSingleton<NoMercyBot.Application.Contracts.Music.IMusicService, Services.Music.MusicService>();

        // FairQueue (transient — instantiated per-consumer; MusicService creates its own internally)
        services.AddTransient(typeof(NoMercyBot.Domain.Interfaces.IFairQueue<>), typeof(Services.General.FairQueue<>));

        // Twitch auth service (scoped — uses IApplicationDbContext)
        services.AddScoped<ITwitchAuthService, TwitchAuthService>();

        // Twitch API service (scoped — uses IApplicationDbContext for tokens)
        services.AddScoped<ITwitchApiService, TwitchApiService>();

        // Twitch IRC chat service (singleton + hosted service — fallback only, used for watch streaks)
        services.AddSingleton<TwitchIrcService>();
        services.AddSingleton<ITwitchChatService>(sp => sp.GetRequiredService<TwitchIrcService>());
        services.AddHostedService(sp => sp.GetRequiredService<TwitchIrcService>());

        // HelixChatProvider: primary IChatProvider using Helix POST /chat/messages (EventSub-first)
        services.AddScoped<HelixChatProvider>();
        services.AddScoped<NoMercyBot.Domain.Interfaces.IChatProvider>(sp => sp.GetRequiredService<HelixChatProvider>());

        // Twitch EventSub service (singleton + hosted service — persistent WebSocket connection)
        services.AddSingleton<TwitchEventSubService>();
        services.AddSingleton<ITwitchEventSubService>(sp => sp.GetRequiredService<TwitchEventSubService>());
        services.AddHostedService(sp => sp.GetRequiredService<TwitchEventSubService>());

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
