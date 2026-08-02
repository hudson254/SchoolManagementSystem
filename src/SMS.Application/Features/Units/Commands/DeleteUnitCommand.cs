using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Units.Commands
{
    public class DeleteUnitCommand : IRequest
    {
        public Guid Id { get; set; }
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
            var unit = await _unitRepository.GetUnitWithDetailsAsync(request.Id, cancellationToken);
            if (unit == null)
            {
                throw new NotFoundException("Unit", request.Id);
            }

            // Check if unit has any enrollments
            if (unit.Enrollments != null && unit.Enrollments.Any())
            {
                throw new BusinessRuleException("Cannot delete unit with existing enrollments");
            }

            await _auditService.LogActivityAsync("Unit", "Delete", unit.Id.ToString(), request.Id.ToString());
            await _unitRepository.DeleteAsync(unit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("DeleteUnit", unit.Id.ToString(), $"Unit deleted: {unit.Code}");
            _logger.LogInformation("Unit deleted: {UnitCode}", unit.Code);
        }
    }
}

