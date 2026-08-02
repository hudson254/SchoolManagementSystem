using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    /// <summary>
    /// Command to manually add houses to an existing lane.
    /// </summary>
    public class CreateHouseCommand : IRequest<IEnumerable<Guid>>
    {
        public Guid LaneId { get; set; }
        public int NumberOfHouses { get; set; } = 1;
        public string? NumberingFormat { get; set; }
        public int? StartingHouseNumber { get; set; }
    }

    public class CreateHouseCommandValidator : AbstractValidator<CreateHouseCommand>
    {
        public CreateHouseCommandValidator()
        {
            RuleFor(x => x.LaneId)
                .NotEmpty().WithMessage("Lane ID is required");

            RuleFor(x => x.NumberOfHouses)
                .GreaterThan(0).WithMessage("Number of houses must be greater than 0")
                .LessThanOrEqualTo(500).WithMessage("Number of houses must not exceed 500");

            RuleFor(x => x.StartingHouseNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Starting house number must be at least 1")
                .When(x => x.StartingHouseNumber.HasValue);

            RuleFor(x => x.NumberingFormat)
                .MaximumLength(20).WithMessage("Numbering format must not exceed 20 characters");
        }
    }

    public class CreateHouseHandler : IRequestHandler<CreateHouseCommand, IEnumerable<Guid>>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateHouseHandler> _logger;

        public CreateHouseHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateHouseHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<IEnumerable<Guid>> Handle(CreateHouseCommand request, CancellationToken cancellationToken)
        {
            var lane = await _repository.GetLaneByIdAsync(request.LaneId, cancellationToken);
            if (lane == null)
                throw new NotFoundException("Lane", request.LaneId);

            if (!lane.IsActive)
                throw new BusinessRuleException("Cannot add houses", "Lane is not active");

            var format = request.NumberingFormat ?? lane.NumberingFormat ?? "D3";
            var startNumber = request.StartingHouseNumber ?? await _repository.GetNextHouseNumberSequenceAsync(request.LaneId, cancellationToken);

            var houses = new List<House>();
            for (int i = 0; i < request.NumberOfHouses; i++)
            {
                var houseNumber = startNumber + i;
                var house = new House
                {
                    LaneId = request.LaneId,
                    HouseNumber = houseNumber.ToString(format),
                    HouseNumberNumeric = houseNumber,
                    Status = HouseStatus.Vacant,
                    IsOccupied = false,
                    IsEnabled = true,
                    IsAvailable = true
                };
                houses.Add(house);
            }

            await _repository.AddHousesRangeAsync(houses, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Create", "House",
                $"Created {request.NumberOfHouses} houses in lane '{lane.LaneName}'. HouseIds: {string.Join(", ", houses.Select(h => h.Id))}");

            _logger.LogInformation("Created {Count} houses in lane {LaneName} ({LaneId})",
                request.NumberOfHouses, lane.LaneName, request.LaneId);

            return houses.Select(h => h.Id);
        }
    }
}
