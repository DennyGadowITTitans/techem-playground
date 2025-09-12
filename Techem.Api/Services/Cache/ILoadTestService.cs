namespace Techem.Api.Services.Cache;

/// <summary>
/// Service for load testing Azure Table Storage with device configurations
/// </summary>
public interface ILoadTestService
{
    /// <summary>
    /// Performs a load test by generating and storing device configurations
    /// </summary>
    /// <param name="numberOfRecords">Number of test records to generate and store</param>
    /// <param name="batchSize">Number of records to process in each batch</param>
    /// <param name="concurrentTasks">Number of concurrent tasks for parallel processing</param>
    /// <returns>Load test results including timing and performance metrics</returns>
    Task<LoadTestResult> RunLoadTestAsync(int numberOfRecords, int batchSize, int concurrentTasks);
}

/// <summary>
/// Result of a load test operation
/// </summary>
public class LoadTestResult
{
    /// <summary>
    /// Number of records processed
    /// </summary>
    public int RecordsProcessed { get; set; }
    
    /// <summary>
    /// Total time taken for the load test
    /// </summary>
    public TimeSpan TotalDuration { get; set; }
    
    /// <summary>
    /// Average time per record
    /// </summary>
    public TimeSpan AverageTimePerRecord { get; set; }
    
    /// <summary>
    /// Records per second throughput
    /// </summary>
    public double RecordsPerSecond { get; set; }
    
    /// <summary>
    /// Number of successful operations
    /// </summary>
    public int SuccessfulOperations { get; set; }
    
    /// <summary>
    /// Number of failed operations
    /// </summary>
    public int FailedOperations { get; set; }
    
    /// <summary>
    /// Batch size used during the load test
    /// </summary>
    public int BatchSize { get; set; }
    
    /// <summary>
    /// Number of concurrent tasks used during the load test
    /// </summary>
    public int ConcurrentTasks { get; set; }
    
    /// <summary>
    /// Start time of the load test
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// End time of the load test
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// Type of cache service used during the load test
    /// </summary>
    public string CacheServiceType { get; set; } = string.Empty;
}