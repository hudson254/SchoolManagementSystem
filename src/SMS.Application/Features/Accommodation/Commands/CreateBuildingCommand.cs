using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    public class CreateBuildingCommand : IRequest<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int TotalFloors { get; set; } = 1;
        public bool HasElevator { get; set; }
        public string? Category { get; set; }
    }

    public class CreateBuildingCommandValidator : AbstractValidator<CreateBuildingCommand>
    {
        public CreateBuildingCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Building name is required")
                .MaximumLength(100).WithMessage("Building name must not exceed 100 characters");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Building code is required");

            RuleFor(x => x.TotalFloors)
                .GreaterThan(0).WithMessage("Total floors must be greater than 0");
        }
    }

    public class CreateBuildingHandler : IRequestHandler<CreateBuildingCommand, Guid>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateBuildingHandler> _logger;

        public CreateBuildingHandler(
            IAccommodationRepository accommodationRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateBuildingHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateBuildingCommand request, CancellationToken cancellationToken)
        {
            // Check if building with same name/code already exists
            var existing = await _accommodationRepository.GetBuildingByCodeAsync(request.Code, cancellationToken);
            if (existing != null)
            {
                throw new ConflictException("Building", "Code", request.Code);
            }

            var building = new Building
            {
                Name = request.Name,
                Address = request.Address,
                TotalFloors = request.TotalFloors,
                HasElevator = request.HasElevator,
                Category = request.Category,
                IsActive = true
            };

            await _accommodationRepository.AddBuildingAsync(building, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Create", "Building", building.Id.ToString());

            _logger.LogInformation("Building created: {BuildingName} ({BuildingId})", building.Name, building.Id);

            return building.Id;
        }
    }
}
