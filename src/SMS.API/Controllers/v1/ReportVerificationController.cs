using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.ReportVerification.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.API.Controllers.v1
{
    /// <summary>
    /// Public controller for report verification.
    /// These endpoints are used to verify report authenticity via QR code scanning.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/verify/report")]
    [ApiController]
    public class ReportVerificationController : BaseApiController
    {
        private readonly ILogger<ReportVerificationController> _logger;

        public ReportVerificationController(ILogger<ReportVerificationController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Verifies a report using its verification token (from QR code).
        /// Public endpoint - no authentication required.
        /// </summary>
        [HttpGet("{token}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ReportVerificationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyByToken(
            string token,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Verification token is required." });
            }

            var query = new VerifyReportByTokenQuery { Token = token };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Verifies a report using its verification token (POST method with optional content for hash check).
        /// Public endpoint - no authentication required.
        /// </summary>
        [HttpPost("verify")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ReportVerificationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyPost(
            [FromBody] VerifyReportRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.Token))
            {
                return BadRequest(new { message = "Verification token is required." });
            }

            var query = new VerifyReportByTokenQuery
            {
                Token = request.Token,
                ReportContent = request.ReportContent
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets the verification status of a report by its Report ID.
        /// Public endpoint - no authentication required.
        /// </summary>
        [HttpGet("status/{reportId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ReportVerificationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStatus(
            string reportId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reportId))
            {
                return BadRequest(new { message = "Report ID is required." });
            }

            var query = new GetReportStatusQuery { ReportId = reportId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }

    /// <summary>
    /// Request model for POST verification with optional report content for hash check.
    /// </summary>
    public class VerifyReportRequest
    {
        public string Token { get; set; } = string.Empty;
        public byte[]? ReportContent { get; set; }
    }
}
