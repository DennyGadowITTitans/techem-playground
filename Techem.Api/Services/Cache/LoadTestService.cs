using System.Diagnostics;
using System.Text.Json;

namespace Techem.Api.Services.Cache;

/// <summary>
/// Service for load testing Azure Table Storage with device configurations
/// </summary>
public class LoadTestService : ILoadTestService
{
    private readonly IConfigurationService _configurationService;
    private readonly IConfigurationDatabaseService _databaseService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<LoadTestService> _logger;
    private readonly IConfiguration _configuration;

    public LoadTestService(
        IConfigurationService configurationService,
        IConfigurationDatabaseService databaseService,
        ICacheService cacheService,
        ILogger<LoadTestService> logger,
        IConfiguration configuration)
    {
        _configurationService = configurationService;
        _databaseService = databaseService;
        _cacheService = cacheService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<LoadTestResult> RunLoadTestAsync(int numberOfRecords, int batchSize, int concurrentTasks)
    {
        _logger.LogInformation("Starting load test with {NumberOfRecords} records", numberOfRecords);
        
        var result = new LoadTestResult
        {
            StartTime = DateTime.UtcNow,
            RecordsProcessed = numberOfRecords,
            BatchSize = batchSize,
            ConcurrentTasks = concurrentTasks,
            CacheServiceType = _cacheService.GetType().Name
        };

        var stopwatch = Stopwatch.StartNew();
        var successCount = 0;
        var failureCount = 0;

        try
        {
            _logger.LogInformation("Processing {NumberOfRecords} records in batches of {BatchSize} with {ConcurrentTasks} concurrent tasks", 
                numberOfRecords, batchSize, concurrentTasks);

            // Generate all PRDV identifiers first
            var prdvs = GenerateValidPrdvs(numberOfRecords);

            // Process records in batches with concurrency control
            var batches = CreateBatches(prdvs, batchSize);
            var semaphore = new SemaphoreSlim(concurrentTasks, concurrentTasks);
            
            var tasks = batches.Select(async batch =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var batchResults = await ProcessBatchAsync(batch);
                    Interlocked.Add(ref successCount, batchResults.successCount);
                    Interlocked.Add(ref failureCount, batchResults.failureCount);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during load test execution");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            result.EndTime = DateTime.UtcNow;
            result.TotalDuration = stopwatch.Elapsed;
            result.SuccessfulOperations = successCount;
            result.FailedOperations = failureCount;
            
            if (stopwatch.Elapsed.TotalSeconds > 0)
            {
                result.RecordsPerSecond = successCount / stopwatch.Elapsed.TotalSeconds;
                result.AverageTimePerRecord = TimeSpan.FromMilliseconds(stopwatch.Elapsed.TotalMilliseconds / numberOfRecords);
            }

            _logger.LogInformation(
                "Load test completed: {SuccessCount} successful, {FailureCount} failed, {Duration:F2}s total, {RecordsPerSecond:F2} records/sec",
                successCount, failureCount, stopwatch.Elapsed.TotalSeconds, result.RecordsPerSecond);
                
            // Save report to file if enabled
            await SaveReportToFileAsync(result);
        }

        return result;
    }

    private List<string> GenerateValidPrdvs(int count)
    {
        _logger.LogInformation("Generating {Count} valid PRDV identifiers", count);
        
        var prdvs = new List<string>(count);
        var deviceTypes = new[] { "HM", "WM", "GM", "EM", "TS", "HS", "PS", "FS" }; // Heat, Water, Gas, Electricity, Temperature, Humidity, Pressure, Flow
        var locations = new[] { "001", "002", "003", "004", "005", "006", "007", "008", "009", "010" };
        
        for (int i = 0; i < count; i++)
        {
            // Generate realistic PRDV format: [DeviceType][Location][SerialNumber]
            var deviceType = deviceTypes[i % deviceTypes.Length];
            var location = locations[Random.Shared.Next(locations.Length)];
            var serialNumber = (1000000 + i).ToString(); // Ensures unique serial numbers
            
            var prdv = $"{deviceType}{location}{serialNumber}";
            prdvs.Add(prdv);
        }
        
        _logger.LogInformation("Generated {Count} PRDV identifiers", prdvs.Count);
        return prdvs;
    }

    private static List<List<string>> CreateBatches(List<string> items, int batchSize)
    {
        var batches = new List<List<string>>();
        
        for (int i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            batches.Add(batch);
        }
        
        return batches;
    }

    private async Task<(int successCount, int failureCount)> ProcessBatchAsync(List<string> prdvs)
    {
        var successCount = 0;
        var failureCount = 0;

        foreach (var prdv in prdvs)
        {
            try
            {
                // Generate configuration using the dummy database service
                var configuration = await _databaseService.GetConfigurationAsync(prdv);
                
                if (configuration != null)
                {
                    // Store directly in cache/table storage via ConfigurationService
                    await _configurationService.SetConfigurationAsync(prdv, configuration);
                    successCount++;
                    
                    if (successCount % 1000 == 0)
                    {
                        _logger.LogInformation("Processed {Count} records successfully", successCount + failureCount);
                    }
                }
                else
                {
                    failureCount++;
                    _logger.LogWarning("Failed to generate configuration for PRDV: {Prdv}", prdv);
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Error processing PRDV: {Prdv}", prdv);
            }
        }

        return (successCount, failureCount);
    }

    private async Task SaveReportToFileAsync(LoadTestResult result)
    {
        try
        {
            // Check if file saving is enabled
            var enableFileSaving = _configuration.GetValue<bool>("LoadTestReports:EnableFileSaving", false);
            if (!enableFileSaving)
            {
                return;
            }

            var reportsDirectory = _configuration.GetValue<string>("LoadTestReports:ReportsDirectory", "LoadTestReports");
            var versionName = _configuration.GetValue<string>("LoadTestReports:VersionName", "Unknown-Version");

            // Create reports directory if it doesn't exist
            Directory.CreateDirectory(reportsDirectory);

            // Create report with version information
            var reportWithVersion = new
            {
                Version = versionName,
                GeneratedAt = DateTime.UtcNow,
                TestResult = result
            };

            // Generate filename with timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var filename = $"LoadTestReport_{versionName}_{timestamp}.json";
            var filePath = Path.Combine(reportsDirectory, filename);

            // Serialize and save to file
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonContent = JsonSerializer.Serialize(reportWithVersion, jsonOptions);
            await File.WriteAllTextAsync(filePath, jsonContent);

            _logger.LogInformation("Load test report saved to: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save load test report to file");
        }
    }

}