using SMS.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.StudyMaterials.Commands;
using SMS.Application.Features.StudyMaterials.Queries;

namespace SMS.API.Controllers.v1
{
    /// <summary>
    /// Study materials (lecture notes) per unit. Lecturers upload and manage
    /// materials for units they teach; students read and download materials for
    /// units they are enrolled in. Authorization is enforced server-side in the
    /// handlers (role + persisted enrollment/teaching relationships), not by
    /// hiding frontend controls.
    /// </summary>
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/study-materials")]
    public class StudyMaterialController : BaseApiController
    {
        private readonly ILogger<StudyMaterialController> _logger;

        public StudyMaterialController(ILogger<StudyMaterialController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// List published study materials for a unit. Lecturers must teach the
        /// unit; students must be enrolled in it; admin/coordinator may view all.
        /// </summary>
        [HttpGet("unit/{unitId}")]
        [ProducesResponseType(typeof(IEnumerable<StudyMaterialDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUnitMaterials(Guid unitId, CancellationToken cancellationToken)
        {
            var query = new GetUnitStudyMaterialsQuery { UnitId = unitId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Upload a study material document for a unit (multipart/form-data).
        /// Lecturers may only upload to units they teach; admin/coordinator may
        /// upload on behalf of a lecturer who teaches the unit.
        /// </summary>
        [HttpPost("unit/{unitId}")]
        [Authorize(Policy = "LecturerAccess")]
        [RequestSizeLimit(52_428_800)]
        [ProducesResponseType(typeof(StudyMaterialDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadMaterial(
            Guid unitId,
            [FromForm] IFormFile? file,
            [FromForm] string? title,
            [FromForm] string? description,
            [FromForm] Guid? specifiedLecturerId,
            CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "A material file is required." });
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest(new { message = "A title is required for the study material." });
            }

            var command = new CreateStudyMaterialCommand
            {
                UnitId = unitId,
                Title = title,
                Description = description,
                FileStream = file.OpenReadStream(),
                OriginalFileName = file.FileName,
                SpecifiedLecturerId = specifiedLecturerId
            };

            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetUnitMaterials),
                new { unitId, version = "1" },
                result);
        }

        /// <summary>
        /// Download/open a study material through the authenticated endpoint.
        /// The same access rules as listing apply.
        /// </summary>
        [HttpGet("{id}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadMaterial(Guid id, CancellationToken cancellationToken)
        {
            var query = new DownloadStudyMaterialQuery { MaterialId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return File(result.Stream, result.ContentType, result.FileName);
        }

        /// <summary>
        /// Delete (soft-delete) a study material. Only the uploading lecturer, a
        /// lecturer teaching the unit, or an admin/coordinator may delete.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMaterial(
            Guid id, [FromQuery] Guid unitId, CancellationToken cancellationToken)
        {
            var command = new DeleteStudyMaterialCommand { MaterialId = id, UnitId = unitId };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
