using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SMS.API.Controllers.v1
{
    /// <summary>
    /// Administrative audit log viewer controller.
    /// Provides search, filtering, pagination, and export capabilities for audit records.
    /// Access is restricted to administrators only.
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "AdministratorAccess")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IAuditService auditService, ILogger<AuditController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Gets a paginated list of audit logs with optional filtering.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<AuditLogListResponse>> GetAuditLogs(
            [FromQuery] string? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityName = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool? success = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var (logs, totalCount) = await _auditService.GetAuditLogsAsync(
                    userId, action, entityName, startDate, endDate, success, page, pageSize);

                var response = new AuditLogListResponse
                {
                    Logs = logs.Select(MapToDto).ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                return StatusCode(500, new { Message = "An error occurred while retrieving audit logs." });
            }
        }

        /// <summary>
        /// Gets a single audit log by ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AuditLogDto>> GetAuditLog(Guid id)
        {
            try
            {
                var (logs, _) = await _auditService.GetAuditLogsAsync(page: 1, pageSize: 1);
                // For single record retrieval, we use a direct query approach
                // In a real implementation, this would use a repository method
                var allLogs = await _auditService.GetRecentAuditLogsAsync(1000);
                var log = allLogs.FirstOrDefault(a => a.Id == id);

                if (log == null)
                    return NotFound(new { Message = "Audit log record not found." });

                return Ok(MapToDto(log));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit log {Id}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the audit log." });
            }
        }

        /// <summary>
        /// Exports audit logs to CSV format.
        /// </summary>
        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportToCsv(
            [FromQuery] string? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityName = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool? success = null)
        {
            try
            {
                var (logs, _) = await _auditService.GetAuditLogsAsync(
                    userId, action, entityName, startDate, endDate, success, 1, 10000);

                var csv = new StringBuilder();
                csv.AppendLine("Audit ID,Timestamp,User ID,Username,User Role,Action,Entity Name,Entity ID,IP Address,Success,Failure Reason,Correlation ID,Details");

                foreach (var log in logs)
                {
                    csv.AppendLine($"\"{log.Id}\",\"{log.Timestamp:O}\",\"{EscapeCsv(log.UserId)}\",\"{EscapeCsv(log.Username)}\",\"{EscapeCsv(log.UserRole)}\",\"{EscapeCsv(log.Action)}\",\"{EscapeCsv(log.EntityName)}\",\"{EscapeCsv(log.EntityId)}\",\"{EscapeCsv(log.IPAddress)}\",\"{log.Success}\",\"{EscapeCsv(log.FailureReason)}\",\"{EscapeCsv(log.CorrelationId)}\",\"{EscapeCsv(log.Details)}\"");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit logs to CSV");
                return StatusCode(500, new { Message = "An error occurred while exporting audit logs." });
            }
        }

        /// <summary>
        /// Exports audit logs to JSON format.
        /// </summary>
        [HttpGet("export/json")]
        public async Task<IActionResult> ExportToJson(
            [FromQuery] string? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityName = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool? success = null)
        {
            try
            {
                var (logs, _) = await _auditService.GetAuditLogsAsync(
                    userId, action, entityName, startDate, endDate, success, 1, 10000);

                var json = JsonSerializer.Serialize(logs.Select(MapToDto).ToList(), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var bytes = Encoding.UTF8.GetBytes(json);
                return File(bytes, "application/json", $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit logs to JSON");
                return StatusCode(500, new { Message = "An error occurred while exporting audit logs." });
            }
        }

        /// <summary>
        /// Gets audit log statistics for dashboard display.
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<AuditStatsDto>> GetStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var (logs, _) = await _auditService.GetAuditLogsAsync(
                    startDate: startDate, endDate: endDate, page: 1, pageSize: 10000);

                var logList = logs.ToList();

                var stats = new AuditStatsDto
                {
                    TotalEvents = logList.Count,
                    SuccessfulEvents = logList.Count(l => l.Success),
                    FailedEvents = logList.Count(l => !l.Success),
                    EventsByAction = logList.GroupBy(l => l.Action)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    EventsByEntity = logList.GroupBy(l => l.EntityName)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    EventsByUser = logList.Where(l => !string.IsNullOrEmpty(l.Username))
                        .GroupBy(l => l.Username!)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit statistics");
                return StatusCode(500, new { Message = "An error occurred while retrieving audit statistics." });
            }
        }

        private static AuditLogDto MapToDto(AuditLog log)
        {
            return new AuditLogDto
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                UserId = log.UserId,
                Username = log.Username,
                UserRole = log.UserRole,
                Action = log.Action,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                IPAddress = log.IPAddress,
                UserAgent = log.UserAgent,
                SessionId = log.SessionId,
                CorrelationId = log.CorrelationId,
                Success = log.Success,
                FailureReason = log.FailureReason,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                Details = log.Details
            };
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");
        }
    }

    #region DTOs

    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? UserRole { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? SessionId { get; set; }
        public string? CorrelationId { get; set; }
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? Details { get; set; }
    }

    public class AuditLogListResponse
    {
        public List<AuditLogDto> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class AuditStatsDto
    {
        public int TotalEvents { get; set; }
        public int SuccessfulEvents { get; set; }
        public int FailedEvents { get; set; }
        public Dictionary<string, int> EventsByAction { get; set; } = new();
        public Dictionary<string, int> EventsByEntity { get; set; } = new();
        public Dictionary<string, int> EventsByUser { get; set; } = new();
    }

    #endregion
}
