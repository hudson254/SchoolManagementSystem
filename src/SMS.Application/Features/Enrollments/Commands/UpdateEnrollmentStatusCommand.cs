using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Enrollments.Commands
{
    public class UpdateEnrollmentStatusCommand : IRequest<EnrollmentDto>
    {
        public Guid EnrollmentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateEnrollmentStatusCommandValidator : AbstractValidator<UpdateEnrollmentStatusCommand>
    {
        public UpdateEnrollmentStatusCommandValidator()
        {
            RuleFor(x => x.EnrollmentId)
                .NotEmpty().WithMessage("Enrollment ID is required");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required")
                .MaximumLength(50).WithMessage("Status must not exceed 50 characters");
        }
    }

    public class UpdateEnrollmentStatusCommandHandler : IRequestHandler<UpdateEnrollmentStatusCommand, EnrollmentDto>
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IBusinessEventNotifier _notifier;
        private readonly ILogger<UpdateEnrollmentStatusCommandHandler> _logger;

        public UpdateEnrollmentStatusCommandHandler(
            IEnrollmentRepository enrollmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IBusinessEventNotifier notifier,
            ILogger<UpdateEnrollmentStatusCommandHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<EnrollmentDto> Handle(UpdateEnrollmentStatusCommand request, CancellationToken cancellationToken)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(request.EnrollmentId, cancellationToken);
            if (enrollment == null)
                throw new NotFoundException("Enrollment", request.EnrollmentId);

            var previousStatus = enrollment.Status;

            enrollment.Status = request.Status;

            await _enrollmentRepository.UpdateAsync(enrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Enrollment", "UpdateStatus", enrollment.Id.ToString());

            _logger.LogInformation("Enrollment {EnrollmentId} status updated to {Status}", request.EnrollmentId, request.Status);

            // AFTER the commit, and only on a real transition. Setting a status to the
            // value it already holds notifies nobody, so a retried request cannot
            // produce duplicate rows for the same event.
            if (!string.Equals(previousStatus, enrollment.Status, StringComparison.OrdinalIgnoreCase))
            {
                await _notifier.NotifyEnrollmentStatusChangedAsync(
                    enrollment.StudentId,
                    enrollment.Status,
                    enrollment.Unit?.Name ?? enrollment.Course?.Name ?? "your unit",
                    cancellationToken);
            }

            return new EnrollmentDto
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId,
                UnitId = enrollment.UnitId ?? Guid.Empty,
                SemesterId = enrollment.SemesterId ?? Guid.Empty,
                EnrollmentDate = enrollment.EnrollmentDate,
                Status = enrollment.Status,
                DropDate = enrollment.DropDate,
                StudentName = enrollment.Student != null ? $"{enrollment.Student.FirstName} {enrollment.Student.LastName}" : string.Empty,
                StudentNumber = enrollment.Student?.StudentNumber ?? string.Empty,
                UnitName = enrollment.Unit?.Name ?? enrollment.Course?.Name ?? string.Empty,
                UnitCode = enrollment.Unit?.Code ?? enrollment.Course?.Code ?? string.Empty,
                Credits = enrollment.Unit?.Credits ?? 0,
                SemesterName = enrollment.Semester?.Name ?? string.Empty
            };
        }
    }
}
