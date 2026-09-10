using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Classes.Commands;
using SMS.Application.Features.Classes.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class ClassesController : BaseApiController
    {
        private readonly ILogger<ClassesController> _logger;

        public ClassesController(ILogger<ClassesController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// List classes (unit scheduled to a lecturer within a semester). Readable
        /// by SystemAdministrator / Administrator / Coordinator (ModeratorAccess).
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<ClassDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClasses(
            [FromQuery] string? searchTerm = null,
            [FromQuery] Guid? semesterId = null,
            [FromQuery] Guid? unitId = null,
            [FromQuery] bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = new GetClassesQuery
            {
                SearchTerm = searchTerm,
                SemesterId = semesterId,
                UnitId = unitId,
                IncludeInactive = includeInactive
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get a single class by ID.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(ClassDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClass(Guid id, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new GetClassQuery { ClassId = id }, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Create a class. Allowed for coordinator and above (ModeratorAccess).
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(ClassDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateClass(
            [FromBody] CreateClassCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetClass), new { id = result.Id }, result);
        }

        /// <summary>
        /// Update a class. Allowed for coordinator and above (ModeratorAccess).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(ClassDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateClass(
            Guid id,
            [FromBody] UpdateClassCommand command,
            CancellationToken cancellationToken)
        {
            if (id != command.Id)
                return BadRequest("ID mismatch");

            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Delete a class. Administrator-only (AdministratorAccess); coordinators
        /// create and modify classes but cannot delete them.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteClass(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteClassCommand { ClassId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}