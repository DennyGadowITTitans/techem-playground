using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Techem.Api.Models.Cache;

namespace Techem.Api.Services.Cache;

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

    public async Task<int> SetConfigurationsBatchAsync(Dictionary<string, DeviceConfiguration> configurations)
    {
        if (configurations == null || !configurations.Any())
        {
            return 0;
        }

        var successCount = 0;
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _defaultExpiration
        };

        try
        {
            // Redis doesn't have native batch operations like Azure Table Storage
            // So we'll use parallel processing for improved performance
            var tasks = configurations.Select(async kvp =>
            {
                try
                {
                    var cacheKey = GetCacheKey(kvp.Key);
                    var serializedData = JsonSerializer.Serialize(kvp.Value);
                    await _distributedCache.SetStringAsync(cacheKey, serializedData, options);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting configuration in batch for PRDV: {Prdv}", kvp.Key);
                    return false;
                }
            });

            var results = await Task.WhenAll(tasks);
            successCount = results.Count(r => r);

            _logger.LogDebug("Batch operation completed: {SuccessCount}/{TotalCount} configurations processed", 
                successCount, configurations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch configuration operation");
        }

        return successCount;
    }

    private static string GetCacheKey(string prdv)
    {
        return $"config:{prdv}";
    }
}