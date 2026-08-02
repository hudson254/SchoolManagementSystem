using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    public class UpdateLaneCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string LaneName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public string? NumberingFormat { get; set; }
    }

    public class UpdateLaneCommandValidator : AbstractValidator<UpdateLaneCommand>
    {
        public UpdateLaneCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.LaneName)
                .NotEmpty().WithMessage("Lane name is required")
                .MaximumLength(100);
        }
    }

    public class UpdateLaneHandler : IRequestHandler<UpdateLaneCommand, bool>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateLaneHandler> _logger;

        public UpdateLaneHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateLaneHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateLaneCommand request, CancellationToken cancellationToken)
        {
            var lane = await _repository.GetLaneByIdAsync(request.Id, cancellationToken);
            if (lane == null)
                throw new SMS.Application.Exceptions.NotFoundException("Lane", request.Id);

            // Check if new name conflicts with existing lane
            var existing = await _repository.GetLaneByNameAsync(request.LaneName, cancellationToken);
            if (existing != null && existing.Id != request.Id)
                throw new SMS.Application.Exceptions.ConflictException("Lane", "Name", request.LaneName);

            var oldName = lane.LaneName;
            lane.LaneName = request.LaneName;
            lane.Description = request.Description;
            lane.IsActive = request.IsActive;
            if (!string.IsNullOrWhiteSpace(request.NumberingFormat))
                lane.NumberingFormat = request.NumberingFormat;

            await _repository.UpdateLaneAsync(lane, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Update", "Lane",
                $"Updated lane '{oldName}' -> '{request.LaneName}'. LaneId: {lane.Id}");

            _logger.LogInformation("Lane updated: {LaneName} ({LaneId})", lane.LaneName, lane.Id);
            return true;
        }
    }
}
