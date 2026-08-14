using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Approvals.Queries
{
    /// <summary>
    /// Returns all pending approvals (students and lecturers awaiting approval).
    /// </summary>
    public class GetPendingApprovalsQuery : IRequest<PendingApprovalsResultDto>
    {
        public string? UserType { get; set; } // null = all, "Student" or "Lecturer"
    }

    public class PendingApprovalItemDto
    {
        public Guid Id { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty; // StudentNumber or EmployeeNumber
        public DateTime SubmittedDate { get; set; }
    }

    public class PendingApprovalsResultDto
    {
        public List<PendingApprovalItemDto> Students { get; set; } = new();
        public List<PendingApprovalItemDto> Lecturers { get; set; } = new();
        public int TotalCount => Students.Count + Lecturers.Count;
    }

    public class GetPendingApprovalsQueryHandler
        : IRequestHandler<GetPendingApprovalsQuery, PendingApprovalsResultDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ILecturerRepository _lecturerRepository;

        public GetPendingApprovalsQueryHandler(
            IStudentRepository studentRepository,
            ILecturerRepository lecturerRepository)
        {
            _studentRepository = studentRepository;
            _lecturerRepository = lecturerRepository;
        }

        public async Task<PendingApprovalsResultDto> Handle(
            GetPendingApprovalsQuery request,
            CancellationToken cancellationToken)
        {
            var result = new PendingApprovalsResultDto();

            if (request.UserType == null || request.UserType == "Student")
            {
                var students = await _studentRepository.GetAllAsync(cancellationToken);
                var pendingStudents = students
                    .Where(s => s.RegistrationStatus == RegistrationStatus.PendingApproval)
                    .Select(s => new PendingApprovalItemDto
                    {
                        Id = s.Id,
                        UserType = "Student",
                        FullName = $"{s.FirstName} {s.LastName}".Trim(),
                        Email = s.Email,
                        Identifier = s.StudentNumber,
                        SubmittedDate = s.EnrollmentDate
                    })
                    .ToList();

                result.Students = pendingStudents;
            }

            if (request.UserType == null || request.UserType == "Lecturer")
            {
                var lecturers = await _lecturerRepository.GetAllAsync(cancellationToken);
                var pendingLecturers = lecturers
                    .Where(l => l.RegistrationStatus == RegistrationStatus.PendingApproval)
                    .Select(l => new PendingApprovalItemDto
                    {
                        Id = l.Id,
                        UserType = "Lecturer",
                        FullName = $"{l.FirstName} {l.LastName}".Trim(),
                        Email = l.Email,
                        Identifier = l.EmployeeNumber,
                        SubmittedDate = l.HireDate
                    })
                    .ToList();

                result.Lecturers = pendingLecturers;
            }

            return result;
        }
    }
}
