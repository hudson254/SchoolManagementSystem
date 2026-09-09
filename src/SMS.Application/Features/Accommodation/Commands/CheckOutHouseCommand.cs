using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Accommodation.Commands
{
    /// <summary>
    /// Records the physical check-out of an assigned occupant from their house.
    /// A checked-out occupant stops consuming house capacity (OccupiedCount is
    /// reduced). The assignment history is preserved with check-out timestamps.
    /// </summary>
    public class CheckOutHouseCommand : IRequest<bool>
    {
        public Guid AssignmentId { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class CheckOutHouseCommandValidator : AbstractValidator<CheckOutHouseCommand>
    {
        public CheckOutHouseCommandValidator()
        {
            RuleFor(x => x.AssignmentId).NotEmpty();
        }
    }

    public class CheckOutHouseHandler : IRequestHandler<CheckOutHouseCommand, bool>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CheckOutHouseHandler> _logger;

        public CheckOutHouseHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CheckOutHouseHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(CheckOutHouseCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _repository.GetAssignmentWithDetailsAsync(request.AssignmentId, cancellationToken);
            if (assignment == null)
                throw new NotFoundException("Accommodation assignment", request.AssignmentId);

            if (assignment.Status != "Active")
                throw new BusinessRuleException("Cannot check out",
                    $"Assignment {request.AssignmentId} is {assignment.Status} and cannot be checked out");

            var checkOutDate = request.CheckOutDate ?? DateTime.UtcNow;

            // Preserve history; close the assignment.
            assignment.CheckOutDate = checkOutDate;
            assignment.MoveOutDate = checkOutDate;
            assignment.VacatedDate = checkOutDate;
            assignment.Status = "CheckedOut";
            if (!string.IsNullOrWhiteSpace(request.Remarks))
                assignment.Remarks = request.Remarks;
            await _repository.UpdateAssignmentAsync(assignment, cancellationToken);

            // Release capacity on the house (multi-occupancy aware).
            var house = await _repository.GetHouseByIdAsync(assignment.HouseId, cancellationToken);
            if (house != null)
            {
                house.OccupiedCount = Math.Max(0, house.OccupiedCount - 1);
                house.IsOccupied = house.OccupiedCount > 0;
                if (house.OccupiedCount == 0)
                {
                    house.OccupantId = null;
                    house.OccupantType = null;
                    house.Status = HouseStatus.Vacant;
                    house.VacatedDate = checkOutDate;
                    house.SemesterId = null;
                }
                else if (house.OccupantId == assignment.StudentId || house.OccupantId == assignment.LecturerId)
                {
                    // The primary occupant left but others remain - promote the first remaining occupant.
                    var remaining = await _repository.GetActiveAssignmentsByHouseAsync(house.Id, cancellationToken);
                    var next = remaining.FirstOrDefault();
                    if (next != null)
                    {
                        house.OccupantId = next.StudentId ?? next.LecturerId;
                        house.OccupantType = next.OccupantType;
                    }
                }
                await _repository.UpdateHouseAsync(house, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("CheckOut", "AccommodationAssignment", assignment.Id.ToString(),
                $"Occupant ({assignment.OccupantType}) checked out of house {house?.HouseNumber ?? assignment.HouseId.ToString()} on {checkOutDate:O}; remaining occupancy {house?.OccupiedCount ?? 0}/{house?.Capacity ?? 0}");

            _logger.LogInformation("Assignment {AssignmentId} checked out on {CheckOutDate:O}", request.AssignmentId, checkOutDate);
            return true;
        }
    }
}