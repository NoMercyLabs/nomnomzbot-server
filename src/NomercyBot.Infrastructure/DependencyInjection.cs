using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoMercyBot.Application.Common.Interfaces;
using NoMercyBot.Application.Contracts.Music;
using NoMercyBot.Application.Contracts.Persistence;
using NoMercyBot.Application.Contracts.Tts;
using NoMercyBot.Application.Contracts.Twitch;
using NoMercyBot.Application.Services;
using NoMercyBot.Domain.Interfaces;
using NoMercyBot.Infrastructure.BackgroundServices;
using NoMercyBot.Infrastructure.Configuration;
using NoMercyBot.Infrastructure.EventBus;
using NoMercyBot.Infrastructure.Persistence;
using NoMercyBot.Infrastructure.Persistence.Interceptors;
using NoMercyBot.Infrastructure.Persistence.Repositories;
using NoMercyBot.Infrastructure.Resilience;
using NoMercyBot.Infrastructure.Services.Application;
using NoMercyBot.Infrastructure.Services.Caching;
using NoMercyBot.Infrastructure.Services.General;
using NoMercyBot.Infrastructure.Services.Identity;
using NoMercyBot.Infrastructure.Services.Migration;
using NoMercyBot.Infrastructure.Services.Moderation;
using NoMercyBot.Infrastructure.Services.Music;
using NoMercyBot.Infrastructure.Services.Registry;
using NoMercyBot.Infrastructure.Services.Security;
using NoMercyBot.Infrastructure.Services.Trust;
using NoMercyBot.Infrastructure.Services.Tts;
using NoMercyBot.Infrastructure.Services.Twitch;
using NoMercyBot.Infrastructure.EventHandlers;
using NoMercyBot.Infrastructure.Pipeline;
using NoMercyBot.Infrastructure.Pipeline.Actions;
using NoMercyBot.Infrastructure.Pipeline.Conditions;

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
        services.AddSingleton<ITemplateResolver, TemplateResolver>();
        services.AddSingleton<ITrustService, TrustService>();

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
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPermissionService, PermissionService>();

        // GDPR + migration (scoped — use DbContext)
        services.AddScoped<GdprService>();
        services.AddScoped<SqliteMigrationService>();

        // Auto-moderation (scoped — uses ICooldownManager which is singleton, fine)
        services.AddScoped<AutoModerationEngine>();

        // Music providers + service (singleton — maintain per-channel queues)
        services.AddHttpClient("edge-tts");
        services.AddSingleton<ITtsProvider, EdgeTtsProvider>();
        services.AddHttpClient("azure-tts");
        services.AddSingleton<ITtsProvider>(sp => new AzureTtsProvider(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AzureTtsProvider>>(),
            configuration["Azure:Tts:ApiKey"],
            configuration["Azure:Tts:Region"] ?? "westeurope"));
        services.AddHttpClient("elevenlabs-tts");
        services.AddSingleton<ITtsProvider>(sp => new ElevenLabsTtsProvider(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ElevenLabsTtsProvider>>(),
            configuration["ElevenLabs:ApiKey"]));
        services.AddSingleton<ITtsService, TtsService>();

        // Spotify HTTP clients with resilience
        services.AddHttpClient("spotify")
            .AddSpotifyResilienceHandler();
        services.AddHttpClient("spotify-auth");

        // Music providers
        services.AddScoped<SpotifyMusicProvider>();
        services.AddScoped<YouTubeMusicProvider>();
        services.AddScoped<IMusicService, MusicService>();

        // ChannelRegistry (singleton + hosted service)
        services.AddSingleton<NoMercyBot.Domain.Interfaces.IChannelRegistry, ChannelRegistry>();
        services.AddHostedService(sp => (ChannelRegistry)sp.GetRequiredService<NoMercyBot.Domain.Interfaces.IChannelRegistry>());

        // Background lifecycle services
        services.AddHostedService<BotLifecycleService>();
        services.AddHostedService<TimerSchedulerService>();
        services.AddHostedService<TimerService>();

        // Twitch options
        services.Configure<TwitchOptions>(configuration.GetSection(TwitchOptions.SectionName));

        // Twitch HTTP clients with resilience
        services.AddHttpClient("twitch-auth");
        services.AddHttpClient("twitch-helix")
            .AddTwitchResilienceHandler();
        services.AddHttpClient("twitch-eventsub");

        // Twitch auth service (scoped — uses IApplicationDbContext)
        services.AddScoped<ITwitchAuthService, TwitchAuthService>();

        // Twitch API service (scoped — uses IApplicationDbContext for tokens)
        services.AddScoped<ITwitchApiService, TwitchApiService>();

        // Twitch IRC chat service (singleton + hosted service — persistent WebSocket connection)
        services.AddSingleton<TwitchIrcService>();
        services.AddSingleton<ITwitchChatService>(sp => sp.GetRequiredService<TwitchIrcService>());
        services.AddHostedService(sp => sp.GetRequiredService<TwitchIrcService>());

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
