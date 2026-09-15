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
    [Route("api/v{version:apiVersion}/assignments")]
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

        /// <summary>
        /// Upload an assignment question document (question sheet). Lecturer-only;
        /// server-side ownership/teaching authorization is enforced in the handler.
        /// </summary>
        [HttpPost("{id}/documents")]
        [Authorize(Policy = "LecturerAccess")]
        [RequestSizeLimit(52_428_800)]
        [ProducesResponseType(typeof(AssignmentDocumentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadDocument(
            Guid id,
            [FromForm] IFormFile? file,
            [FromForm] string? description,
            CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "A document file is required." });
            }

            var command = new UploadAssignmentDocumentCommand
            {
                AssignmentId = id,
                FileStream = file.OpenReadStream(),
                OriginalFileName = file.FileName,
                Description = description
            };

            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// List the question documents attached to an assignment. Access is
        /// enforced server-side (owner/unit lecturer, admin/coordinator, or
        /// student enrolled in the assignment's unit).
        /// </summary>
        [HttpGet("{id}/documents")]
        [ProducesResponseType(typeof(IEnumerable<AssignmentDocumentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDocuments(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetAssignmentDocumentsQuery { AssignmentId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Download an assignment question document through the authenticated
        /// endpoint (never a raw storage path).
        /// </summary>
        [HttpGet("{id}/documents/{fileId}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadDocument(
            Guid id, Guid fileId, CancellationToken cancellationToken)
        {
            var query = new DownloadAssignmentDocumentQuery { AssignmentId = id, FileId = fileId };
            var result = await Mediator.Send(query, cancellationToken);
            return File(result.Stream, result.ContentType, result.FileName);
        }

        /// <summary>
        /// Delete (soft-delete) an assignment question document.
        /// Lecturer-only; ownership authorization is enforced in the handler.
        /// </summary>
        [HttpDelete("{id}/documents/{fileId}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDocument(
            Guid id, Guid fileId, CancellationToken cancellationToken)
        {
            var command = new DeleteAssignmentDocumentCommand
            {
                AssignmentId = id,
                FileId = fileId
            };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}

