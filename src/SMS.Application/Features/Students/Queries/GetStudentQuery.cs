using MediatR;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Students.Queries
{
    public class GetStudentQuery : IRequest<StudentDetailsDto>
    {
        public Guid StudentId { get; set; }
    }

    public class GetStudentQueryHandler : IRequestHandler<GetStudentQuery, StudentDetailsDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<GetStudentQueryHandler> _logger;

        public GetStudentQueryHandler(
            IStudentRepository studentRepository,
            ILogger<GetStudentQueryHandler> logger)
        {
            _studentRepository = studentRepository;
            _logger = logger;
        }

        public async Task<StudentDetailsDto> Handle(GetStudentQuery request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetStudentWithDetailsAsync(request.StudentId, cancellationToken);

            if (student == null)
            {
                throw new NotFoundException("Student", request.StudentId);
            }

            return new StudentDetailsDto
            {
                Id = student.Id,
                UserId = student.UserId,
                StudentNumber = student.StudentNumber,
                FirstName = student.User.FirstName,
                LastName = student.User.LastName,
                FullName = student.User.FullName,
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
                CurrentSemesterId = student.CurrentSemesterId,
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
                    UnitId = g.Enrollment.UnitId,
                    UnitName = g.Enrollment.Unit.Name,
                    UnitCode = g.Enrollment.Unit.Code,
                    Credits = g.Enrollment.Unit.Credits,
                    Grade = g.GradeValue,
                    Score = g.Score,
                    Remarks = g.Remarks,
                    SemesterId = g.Enrollment.SemesterId,
                    SemesterName = g.Enrollment.Semester.Name
                }).ToList(),
                CreatedDate = student.CreatedDate
            };
        }
    }
}