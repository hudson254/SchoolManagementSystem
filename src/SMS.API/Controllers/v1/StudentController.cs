using SMS.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Students.Commands;
using SMS.Application.Features.Students.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/students")]
    public class StudentController : BaseApiController
    {
        private readonly ILogger<StudentController> _logger;

        public StudentController(ILogger<StudentController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(PagedResult<StudentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudents(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetStudentsQuery
            {
                SearchTerm = searchTerm ?? string.Empty,
                Page = page,
                PageSize = pageSize
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(StudentDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStudent(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetStudentQuery { StudentId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateStudent(
            [FromBody] CreateStudentCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetStudent), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStudent(
            Guid id,
            [FromBody] UpdateStudentCommand command,
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
        public async Task<IActionResult> DeleteStudent(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteStudentCommand { StudentId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpGet("{id}/enrollments")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(IEnumerable<EnrollmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentEnrollments(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetStudentEnrollmentsQuery { StudentId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}/grades")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(IEnumerable<GradeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentGrades(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetStudentGradesQuery { StudentId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}/transcript")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(TranscriptDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentTranscript(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetStudentTranscriptQuery { StudentId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
