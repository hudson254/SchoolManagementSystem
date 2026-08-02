using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.PasswordReset.Commands;
using SMS.Application.Features.PasswordReset.Queries;
using SMS.Domain.Entities;

namespace SMS.API.Controllers.v1
{
    [ApiController]
    [Route("api/v1/admin/password-resets")]
    [Produces("application/json")]

    // ADMIN-RESET fix: These endpoints reset user passwords and manage
    // password-reset requests. They were previously unauthenticated, allowing
    // any anonymous caller to fulfill/reject password resets (arbitrary
    // account takeover). All endpoints now require the Administrator role
    // via the "AdministratorAccess" policy (RequireRole("Administrator")).
    [Authorize(Policy = "AdministratorAccess")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PasswordResetController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all password reset requests, optionally filtered by status.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PasswordResetRequest>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] PasswordResetRequestStatus? status, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllPasswordResetRequestsQuery { StatusFilter = status }, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get pending password reset requests.
        /// </summary>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(List<PasswordResetRequest>), 200)]
        public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPendingPasswordResetRequestsQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Fulfill a password reset request by generating a temporary password and resetting the user's password.
        /// </summary>
        [HttpPost("{requestId:guid}/fulfill")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Fulfill([FromRoute] Guid requestId, [FromBody] FulfillPasswordResetRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            await _mediator.Send(new FulfillPasswordResetCommand
            {
                RequestId = requestId,
                AdminUserId = request.AdminUserId,
                ResolutionNote = request.ResolutionNote
            }, cancellationToken);

            return NoContent();
        }

        /// <summary>
        /// Reject a password reset request.
        /// </summary>
        [HttpPost("{requestId:guid}/reject")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject([FromRoute] Guid requestId, [FromBody] RejectPasswordResetRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            await _mediator.Send(new RejectPasswordResetCommand
            {
                RequestId = requestId,
                AdminUserId = request.AdminUserId,
                ResolutionNote = request.ResolutionNote
            }, cancellationToken);

            return NoContent();
        }
    }

    public class FulfillPasswordResetRequest
    {
        public string AdminUserId { get; set; } = string.Empty;
        public string? ResolutionNote { get; set; }
    }

    public class RejectPasswordResetRequest
    {
        public string AdminUserId { get; set; } = string.Empty;
        public string? ResolutionNote { get; set; }
    }
}
