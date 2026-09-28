using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
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

    /// <summary>
    /// Returns the active units of a course for the PUBLIC registration page, so
    /// the registration wizard's course/unit verification step can be served
    /// before the registrant has an account (and therefore no bearer token).
    ///
    /// This is deliberately separate from both
    /// <c>GET /api/v1/courses/{id}/units</c> (ModeratorAccess) and
    /// <c>GET /api/v1/enrollment/available-courses/{id}/units</c> (StudentAccess):
    /// neither is reachable by an anonymous registrant, which is why the review
    /// step could not show any units.
    ///
    /// The handler applies the SAME filter as the enrollment command
    /// (IsActive &amp;&amp; !IsDeleted through the tenant-scoped repository), so
    /// the units shown on the verification page are exactly the units that will
    /// be persisted. Tenant scoping comes from the repository's global query
    /// filter, so a course id from another tenant resolves to null and yields a
    /// 404 rather than leaking cross-tenant curriculum.
    /// </summary>
    public class GetCourseUnitsForRegistrationQuery : IRequest<IEnumerable<RegistrationUnitDto>>
    {
        public Guid CourseId { get; set; }
    }

    /// <summary>
    /// A unit shown on the registration verification page.
    /// </summary>
    public class RegistrationUnitDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Credits { get; set; }
        public int Semester { get; set; }
    }

    public class GetCourseUnitsForRegistrationQueryHandler
        : IRequestHandler<GetCourseUnitsForRegistrationQuery, IEnumerable<RegistrationUnitDto>>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<GetCourseUnitsForRegistrationQueryHandler> _logger;

        public GetCourseUnitsForRegistrationQueryHandler(
            ICourseRepository courseRepository,
            IUnitRepository unitRepository,
            ILogger<GetCourseUnitsForRegistrationQueryHandler> logger)
        {
            _courseRepository = courseRepository;
            _unitRepository = unitRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<RegistrationUnitDto>> Handle(
            GetCourseUnitsForRegistrationQuery request,
            CancellationToken cancellationToken)
        {
            if (request.CourseId == Guid.Empty)
                throw new ValidationException("Course id is required");

            // Tenant-scoped: a course belonging to another tenant resolves to null.
            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null || course.IsDeleted || !course.IsActive)
                throw new NotFoundException("Course", request.CourseId);

            var units = await _unitRepository.GetUnitsByCourseIdAsync(course.Id, cancellationToken);

            var result = units
                .Where(u => u.IsActive && !u.IsDeleted)
                .Select(u => new RegistrationUnitDto
                {
                    Id = u.Id,
                    Code = u.Code,
                    Name = u.Name,
                    Credits = u.Credits,
                    Semester = u.Semester
                })
                .OrderBy(u => u.Code)
                .ToList();

            _logger.LogInformation(
                "Returned {UnitCount} registration unit(s) for course {CourseId} ({CourseCode})",
                result.Count, course.Id, course.Code);

            return result;
        }
    }
}
