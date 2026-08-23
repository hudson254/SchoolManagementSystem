using SMS.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Students.Commands;
using SMS.Application.Features.Students.Queries;
using SMS.Domain.Interfaces;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/students")]
    public class StudentController : BaseApiController
    {
        private readonly ILogger<StudentController> _logger;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IStudentRepository _studentRepository;

        // Staff roles that are allowed to view/update any student's record.
        private static readonly string[] StaffRoles = { "Administrator", "Coordinator", "Lecturer", "Receptionist" };

        public StudentController(
            ILogger<StudentController> logger,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IStudentRepository studentRepository)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _studentRepository = studentRepository;
        }

        /// <summary>
        /// RISK-09: Enforces that a caller with the "Student" role can only
        /// access their OWN student record. Staff roles (Administrator,
        /// Moderator, Lecturer, Receptionist) retain full access to any
        /// student's data. Returns 403 Forbidden when a student tries to
        /// access another student's record (IDOR).
        /// </summary>
        private async Task<bool> IsOwnerOrStaffAsync(Guid studentId, CancellationToken cancellationToken)
        {
            // Staff roles bypass the ownership check.
            if (_currentUserService.Roles.Any(r => StaffRoles.Contains(r)))
                return true;

            // Only a Student role reaches here. Verify the requested student
            // record belongs to the authenticated user.
            var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
                return false;

            var currentUserId = _currentUserService.UserId;
            return !string.IsNullOrEmpty(currentUserId)
                && string.Equals(student.UserId, currentUserId, StringComparison.OrdinalIgnoreCase);
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
            // RISK-09: students may only view their own record.
            if (!await IsOwnerOrStaffAsync(id, cancellationToken))
                return Forbid();

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

            // RISK-09: students may only update their own record.
            if (!await IsOwnerOrStaffAsync(id, cancellationToken))
                return Forbid();

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
            // RISK-09: students may only view their own enrollments.
            if (!await IsOwnerOrStaffAsync(id, cancellationToken))
                return Forbid();

            var query = new GetStudentEnrollmentsQuery { StudentId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}/grades")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(IEnumerable<GradeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentGrades(Guid id, CancellationToken cancellationToken)
        {
            // RISK-09: students may only view their own grades.
            if (!await IsOwnerOrStaffAsync(id, cancellationToken))
                return Forbid();

            var query = new GetStudentGradesQuery { StudentId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}/transcript")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(TranscriptDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentTranscript(Guid id, CancellationToken cancellationToken)
        {
            // RISK-09: students may only view their own transcript.
            if (!await IsOwnerOrStaffAsync(id, cancellationToken))
                return Forbid();

            var query = new GetStudentTranscriptQuery { StudentId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
