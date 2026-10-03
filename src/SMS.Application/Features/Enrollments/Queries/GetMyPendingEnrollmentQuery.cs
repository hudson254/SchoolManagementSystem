using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Enrollments.Queries
{
    /// <summary>
    /// Returns the current student's pending enrollment status and details.
    /// Used after registration to check if course selection has been completed.
    /// </summary>
    public class GetMyPendingEnrollmentQuery : IRequest<StudentEnrollmentStatusDto>
    {
    }

    public class StudentEnrollmentStatusDto
    {
        public Guid StudentId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RegistrationStatus { get; set; } = string.Empty;
        public bool HasSelectedCourse { get; set; }
        public Guid? SelectedCourseId { get; set; }
        public string? SelectedCourseName { get; set; }
        public int UnitsCount { get; set; }
        public bool NeedsCourseSelection { get; set; }
        public bool IsPendingApproval { get; set; }
        public bool IsApproved { get; set; }
        public string? Message { get; set; }
    }

    public class GetMyPendingEnrollmentQueryHandler
        : IRequestHandler<GetMyPendingEnrollmentQuery, StudentEnrollmentStatusDto>
    {
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<GetMyPendingEnrollmentQueryHandler> _logger;

        public GetMyPendingEnrollmentQueryHandler(
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IStudentRepository studentRepository,
            ILogger<GetMyPendingEnrollmentQueryHandler> logger)
        {
            _currentUserService = currentUserService;
            _studentRepository = studentRepository;
            _logger = logger;
        }

        public async Task<StudentEnrollmentStatusDto> Handle(
            GetMyPendingEnrollmentQuery request,
            CancellationToken cancellationToken)
        {
            var email = _currentUserService.Email;
            if (string.IsNullOrEmpty(email))
                throw new UnauthorizedAccessException("User not authenticated");

            var student = await _studentRepository.GetStudentByEmailAsync(email);
            if (student == null)
            {
                return new StudentEnrollmentStatusDto
                {
                    Message = "Student record not found. Please complete registration first."
                };
            }

            var status = student.RegistrationStatus;
            var needsCourseSelection = status == RegistrationStatus.PendingCourseSelection;
            var isPendingApproval = status == RegistrationStatus.PendingApproval;
            var isApproved = status == RegistrationStatus.Approved;

            // The course the student chose is persisted on the student record
            // (Student.SelectedCourseId) at registration and again when the
            // selection wizard is submitted. Reading it from
            // student.Enrollments — as this handler used to — returned nothing,
            // because registration creates no Enrollment rows, so
            // hasSelectedCourse was hard-wired to false.
            // The Enrollments fallback is retained for records that predate the
            // SelectedCourseId column and were never backfilled.
            var persistedCourseId = student.SelectedCourseId
                ?? student.Enrollments?.FirstOrDefault()?.CourseId;
            var courseName = student.SelectedCourse?.Name
                ?? student.Enrollments?.FirstOrDefault()?.Course?.Name;

            // UnitsCount must reflect the student's REAL, persisted unit enrollments.
            // It is derived from the authoritative enrollment relationship
            // (Student -> Enrollments, each row one course unit) rather than
            // hard-coded, and the query is already tenant scoped (the Student is
            // resolved through the tenant-filtered DbSet) and user scoped (the rows
            // hang off this student only). Soft-deleted enrollments never count, and
            // the count is identical before and after approval so the reported
            // number matches the selection the student actually submitted.
            var enrollments = student.Enrollments?.Where(e => !e.IsDeleted).ToList()
                              ?? new List<Enrollment>();
            var unitsCount = enrollments.Count;

            // Never report a selection that does not exist, and never report "no
            // selection" when one was persisted. needsCourseSelection stays
            // driven by RegistrationStatus so the two fields cannot disagree.
            var hasSelectedCourse = persistedCourseId.HasValue;

            string? message = status switch
            {
                RegistrationStatus.PendingCourseSelection => hasSelectedCourse
                    ? $"You selected {courseName ?? "a course"} at registration. Review your selection and submit it for approval."
                    : "Please select a course to complete your enrollment.",
                RegistrationStatus.PendingApproval => "Your enrollment has been submitted and is awaiting approval.",
                RegistrationStatus.Approved => "Your enrollment has been approved.",
                RegistrationStatus.Rejected => "Your enrollment was rejected. Please contact administration.",
                _ => null
            };

            return new StudentEnrollmentStatusDto
            {
                StudentId = student.Id,
                StudentNumber = student.StudentNumber,
                FullName = $"{student.FirstName} {student.LastName}".Trim(),
                Email = student.Email,
                RegistrationStatus = status.ToString(),
                HasSelectedCourse = hasSelectedCourse,
                SelectedCourseId = persistedCourseId,
                SelectedCourseName = courseName,
                UnitsCount = unitsCount,
                NeedsCourseSelection = needsCourseSelection && !hasSelectedCourse,
                IsPendingApproval = isPendingApproval,
                IsApproved = isApproved,
                Message = message
            };
        }
    }
}
