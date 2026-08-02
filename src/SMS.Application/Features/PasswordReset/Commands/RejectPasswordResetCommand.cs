using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.PasswordReset.Commands
{
    public class RejectPasswordResetCommand : IRequest<MediatR.Unit>
    {
        public Guid RequestId { get; set; }
        public string AdminUserId { get; set; } = string.Empty;
        public string? ResolutionNote { get; set; }
    }

    public class RejectPasswordResetCommandHandler : IRequestHandler<RejectPasswordResetCommand, MediatR.Unit>
    {
        private readonly IPasswordResetRequestRepository _repository;
        private readonly ILogger<RejectPasswordResetCommandHandler> _logger;

        public RejectPasswordResetCommandHandler(
            IPasswordResetRequestRepository repository,
            ILogger<RejectPasswordResetCommandHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(RejectPasswordResetCommand request, CancellationToken cancellationToken)
        {
            var resetRequest = await _repository.GetByIdAsync(request.RequestId);
            if (resetRequest == null)
                throw new InvalidOperationException("Password reset request not found.");

            if (resetRequest.Status != PasswordResetRequestStatus.Pending)
                throw new InvalidOperationException($"Request is already {resetRequest.Status}.");

            // Update request status
            resetRequest.Status = PasswordResetRequestStatus.Rejected;
            resetRequest.FulfilledByUserId = request.AdminUserId;
            resetRequest.FulfilledAt = DateTime.UtcNow;
            resetRequest.ResolutionNote = request.ResolutionNote;

            await _repository.UpdateAsync(resetRequest);

            _logger.LogInformation("Password reset request {RequestId} rejected by admin {AdminUserId}",
                request.RequestId, request.AdminUserId);

            return MediatR.Unit.Value;
        }
    }
}
