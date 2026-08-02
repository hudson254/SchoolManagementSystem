using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.ReportVerification.Commands;
using SMS.Application.Features.ReportVerification.Queries;
using SMS.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.API.Controllers.v1
{
    /// <summary>
    /// Admin controller for managing report verification.
    /// All endpoints require Administrator or Moderator role.
    /// </summary>
    [ApiVersion("1.0")]
    [Authorize(Policy = "ModeratorAccess")]
    [Route("api/v{version:apiVersion}/admin/reports")]
    [ApiController]
    public class ReportAdminController : BaseApiController
    {
        private readonly ILogger<ReportAdminController> _logger;

        public ReportAdminController(ILogger<ReportAdminController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Searches and filters report verification records.
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ReportVerificationListDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchReports(
            [FromQuery] string? reportType = null,
            [FromQuery] string? reportId = null,
            [FromQuery] string? generatedBy = null,
            [FromQuery] ReportVerificationStatus? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var query = new SearchReportsQuery
            {
                ReportType = reportType,
                ReportId = reportId,
                GeneratedBy = generatedBy,
                Status = status,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                PageSize = pageSize
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets verification history for a specific report.
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(List<VerificationHistoryEntryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVerificationHistory(
            [FromQuery] string? reportId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var query = new GetVerificationHistoryQuery
            {
                ReportId = reportId,
                Page = page,
                PageSize = pageSize
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets verification statistics/counts.
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken = default)
        {
            return Ok(new
            {
                message = "Statistics endpoint ready for implementation"
            });
        }

        /// <summary>
        /// Revokes a report, making it fail verification.
        /// </summary>
        [HttpPost("revoke")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RevokeReport(
            [FromBody] RevokeReportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.ReportId))
            {
                return BadRequest(new { message = "Report ID is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { message = "Revocation reason is required." });
            }

            var command = new RevokeReportCommand
            {
                ReportId = request.ReportId,
                Reason = request.Reason
            };
            var result = await Mediator.Send(command, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Report {ReportId} revoked successfully", request.ReportId);
                return Ok(new { message = "Report revoked successfully." });
            }

            return BadRequest(new { message = "Failed to revoke report. Report not found or already revoked." });
        }

        /// <summary>
        /// Restores a previously revoked report.
        /// </summary>
        [HttpPost("restore")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RestoreReport(
            [FromBody] RestoreReportRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.ReportId))
            {
                return BadRequest(new { message = "Report ID is required." });
            }

            var command = new RestoreReportCommand { ReportId = request.ReportId };
            var result = await Mediator.Send(command, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Report {ReportId} restored successfully", request.ReportId);
                return Ok(new { message = "Report restored successfully." });
            }

            return BadRequest(new { message = "Failed to restore report. Report not found or not revoked." });
        }

        /// <summary>
        /// Exports verification logs.
        /// </summary>
        [HttpGet("export")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportVerificationLogs(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Exporting verification logs from {StartDate} to {EndDate}",
                startDate, endDate);
            return Ok(new { message = "Export endpoint ready for implementation" });
        }
    }

    /// <summary>
    /// Request model for restoring a report.
    /// </summary>
    public class RestoreReportRequest
    {
        public string ReportId { get; set; } = string.Empty;
    }
}
