using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Dashboard.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class DashboardController : BaseApiController
    {
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ILogger<DashboardController> logger)
        {
            _logger = logger;
        }

        [HttpGet("statistics")]
        [ProducesResponseType(typeof(DashboardStatisticsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
        {
            var query = new GetDashboardStatisticsQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("activities")]
        [ProducesResponseType(typeof(IEnumerable<ActivityDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecentActivities(
            [FromQuery] int count = 10,
            CancellationToken cancellationToken = default)
        {
            var query = new GetRecentActivitiesQuery { Count = count };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("enrollment-trends")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(EnrollmentTrendsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEnrollmentTrends(
            [FromQuery] int? academicYearId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetEnrollmentTrendsQuery { AcademicYearId = academicYearId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("upcoming-events")]
        [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingEvents(
            [FromQuery] int days = 30,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUpcomingEventsQuery { Days = days };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("performance-metrics")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(PerformanceMetricsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPerformanceMetrics(CancellationToken cancellationToken)
        {
            var query = new GetPerformanceMetricsQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("top-students")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<TopStudentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTopStudents(
            [FromQuery] int count = 10,
            [FromQuery] Guid? semesterId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetTopStudentsQuery { Count = count, SemesterId = semesterId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("course-statistics")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<CourseStatisticsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourseStatistics(
            [FromQuery] Guid? semesterId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetCourseStatisticsQuery { SemesterId = semesterId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}