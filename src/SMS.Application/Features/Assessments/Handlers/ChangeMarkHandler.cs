using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Features.Assessments.Commands;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Post-publication (or otherwise authorized) mark change. A reason is
    /// mandatory and the previous value is always retained in the grade change
    /// history. Recalculation, eligibility and transcript propagation are handled
    /// by the centralized assessment engine.
    /// </summary>
    public class ChangeMarkHandler : IRequestHandler<ChangeMarkCommand, MediatR.Unit>
    {
        private readonly IAssessmentEngine _engine;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;
        private readonly ILogger<ChangeMarkHandler> _logger;

        public ChangeMarkHandler(
            IAssessmentEngine engine,
            SMS.Domain.Interfaces.ICurrentUserService currentUser,
            ILogger<ChangeMarkHandler> logger)
        {
            _engine = engine;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(ChangeMarkCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new InvalidOperationException("A reason is required when changing a mark.");

            await _engine.UpdateMarkAsync(
                request.MarkId,
                request.NewScore,
                request.Reason,
                null,
                false,
                _currentUser.Username,
                cancellationToken);

            _logger.LogInformation("Mark {MarkId} changed to {Score} by {User}",
                request.MarkId, request.NewScore, _currentUser.Username);

            return MediatR.Unit.Value;
        }
    }
}