using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Courses.Queries
{
    /// <summary>
    /// Returns only active courses suitable for the public registration page.
    /// Minimal DTO — no pagination, lightweight response for dropdowns.
    /// </summary>
    public class GetActiveCoursesForRegistrationQuery : IRequest<IEnumerable<CourseDto>>
    {
    }

    public class GetActiveCoursesForRegistrationQueryHandler
        : IRequestHandler<GetActiveCoursesForRegistrationQuery, IEnumerable<CourseDto>>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ILogger<GetActiveCoursesForRegistrationQueryHandler> _logger;

        public GetActiveCoursesForRegistrationQueryHandler(
            ICourseRepository courseRepository,
            ILogger<GetActiveCoursesForRegistrationQueryHandler> logger)
        {
            _courseRepository = courseRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<CourseDto>> Handle(
            GetActiveCoursesForRegistrationQuery request,
            CancellationToken cancellationToken)
        {
            var courses = await _courseRepository.GetActiveCoursesAsync();

            return courses.Select(c => new CourseDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Credits = c.Credits,
                Duration = c.Duration,
                Description = c.Description,
                DepartmentId = c.DepartmentId,
                ProgrammeId = c.ProgrammeId,
                IsActive = c.IsActive,
                TotalCredits = c.TotalCredits,
                CreatedDate = c.CreatedDate ?? DateTime.UtcNow
            }).ToList();
        }
    }
}

