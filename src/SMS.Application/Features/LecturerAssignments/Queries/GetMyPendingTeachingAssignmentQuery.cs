using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.LecturerAssignments.Queries
{
    /// <summary>
    /// Returns the current lecturer's pending teaching assignment status and details.
    /// Used after registration to check if course/unit selection has been completed.
    /// </summary>
    public class GetMyPendingTeachingAssignmentQuery : IRequest<LecturerAssignmentStatusDto>
    {
    }

    public class LecturerAssignmentStatusDto
    {
        public Guid LecturerId { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RegistrationStatus { get; set; } = string.Empty;
        public bool HasSelectedCourses { get; set; }
        public int CoursesCount { get; set; }
        public int UnitsCount { get; set; }
        public bool NeedsCourseSelection { get; set; }
        public bool IsPendingApproval { get; set; }
        public bool IsApproved { get; set; }
        public string? Message { get; set; }
    }

    public class GetMyPendingTeachingAssignmentQueryHandler
        : IRequestHandler<GetMyPendingTeachingAssignmentQuery, LecturerAssignmentStatusDto>
    {
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ILogger<GetMyPendingTeachingAssignmentQueryHandler> _logger;

        public GetMyPendingTeachingAssignmentQueryHandler(
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILecturerRepository lecturerRepository,
            ILogger<GetMyPendingTeachingAssignmentQueryHandler> logger)
        {
            _currentUserService = currentUserService;
            _lecturerRepository = lecturerRepository;
            _logger = logger;
        }

        public async Task<LecturerAssignmentStatusDto> Handle(
            GetMyPendingTeachingAssignmentQuery request,
            CancellationToken cancellationToken)
        {
            var email = _currentUserService.Email;
            if (string.IsNullOrEmpty(email))
                throw new UnauthorizedAccessException("User not authenticated");

            var lecturer = await _lecturerRepository.GetLecturerByEmailAsync(email);
            if (lecturer == null)
            {
                return new LecturerAssignmentStatusDto
                {
                    Message = "Lecturer record not found. Please complete registration first."
                };
            }

            var status = lecturer.RegistrationStatus;
            var needsCourseSelection = status == RegistrationStatus.PendingCourseSelection;
            var isPendingApproval = status == RegistrationStatus.PendingApproval;
            var isApproved = status == RegistrationStatus.Approved;

            string? message = status switch
            {
                RegistrationStatus.PendingCourseSelection => "Please select courses and units to complete your teaching assignment.",
                RegistrationStatus.PendingApproval => "Your teaching assignment has been submitted and is awaiting approval.",
                RegistrationStatus.Approved => "Your teaching assignment has been approved.",
                RegistrationStatus.Rejected => "Your teaching assignment was rejected. Please contact administration.",
                _ => null
            };

            return new LecturerAssignmentStatusDto
            {
                LecturerId = lecturer.Id,
                EmployeeNumber = lecturer.EmployeeNumber,
                FullName = $"{lecturer.FirstName} {lecturer.LastName}".Trim(),
                Email = lecturer.Email,
                RegistrationStatus = status.ToString(),
                HasSelectedCourses = !needsCourseSelection,
                CoursesCount = 0, // Will be populated from allocations
                UnitsCount = 0,   // Will be populated from allocations
                NeedsCourseSelection = needsCourseSelection,
                IsPendingApproval = isPendingApproval,
                IsApproved = isApproved,
                Message = message
            };
        }
    }
}
