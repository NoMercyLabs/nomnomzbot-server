using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NoMercyBot.Application.Common.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Caching;

/// <summary>
/// ICacheService implementation backed by IMemoryCache.
/// Provides a simple typed caching layer with optional expiration.
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        T? value = _cache.TryGetValue(key, out T? result) ? result : default;
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default
    )
    {
        MemoryCacheEntryOptions options = new();

        if (expiration.HasValue)
        {
            options.SetAbsoluteExpiration(expiration.Value);
        }
        else
        {
            // Default 5-minute sliding expiration
            options.SetSlidingExpiration(TimeSpan.FromMinutes(5));
        }

        _cache.Set(key, value, options);
        _logger.LogTrace(
            "Cache SET: {Key} (expiration={Expiration})",
            key,
            expiration?.ToString() ?? "sliding:5m"
        );

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        _logger.LogTrace("Cache REMOVE: {Key}", key);
        return Task.CompletedTask;
    }

    public Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default
    )
    {
        return _cache.GetOrCreateAsync(
            key,
            async entry =>
            {
                if (expiration.HasValue)
                {
                    entry.SetAbsoluteExpiration(expiration.Value);
                }
                else
                {
                    entry.SetSlidingExpiration(TimeSpan.FromMinutes(5));
                }

                _logger.LogTrace("Cache MISS: {Key}, executing factory", key);
                return await factory();
            }
        )!;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cache.TryGetValue(key, out _));
    }
}
