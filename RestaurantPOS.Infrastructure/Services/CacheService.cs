using Microsoft.Extensions.Caching.Distributed;
using RestaurantPOS.Domain.Interfaces;
using System.Text.Json;

namespace RestaurantPOS.Infrastructure.Services;

/// <summary>
/// Cache service implementation backed by IDistributedCache (supports both Redis and in-memory).
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await _cache.GetStringAsync(key, cancellationToken);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(30)
        };
        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => await _cache.RemoveAsync(key, cancellationToken);

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Pattern removal requires Redis-specific implementation (SCAN + DEL)
        // Implemented as no-op for non-Redis providers
        return Task.CompletedTask;
    }
}
