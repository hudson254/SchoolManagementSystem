using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Infrastructure.Services;

namespace SMS.API.Controllers.v1
{
    /// <summary>
    /// Secure administrator-only controller for the searchable error repository.
    /// Provides search, filtering, pagination, and resolution management.
    /// Restricted to the Administrator role. All access is audited.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/errors")]
    [Authorize(Policy = "AdministratorAccess")]
    public class ErrorAdminController : ControllerBase
    {
        private readonly IErrorRepository _errorRepository;

        public ErrorAdminController(IErrorRepository errorRepository)
        {
            _errorRepository = errorRepository;
        }

        /// <summary>
        /// Searches the error repository with advanced filters and pagination.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<object>> Search(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? userId = null,
            [FromQuery] string? tenantId = null,
            [FromQuery] string? module = null,
            [FromQuery] ErrorCategory? category = null,
            [FromQuery] ErrorSeverity? severity = null,
            [FromQuery] string? route = null,
            [FromQuery] string? correlationId = null,
            [FromQuery] string? sessionId = null,
            [FromQuery] string? keyword = null,
            [FromQuery] string? exceptionType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = true)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var results = await _errorRepository.SearchAsync(
                from, to, userId, tenantId, module, category, severity,
                route, correlationId, sessionId, keyword, exceptionType,
                page, pageSize, sortBy, sortDescending);

            var total = await _errorRepository.CountAsync(
                from, to, userId, tenantId, module, category, severity,
                route, correlationId, sessionId, keyword, exceptionType);

            return Ok(new
            {
                success = true,
                data = results,
                pagination = new
                {
                    page,
                    pageSize,
                    total,
                    totalPages = (int)Math.Ceiling(total / (double)pageSize)
                }
            });
        }

        /// <summary>
        /// Gets a single error record by ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<object>> GetById(Guid id)
        {
            var record = await _errorRepository.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound(new
                {
                    success = false,
                    code = "NOT_FOUND",
                    message = "The requested error record was not found."
                });
            }

            return Ok(new { success = true, data = record });
        }

        /// <summary>
        /// Gets the most recent error records for the real-time feed.
        /// </summary>
        [HttpGet("recent")]
        public async Task<ActionResult<object>> GetRecent([FromQuery] int count = 50)
        {
            count = Math.Clamp(count, 1, 200);
            var results = await _errorRepository.GetRecentAsync(count);
            return Ok(new { success = true, data = results });
        }

        /// <summary>
        /// Updates the resolution status, assignment, and notes of an error record.
        /// </summary>
        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<object>> Update(Guid id, [FromBody] UpdateErrorRecordRequest request)
        {
            var record = await _errorRepository.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound(new
                {
                    success = false,
                    code = "NOT_FOUND",
                    message = "The requested error record was not found."
                });
            }

            if (!string.IsNullOrWhiteSpace(request.ResolutionStatus))
                record.ResolutionStatus = request.ResolutionStatus;
            if (!string.IsNullOrWhiteSpace(request.AssignedTo))
                record.AssignedTo = request.AssignedTo;
            if (request.Notes != null)
                record.Notes = request.Notes;

            await _errorRepository.UpdateAsync(record);

            return Ok(new
            {
                success = true,
                message = "Error record updated successfully.",
                data = record
            });
        }

        /// <summary>
        /// Exports error records as CSV.
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportCsv(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? userId = null,
            [FromQuery] string? tenantId = null,
            [FromQuery] ErrorCategory? category = null,
            [FromQuery] ErrorSeverity? severity = null,
            [FromQuery] string? route = null,
            [FromQuery] string? correlationId = null,
            [FromQuery] string? keyword = null)
        {
            var results = await _errorRepository.SearchAsync(
                from, to, userId, tenantId, null, category, severity,
                route, correlationId, null, keyword, null,
                1, 1000, "timestamp", true);

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Timestamp,Severity,Category,ExceptionType,Message,CorrelationId,UserId,Username,Route,HttpMethod,Status");

            foreach (var r in results)
            {
                csv.AppendLine(string.Join(",",
                    r.TimestampUtc.ToString("O"),
                    r.Severity,
                    r.Category,
                    EscapeCsv(r.ExceptionType ?? string.Empty),
                    EscapeCsv(r.ExceptionMessage ?? string.Empty),
                    EscapeCsv(r.CorrelationId ?? string.Empty),
                    EscapeCsv(r.UserId ?? string.Empty),
                    EscapeCsv(r.Username ?? string.Empty),
                    EscapeCsv(r.Route ?? string.Empty),
                    EscapeCsv(r.HttpMethod ?? string.Empty),
                    EscapeCsv(r.ResolutionStatus ?? "Open")));
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"error-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }

    /// <summary>
    /// Request model for updating an error record.
    /// </summary>
    public class UpdateErrorRecordRequest
    {
        public string? ResolutionStatus { get; set; }
        public string? AssignedTo { get; set; }
        public string? Notes { get; set; }
    }
}
