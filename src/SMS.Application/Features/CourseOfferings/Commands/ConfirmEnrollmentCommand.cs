using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Commands
{
    public class ConfirmEnrollmentCommand : IRequest<CourseOfferingEnrollmentDto>
    {
        public Guid EnrollmentId { get; set; }
        public bool Confirm { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class ConfirmEnrollmentCommandValidator : AbstractValidator<ConfirmEnrollmentCommand>
    {
        public ConfirmEnrollmentCommandValidator()
        {
            RuleFor(x => x.EnrollmentId)
                .NotEmpty().WithMessage("Enrollment ID is required");
        }
    }

    public class ConfirmEnrollmentCommandHandler
        : IRequestHandler<ConfirmEnrollmentCommand, CourseOfferingEnrollmentDto>
    {
        private readonly ICourseOfferingEnrollmentRepository _enrollmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<ConfirmEnrollmentCommandHandler> _logger;

        public ConfirmEnrollmentCommandHandler(
            ICourseOfferingEnrollmentRepository enrollmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<ConfirmEnrollmentCommandHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CourseOfferingEnrollmentDto> Handle(
            ConfirmEnrollmentCommand request,
            CancellationToken cancellationToken)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(request.EnrollmentId, cancellationToken);
            if (enrollment == null)
                throw new NotFoundException("CourseOfferingEnrollment", request.EnrollmentId);

            if (request.Confirm)
            {
                enrollment.ConfirmationStatus = ConfirmationStatus.Confirmed;
                enrollment.Status = "Active";
                enrollment.ConfirmedDate = DateTime.UtcNow;
                enrollment.Notes = request.Notes ?? enrollment.Notes;
            }
            else
            {
                enrollment.ConfirmationStatus = ConfirmationStatus.Pending;
                enrollment.Status = "PendingConfirmation";
                enrollment.ConfirmedDate = null;
                enrollment.Notes = request.Notes ?? enrollment.Notes;
            }

            await _enrollmentRepository.UpdateAsync(enrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("CourseOfferingEnrollment", request.Confirm ? "Confirm" : "Unconfirm",
                enrollment.Id.ToString());

            _logger.LogInformation("Enrollment {EnrollmentId} confirmed={Confirm} for offering {OfferingId}",
                enrollment.Id, request.Confirm, enrollment.CourseOfferingId);

            return new CourseOfferingEnrollmentDto
            {
                Id = enrollment.Id,
                CourseOfferingId = enrollment.CourseOfferingId,
                OfferingCode = enrollment.CourseOffering?.OfferingCode,
                StudentId = enrollment.StudentId,
                StudentName = enrollment.Student?.User != null
                    ? $"{enrollment.Student.User.FirstName} {enrollment.Student.User.LastName}"
                    : null,
                StudentNumber = enrollment.Student?.StudentNumber,
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
