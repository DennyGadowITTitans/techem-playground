using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Techem.Cache.Models;

namespace Techem.Cache.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1);

    public RedisCacheService(IDistributedCache distributedCache, ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<DeviceConfiguration?> GetConfigurationAsync(string prdv)
    {
        try
        {
            var cacheKey = GetCacheKey(prdv);
            var cachedData = await _distributedCache.GetStringAsync(cacheKey);
            
            if (cachedData == null)
            {
                _logger.LogDebug("Cache miss for PRDV: {Prdv}", prdv);
                return null;
            }

            _logger.LogDebug("Cache hit for PRDV: {Prdv}", prdv);
            return JsonSerializer.Deserialize<DeviceConfiguration>(cachedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration from cache for PRDV: {Prdv}", prdv);
            return null;
        }
    }

    public async Task SetConfigurationAsync(string prdv, DeviceConfiguration configuration)
    {
        try
        {
            var cacheKey = GetCacheKey(prdv);
            var serializedData = JsonSerializer.Serialize(configuration);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultExpiration
            };

            await _distributedCache.SetStringAsync(cacheKey, serializedData, options);
            _logger.LogDebug("Configuration cached for PRDV: {Prdv}", prdv);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration in cache for PRDV: {Prdv}", prdv);
        }
    }

    public async Task<bool> ExistsAsync(string prdv)
    {
        try
        {
            var cacheKey = GetCacheKey(prdv);
            var cachedData = await _distributedCache.GetStringAsync(cacheKey);
            return cachedData != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for PRDV: {Prdv}", prdv);
            return false;
        }
    }

    private static string GetCacheKey(string prdv)
    {
        return $"config:{prdv}";
    }
}