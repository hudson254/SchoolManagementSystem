using SMS.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Grades.Commands;
using SMS.Application.Features.Grades.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class GradeController : BaseApiController
    {
        private readonly ILogger<GradeController> _logger;

        public GradeController(ILogger<GradeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get all grades with pagination
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(PagedResult<GradeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGrades(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? studentId = null,
            [FromQuery] Guid? unitId = null,
            [FromQuery] Guid? semesterId = null,
            [FromQuery] bool? isPublished = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetGradesQuery
            {
                Page = page,
                PageSize = pageSize,
                StudentId = studentId,
                UnitId = unitId,
                SemesterId = semesterId,
                IsPublished = isPublished
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get grade by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(GradeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGrade(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetGradeQuery { GradeId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Create a new grade
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(GradeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGrade(
            [FromBody] CreateGradeCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetGrade), new { id = result.Id }, result);
        }

        /// <summary>
        /// Update a grade
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(GradeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateGrade(
            Guid id,
            [FromBody] UpdateGradeCommand command,
            CancellationToken cancellationToken)
        {
            if (id != command.Id)
                return BadRequest("ID mismatch");

            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Delete a grade
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteGrade(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteGradeCommand { GradeId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Publish grades for a unit
        /// </summary>
        [HttpPost("publish")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishGrades(
            [FromBody] PublishGradesCommand command,
            CancellationToken cancellationToken)
        {
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Get grades for a specific student
        /// </summary>
        [HttpGet("student/{studentId}")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(IEnumerable<GradeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentGrades(
            Guid studentId,
            [FromQuery] Guid? semesterId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetStudentGradesQuery
            {
                StudentId = studentId,
                SemesterId = semesterId
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get grades for a specific unit
        /// </summary>
        [HttpGet("unit/{unitId}")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<GradeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnitGrades(
            Guid unitId,
            [FromQuery] Guid? semesterId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUnitGradesQuery
            {
                UnitId = unitId,
                SemesterId = semesterId
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get student transcript
        /// </summary>
        [HttpGet("transcript/{studentId}")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(TranscriptDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTranscript(Guid studentId, CancellationToken cancellationToken)
        {
            var query = new GetStudentTranscriptQuery { StudentId = studentId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Export grades to Excel
        /// </summary>
        [HttpGet("export")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportGrades(
            [FromQuery] Guid unitId,
            [FromQuery] Guid? semesterId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new ExportGradesQuery
            {
                UnitId = unitId,
                SemesterId = semesterId
            };
            var result = await Mediator.Send(query, cancellationToken);
            return File(result.FileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
        }
    }
}
