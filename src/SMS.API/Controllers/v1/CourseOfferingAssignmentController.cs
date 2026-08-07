using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.CourseOfferings.Commands;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class CourseOfferingAssignmentController : BaseApiController
    {
        private readonly ILogger<CourseOfferingAssignmentController> _logger;

        public CourseOfferingAssignmentController(ILogger<CourseOfferingAssignmentController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Assign a student to an active course offering.
        /// </summary>
        [HttpPost("{offeringId}/students")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(CourseOfferingEnrollmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignStudentToOffering(
            Guid offeringId,
            [FromBody] AssignStudentToOfferingCommand command,
            CancellationToken cancellationToken)
        {
            command.CourseOfferingId = offeringId;
            var result = await Mediator.Send(command, cancellationToken);
            _logger.LogInformation("Assigned student {StudentId} to offering {OfferingId}",
                command.StudentId, offeringId);
            return CreatedAtAction(nameof(AssignStudentToOffering), new { offeringId }, result);
        }

        /// <summary>
        /// Assign a lecturer to teach a course offering.
        /// </summary>
        [HttpPost("{offeringId}/lecturers")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(CourseOfferingLecturerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignLecturerToOffering(
            Guid offeringId,
            [FromBody] AssignLecturerToOfferingCommand command,
            CancellationToken cancellationToken)
        {
            command.CourseOfferingId = offeringId;
            var result = await Mediator.Send(command, cancellationToken);
            _logger.LogInformation("Assigned lecturer {LecturerId} to offering {OfferingId}",
                command.LecturerId, offeringId);
            return CreatedAtAction(nameof(AssignLecturerToOffering), new { offeringId }, result);
        }
    }
}
