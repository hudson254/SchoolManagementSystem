using System;
using System.Linq;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Dashboard.Queries
{
    /// <summary>
    /// Returns the dashboard payload for the currently authenticated lecturer:
    /// real course offerings they teach (with units), unit allocations, and the
    /// accommodation assignment from the accommodation module. All relationships
    /// are resolved server-side from persisted data — nothing is hardcoded or
    /// taken from client state.
    /// </summary>
    public class GetMyLecturerDashboardQuery : IRequest<MyLecturerDashboardDto>
    {
    }

    public class GetMyLecturerDashboardQueryHandler
        : IRequestHandler<GetMyLecturerDashboardQuery, MyLecturerDashboardDto>
    {
        private readonly IAcademicAccessService _academicAccessService;
        private readonly ICourseOfferingLecturerRepository _courseOfferingLecturers;
        private readonly ICourseOfferingRepository _courseOfferings;
        private readonly ICourseOfferingUnitRepository _courseOfferingUnits;
        private readonly IUnitAllocationRepository _unitAllocations;
        private readonly IUnitRepository _units;
        private readonly IAccommodationRepository _accommodations;
        private readonly ILogger<GetMyLecturerDashboardQueryHandler> _logger;

        public GetMyLecturerDashboardQueryHandler(
            IAcademicAccessService academicAccessService,
            ICourseOfferingLecturerRepository courseOfferingLecturers,
            ICourseOfferingRepository courseOfferings,
            ICourseOfferingUnitRepository courseOfferingUnits,
            IUnitAllocationRepository unitAllocations,
            IUnitRepository units,
            IAccommodationRepository accommodations,
            ILogger<GetMyLecturerDashboardQueryHandler> logger)
        {
            _academicAccessService = academicAccessService;
            _courseOfferingLecturers = courseOfferingLecturers;
            _courseOfferings = courseOfferings;
            _courseOfferingUnits = courseOfferingUnits;
            _unitAllocations = unitAllocations;
            _units = units;
            _accommodations = accommodations;
            _logger = logger;
        }

        public async Task<MyLecturerDashboardDto> Handle(
            GetMyLecturerDashboardQuery request, CancellationToken cancellationToken)
        {
            var lecturer = await _academicAccessService.GetCurrentLecturerAsync(cancellationToken);
            if (lecturer == null)
            {
                throw new NotFoundException("Lecturer (current user)");
            }

            // 1. Course offerings this lecturer teaches (persisted teaching assignments).
            var teachingLinks = (await _courseOfferingLecturers.GetActiveByLecturerAsync(
                    lecturer.Id, cancellationToken))
                .Where(l => l.IsActive && !l.IsDeleted)
                .ToList();

            var courses = new List<LecturerCourseDto>();
            foreach (var link in teachingLinks)
            {
                var offering = await _courseOfferings.GetWithDetailsAsync(link.CourseOfferingId, cancellationToken);
                if (offering == null || offering.IsDeleted)
                {
                    continue;
                }

                var offeringUnits = await _courseOfferingUnits.GetOrderedUnitsAsync(offering.Id, cancellationToken);
                var unitDtos = offeringUnits
                    .Where(u => u.IsActive && !u.IsDeleted)
                    .Select(u => new DashboardUnitDto
                    {
                        UnitId = u.UnitId ?? Guid.Empty,
                        CourseOfferingUnitId = u.Id,
                        Name = u.Name,
                        Code = u.Code,
                        Credits = u.Credits
                    })
                    .ToList();

                courses.Add(new LecturerCourseDto
                {
                    CourseOfferingId = offering.Id,
                    OfferingCode = offering.OfferingCode,
                    CourseId = offering.CourseId,
                    CourseName = offering.Course?.Name ?? string.Empty,
                    CourseCode = offering.Course?.Code ?? string.Empty,
                    AcademicYearName = offering.AcademicYearName,
                    SemesterName = offering.SemesterName,
                    Intake = offering.Intake,
                    Status = offering.Status.ToString(),
                    IsPrimary = link.IsPrimary,
                    Units = unitDtos
                });
            }

            // 2. Direct unit allocations (supplementary teaching assignments).
            var allocations = (await _unitAllocations.GetByLecturerAsync(lecturer.Id))
                .Where(a => a.Status == "Active" && !a.IsDeleted)
                .ToList();

            var allocationUnitIds = allocations.Select(a => a.UnitId).Distinct().ToList();
            var allocationUnits = allocationUnitIds.Count > 0
                ? (await _units.GetAllAsync(cancellationToken))
                    .Where(u => allocationUnitIds.Contains(u.Id) && !u.IsDeleted)
                    .ToDictionary(u => u.Id, u => u)
                : new System.Collections.Generic.Dictionary<Guid, SMS.Domain.Entities.Unit>();

            var unitAllocations = allocations
                .Select(a =>
                {
                    allocationUnits.TryGetValue(a.UnitId, out var unit);
                    return new DashboardUnitDto
                    {
                        UnitId = a.UnitId,
                        Name = unit?.Name ?? string.Empty,
                        Code = unit?.Code ?? string.Empty,
                        Credits = unit?.Credits ?? 0
                    };
                })
                .ToList();

            // 3. Accommodation assignment (existing accommodation module).
            var accommodationAssignment = await _accommodations.GetAssignmentByLecturerAsync(
                lecturer.Id, cancellationToken);

            _logger.LogInformation(
                "Lecturer dashboard loaded for {LecturerId}: {CourseCount} courses, {UnitCount} unit allocations",
                lecturer.Id, courses.Count, unitAllocations.Count);

            return new MyLecturerDashboardDto
            {
                LecturerId = lecturer.Id,
                FullName = lecturer.User != null
                    ? $"{lecturer.User.FirstName} {lecturer.User.LastName}".Trim()
                    : $"{lecturer.FirstName} {lecturer.LastName}".Trim(),
                Title = lecturer.Title,
                Email = lecturer.Email,
                EmployeeNumber = lecturer.EmployeeNumber,
                Courses = courses,
                UnitAllocations = unitAllocations,
                Accommodation = accommodationAssignment != null
                    ? AccommodationDtoMappings.ToAssignmentDto(accommodationAssignment)
                    : null
            };
        }
    }

    /// <summary>
    /// Returns the dashboard payload for the currently authenticated student:
    /// real active course-offering enrollments (with offering units), and the
    /// accommodation assignment from the accommodation module.
    /// </summary>
    public class GetMyStudentDashboardQuery : IRequest<MyStudentDashboardDto>
    {
    }

    public class GetMyStudentDashboardQueryHandler
        : IRequestHandler<GetMyStudentDashboardQuery, MyStudentDashboardDto>
    {
        private readonly IAcademicAccessService _academicAccessService;
        private readonly ICourseOfferingEnrollmentRepository _courseOfferingEnrollments;
        private readonly ICourseOfferingRepository _courseOfferings;
        private readonly ICourseOfferingUnitRepository _courseOfferingUnits;
        private readonly IAccommodationRepository _accommodations;
        private readonly ILogger<GetMyStudentDashboardQueryHandler> _logger;

        public GetMyStudentDashboardQueryHandler(
            IAcademicAccessService academicAccessService,
            ICourseOfferingEnrollmentRepository courseOfferingEnrollments,
            ICourseOfferingRepository courseOfferings,
            ICourseOfferingUnitRepository courseOfferingUnits,
            IAccommodationRepository accommodations,
            ILogger<GetMyStudentDashboardQueryHandler> logger)
        {
            _academicAccessService = academicAccessService;
            _courseOfferingEnrollments = courseOfferingEnrollments;
            _courseOfferings = courseOfferings;
            _courseOfferingUnits = courseOfferingUnits;
            _accommodations = accommodations;
            _logger = logger;
        }

        public async Task<MyStudentDashboardDto> Handle(
            GetMyStudentDashboardQuery request, CancellationToken cancellationToken)
        {
            var student = await _academicAccessService.GetCurrentStudentAsync(cancellationToken);
            if (student == null)
            {
                throw new NotFoundException("Student (current user)");
            }

            // Active course-offering enrollments are the source of truth for the
            // student's course. Multiple active enrollments are supported.
            var enrollments = (await _courseOfferingEnrollments.GetActiveByStudentAsync(
                    student.Id, cancellationToken))
                .Where(e => e.IsActive && !e.IsDeleted)
                .ToList();

            var enrolledCourses = new List<StudentCourseDto>();
            foreach (var enrollment in enrollments)
            {
                var offering = await _courseOfferings.GetWithDetailsAsync(
                    enrollment.CourseOfferingId, cancellationToken);
                if (offering == null || offering.IsDeleted)
                {
                    continue;
                }

                var offeringUnits = await _courseOfferingUnits.GetOrderedUnitsAsync(offering.Id, cancellationToken);
                var unitDtos = offeringUnits
                    .Where(u => u.IsActive && !u.IsDeleted)
                    .Select(u => new DashboardUnitDto
                    {
                        UnitId = u.UnitId ?? Guid.Empty,
                        CourseOfferingUnitId = u.Id,
                        Name = u.Name,
                        Code = u.Code,
                        Credits = u.Credits
                    })
                    .ToList();

                enrolledCourses.Add(new StudentCourseDto
                {
                    CourseOfferingId = offering.Id,
                    OfferingCode = offering.OfferingCode,
                    CourseId = offering.CourseId,
                    CourseName = offering.Course?.Name ?? string.Empty,
                    CourseCode = offering.Course?.Code ?? string.Empty,
                    AcademicYearName = offering.AcademicYearName,
                    SemesterName = offering.SemesterName,
                    Status = enrollment.Status,
                    ConfirmationStatus = enrollment.ConfirmationStatus.ToString(),
                    AttemptNumber = enrollment.AttemptNumber,
                    Units = unitDtos
                });
            }

            var accommodationAssignment = await _accommodations.GetAssignmentByStudentAsync(
                student.Id, cancellationToken);

            _logger.LogInformation(
                "Student dashboard loaded for {StudentId}: {CourseCount} enrolled courses",
                student.Id, enrolledCourses.Count);

            return new MyStudentDashboardDto
            {
                StudentId = student.Id,
                FullName = student.User != null
                    ? $"{student.User.FirstName} {student.User.LastName}".Trim()
                    : $"{student.FirstName} {student.LastName}".Trim(),
                Title = student.Title,
                Email = student.Email,
                StudentNumber = student.StudentNumber,
                AcademicStatus = student.AcademicStatus,
                Enrollments = enrolledCourses,
                Accommodation = accommodationAssignment != null
                    ? AccommodationDtoMappings.ToAssignmentDto(accommodationAssignment)
                    : null
            };
        }
    }
}

