using SMS.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Timetables.Commands;
using SMS.Application.Features.Timetables.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TimetableController : BaseApiController
    {
        private readonly ILogger<TimetableController> _logger;

        public TimetableController(ILogger<TimetableController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get timetable entries with pagination
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(PagedResult<TimetableDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTimetables(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? classId = null,
            [FromQuery] Guid? semesterId = null,
            [FromQuery] string? dayOfWeek = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetTimetablesQuery
            {
                Page = page,
                PageSize = pageSize,
                ClassId = classId,
                SemesterId = semesterId,
                DayOfWeek = dayOfWeek
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get timetable entry by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(TimetableDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTimetable(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetTimetableQuery { TimetableId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Create a new timetable entry
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(TimetableDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTimetable(
            [FromBody] CreateTimetableCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetTimetable), new { id = result.Id }, result);
        }

        /// <summary>
        /// Update a timetable entry
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(TimetableDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTimetable(
            Guid id,
            [FromBody] UpdateTimetableCommand command,
            CancellationToken cancellationToken)
        {
            if (id != command.Id)
                return BadRequest("ID mismatch");

            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Delete a timetable entry
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTimetable(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteTimetableCommand { TimetableId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Get timetable for a specific class
        /// </summary>
        [HttpGet("class/{classId}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(IEnumerable<TimetableDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClassTimetable(
            Guid classId,
            [FromQuery] string? dayOfWeek = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetClassTimetableQuery
            {
                ClassId = classId,
                DayOfWeek = dayOfWeek
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get timetable for a specific lecturer
        /// </summary>
        [HttpGet("lecturer/{lecturerId}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(IEnumerable<TimetableDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLecturerTimetable(
            Guid lecturerId,
            [FromQuery] Guid semesterId,
            [FromQuery] string? dayOfWeek = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetLecturerTimetableQuery
            {
                LecturerId = lecturerId,
                SemesterId = semesterId,
                DayOfWeek = dayOfWeek
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get timetable for a specific student
        /// </summary>
        [HttpGet("student/{studentId}")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(IEnumerable<TimetableDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentTimetable(
            Guid studentId,
            [FromQuery] Guid semesterId,
            [FromQuery] string? dayOfWeek = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetStudentTimetableQuery
            {
                StudentId = studentId,
                SemesterId = semesterId,
                DayOfWeek = dayOfWeek
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Check for timetable conflicts
        /// </summary>
        [HttpPost("check-conflicts")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(ConflictCheckResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckConflicts(
            [FromBody] CheckTimetableConflictsQuery query,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Generate weekly timetable for a class
        /// </summary>
        [HttpGet("weekly/class/{classId}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(WeeklyTimetableDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWeeklyTimetable(
            Guid classId,
            [FromQuery] DateTime weekStartDate,
            CancellationToken cancellationToken = default)
        {
            var query = new GetWeeklyTimetableQuery
            {
                ClassId = classId,
                WeekStartDate = weekStartDate
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get available venues for a time slot
        /// </summary>
        [HttpGet("available-venues")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableVenues(
            [FromQuery] string dayOfWeek,
            [FromQuery] TimeSpan startTime,
            [FromQuery] TimeSpan endTime,
            [FromQuery] Guid semesterId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetAvailableVenuesQuery
            {
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                SemesterId = semesterId
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
