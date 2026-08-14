using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.Approvals.Commands;
using SMS.Application.Features.Approvals.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize(Roles = "Admin,Registrar,Coordinator,Receptionist")]
    public class ApprovalController : BaseApiController
    {
        private readonly ILogger<ApprovalController> _logger;

        public ApprovalController(ILogger<ApprovalController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get all pending registrations awaiting approval.
        /// </summary>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(PendingApprovalsResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingApprovals(
            [FromQuery] string? userType,
            CancellationToken cancellationToken)
        {
            var query = new GetPendingApprovalsQuery { UserType = userType };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Approve a single registration.
        /// </summary>
        [HttpPost("approve")]
        [ProducesResponseType(typeof(ApprovalResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Approve(
            [FromBody] ApproveRegistrationCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            _logger.LogInformation("Registration approved for {UserType} {UserId}", command.UserType, command.UserId);
            return Ok(result);
        }

        /// <summary>
        /// Bulk approve multiple registrations.
        /// </summary>
        [HttpPost("bulk-approve")]
        [ProducesResponseType(typeof(BulkApprovalResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkApprove(
            [FromBody] BulkApproveRegistrationsCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            _logger.LogInformation("Bulk approval completed: {SuccessCount} succeeded, {FailureCount} failed",
                result.SuccessCount, result.FailureCount);
            return Ok(result);
        }

        /// <summary>
        /// Reject a registration with a reason.
        /// </summary>
        [HttpPost("reject")]
        [ProducesResponseType(typeof(ApprovalResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reject(
            [FromBody] RejectRegistrationCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            _logger.LogInformation("Registration rejected for {UserType} {UserId}: {Reason}",
                command.UserType, command.UserId, command.Reason);
            return Ok(result);
        }
    }
}
