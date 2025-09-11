using Grpc.Core;
using Techem.Cache.Protos;
using DeviceConfiguration = Techem.Cache.Models.DeviceConfiguration;

namespace Techem.Cache.Services;

public class ConfigurationService : Protos.ConfigurationService.ConfigurationServiceBase
{
    private readonly ICacheService _cacheService;
    private readonly IConfigurationDatabaseService _databaseService;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        ICacheService cacheService, 
        IConfigurationDatabaseService databaseService, 
        ILogger<ConfigurationService> logger)
    {
        _cacheService = cacheService;
        _databaseService = databaseService;
        _logger = logger;
    }

    public override async Task<GetConfigurationResponse> GetConfiguration(GetConfigurationRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received device configuration request for PRDV: {Prdv}", request.Prdv);

        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Prdv))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "PRDV cannot be empty"));
            }

            // Step 1: Check cache first (cache-aside pattern)
            var cachedConfig = await _cacheService.GetConfigurationAsync(request.Prdv);
            
            if (cachedConfig != null)
            {
                _logger.LogInformation("Returning cached device configuration for PRDV: {Prdv}", request.Prdv);
                return MapToGrpcResponse(cachedConfig, found: true);
            }

            // Step 2: Cache miss - query database
            _logger.LogInformation("Cache miss for PRDV: {Prdv}, querying database", request.Prdv);
            
            var dbConfig = await _databaseService.GetConfigurationAsync(request.Prdv);

            if (dbConfig == null)
            {
                _logger.LogWarning("Device configuration not found in database for PRDV: {Prdv}", request.Prdv);
                return MapToGrpcResponse(null, found: false);
            }

            // Step 3: Cache the result from database
            await _cacheService.SetConfigurationAsync(request.Prdv, dbConfig);
            _logger.LogInformation("Cached device configuration for PRDV: {Prdv}, StorageInterval: {StorageInterval}", 
                request.Prdv, dbConfig.StorageInterval);

            // Step 4: Return the result
            return MapToGrpcResponse(dbConfig, found: true);
        }
        catch (RpcException)
        {
            throw; // Re-throw RpcExceptions as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing device configuration request for PRDV: {Prdv}", request.Prdv);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    private static GetConfigurationResponse MapToGrpcResponse(DeviceConfiguration? config, bool found)
    {
        var response = new GetConfigurationResponse
        {
            Found = found
        };

        if (config != null)
        {
            response.Configuration = new Protos.DeviceConfiguration
            {
                Prdv = config.PrDv,
                StorageInterval = MapStorageIntervalToGrpc(config.StorageInterval),
                IsStorageEnabled = config.IsStorageEnabled,
                MaxDataAgeInDays = config.MaxDataAgeInDays,
                DeviceType = config.DeviceType,
                LastUpdated = config.LastUpdated.ToString("O") // ISO 8601 format
            };

            // Add additional properties
            foreach (var kvp in config.AdditionalProperties)
            {
                response.Configuration.AdditionalProperties.Add(kvp.Key, kvp.Value);
            }
        }

        return response;
    }

    private static StorageInterval MapStorageIntervalToGrpc(Models.StorageInterval interval)
    {
        return interval switch
        {
            Models.StorageInterval.Every15Minutes => StorageInterval.Every15Minutes,
            Models.StorageInterval.Hourly => StorageInterval.Hourly,
            Models.StorageInterval.Daily => StorageInterval.Daily,
            Models.StorageInterval.Weekly => StorageInterval.Weekly,
            Models.StorageInterval.Every15Days => StorageInterval.Every15Days,
            Models.StorageInterval.Monthly => StorageInterval.Monthly,
            Models.StorageInterval.NoStorage => StorageInterval.NoStorage,
            _ => StorageInterval.Unknown
        };
    }
}