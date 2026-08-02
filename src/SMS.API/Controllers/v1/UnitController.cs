using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Units.Commands;
using SMS.Application.Features.Units.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class UnitController : BaseApiController
    {
        private readonly ILogger<UnitController> _logger;

        public UnitController(ILogger<UnitController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(PagedResult<UnitDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnits(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] Guid? courseId = null,
            [FromQuery] bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUnitsQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                CourseId = courseId,
                IsActive = isActive
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(UnitDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUnit(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetUnitQuery { UnitId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(UnitDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUnit(
            [FromBody] CreateUnitCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetUnit), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(UnitDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUnit(
            Guid id,
            [FromBody] UpdateUnitCommand command,
            CancellationToken cancellationToken)
        {
            if (id != command.Id)
                return BadRequest("ID mismatch");

            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUnit(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteUnitCommand { UnitId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpGet("{id}/lecturers")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<LecturerDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnitLecturers(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetUnitLecturersQuery { UnitId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}/students")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<StudentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnitStudents(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetUnitStudentsQuery { UnitId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}