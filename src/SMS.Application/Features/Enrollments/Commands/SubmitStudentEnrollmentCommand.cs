using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Enrollments.Commands
{
    /// <summary>
    /// Submits a student's course enrollment request after initial registration.
    /// Creates Enrollment records for all units in the selected course and sets
    /// status to PendingApproval.
    /// </summary>
    public class SubmitStudentEnrollmentCommand : IRequest<EnrollmentSubmissionResultDto>
    {
        public Guid CourseId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    public class SubmitStudentEnrollmentCommandValidator : AbstractValidator<SubmitStudentEnrollmentCommand>
    {
        public SubmitStudentEnrollmentCommandValidator()
        {
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Course selection is required");
        }
    }

    public class EnrollmentSubmissionResultDto
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int UnitsEnrolled { get; set; }
        public string Status { get; set; } = "PendingApproval";
        public string Message { get; set; } = string.Empty;
    }

    public class SubmitStudentEnrollmentCommandHandler
        : IRequestHandler<SubmitStudentEnrollmentCommand, EnrollmentSubmissionResultDto>
    {
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IStudentRepository _studentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IAuditService _auditService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SubmitStudentEnrollmentCommandHandler> _logger;

        public SubmitStudentEnrollmentCommandHandler(
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IStudentRepository studentRepository,
            ICourseRepository courseRepository,
            IUnitRepository unitRepository,
            IEnrollmentRepository enrollmentRepository,
            IAuditService auditService,
            IUnitOfWork unitOfWork,
            ILogger<SubmitStudentEnrollmentCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _studentRepository = studentRepository;
            _courseRepository = courseRepository;
            _unitRepository = unitRepository;
            _enrollmentRepository = enrollmentRepository;
            _auditService = auditService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<EnrollmentSubmissionResultDto> Handle(
            SubmitStudentEnrollmentCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User not authenticated");

            // Find the student record linked to this user via email
            var userEmail = _currentUserService.Email;
            if (string.IsNullOrEmpty(userEmail))
                throw new UnauthorizedAccessException("User email not found");

            var student = await _studentRepository.GetStudentByEmailAsync(userEmail);
            if (student == null)
                throw new NotFoundException("Student record not found for current user");

            // Verify student is in PendingCourseSelection status
            if (student.RegistrationStatus != RegistrationStatus.PendingCourseSelection)
                throw new SMS.Application.Exceptions.ValidationException(
                    $"Cannot submit enrollment. Current status: {student.RegistrationStatus}. " +
                    "Expected: PendingCourseSelection");

            // Validate the course
            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null || !course.IsActive)
                throw new NotFoundException("Course", request.CourseId);

            // Get active units for the course
            var units = await _unitRepository.GetUnitsByCourseIdAsync(course.Id, cancellationToken);
            var activeUnits = units.Where(u => u.IsActive).ToList();

            if (!activeUnits.Any())
                throw new SMS.Application.Exceptions.ValidationException(
                    "No active units found for the selected course");

            // Create enrollment records for each unit
            foreach (var unit in activeUnits)
            {
                var enrollment = new Enrollment
                {
                    StudentId = student.Id,
                    CourseId = course.Id,
                    UnitId = unit.Id,
                    SemesterId = request.SemesterId ?? course.SemesterId,
                    EnrollmentDate = DateTime.UtcNow,
                    Status = "PendingApproval",  // Not active until approved
                    IsActive = false              // Not active until approved
                };
                student.Enrollments.Add(enrollment);
            }

            // Update student status to PendingApproval
            student.RegistrationStatus = RegistrationStatus.PendingApproval;
            student.IsEnrolled = false; // Still not fully enrolled until approved

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("SubmitEnrollment", student.Id.ToString(),
                $"Student submitted enrollment for course {course.Code} ({activeUnits.Count} units)");

            _logger.LogInformation(
                "Student {StudentId} submitted enrollment for course {CourseCode} with {UnitCount} units",
                student.Id, course.Code, activeUnits.Count);

            return new EnrollmentSubmissionResultDto
            {
                StudentId = student.Id,
                CourseId = course.Id,
                CourseName = course.Name,
                UnitsEnrolled = activeUnits.Count,
                Status = "PendingApproval",
                Message = $"Enrollment submitted for {activeUnits.Count} units. Awaiting approval."
            };
        }
    }
}
