using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Commands
{
    public class AssignStudentToOfferingCommand : IRequest<CourseOfferingEnrollmentDto>
    {
        public Guid CourseOfferingId { get; set; }
        public Guid StudentId { get; set; }
        public string? Notes { get; set; }
    }

    public class AssignStudentToOfferingCommandValidator : AbstractValidator<AssignStudentToOfferingCommand>
    {
        public AssignStudentToOfferingCommandValidator()
        {
            RuleFor(x => x.CourseOfferingId)
                .NotEmpty().WithMessage("Course Offering ID is required");

            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("Student ID is required");
        }
    }

    public class AssignStudentToOfferingCommandHandler
        : IRequestHandler<AssignStudentToOfferingCommand, CourseOfferingEnrollmentDto>
    {
        private readonly ICourseOfferingEnrollmentRepository _enrollmentRepository;
        private readonly ICourseOfferingRepository _offeringRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<AssignStudentToOfferingCommandHandler> _logger;

        public AssignStudentToOfferingCommandHandler(
            ICourseOfferingEnrollmentRepository enrollmentRepository,
            ICourseOfferingRepository offeringRepository,
            IStudentRepository studentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<AssignStudentToOfferingCommandHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _offeringRepository = offeringRepository;
            _studentRepository = studentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CourseOfferingEnrollmentDto> Handle(
            AssignStudentToOfferingCommand request,
            CancellationToken cancellationToken)
        {
            var offering = await _offeringRepository.GetByIdAsync(request.CourseOfferingId, cancellationToken);
            if (offering == null)
                throw new NotFoundException("CourseOffering", request.CourseOfferingId);

            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student == null)
                throw new NotFoundException("Student", request.StudentId);

            // Prevent duplicate active enrollment for the same offering
            var exists = await _enrollmentRepository.ExistsByOfferingAndStudentAsync(
                request.CourseOfferingId, request.StudentId, cancellationToken);
            if (exists)
                throw new ConflictException("Student is already enrolled in this course offering.");

            // Determine attempt number based on prior enrollments in any offering of the same course
            var attemptCount = await _enrollmentRepository.GetAttemptCountAsync(
                request.CourseOfferingId, request.StudentId, cancellationToken);

            var enrollment = new CourseOfferingEnrollment
            {
                CourseOfferingId = request.CourseOfferingId,
                StudentId = request.StudentId,
                Status = "PendingConfirmation",
                IsActive = true,
                AttemptNumber = attemptCount + 1,
                ConfirmationStatus = ConfirmationStatus.Pending,
                EnrollmentDate = DateTime.UtcNow,
                Notes = request.Notes
            };

            await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("CourseOfferingEnrollment", "AssignStudent", enrollment.Id.ToString());

            _logger.LogInformation("Student {StudentId} assigned to offering {OfferingCode} (attempt {AttemptNumber})",
                request.StudentId, offering.OfferingCode, enrollment.AttemptNumber);

            return new CourseOfferingEnrollmentDto
            {
                Id = enrollment.Id,
                CourseOfferingId = enrollment.CourseOfferingId,
                OfferingCode = offering.OfferingCode,
                StudentId = enrollment.StudentId,
                StudentName = student.User != null ? $"{student.User.FirstName} {student.User.LastName}" : null,
                StudentNumber = student.StudentNumber,
                EnrollmentDate = enrollment.EnrollmentDate,
                Status = enrollment.Status,
                IsActive = enrollment.IsActive,
                AttemptNumber = enrollment.AttemptNumber,
                ConfirmationStatus = enrollment.ConfirmationStatus,
                ConfirmedDate = enrollment.ConfirmedDate,
                DropDate = enrollment.DropDate,
                Notes = enrollment.Notes
            };
        }
    }
}
