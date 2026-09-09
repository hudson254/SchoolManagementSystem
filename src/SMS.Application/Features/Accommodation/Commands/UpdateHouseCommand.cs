using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    /// <summary>
    /// Command to update a house's details (rename, change status, etc.).
    /// </summary>
    public class UpdateHouseCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string? HouseNumber { get; set; }
        public string? HouseName { get; set; }
        public string? Status { get; set; }
        public bool? IsEnabled { get; set; }
        public bool? IsAvailable { get; set; }
        public int? Capacity { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateHouseCommandValidator : AbstractValidator<UpdateHouseCommand>
    {
        public UpdateHouseCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.HouseNumber)
                .MaximumLength(20).WithMessage("House number must not exceed 20 characters")
                .When(x => !string.IsNullOrEmpty(x.HouseNumber));

            RuleFor(x => x.HouseName)
                .MaximumLength(100).WithMessage("House name must not exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.HouseName));

            RuleFor(x => x.Status)
                .Must((context, status) => string.IsNullOrEmpty(status) || HouseStatus.All.Contains(status))
                .WithMessage("Invalid house status. Valid values: " + string.Join(", ", HouseStatus.All))
                .When(x => !string.IsNullOrEmpty(x.Status));

            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be at least 1")
                .LessThanOrEqualTo(1000).WithMessage("Capacity must not exceed 1000")
                .When(x => x.Capacity.HasValue);

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes must not exceed 500 characters");
        }
    }

    public class UpdateHouseHandler : IRequestHandler<UpdateHouseCommand, bool>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateHouseHandler> _logger;

        public UpdateHouseHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateHouseHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateHouseCommand request, CancellationToken cancellationToken)
        {
            var house = await _repository.GetHouseByIdAsync(request.Id, cancellationToken);
            if (house == null)
                throw new NotFoundException("House", request.Id);

            var oldValues = new Dictionary<string, object?>
            {
                ["HouseNumber"] = house.HouseNumber,
                ["HouseName"] = house.HouseName,
                ["Status"] = house.Status,
                ["IsEnabled"] = house.IsEnabled,
                ["IsAvailable"] = house.IsAvailable,
                ["Capacity"] = house.Capacity,
                ["OccupiedCount"] = house.OccupiedCount,
                ["Notes"] = house.Notes
            };

            // Check for house number uniqueness within the lane
            if (!string.IsNullOrEmpty(request.HouseNumber) && request.HouseNumber != house.HouseNumber)
            {
                if (await _repository.HouseExistsInLaneAsync(house.LaneId, request.HouseNumber, cancellationToken))
                    throw new ConflictException("House", "HouseNumber", request.HouseNumber);

                house.HouseNumber = request.HouseNumber;
            }

            if (request.HouseName != null)
                house.HouseName = string.IsNullOrWhiteSpace(request.HouseName) ? null : request.HouseName.Trim();

            if (request.Capacity.HasValue)
            {
                if (request.Capacity.Value < house.OccupiedCount)
                    throw new BusinessRuleException("Cannot update capacity",
                        $"House {house.HouseNumber} currently has {house.OccupiedCount} occupant(s); capacity cannot be reduced below the current occupancy.");

                house.Capacity = request.Capacity.Value;
            }

            if (!string.IsNullOrEmpty(request.Status))
                house.Status = request.Status;

            if (request.IsEnabled.HasValue)
                house.IsEnabled = request.IsEnabled.Value;

            if (request.IsAvailable.HasValue)
                house.IsAvailable = request.IsAvailable.Value;

            if (request.Notes != null)
                house.Notes = request.Notes;

            // Sync IsOccupied/Status with the capacity-based occupancy model
            house.IsOccupied = house.OccupiedCount > 0;
            if (house.OccupiedCount > 0)
            {
                if (house.Status == HouseStatus.Vacant)
                    house.Status = HouseStatus.Occupied;
            }
            else if (house.Status == HouseStatus.Occupied)
            {
                house.Status = HouseStatus.Vacant;
            }

            await _repository.UpdateHouseAsync(house, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var newValues = new Dictionary<string, object?>
            {
                ["HouseNumber"] = house.HouseNumber,
                ["HouseName"] = house.HouseName,
                ["Status"] = house.Status,
                ["IsEnabled"] = house.IsEnabled,
                ["IsAvailable"] = house.IsAvailable,
                ["Capacity"] = house.Capacity,
                ["OccupiedCount"] = house.OccupiedCount,
                ["Notes"] = house.Notes
            };

            await _auditService.LogDataChangeAsync("House", house.Id.ToString(), "Update",
                $"Old: {System.Text.Json.JsonSerializer.Serialize(oldValues)}, New: {System.Text.Json.JsonSerializer.Serialize(newValues)}");

            _logger.LogInformation("House updated: {HouseNumber} ({HouseId})", house.HouseNumber, house.Id);
            return true;
        }
    }
}
