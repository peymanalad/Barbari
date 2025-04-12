using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using System;
using BarcopoloWebApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

public class ComprehensiveHealthCheck : IHealthCheck
{
    private readonly DataBaseContext _context;
    private readonly ILogger<ComprehensiveHealthCheck> _logger;

    private const long MemoryThresholdBytes = 500 * 1024 * 1024;
    private const int QueryExecutionThresholdMs = 2000;

    public ComprehensiveHealthCheck(DataBaseContext context, ILogger<ComprehensiveHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            bool dbConnected = await _context.Database.CanConnectAsync(cancellationToken);
            if (!dbConnected)
            {
                _logger.LogError("Database connectivity check failed.");
                return HealthCheckResult.Unhealthy("Cannot connect to the database.");
            }

            var stopwatch = Stopwatch.StartNew();
            int personCount = await _context.Persons.CountAsync(cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("Database is healthy. Person count: {Count}", personCount);

            if (stopwatch.ElapsedMilliseconds > QueryExecutionThresholdMs)
            {
                _logger.LogWarning("Query execution time is high: {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
                return HealthCheckResult.Degraded($"Query execution time is high: {stopwatch.ElapsedMilliseconds} ms");
            }

            long currentMemoryUsage = GC.GetTotalMemory(forceFullCollection: false);
            _logger.LogInformation("Current memory usage: {MemoryBytes} bytes", currentMemoryUsage);
            if (currentMemoryUsage > MemoryThresholdBytes)
            {
                _logger.LogWarning("Memory usage is high: {MemoryUsage} bytes", currentMemoryUsage);
                return HealthCheckResult.Degraded($"Memory usage is high: {currentMemoryUsage} bytes");
            }

            _logger.LogInformation("All internal health checks passed.");
            return HealthCheckResult.Healthy("All systems are healthy.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred during health check.");
            return HealthCheckResult.Unhealthy("An exception occurred during health check.", ex);
        }
    }
}
