using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    public class DeleteLaneCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }

    public class DeleteLaneCommandValidator : AbstractValidator<DeleteLaneCommand>
    {
        public DeleteLaneCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class DeleteLaneHandler : IRequestHandler<DeleteLaneCommand, bool>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteLaneHandler> _logger;

        public DeleteLaneHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DeleteLaneHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteLaneCommand request, CancellationToken cancellationToken)
        {
            var lane = await _repository.GetLaneByIdAsync(request.Id, cancellationToken);
            if (lane == null)
                throw new SMS.Application.Exceptions.NotFoundException("Lane", request.Id);

            // Check if lane has houses
            var houseCount = await _repository.CountHousesInLaneAsync(request.Id, cancellationToken);
            if (houseCount > 0)
                throw new SMS.Application.Exceptions.ValidationException($"Cannot delete lane '{lane.LaneName}' because it has {houseCount} houses. Remove all houses first.");

            await _repository.DeleteLaneAsync(request.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Delete", "Lane",
                $"Deleted lane '{lane.LaneName}'. LaneId: {request.Id}");

            _logger.LogInformation("Lane deleted: {LaneName} ({LaneId})", lane.LaneName, request.Id);
            return true;
        }
    }
}
