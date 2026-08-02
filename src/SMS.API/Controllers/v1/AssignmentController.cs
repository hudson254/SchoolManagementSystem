using SMS.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Assignments.Commands;
using SMS.Application.Features.Assignments.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AssignmentController : BaseApiController
    {
        private readonly ILogger<AssignmentController> _logger;

        public AssignmentController(ILogger<AssignmentController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get all assignments with pagination
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? unitId = null,
            [FromQuery] Guid? lecturerId = null,
            [FromQuery] Guid? semesterId = null,
            [FromQuery] string? status = null,
            [FromQuery] bool? isGraded = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetAssignmentsQuery
            {
                Page = page,
                PageSize = pageSize,
                UnitId = unitId,
                LecturerId = lecturerId,
                SemesterId = semesterId,
                Status = status,
                IsGraded = isGraded
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get assignment by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAssignment(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetAssignmentQuery { AssignmentId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Create a new assignment
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAssignment(
            [FromBody] CreateAssignmentCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetAssignment), new { id = result.Id }, result);
        }

        /// <summary>
        /// Update an assignment
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAssignment(
            Guid id,
            [FromBody] UpdateAssignmentCommand command,
            CancellationToken cancellationToken)
        {
            if (id != command.Id)
                return BadRequest("ID mismatch");

            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Delete an assignment
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAssignment(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteAssignmentCommand { AssignmentId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Get submissions for an assignment
        /// </summary>
        [HttpGet("{id}/submissions")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(IEnumerable<AssignmentSubmissionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubmissions(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetAssignmentSubmissionsQuery { AssignmentId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get a specific submission
        /// </summary>
        [HttpGet("submissions/{submissionId}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(AssignmentSubmissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSubmission(Guid submissionId, CancellationToken cancellationToken)
        {
            var query = new GetSubmissionQuery { SubmissionId = submissionId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Submit an assignment
        /// </summary>
        [HttpPost("submit")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(AssignmentSubmissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitAssignment(
            [FromBody] SubmitAssignmentCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Grade a submission
        /// </summary>
        [HttpPut("submissions/{submissionId}/grade")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(AssignmentSubmissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GradeSubmission(
            Guid submissionId,
            [FromBody] GradeAssignmentCommand command,
            CancellationToken cancellationToken)
        {
            command.SubmissionId = submissionId;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get assignments for a specific student
        /// </summary>
        [HttpGet("student/{studentId}")]
        [Authorize(Policy = "StudentAccess")]
        [ProducesResponseType(typeof(IEnumerable<AssignmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentAssignments(
            Guid studentId,
            [FromQuery] Guid? semesterId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetStudentAssignmentsQuery
            {
                StudentId = studentId,
                SemesterId = semesterId
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
