using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.LecturerAssignments.Commands;
using SMS.Application.Features.LecturerAssignments.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class LecturerAssignmentController : BaseApiController
    {
        private readonly ILogger<LecturerAssignmentController> _logger;

        public LecturerAssignmentController(ILogger<LecturerAssignmentController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Submit a lecturer's teaching assignment (course + unit selection).
        /// Only available for lecturers with PendingCourseSelection status.
        /// </summary>
        [HttpPost("submit-teaching-assignment")]
        [ProducesResponseType(typeof(TeachingAssignmentResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SubmitTeachingAssignment(
            [FromBody] SubmitLecturerTeachingAssignmentCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get the current lecturer's teaching assignment status.
        /// </summary>
        [HttpGet("my-status")]
        [ProducesResponseType(typeof(LecturerAssignmentStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyTeachingStatus(CancellationToken cancellationToken)
        {
            var query = new GetMyPendingTeachingAssignmentQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
