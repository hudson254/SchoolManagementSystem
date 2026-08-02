using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    public class CreateLaneCommand : IRequest<Guid>
    {
        public string LaneName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int NumberOfHouses { get; set; } = 10;
        public string? NumberingFormat { get; set; }
        public int StartingHouseNumber { get; set; } = 1;
    }

    public class CreateLaneCommandValidator : AbstractValidator<CreateLaneCommand>
    {
        public CreateLaneCommandValidator()
        {
            RuleFor(x => x.LaneName)
                .NotEmpty().WithMessage("Lane name is required")
                .MaximumLength(100).WithMessage("Lane name must not exceed 100 characters");

            RuleFor(x => x.NumberOfHouses)
                .GreaterThan(0).WithMessage("Number of houses must be greater than 0")
                .LessThanOrEqualTo(500).WithMessage("Number of houses must not exceed 500");

            RuleFor(x => x.StartingHouseNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Starting house number must be at least 1");

            RuleFor(x => x.NumberingFormat)
                .MaximumLength(20).WithMessage("Numbering format must not exceed 20 characters");
        }
    }

    public class CreateLaneHandler : IRequestHandler<CreateLaneCommand, Guid>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateLaneHandler> _logger;

        public CreateLaneHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateLaneHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateLaneCommand request, CancellationToken cancellationToken)
        {
            var exists = await _repository.LaneExistsAsync(request.LaneName, cancellationToken);
            if (exists)
            {
                throw new SMS.Application.Exceptions.ConflictException("Lane", "Name", request.LaneName);
            }

            var lane = new Lane
            {
                LaneName = request.LaneName,
                Description = request.Description,
                IsActive = true,
                NumberingFormat = request.NumberingFormat ?? "D3",
                StartingHouseNumber = request.StartingHouseNumber
            };

            await _repository.AddLaneAsync(lane, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var houses = new List<House>();
            var format = request.NumberingFormat ?? "D3";

            for (int i = 0; i < request.NumberOfHouses; i++)
            {
                var houseNumber = request.StartingHouseNumber + i;
                var house = new House
                {
                    LaneId = lane.Id,
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

            await _auditService.LogAsync("Create", "Lane",
                $"Created lane '{lane.LaneName}' with {request.NumberOfHouses} houses. LaneId: {lane.Id}");

            _logger.LogInformation("Lane created: {LaneName} ({LaneId}) with {Count} houses",
                lane.LaneName, lane.Id, request.NumberOfHouses);

            return lane.Id;
        }
    }
}
