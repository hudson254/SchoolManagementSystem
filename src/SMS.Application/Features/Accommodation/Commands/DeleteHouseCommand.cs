using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    /// <summary>
    /// Command to delete (soft-delete) a house.
    /// A house can only be deleted if it has no active occupants.
    /// </summary>
    public class DeleteHouseCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }

    public class DeleteHouseCommandValidator : AbstractValidator<DeleteHouseCommand>
    {
        public DeleteHouseCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class DeleteHouseHandler : IRequestHandler<DeleteHouseCommand, bool>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteHouseHandler> _logger;

        public DeleteHouseHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DeleteHouseHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteHouseCommand request, CancellationToken cancellationToken)
        {
            var house = await _repository.GetHouseByIdAsync(request.Id, cancellationToken);
            if (house == null)
                throw new NotFoundException("House", request.Id);

            // Cannot delete a house that has an active occupant
            if (house.IsOccupied || house.OccupantId.HasValue)
                throw new BusinessRuleException("Cannot delete house",
                    $"House {house.HouseNumber} has an active occupant. Vacate the house first.");

            await _repository.DeleteHouseAsync(request.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Delete", "House",
                $"Deleted house '{house.HouseNumber}' (HouseId: {request.Id}, LaneId: {house.LaneId})");

            _logger.LogInformation("House deleted: {HouseNumber} ({HouseId})", house.HouseNumber, request.Id);
            return true;
        }
    }
}
