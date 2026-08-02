using MediatR;
using FluentValidation;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Students.Commands
{
    public class DropStudentCommand : IRequest
    {
        public Guid StudentId { get; set; }
        public Guid UnitId { get; set; }
        public Guid SemesterId { get; set; }
        public string? Reason { get; set; }
    }

    public class DropStudentCommandValidator : AbstractValidator<DropStudentCommand>
    {
        public DropStudentCommandValidator()
        {
            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("Student ID is required");

            RuleFor(x => x.UnitId)
                .NotEmpty().WithMessage("Unit ID is required");

            RuleFor(x => x.SemesterId)
                .NotEmpty().WithMessage("Semester ID is required");
        }
    }

    public class DropStudentCommandHandler : IRequestHandler<DropStudentCommand>
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DropStudentCommandHandler> _logger;

        public DropStudentCommandHandler(
            IEnrollmentRepository enrollmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DropStudentCommandHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(DropStudentCommand request, CancellationToken cancellationToken)
        {
            var enrollment = await _enrollmentRepository.GetEnrollmentAsync(
                request.StudentId,
                request.UnitId,
                request.SemesterId,
                cancellationToken);

            if (enrollment == null)
            {
                throw new NotFoundException("Enrollment", $"{request.StudentId}-{request.UnitId}-{request.SemesterId}");
            }

            enrollment.Status = "Dropped";
            enrollment.DropDate = DateTime.UtcNow;

            _enrollmentRepository.Update(enrollment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Enrollment", "Drop", enrollment.Id, null, $"Reason: {request.Reason ?? "Not specified"}");

            _logger.LogInformation("Student {StudentId} dropped from unit {UnitId}", request.StudentId, request.UnitId);
        }
    }
}