using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Courses.Queries
{
    /// <summary>
    /// Returns the courses a logged-in student may choose from during the
    /// course-selection wizard: active, not soft-deleted courses of the current
    /// tenant. This is a separate, student-authorized read path - the
    /// administrator/curriculum surface at GET /api/v1/courses stays behind
    /// the ModeratorAccess policy and is deliberately not reused here.
    /// </summary>
    public class GetAvailableCoursesForStudentSelectionQuery : IRequest<IEnumerable<StudentSelectableCourseDto>>
    {
    }

    /// <summary>
    /// A course the student can select. Shape mirrors the fields the
    /// course-selection UI already consumes (id, name, code, description).
    /// </summary>
    public class StudentSelectableCourseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; }
        public int Duration { get; set; }
    }

    public class GetAvailableCoursesForStudentSelectionQueryHandler
        : IRequestHandler<GetAvailableCoursesForStudentSelectionQuery, IEnumerable<StudentSelectableCourseDto>>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ILogger<GetAvailableCoursesForStudentSelectionQueryHandler> _logger;

        public GetAvailableCoursesForStudentSelectionQueryHandler(
            ICourseRepository courseRepository,
            ILogger<GetAvailableCoursesForStudentSelectionQueryHandler> logger)
        {
            _courseRepository = courseRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<StudentSelectableCourseDto>> Handle(
            GetAvailableCoursesForStudentSelectionQuery request,
            CancellationToken cancellationToken)
        {
            // GetActiveCoursesAsync applies the tenant global query filter and
            // filters on IsActive && !IsDeleted.
            var courses = await _courseRepository.GetActiveCoursesAsync();

            var result = courses
                .Select(c => new StudentSelectableCourseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code,
                    Description = c.Description,
                    Credits = c.Credits,
                    Duration = c.Duration
                })
                .OrderBy(c => c.Name)
                .ToList();

            _logger.LogInformation(
                "Returned {CourseCount} selectable courses for student course selection",
                result.Count);

            return result;
        }
    }

    /// <summary>
    /// Returns the active units of a course that a student is choosing from, so
    /// the wizard's "Confirm Units" step can be served to a student without the
    /// moderator-only GET /api/v1/courses/{id}/units endpoint.
    /// </summary>
    public class GetAvailableCourseUnitsForStudentSelectionQuery : IRequest<IEnumerable<StudentSelectableUnitDto>>
    {
        public Guid CourseId { get; set; }
    }

    /// <summary>
    /// A unit of a selectable course. Shape mirrors the fields the
    /// course-selection UI already consumes (id, code, name, credits).
    /// </summary>
    public class StudentSelectableUnitDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Credits { get; set; }
    }

    public class GetAvailableCourseUnitsForStudentSelectionQueryHandler
        : IRequestHandler<GetAvailableCourseUnitsForStudentSelectionQuery, IEnumerable<StudentSelectableUnitDto>>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<GetAvailableCourseUnitsForStudentSelectionQueryHandler> _logger;

        public GetAvailableCourseUnitsForStudentSelectionQueryHandler(
            ICourseRepository courseRepository,
            IUnitRepository unitRepository,
            ILogger<GetAvailableCourseUnitsForStudentSelectionQueryHandler> logger)
        {
            _courseRepository = courseRepository;
            _unitRepository = unitRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<StudentSelectableUnitDto>> Handle(
            GetAvailableCourseUnitsForStudentSelectionQuery request,
            CancellationToken cancellationToken)
        {
            if (request.CourseId == Guid.Empty)
                throw new ValidationException("Course id is required");

            // A student may only browse units of a selectable (active, not
            // deleted) course, so an inactive or unknown course is a 404 rather
            // than a silent empty list.
            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null || course.IsDeleted || !course.IsActive)
                throw new NotFoundException("Course", request.CourseId);

            var units = await _unitRepository.GetUnitsByCourseIdAsync(course.Id, cancellationToken);

            var result = units
                .Where(u => u.IsActive && !u.IsDeleted)
                .Select(u => new StudentSelectableUnitDto
                {
                    Id = u.Id,
                    Code = u.Code,
                    Name = u.Name,
                    Credits = u.Credits
                })
                .OrderBy(u => u.Code)
                .ToList();

            _logger.LogInformation(
                "Returned {UnitCount} selectable units for course {CourseId} ({CourseCode})",
                result.Count, course.Id, course.Code);

            return result;
        }
    }
}
