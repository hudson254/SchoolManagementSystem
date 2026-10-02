using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;

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
        private readonly IBusinessEventNotifier _notifier;
        private readonly ILogger<VacateHouseHandler> _logger;

        public VacateHouseHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IBusinessEventNotifier notifier,
            ILogger<VacateHouseHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _notifier = notifier;
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

            // AFTER the commit. Each occupant is notified individually because
            // vacating a house closes EVERY active assignment on it, and each of those
            // occupants is a different user. Distinct() guards against a duplicate
            // occupant row producing two copies of the same notification.
            var notified = new HashSet<Guid>();
            foreach (var closed in activeAssignments)
            {
                var occupantId = closed.StudentId ?? closed.LecturerId;
                if (!occupantId.HasValue || occupantId.Value == Guid.Empty) continue;
                if (!notified.Add(occupantId.Value)) continue;

                await _notifier.NotifyAccommodationEndedAsync(
                    closed.StudentId,
                    closed.LecturerId,
                    $"Your accommodation at house {house.HouseNumber} has ended. Please complete any outstanding check-out formalities.",
                    house.Id,
                    cancellationToken);
            }

            return true;
        }
    }
}
