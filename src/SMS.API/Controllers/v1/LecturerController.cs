using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Lecturers.Commands;
using SMS.Application.Features.Lecturers.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class LecturerController : BaseApiController
    {
        private readonly ILogger<LecturerController> _logger;

        public LecturerController(ILogger<LecturerController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(PagedResult<LecturerDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLecturers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? isVerified = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetLecturersQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                IsVerified = isVerified
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(LecturerDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLecturer(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetLecturerQuery { LecturerId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(LecturerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateLecturer(
            [FromBody] CreateLecturerCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetLecturer), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(LecturerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLecturer(
            Guid id,
            [FromBody] UpdateLecturerCommand command,
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
        public async Task<IActionResult> DeleteLecturer(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteLecturerCommand { LecturerId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id}/verify")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(LecturerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyLecturer(
            Guid id,
            CancellationToken cancellationToken)
        {
            var command = new VerifyLecturerCommand { LecturerId = id };
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}/units")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(IEnumerable<UnitDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLecturerUnits(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetLecturerUnitsQuery { LecturerId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id}/allocate-unit")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(UnitAllocationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AllocateUnit(
            Guid id,
            [FromBody] AllocateUnitCommand command,
            CancellationToken cancellationToken)
        {
            command.LecturerId = id;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}