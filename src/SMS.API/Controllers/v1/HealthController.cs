using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            HealthCheckService healthCheckService,
            ILogger<HealthController> logger)
        {
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        /// <summary>
        /// Get overall health status
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Get()
        {
            var result = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                Status = result.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Checks = result.Entries.Select(e => new
                {
                    Component = e.Key,
                    Status = e.Value.Status.ToString(),
                    Description = e.Value.Description,
                    Data = e.Value.Data
                }),
                TotalDuration = result.TotalDuration
            };

            return result.Status == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(503, response);
        }

        /// <summary>
        /// Readiness probe for Kubernetes/Docker
        /// </summary>
        [HttpGet("ready")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Ready()
        {
            var result = await _healthCheckService.CheckHealthAsync();
            return result.Status == HealthStatus.Healthy ? Ok() : StatusCode(503);
        }

        /// <summary>
        /// Liveness probe for Kubernetes/Docker
        /// </summary>
        [HttpGet("live")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Live()
        {
            return Ok(new
            {
                Status = "Alive",
                Timestamp = DateTime.UtcNow,
                Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime
            });
        }

        /// <summary>
        /// Get detailed metrics
        /// </summary>
        [HttpGet("metrics")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Metrics()
        {
            var process = Process.GetCurrentProcess();
            var memory = GC.GetTotalMemory(false);

            return Ok(new
            {
                Process = new
                {
                    Id = process.Id,
                    ProcessName = process.ProcessName,
                    StartTime = process.StartTime,
                    Uptime = DateTime.UtcNow - process.StartTime,
                    Threads = process.Threads.Count,
                    HandleCount = process.HandleCount,
                    WorkingSet = process.WorkingSet64,
                    PrivateMemory = process.PrivateMemorySize64,
                    VirtualMemory = process.VirtualMemorySize64,
                    PeakWorkingSet = process.PeakWorkingSet64,
                    PeakPagedMemory = process.PeakPagedMemorySize64
                },
                Memory = new
                {
                    TotalAllocated = memory,
                    Generation0 = GC.CollectionCount(0),
                    Generation1 = GC.CollectionCount(1),
                    Generation2 = GC.CollectionCount(2),
                    TotalMemory = GC.GetTotalMemory(false)
                },
                ThreadPool = new
                {
                    AvailableThreads = ThreadPool.ThreadCount,
                    PendingWorkItems = ThreadPool.PendingWorkItemCount,
                    CompletedWorkItems = ThreadPool.CompletedWorkItemCount
                },
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Check database health
        /// </summary>
        [HttpGet("database")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> DatabaseHealth()
        {
            var result = await _healthCheckService.CheckHealthAsync(
                healthCheck => healthCheck.Tags.Contains("database"));

            return result.Status == HealthStatus.Healthy
                ? Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow })
                : StatusCode(503, new { Status = "Unhealthy", Timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Check disk space
        /// </summary>
        [HttpGet("disk")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult DiskSpace()
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => new
                {
                    Name = d.Name,
                    TotalSize = d.TotalSize,
                    AvailableFreeSpace = d.AvailableFreeSpace,
                    UsedSpace = d.TotalSize - d.AvailableFreeSpace,
                    UsagePercentage = d.TotalSize > 0
                        ? (double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize * 100
                        : 0
                });

            return Ok(new
            {
                Drives = drives,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
