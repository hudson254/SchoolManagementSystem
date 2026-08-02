using SMS.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Enrollments.Commands;
using SMS.Application.Features.Enrollments.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class EnrollmentController : BaseApiController
    {
        private readonly ILogger<EnrollmentController> _logger;

        public EnrollmentController(ILogger<EnrollmentController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get all enrollments with pagination
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(PagedResult<EnrollmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEnrollments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? studentId = null,
            [FromQuery] Guid? unitId = null,
            [FromQuery] Guid? semesterId = null,
            [FromQuery] string? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetEnrollmentsQuery
            {
                Page = page,
                PageSize = pageSize,
                StudentId = studentId,
                UnitId = unitId,
                SemesterId = semesterId,
                Status = status
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get enrollment by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrollment(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetEnrollmentQuery { EnrollmentId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Enroll a student in a unit
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateEnrollment(
            [FromBody] CreateEnrollmentCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetEnrollment), new { id = result.Id }, result);
        }

        /// <summary>
        /// Bulk enroll multiple students in a unit
        /// </summary>
        [HttpPost("bulk")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(BulkEnrollmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkEnroll(
            [FromBody] BulkEnrollCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Drop a student from a unit
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DropEnrollment(Guid id, CancellationToken cancellationToken)
        {
            var command = new DropEnrollmentCommand { EnrollmentId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Get enrollments for a specific student
        /// </summary>
        [HttpGet("student/{studentId}")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(IEnumerable<EnrollmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentEnrollments(
            Guid studentId,
            [FromQuery] Guid? semesterId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetStudentEnrollmentsQuery
            {
                StudentId = studentId,
                SemesterId = semesterId
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Update enrollment status
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateEnrollmentStatus(
            Guid id,
            [FromBody] UpdateEnrollmentStatusCommand command,
            CancellationToken cancellationToken)
        {
            command.EnrollmentId = id;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
