using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.ReturningUser.Commands;
using SMS.Application.Features.ReturningUser.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class ReturningUserController : BaseApiController
    {
        private readonly ILogger<ReturningUserController> _logger;

        public ReturningUserController(ILogger<ReturningUserController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Enroll a returning (Approved) student into a new course for a new semester
        /// </summary>
        [HttpPost("enroll")]
        [ProducesResponseType(typeof(ReturningEnrollmentResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Enroll(
            [FromBody] SubmitReturningStudentEnrollmentCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            _logger.LogInformation("Returning student enrolled in course {CourseId}", command.CourseId);
            return Ok(result);
        }

        /// <summary>
        /// Get the current student's enrollment history
        /// </summary>
        [HttpGet("course-history")]
        [ProducesResponseType(typeof(CourseHistoryDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourseHistory(CancellationToken cancellationToken)
        {
            var query = new GetStudentCourseHistoryQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
