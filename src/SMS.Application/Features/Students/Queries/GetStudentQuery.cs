using FluentValidation;
using SMS.Shared.DTOs;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Students.Queries
{
    public class GetStudentQuery : IRequest<StudentDetailsDto>
    {
        public Guid StudentId { get; set; }
    }

    public class GetStudentQueryHandler : IRequestHandler<GetStudentQuery, StudentDetailsDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ICourseOfferingEnrollmentRepository _courseOfferingEnrollmentRepository;
        private readonly ILogger<GetStudentQueryHandler> _logger;

        public GetStudentQueryHandler(
            IStudentRepository studentRepository,
            IAccommodationRepository accommodationRepository,
            ICourseOfferingEnrollmentRepository courseOfferingEnrollmentRepository,
            ILogger<GetStudentQueryHandler> logger)
        {
            _studentRepository = studentRepository;
            _accommodationRepository = accommodationRepository;
            _courseOfferingEnrollmentRepository = courseOfferingEnrollmentRepository;
            _logger = logger;
        }

        public async Task<StudentDetailsDto> Handle(GetStudentQuery request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetStudentWithDetailsAsync(request.StudentId, cancellationToken);

            if (student == null)
            {
                throw new NotFoundException("Student", request.StudentId);
            }

            // Current accommodation assignment (Lane → House model). Null-safe so
            // students without an assignment simply show "no accommodation".
            var accommodationAssignment = await _accommodationRepository
                .GetAssignmentByStudentAsync(request.StudentId, cancellationToken);

            // Course-offering level enrollments (course, offering, academic year,
            // semester, status) from the existing relationship.
            var courseEnrollments = await _courseOfferingEnrollmentRepository
                .GetActiveByStudentAsync(request.StudentId, cancellationToken);

            return new StudentDetailsDto
            {
                Id = student.Id,
                UserId = student.UserId,
                StudentNumber = student.StudentNumber,
                FirstName = student.User.FirstName,
                LastName = student.User.LastName,
                // FullName is computed from FirstName/LastName
                Email = student.User.Email ?? string.Empty,
                PhoneNumber = student.User.PhoneNumber ?? string.Empty,
                Organization = student.User.Organization,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                Address = student.Address,
                EnrollmentDate = student.EnrollmentDate,
                ProgrammeId = student.ProgrammeId,
                ProgrammeName = student.Programme?.Name,
                AcademicStatus = student.AcademicStatus,
                IsEnrolled = student.IsEnrolled,
                CumulativeGPA = student.CumulativeGPA,
                TotalCreditsEarned = student.TotalCreditsEarned,
                EmergencyContactName = student.EmergencyContactName,
                EmergencyContactPhone = student.EmergencyContactPhone,
                EmergencyContactRelation = student.EmergencyContactRelation,
                CurrentSemesterId = student.CurrentSemesterId ?? Guid.Empty,
                CurrentSemesterName = student.CurrentSemester?.Name,
                CurrentSemesterNumber = student.CurrentSemester?.SemesterNumber ?? 0,
                TotalEnrollments = student.Enrollments.Count,
                CompletedUnits = student.Enrollments.Count(e => e.Status == "Completed"),
                InProgressUnits = student.Enrollments.Count(e => e.Status == "InProgress"),
                Enrollments = student.Enrollments.Select(e => new EnrollmentSummaryDto
                {
                    Id = e.Id,
                    UnitId = e.UnitId,
                    UnitName = e.Unit.Name,
                    UnitCode = e.Unit.Code,
                    Credits = e.Unit.Credits,
                    Status = e.Status,
                    SemesterId = e.SemesterId,
                    SemesterName = e.Semester.Name,
                    EnrollmentDate = e.EnrollmentDate
                }).ToList(),
                Grades = student.Grades.Select(g => new GradeSummaryDto
                {
                    Id = g.Id,
                    UnitId = (Guid?)(g.Enrollment?.UnitId ?? g.UnitId) ?? Guid.Empty,
                    UnitName = g.Enrollment?.Unit?.Name ?? g.Unit?.Name ?? "",
                    UnitCode = g.Enrollment?.Unit?.Code ?? g.Unit?.Code ?? "",
                    Credits = g.Enrollment?.Unit?.Credits ?? g.Unit?.Credits ?? 0,
                    Grade = g.GradeValue,
                    Score = g.Score,
                    Remarks = g.Remarks,
                    SemesterId = g.Enrollment?.SemesterId ?? g.SemesterId,
                    SemesterName = g.Enrollment?.Semester?.Name ?? g.Semester?.Name ?? ""
                }).ToList(),
                Accommodation = accommodationAssignment == null ? null : ToStudentAccommodationDto(accommodationAssignment),
                CourseEnrollments = courseEnrollments
                    .Select(ToCourseEnrollmentDto)
                    .ToList(),
                CreatedDate = student.CreatedDate ?? DateTime.UtcNow
            };
        }

        private static StudentAccommodationDetailDto? ToStudentAccommodationDto(Domain.Entities.AccommodationAssignment a)
        {
            return new StudentAccommodationDetailDto
            {
                AssignmentId = a.Id,
                HouseId = a.HouseId != Guid.Empty ? a.HouseId : null,
                HouseNumber = a.House?.HouseNumber ?? string.Empty,
                HouseName = a.House?.HouseName,
                LaneId = a.House?.LaneId ?? (a.LaneId != Guid.Empty ? a.LaneId : null),
                LaneName = a.Lane?.LaneName ?? a.House?.Lane?.LaneName ?? string.Empty,
                RoomNumber = a.Room?.RoomNumber ?? string.Empty,
                Status = a.Status,
                AssignedDate = a.AssignedDate,
                VacatedDate = a.VacatedDate,
                MoveInDate = a.MoveInDate,
                MoveOutDate = a.MoveOutDate,
                CheckInDate = a.CheckInDate,
                CheckOutDate = a.CheckOutDate
            };
        }

        private static CourseOfferingEnrollmentDto ToCourseEnrollmentDto(Domain.Entities.CourseOfferingEnrollment e)
        {
            return new CourseOfferingEnrollmentDto
            {
                Id = e.Id,
                CourseOfferingId = e.CourseOfferingId,
                OfferingCode = e.CourseOffering?.OfferingCode,
                CourseId = e.CourseOffering?.CourseId,
                CourseName = e.CourseOffering?.Course?.Name,
                CourseCode = e.CourseOffering?.Course?.Code,
                AcademicYearName = e.CourseOffering?.AcademicYearName,
                SemesterName = e.CourseOffering?.SemesterName,
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
            };
        }
    }
}





