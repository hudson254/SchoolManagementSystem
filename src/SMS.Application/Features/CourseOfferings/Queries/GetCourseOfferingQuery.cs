using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Queries
{
    public class GetCourseOfferingQuery : IRequest<CourseOfferingDetailsDto>
    {
        public Guid Id { get; set; }
    }

    public class GetCourseOfferingQueryHandler
        : IRequestHandler<GetCourseOfferingQuery, CourseOfferingDetailsDto>
    {
        private readonly ICourseOfferingRepository _courseOfferingRepository;
        private readonly ICourseOfferingUnitRepository _courseOfferingUnitRepository;
        private readonly ICourseOfferingEnrollmentRepository _courseOfferingEnrollmentRepository;
        private readonly ICourseOfferingLecturerRepository _courseOfferingLecturerRepository;
        private readonly ILogger<GetCourseOfferingQueryHandler> _logger;

        public GetCourseOfferingQueryHandler(
            ICourseOfferingRepository courseOfferingRepository,
            ICourseOfferingUnitRepository courseOfferingUnitRepository,
            ICourseOfferingEnrollmentRepository courseOfferingEnrollmentRepository,
            ICourseOfferingLecturerRepository courseOfferingLecturerRepository,
            ILogger<GetCourseOfferingQueryHandler> logger)
        {
            _courseOfferingRepository = courseOfferingRepository;
            _courseOfferingUnitRepository = courseOfferingUnitRepository;
            _courseOfferingEnrollmentRepository = courseOfferingEnrollmentRepository;
            _courseOfferingLecturerRepository = courseOfferingLecturerRepository;
            _logger = logger;
        }

        public async Task<CourseOfferingDetailsDto> Handle(
            GetCourseOfferingQuery request,
            CancellationToken cancellationToken)
        {
            var offering = await _courseOfferingRepository.GetWithDetailsAsync(request.Id, cancellationToken);
            if (offering == null)
                throw new NotFoundException("CourseOffering", request.Id);

            var units = await _courseOfferingUnitRepository.GetByOfferingIdAsync(request.Id, cancellationToken);
            var enrollments = await _courseOfferingEnrollmentRepository.GetByOfferingIdAsync(request.Id, cancellationToken);
            var lecturers = await _courseOfferingLecturerRepository.GetByOfferingIdAsync(request.Id, cancellationToken);

            return new CourseOfferingDetailsDto
            {
                Id = offering.Id,
                OfferingCode = offering.OfferingCode,
                CourseId = offering.CourseId,
                CourseName = offering.Course?.Name,
                CourseCode = offering.Course?.Code,
                AcademicYearId = offering.AcademicYearId,
                AcademicYearName = offering.AcademicYear?.Name,
                SemesterId = offering.SemesterId,
                SemesterName = offering.Semester?.Name,
                Intake = offering.Intake,
                StartDate = offering.StartDate,
                EndDate = offering.EndDate,
                RegistrationStartDate = offering.RegistrationStartDate,
                RegistrationEndDate = offering.RegistrationEndDate,
                Status = offering.Status,
                IsActive = offering.IsActive,
                Notes = offering.Notes,
                TotalUnits = units?.Count() ?? 0,
                TotalEnrollments = enrollments?.Count() ?? 0,
                TotalLecturers = lecturers?.Count() ?? 0,
                CreatedDate = offering.CreatedDate ?? DateTime.UtcNow,
                Units = units?.Select(u => new CourseOfferingUnitDto
                {
                    Id = u.Id,
                    CourseOfferingId = u.CourseOfferingId,
                    UnitId = u.UnitId,
                    Name = u.Name,
                    Code = u.Code,
                    Description = u.Description,
                    Credits = u.Credits,
                    ContactHours = u.ContactHours,
                    Order = u.Order,
                    LearningOutcomes = u.LearningOutcomes,
                    AssessmentMethods = u.AssessmentMethods,
                    AssessmentWeighting = u.AssessmentWeighting,
                    IsActive = u.IsActive
                }).ToList() ?? new List<CourseOfferingUnitDto>(),
                Lecturers = lecturers?.Select(l => new CourseOfferingLecturerDto
                {
                    Id = l.Id,
                    CourseOfferingId = l.CourseOfferingId,
                    LecturerId = l.LecturerId,
                    LecturerName = l.Lecturer?.User != null
                        ? $"{l.Lecturer.User.FirstName} {l.Lecturer.User.LastName}"
                        : null,
                    IsPrimary = l.IsPrimary,
                    Role = l.Status,
                    AssignedDate = l.AssignmentDate,
                    IsActive = l.IsActive
                }).ToList() ?? new List<CourseOfferingLecturerDto>(),
                Enrollments = enrollments?.Select(e => new CourseOfferingEnrollmentDto
                {
                    Id = e.Id,
                    CourseOfferingId = e.CourseOfferingId,
                    OfferingCode = offering.OfferingCode,
                    StudentId = e.StudentId,
                    StudentName = e.Student?.User != null
                        ? $"{e.Student.User.FirstName} {e.Student.User.LastName}"
                        : null,
                    StudentNumber = e.Student?.StudentNumber,
                    EnrollmentDate = e.EnrollmentDate,
                    Status = e.Status,
                    IsActive = e.IsActive,
                    AttemptNumber = e.AttemptNumber,
                    ConfirmationStatus = e.ConfirmationStatus,
                    ConfirmedDate = e.ConfirmedDate,
                    DropDate = e.DropDate,
                    Notes = e.Notes
                }).ToList() ?? new List<CourseOfferingEnrollmentDto>()
            };
        }
    }
}
