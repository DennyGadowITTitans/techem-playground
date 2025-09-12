using Microsoft.AspNetCore.Mvc;
using Techem.Api.Services.Cache;

namespace Techem.Api.Controllers;

/// <summary>
/// Controller for Azure Table Storage load testing operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LoadTestController : ControllerBase
{
    private readonly ILoadTestService _loadTestService;
    private readonly ILogger<LoadTestController> _logger;
    private readonly IConfiguration _configuration;

    public LoadTestController(
        ILoadTestService loadTestService,
        ILogger<LoadTestController> logger,
        IConfiguration configuration)
    {
        _loadTestService = loadTestService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Runs a load test with specified parameters
    /// </summary>
    /// <param name="recordCount">Number of records to generate and store (default: 10000)</param>
    /// <param name="batchSize">Number of records to process in each batch (default: 100)</param>
    /// <param name="concurrentTasks">Number of concurrent tasks for parallel processing (default: 10)</param>
    /// <returns>Load test results including performance metrics</returns>
    [HttpPost("run")]
    public async Task<ActionResult<LoadTestResult>> RunLoadTest(
        [FromQuery] int recordCount = 10000,
        [FromQuery] int batchSize = 100,
        [FromQuery] int concurrentTasks = 10)
    {
        if (recordCount <= 0)
        {
            return BadRequest("Record count must be greater than 0");
        }

        if (recordCount > 100000)
        {
            return BadRequest("Record count cannot exceed 100,000 for safety reasons");
        }

        if (batchSize <= 0)
        {
            return BadRequest("Batch size must be greater than 0");
        }

        if (concurrentTasks <= 0 || concurrentTasks > 50)
        {
            return BadRequest("Concurrent tasks must be between 1 and 50");
        }

        _logger.LogInformation("Starting load test via API with {RecordCount} records, batch size {BatchSize}, concurrent tasks {ConcurrentTasks}", 
            recordCount, batchSize, concurrentTasks);

        try
        {
            var result = await _loadTestService.RunLoadTestAsync(recordCount, batchSize, concurrentTasks);
            
            _logger.LogInformation("Load test completed successfully: {SuccessCount}/{TotalCount} records processed in {Duration}",
                result.SuccessfulOperations, result.RecordsProcessed, result.TotalDuration);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load test failed");
            return StatusCode(500, new { error = "Load test failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets the default load test configuration settings
    /// </summary>
    /// <returns>Default load test configuration</returns>
    [HttpGet("config")]
    public ActionResult<LoadTestConfig> GetLoadTestConfig()
    {
        var config = new LoadTestConfig
        {
            DefaultRecordCount = 10000,
            BatchSize = 100,
            ConcurrentTasks = 10,
            EnableProgressLogging = true
        };

        return Ok(config);
    }

    /// <summary>
    /// Runs a small load test to verify the system is working
    /// </summary>
    /// <returns>Load test results for verification</returns>
    [HttpPost("verify")]
    public async Task<ActionResult<LoadTestResult>> VerifyLoadTest()
    {
        _logger.LogInformation("Running verification load test with 10 records");

        try
        {
            var result = await _loadTestService.RunLoadTestAsync(10, 10, 1);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verification load test failed");
            return StatusCode(500, new { error = "Verification test failed", details = ex.Message });
        }
    }
}

/// <summary>
/// Load test configuration model
/// </summary>
public class LoadTestConfig
{
    /// <summary>
    /// Default number of records to process
    /// </summary>
    public int DefaultRecordCount { get; set; }
    
    /// <summary>
    /// Number of records to process in each batch
    /// </summary>
    public int BatchSize { get; set; }
    
    /// <summary>
    /// Number of concurrent tasks for parallel processing
    /// </summary>
    public int ConcurrentTasks { get; set; }
    
    /// <summary>
    /// Whether to enable progress logging during load test
    /// </summary>
    public bool EnableProgressLogging { get; set; }
}