using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Courses.Queries
{
    public class GetCourseUnitsQuery : IRequest<IEnumerable<UnitDto>>
    {
        public Guid CourseId { get; set; }
    }

    public class GetCourseUnitsHandler : IRequestHandler<GetCourseUnitsQuery, IEnumerable<UnitDto>>
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<GetCourseUnitsHandler> _logger;

        public GetCourseUnitsHandler(IUnitRepository unitRepository, ILogger<GetCourseUnitsHandler> logger)
        {
            _unitRepository = unitRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<UnitDto>> Handle(GetCourseUnitsQuery request, CancellationToken cancellationToken)
        {
            var units = await _unitRepository.GetAllAsync(cancellationToken);
            var filteredUnits = units.Where(u => u.CourseId == request.CourseId && !u.IsDeleted).ToList();
            return filteredUnits.Select(u => new UnitDto
            {
                Id = u.Id,
                Name = u.Name,
                Code = u.Code,
                Description = u.Description ?? string.Empty,
                Credits = u.Credits,
                CourseId = u.CourseId,
                IsActive = u.IsActive,
                Status = "Active"
            }).ToList();
        }
    }

    public class GetCourseProgrammesQuery : IRequest<IEnumerable<ProgrammeDto>>
    {
        public Guid CourseId { get; set; }
    }

    public class GetCourseProgrammesHandler : IRequestHandler<GetCourseProgrammesQuery, IEnumerable<ProgrammeDto>>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ILogger<GetCourseProgrammesHandler> _logger;

        public GetCourseProgrammesHandler(ICourseRepository courseRepository, ILogger<GetCourseProgrammesHandler> logger)
        {
            _courseRepository = courseRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<ProgrammeDto>> Handle(GetCourseProgrammesQuery request, CancellationToken cancellationToken)
        {
            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course?.Programme == null)
                return Enumerable.Empty<ProgrammeDto>();

            return new List<ProgrammeDto>
            {
                new ProgrammeDto
                {
                    Id = 0,
                    Name = course.Programme.Name ?? string.Empty,
                    Code = course.Programme.Code ?? string.Empty,
                    Description = course.Programme.Description ?? string.Empty,
                    Duration = course.Programme.Duration,
                    IsActive = course.Programme.IsActive
                }
            };
        }
    }
}
