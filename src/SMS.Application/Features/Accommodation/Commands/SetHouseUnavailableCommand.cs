using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    /// <summary>
    /// Command to mark a house as unavailable or restore it to available.
    /// </summary>
    public class SetHouseUnavailableCommand : IRequest<bool>
    {
        public Guid HouseId { get; set; }
        public bool IsUnavailable { get; set; }
        public string? Notes { get; set; }
    }

    public class SetHouseUnavailableCommandValidator : AbstractValidator<SetHouseUnavailableCommand>
    {
        public SetHouseUnavailableCommandValidator()
        {
            RuleFor(x => x.HouseId).NotEmpty();
        }
    }

    public class SetHouseUnavailableHandler : IRequestHandler<SetHouseUnavailableCommand, bool>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<SetHouseUnavailableHandler> _logger;

        public SetHouseUnavailableHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<SetHouseUnavailableHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(SetHouseUnavailableCommand request, CancellationToken cancellationToken)
        {
            var house = await _repository.GetHouseByIdAsync(request.HouseId, cancellationToken);
            if (house == null)
                throw new NotFoundException("House", request.HouseId);

            var oldStatus = house.Status;

            if (request.IsUnavailable)
            {
                if (house.IsOccupied)
                    throw new BusinessRuleException("Cannot set unavailable",
                        $"House {house.HouseNumber} is currently occupied. Vacate the house first.");

                house.Status = HouseStatus.Unavailable;
                house.IsAvailable = false;
            }
            else
            {
                // Restore to available - set back to vacant if not occupied
                if (!house.IsOccupied)
                {
                    house.Status = HouseStatus.Vacant;
                    house.IsAvailable = true;
                }
            }

            if (!string.IsNullOrEmpty(request.Notes))
                house.Notes = request.Notes;

            await _repository.UpdateHouseAsync(house, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogDataChangeAsync("House", house.Id.ToString(), "AvailabilityChange",
                $"Old: {oldStatus}, New: {house.Status}, Notes: {request.Notes ?? "N/A"}");

            _logger.LogInformation("House {HouseNumber} availability changed to {Status}",
                house.HouseNumber, house.Status);
            return true;
        }
    }
}
