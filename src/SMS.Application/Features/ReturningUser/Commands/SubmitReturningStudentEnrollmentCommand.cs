using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.ReturningUser.Commands
{
    /// <summary>
    /// Allows a returning student with Approved status to enroll in a new course for a new semester.
    /// </summary>
    public class SubmitReturningStudentEnrollmentCommand : IRequest<ReturningEnrollmentResultDto>
    {
        public Guid CourseId { get; set; }
        public Guid SemesterId { get; set; }
    }

    public class SubmitReturningStudentEnrollmentCommandValidator
        : AbstractValidator<SubmitReturningStudentEnrollmentCommand>
    {
        public SubmitReturningStudentEnrollmentCommandValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
            RuleFor(x => x.SemesterId).NotEmpty();
        }
    }

    public class ReturningEnrollmentResultDto
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int UnitsEnrolled { get; set; }
        public string Status { get; set; } = "Active";
        public string Message { get; set; } = string.Empty;
    }

    public class SubmitReturningStudentEnrollmentCommandHandler
        : IRequestHandler<SubmitReturningStudentEnrollmentCommand, ReturningEnrollmentResultDto>
    {
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IStudentRepository _studentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IAuditService _auditService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SubmitReturningStudentEnrollmentCommandHandler> _logger;

        public SubmitReturningStudentEnrollmentCommandHandler(
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IStudentRepository studentRepository,
            ICourseRepository courseRepository,
            IUnitRepository unitRepository,
            IEnrollmentRepository enrollmentRepository,
            IAuditService auditService,
            IUnitOfWork unitOfWork,
            ILogger<SubmitReturningStudentEnrollmentCommandHandler> logger)
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

        public async Task<ReturningEnrollmentResultDto> Handle(
            SubmitReturningStudentEnrollmentCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User not authenticated");

            var userEmail = _currentUserService.Email;
            if (string.IsNullOrEmpty(userEmail))
                throw new UnauthorizedAccessException("User email not found");

            var student = await _studentRepository.GetStudentByEmailAsync(userEmail);
            if (student == null)
                throw new NotFoundException("Student record not found");

            // Verify student is already approved (returning user)
            if (student.RegistrationStatus != RegistrationStatus.Approved)
                throw new SMS.Application.Exceptions.ValidationException(
                    $"Cannot enroll. Current status: {student.RegistrationStatus}. Expected: Approved");

            // Validate course
            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null || !course.IsActive)
                throw new NotFoundException("Course", request.CourseId);

            // Get active units for the course
            var units = await _unitRepository.GetUnitsByCourseIdAsync(course.Id, cancellationToken);
            var activeUnits = units.Where(u => u.IsActive).ToList();

            if (!activeUnits.Any())
                throw new SMS.Application.Exceptions.ValidationException(
                    "No active units found for the selected course");

            // Check for duplicate enrollments in the same semester
            foreach (var unit in activeUnits)
            {
                var isEnrolled = await _enrollmentRepository.IsStudentEnrolledAsync(student.Id, unit.Id);
                if (isEnrolled)
                    throw new SMS.Application.Exceptions.ValidationException(
                        $"Student is already enrolled in unit {unit.Code} for this semester");
            }

            // Create enrollment records
            foreach (var unit in activeUnits)
            {
                var enrollment = new Enrollment
                {
                    StudentId = student.Id,
                    CourseId = course.Id,
                    UnitId = unit.Id,
                    SemesterId = request.SemesterId,
                    EnrollmentDate = DateTime.UtcNow,
                    Status = "Active",
                    IsActive = true
                };
                student.Enrollments.Add(enrollment);
            }

            student.CurrentSemesterId = request.SemesterId;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("ReturningEnrollment", student.Id.ToString(),
                $"Returning student enrolled in course {course.Code} ({activeUnits.Count} units)");

            _logger.LogInformation(
                "Returning student {StudentId} enrolled in course {CourseCode} with {UnitCount} units",
                student.Id, course.Code, activeUnits.Count);

            return new ReturningEnrollmentResultDto
            {
                StudentId = student.Id,
                CourseId = course.Id,
                CourseName = course.Name,
                UnitsEnrolled = activeUnits.Count,
                Status = "Active",
                Message = $"Enrolled in {activeUnits.Count} units for {course.Name}. Welcome back!"
            };
        }
    }
}
