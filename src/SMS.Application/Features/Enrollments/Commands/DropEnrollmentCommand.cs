using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Enrollments.Commands
{
    public class DropEnrollmentCommand : IRequest<MediatR.Unit>
    {
        public Guid EnrollmentId { get; set; }
    }

    public class DropEnrollmentCommandHandler : IRequestHandler<DropEnrollmentCommand, MediatR.Unit>
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DropEnrollmentCommandHandler> _logger;

        public DropEnrollmentCommandHandler(
            IEnrollmentRepository enrollmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DropEnrollmentCommandHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(DropEnrollmentCommand request, CancellationToken cancellationToken)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(request.EnrollmentId, cancellationToken);
            if (enrollment == null)
                throw new NotFoundException("Enrollment", request.EnrollmentId);

            enrollment.Status = "Dropped";
            enrollment.DropDate = DateTime.UtcNow;
            enrollment.IsActive = false;

            await _enrollmentRepository.UpdateAsync(enrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Enrollment", "Drop", enrollment.Id.ToString());

            _logger.LogInformation("Enrollment {EnrollmentId} dropped", request.EnrollmentId);

            return MediatR.Unit.Value;
        }
    }
}
