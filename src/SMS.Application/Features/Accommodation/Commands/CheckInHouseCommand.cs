using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Accommodation.Commands
{
    /// <summary>
    /// Records the physical check-in of an assigned occupant to their house.
    /// A check-in is idempotent: repeating it never produces contradictory state.
    /// </summary>
    public class CheckInHouseCommand : IRequest<bool>
    {
        public Guid AssignmentId { get; set; }
        public DateTime? CheckInDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class CheckInHouseCommandValidator : AbstractValidator<CheckInHouseCommand>
    {
        public CheckInHouseCommandValidator()
        {
            RuleFor(x => x.AssignmentId).NotEmpty();
        }
    }

    public class CheckInHouseHandler : IRequestHandler<CheckInHouseCommand, bool>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CheckInHouseHandler> _logger;

        public CheckInHouseHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CheckInHouseHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(CheckInHouseCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _repository.GetAssignmentWithDetailsAsync(request.AssignmentId, cancellationToken);
            if (assignment == null)
                throw new NotFoundException("Accommodation assignment", request.AssignmentId);

            if (assignment.Status == "Active" && assignment.CheckInDate.HasValue)
            {
                // Idempotent: already checked in.
                _logger.LogInformation("Assignment {AssignmentId} already checked in", request.AssignmentId);
                return true;
            }

            if (assignment.Status != "Active")
                throw new BusinessRuleException("Cannot check in",
                    $"Assignment {request.AssignmentId} is {assignment.Status} and cannot be checked in");

            var checkInDate = request.CheckInDate ?? DateTime.UtcNow;
            assignment.CheckInDate = checkInDate;
            if (!assignment.MoveInDate.HasValue)
                assignment.MoveInDate = checkInDate;
            if (!string.IsNullOrWhiteSpace(request.Remarks))
                assignment.Remarks = request.Remarks;
            await _repository.UpdateAssignmentAsync(assignment, cancellationToken);

            // Record the move-in date on the house if it is the first occupant.
            var house = await _repository.GetHouseByIdAsync(assignment.HouseId, cancellationToken);
            if (house != null && !house.OccupiedDate.HasValue)
            {
                house.OccupiedDate = checkInDate;
                await _repository.UpdateHouseAsync(house, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("CheckIn", "AccommodationAssignment", assignment.Id.ToString(),
                $"Occupant ({assignment.OccupantType}) checked in to house {house?.HouseNumber ?? assignment.HouseId.ToString()} on {checkInDate:O}");

            _logger.LogInformation("Assignment {AssignmentId} checked in on {CheckInDate:O}", request.AssignmentId, checkInDate);
            return true;
        }
    }
}