using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    public class VacateHouseCommand : IRequest<bool>
    {
        public Guid HouseId { get; set; }
        public DateTime? VacatedDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class VacateHouseCommandValidator : AbstractValidator<VacateHouseCommand>
    {
        public VacateHouseCommandValidator()
        {
            RuleFor(x => x.HouseId).NotEmpty();
        }
    }

    public class VacateHouseHandler : IRequestHandler<VacateHouseCommand, bool>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<VacateHouseHandler> _logger;

        public VacateHouseHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<VacateHouseHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(VacateHouseCommand request, CancellationToken cancellationToken)
        {
            var house = await _repository.GetHouseByIdAsync(request.HouseId, cancellationToken);
            if (house == null)
                throw new SMS.Application.Exceptions.NotFoundException("House", request.HouseId);

            if (house.OccupiedCount <= 0 && !house.IsOccupied && house.OccupantId == null)
                throw new SMS.Application.Exceptions.ValidationException($"House {house.HouseNumber} is not currently occupied");

            var vacatedDate = request.VacatedDate ?? DateTime.UtcNow;

            // Close every active assignment for this house (multi-occupancy aware)
            var activeAssignments = await _repository.GetActiveAssignmentsByHouseAsync(request.HouseId, cancellationToken);
            foreach (var assignment in activeAssignments)
            {
                assignment.Status = "Vacated";
                assignment.VacatedDate = vacatedDate;
                assignment.MoveOutDate = vacatedDate;
                if (!assignment.CheckOutDate.HasValue)
                    assignment.CheckOutDate = vacatedDate;
                if (request.Remarks != null)
                    assignment.Remarks = request.Remarks;
                await _repository.UpdateAssignmentAsync(assignment, cancellationToken);
            }

            // Reset house occupancy state
            house.OccupiedCount = 0;
            house.IsOccupied = false;
            house.OccupantId = null;
            house.OccupantType = null;
            house.Status = HouseStatus.Vacant;
            house.VacatedDate = vacatedDate;
            house.SemesterId = null;
            await _repository.UpdateHouseAsync(house, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Vacate", "House",
                $"Vacated house {house.HouseNumber} (HouseId: {house.Id}, LaneId: {house.LaneId}, {activeAssignments.Count()} active assignment(s) closed)");

            _logger.LogInformation("House {HouseNumber} vacated ({Count} assignments closed)", house.HouseNumber, activeAssignments.Count());
            return true;
        }
    }
}
