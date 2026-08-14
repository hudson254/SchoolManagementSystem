using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.ReturningUser.Queries
{
    /// <summary>
    /// Returns the current student's course enrollment history.
    /// </summary>
    public class GetStudentCourseHistoryQuery : IRequest<CourseHistoryDto>
    {
    }

    public class CourseHistoryItemDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
        public DateTime EnrolledDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CourseHistoryDto
    {
        public Guid StudentId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Message { get; set; }
        public List<CourseHistoryItemDto> Enrollments { get; set; } = new();
        public int TotalCount => Enrollments.Count;
    }

    public class GetStudentCourseHistoryQueryHandler
        : IRequestHandler<GetStudentCourseHistoryQuery, CourseHistoryDto>
    {
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IStudentRepository _studentRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ILogger<GetStudentCourseHistoryQueryHandler> _logger;

        public GetStudentCourseHistoryQueryHandler(
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IStudentRepository studentRepository,
            IEnrollmentRepository enrollmentRepository,
            ILogger<GetStudentCourseHistoryQueryHandler> logger)
        {
            _currentUserService = currentUserService;
            _studentRepository = studentRepository;
            _enrollmentRepository = enrollmentRepository;
            _logger = logger;
        }

        public async Task<CourseHistoryDto> Handle(
            GetStudentCourseHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var email = _currentUserService.Email;
            if (string.IsNullOrEmpty(email))
                throw new UnauthorizedAccessException("User not authenticated");

            var student = await _studentRepository.GetStudentByEmailAsync(email);
            if (student == null)
            {
                return new CourseHistoryDto
                {
                    Message = "Student record not found."
                };
            }

            var enrollments = await _enrollmentRepository.GetStudentEnrollmentsAsync(student.Id, cancellationToken);

            var history = enrollments
                .GroupBy(e => new { e.CourseId, e.Course?.Name, e.Course?.Code })
                .Select(g => new CourseHistoryItemDto
                {
                    CourseId = g.Key.CourseId,
                    CourseName = g.Key.Name ?? "Unknown",
                    CourseCode = g.Key.Code ?? "N/A",
                    SemesterName = g.First().Semester?.Name ?? "N/A",
                    EnrolledDate = g.Min(e => e.EnrollmentDate),
                    Status = g.Any(e => e.IsActive) ? "Active" : "Completed"
                })
                .OrderByDescending(h => h.EnrolledDate)
                .ToList();

            return new CourseHistoryDto
            {
                StudentId = student.Id,
                StudentNumber = student.StudentNumber,
                FullName = $"{student.FirstName} {student.LastName}".Trim(),
                Enrollments = history
            };
        }
    }
}
