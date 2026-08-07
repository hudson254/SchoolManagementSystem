using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.CourseOfferings.Commands;
using SMS.Application.Features.CourseOfferings.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class ConfirmationController : BaseApiController
    {
        private readonly ILogger<ConfirmationController> _logger;

        public ConfirmationController(ILogger<ConfirmationController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get pending enrollment confirmations for a student.
        /// </summary>
        [HttpGet("enrollments/pending/{studentId}")]
        [ProducesResponseType(typeof(IEnumerable<CourseOfferingEnrollmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingEnrollments(
            Guid studentId,
            CancellationToken cancellationToken)
        {
            var query = new GetStudentCourseEnrollmentsQuery
            {
                StudentId = studentId,
                Status = "pending"
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Confirm or reject an enrollment assignment.
        /// </summary>
        [HttpPost("enrollments/{id}/confirm")]
        [ProducesResponseType(typeof(CourseOfferingEnrollmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmEnrollment(
            Guid id,
            [FromBody] ConfirmEnrollmentCommand command,
            CancellationToken cancellationToken)
        {
            command.EnrollmentId = id;
            var result = await Mediator.Send(command, cancellationToken);
            _logger.LogInformation("Enrollment {EnrollmentId} confirmation processed", id);
            return Ok(result);
        }

        /// <summary>
        /// Confirm or reject a teaching assignment.
        /// </summary>
        [HttpPost("teaching/{id}/confirm")]
        [ProducesResponseType(typeof(CourseOfferingLecturerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmTeachingAssignment(
            Guid id,
            [FromBody] ConfirmTeachingAssignmentCommand command,
            CancellationToken cancellationToken)
        {
            command.AssignmentId = id;
            var result = await Mediator.Send(command, cancellationToken);
            _logger.LogInformation("Teaching assignment {AssignmentId} confirmation processed", id);
            return Ok(result);
        }

        /// <summary>
        /// Report an issue with an enrollment or teaching assignment.
        /// </summary>
        [HttpPost("issues")]
        [ProducesResponseType(typeof(AssignmentIssueReportDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReportIssue(
            [FromBody] ReportAssignmentIssueCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            _logger.LogInformation("Issue reported for offering {CourseOfferingId}", command.CourseOfferingId);
            return CreatedAtAction(nameof(ReportIssue), new { id = result.Id }, result);
        }
    }
}
