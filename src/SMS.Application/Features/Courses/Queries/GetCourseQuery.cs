using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Courses.Queries
{
    public class GetCourseQuery : IRequest<CourseDetailsDto>
    {
        public Guid CourseId { get; set; }
    }

    public class GetCourseQueryHandler : IRequestHandler<GetCourseQuery, CourseDetailsDto>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<GetCourseQueryHandler> _logger;

        public GetCourseQueryHandler(
            ICourseRepository courseRepository,
            IUnitRepository unitRepository,
            ILogger<GetCourseQueryHandler> logger)
        {
            _courseRepository = courseRepository;
            _unitRepository = unitRepository;
            _logger = logger;
        }

        public async Task<CourseDetailsDto> Handle(GetCourseQuery request, CancellationToken cancellationToken)
        {
            var course = await _courseRepository.GetCourseWithDetailsAsync(request.CourseId, cancellationToken);

            if (course == null)
            {
                throw new NotFoundException("Course", request.CourseId);
            }

            var units = await _unitRepository.GetUnitsByCourseIdAsync(request.CourseId, cancellationToken);

            return new CourseDetailsDto
            {
                Id = course.Id,
                Name = course.Name,
                Code = course.Code,
                Description = course.Description,
                Duration = course.Duration,
                TotalCredits = course.TotalCredits,
                IsActive = course.IsActive,
                DepartmentId = course.DepartmentId,
                DepartmentName = course.Department.Name,
                DepartmentCode = course.Department.Code,
                AdmissionRequirements = course.AdmissionRequirements,
                Objectives = course.Objectives,
                TotalUnits = units.Count(),
                TotalProgrammes = course.Programmes.Count,
                TotalStudents = course.Programmes.SelectMany(p => p.Students).Count(),
                Units = units.Select(u => new UnitSummaryDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Code = u.Code,
                    Credits = u.Credits
                }).ToList(),
                Programmes = course.Programmes.Select(p => new ProgrammeSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    Duration = p.Duration,
                    TotalCredits = 0
                }).ToList(),
                CreatedDate = course.CreatedDate ?? DateTime.UtcNow
            };
        }
    }
}






