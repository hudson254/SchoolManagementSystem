using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Reports.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ReportController : BaseApiController
    {
        private readonly ILogger<ReportController> _logger;

        public ReportController(ILogger<ReportController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generate a student enrollment report
        /// </summary>
        [HttpGet("student-enrollment")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(StudentReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentEnrollmentReport(
            [FromQuery] Guid? semesterId = null,
            [FromQuery] Guid? programmeId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetStudentEnrollmentReportQuery
            {
                SemesterId = semesterId,
                ProgrammeId = programmeId
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Generate a lecturer workload report
        /// </summary>
        [HttpGet("lecturer-workload")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<LecturerWorkloadReportDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLecturerWorkloadReport(
            [FromQuery] Guid semesterId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetLecturerWorkloadReportQuery { SemesterId = semesterId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Generate a course statistics report
        /// </summary>
        [HttpGet("course-statistics")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<CourseStatisticsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourseStatisticsReport(
            [FromQuery] Guid? semesterId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetCourseStatisticsReportQuery { SemesterId = semesterId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Generate an assignment completion report
        /// </summary>
        [HttpGet("assignment-completion")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<AssignmentCompletionReportDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignmentCompletionReport(
            [FromQuery] Guid assignmentId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetAssignmentCompletionReportQuery { AssignmentId = assignmentId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Generate a grade distribution report
        /// </summary>
        [HttpGet("grade-distribution")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(GradeDistributionReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGradeDistributionReport(
            [FromQuery] Guid? semesterId = null,
            [FromQuery] Guid? unitId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetGradeDistributionReportQuery
            {
                SemesterId = semesterId,
                UnitId = unitId
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Generate an accommodation occupancy report
        /// </summary>
        [HttpGet("occupancy")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(OccupancyReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOccupancyReport(
            [FromQuery] Guid? buildingId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetOccupancyReportQuery { BuildingId = buildingId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Generate a user activity report
        /// </summary>
        [HttpGet("user-activity")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(UserActivityReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserActivityReport(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserActivityReportQuery
            {
                FromDate = fromDate,
                ToDate = toDate
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Generate a timetable utilization report
        /// </summary>
        [HttpGet("timetable-utilization")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(TimetableUtilizationReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTimetableUtilizationReport(
            [FromQuery] Guid semesterId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetTimetableUtilizationReportQuery { SemesterId = semesterId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Export report as PDF
        /// </summary>
        [HttpGet("export/pdf")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportReportAsPdf(
            [FromQuery] string reportType,
            [FromQuery] Guid? entityId = null,
            [FromQuery] Guid? semesterId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new ExportReportQuery
            {
                ReportType = reportType,
                EntityId = entityId,
                SemesterId = semesterId,
                Format = "PDF"
            };
            var result = await Mediator.Send(query, cancellationToken);
            return File(result.FileContent, "application/pdf", result.FileName);
        }

        /// <summary>
        /// Export report as Excel
        /// </summary>
        [HttpGet("export/excel")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportReportAsExcel(
            [FromQuery] string reportType,
            [FromQuery] Guid? entityId = null,
            [FromQuery] Guid? semesterId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new ExportReportQuery
            {
                ReportType = reportType,
                EntityId = entityId,
                SemesterId = semesterId,
                Format = "Excel"
            };
            var result = await Mediator.Send(query, cancellationToken);
            return File(result.FileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
        }

        /// <summary>
        /// Export report as CSV
        /// </summary>
        [HttpGet("export/csv")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportReportAsCsv(
            [FromQuery] string reportType,
            [FromQuery] Guid? entityId = null,
            [FromQuery] Guid? semesterId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new ExportReportQuery
            {
                ReportType = reportType,
                EntityId = entityId,
                SemesterId = semesterId,
                Format = "CSV"
            };
            var result = await Mediator.Send(query, cancellationToken);
            return File(result.FileContent, "text/csv", result.FileName);
        }
    }
}