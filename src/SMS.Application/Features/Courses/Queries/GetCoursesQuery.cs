using MediatR;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Courses.Queries
{
    public class GetCoursesQuery : IRequest<PagedResult<CourseDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public Guid? DepartmentId { get; set; }
        public bool? IsActive { get; set; }
        public string SortBy { get; set; } = "CreatedDate";
        public bool SortDescending { get; set; } = false;
    }

    public class GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, PagedResult<CourseDto>>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ILogger<GetCoursesQueryHandler> _logger;

        public GetCoursesQueryHandler(
            ICourseRepository courseRepository,
            ILogger<GetCoursesQueryHandler> logger)
        {
            _courseRepository = courseRepository;
            _logger = logger;
        }

        public async Task<PagedResult<CourseDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
        {
            var courses = await _courseRepository.GetCoursesAsync(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                request.DepartmentId,
                request.IsActive,
                request.SortBy,
                request.SortDescending,
                cancellationToken);

            var totalCount = await _courseRepository.CountCoursesAsync(
                request.SearchTerm,
                request.DepartmentId,
                request.IsActive,
                cancellationToken);

            var dtos = courses.Select(c => new CourseDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Description = c.Description,
                Duration = c.Duration,
                TotalCredits = c.TotalCredits,
                IsActive = c.IsActive,
                DepartmentId = c.DepartmentId,
                DepartmentName = c.Department.Name,
                DepartmentCode = c.Department.Code,
                CreatedDate = c.CreatedDate
            }).ToList();

            return new PagedResult<CourseDto>
            {
                Items = dtos,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
    }
}