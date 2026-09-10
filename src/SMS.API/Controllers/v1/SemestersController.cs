using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Semesters.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class SemestersController : BaseApiController
    {
        private readonly ILogger<SemestersController> _logger;

        public SemestersController(ILogger<SemestersController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// List semesters used by academic forms (class creation, timetable
        /// filters). Readable by coordinator and above (ModeratorAccess).
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<SemesterDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSemesters(
            [FromQuery] bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = new GetSemestersQuery { IncludeInactive = includeInactive };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}