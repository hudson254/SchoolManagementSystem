using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.Courses.Queries;
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

        /// <summary>
        /// Courses the logged-in student may choose from in the course-selection
        /// wizard. Student-authorized read path: GET /api/v1/courses stays behind
        /// the ModeratorAccess policy because it is the administrator/curriculum
        /// surface, so the student wizard uses this dedicated endpoint instead.
        /// </summary>
        [HttpGet("available-courses")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(IEnumerable<StudentSelectableCourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAvailableCourses(CancellationToken cancellationToken)
        {
            var query = new GetAvailableCoursesForStudentSelectionQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Active units of a selectable course, for the wizard's "Confirm Units"
        /// step. Student-authorized; the moderator-only
        /// GET /api/v1/courses/{id}/units is unchanged and still rejects students.
        /// </summary>
        [HttpGet("available-courses/{courseId:guid}/units")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(IEnumerable<StudentSelectableUnitDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAvailableCourseUnits(
            Guid courseId,
            CancellationToken cancellationToken)
        {
            var query = new GetAvailableCourseUnitsForStudentSelectionQuery { CourseId = courseId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
