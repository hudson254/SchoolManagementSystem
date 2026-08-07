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
    public class CourseOfferingController : BaseApiController
    {
        private readonly ILogger<CourseOfferingController> _logger;

        public CourseOfferingController(ILogger<CourseOfferingController> logger)
        {
            _logger = logger;
        }

        // ===== Course Offerings =====

        [HttpGet]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<CourseOfferingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourseOfferings(
            [FromQuery] Guid? courseId = null,
            [FromQuery] Guid? academicYearId = null,
            [FromQuery] Guid? semesterId = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = new GetCourseOfferingsQuery
            {
                CourseId = courseId,
                AcademicYearId = academicYearId,
                SemesterId = semesterId,
                SearchTerm = searchTerm,
                IncludeInactive = includeInactive
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(CourseOfferingDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCourseOffering(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetCourseOfferingQuery { Id = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}/units")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<CourseOfferingUnitDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCourseOfferingUnits(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetCourseOfferingUnitsQuery { CourseOfferingId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(CourseOfferingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCourseOffering(
            [FromBody] CreateCourseOfferingCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetCourseOffering), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(CourseOfferingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCourseOffering(
            Guid id,
            [FromBody] UpdateCourseOfferingCommand command,
            CancellationToken cancellationToken)
        {
            command.Id = id;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCourseOffering(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteCourseOfferingCommand { Id = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        // ===== Course Offering Units =====

        [HttpPost("{id}/units")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(CourseOfferingUnitDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateCourseOfferingUnit(
            Guid id,
            [FromBody] CreateCourseOfferingUnitCommand command,
            CancellationToken cancellationToken)
        {
            command.CourseOfferingId = id;
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetCourseOfferingUnits), new { id = result.CourseOfferingId }, result);
        }

        [HttpPut("units/{unitId}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(CourseOfferingUnitDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCourseOfferingUnit(
            Guid unitId,
            [FromBody] UpdateCourseOfferingUnitCommand command,
            CancellationToken cancellationToken)
        {
            command.Id = unitId;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("units/{unitId}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCourseOfferingUnit(Guid unitId, CancellationToken cancellationToken)
        {
            var command = new DeleteCourseOfferingUnitCommand { Id = unitId };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        // ===== Student & Lecturer Assignment =====

        [HttpPost("{id}/students")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(CourseOfferingEnrollmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignStudentToOffering(
            Guid id,
            [FromBody] AssignStudentToOfferingCommand command,
            CancellationToken cancellationToken)
        {
            command.CourseOfferingId = id;
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetCourseOffering), new { id = result.CourseOfferingId }, result);
        }

        [HttpPost("{id}/lecturers")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(CourseOfferingLecturerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignLecturerToOffering(
            Guid id,
            [FromBody] AssignLecturerToOfferingCommand command,
            CancellationToken cancellationToken)
        {
            command.CourseOfferingId = id;
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetCourseOffering), new { id = result.CourseOfferingId }, result);
        }
    }
}
