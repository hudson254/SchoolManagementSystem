using MediatR;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Units.Commands
{
    public class DeleteUnitCommand : IRequest
    {
        public Guid UnitId { get; set; }
    }

    public class DeleteUnitCommandHandler : IRequestHandler<DeleteUnitCommand>
    {
        private readonly IUnitRepository _unitRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteUnitCommandHandler> _logger;

        public DeleteUnitCommandHandler(
            IUnitRepository unitRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DeleteUnitCommandHandler> logger)
        {
            _unitRepository = unitRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(DeleteUnitCommand request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetUnitWithDetailsAsync(request.UnitId, cancellationToken);
            if (unit == null)
            {
                throw new NotFoundException("Unit", request.UnitId);
            }

            // Check if unit has any active enrollments
            var hasActiveEnrollments = unit.Enrollments.Any(e => e.Status == "Enrolled" || e.Status == "InProgress");
            if (hasActiveEnrollments)
            {
                throw new BusinessRuleException(
                    "Cannot delete unit",
                    "Unit has active student enrollments. Please drop all students before deleting the unit.");
            }

            await _unitRepository.DeleteAsync(unit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Unit", "Delete", unit.Id, null, $"Unit: {unit.Code}");

            _logger.LogInformation("Unit deleted: {UnitCode}", unit.Code);
        }
    }
}