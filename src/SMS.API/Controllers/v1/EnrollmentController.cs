using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.Enrollments.Commands;
using SMS.Application.Features.Enrollments.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class EnrollmentController : BaseApiController
    {
        private readonly ILogger<EnrollmentController> _logger;

        public EnrollmentController(ILogger<EnrollmentController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Submit a student's course enrollment (course + unit selection).
        /// Only available for students with PendingCourseSelection status.
        /// </summary>
        [HttpPost("submit-enrollment")]
        [ProducesResponseType(typeof(EnrollmentSubmissionResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SubmitEnrollment(
            [FromBody] SubmitStudentEnrollmentCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get the current student's enrollment status.
        /// Returns registration status, course selection info, and pending approval state.
        /// </summary>
        [HttpGet("my-status")]
        [ProducesResponseType(typeof(StudentEnrollmentStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyEnrollmentStatus(CancellationToken cancellationToken)
        {
            var query = new GetMyPendingEnrollmentQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
