using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Features.Assessments.Commands;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Approves pending-review results (Coordinator / Administrator). Only results
    /// in Pending Review state are approved; approval is persisted and audited by
    /// the centralized engine. Approved-but-unpublished results remain invisible
    /// to students.
    /// </summary>
    public class ApproveResultsHandler : IRequestHandler<ApproveResultsCommand, MediatR.Unit>
    {
        private readonly IAssessmentEngine _engine;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;
        private readonly ILogger<ApproveResultsHandler> _logger;

        public ApproveResultsHandler(
            IAssessmentEngine engine,
            SMS.Domain.Interfaces.ICurrentUserService currentUser,
            ILogger<ApproveResultsHandler> logger)
        {
            _engine = engine;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(ApproveResultsCommand request, CancellationToken cancellationToken)
        {
            await _engine.ApproveUnitResultsAsync(request.UnitId, request.CourseOfferingId, _currentUser.Username, cancellationToken);

            _logger.LogInformation("Results for unit {UnitId} approved by {User}", request.UnitId, _currentUser.Username);

            return MediatR.Unit.Value;
        }
    }

    /// <summary>
    /// Publishes approved results (Coordinator / Administrator). Only approved
    /// results are published and become visible to students. The publication
    /// event is persisted and audited by the centralized engine.
    /// </summary>
    public class PublishResultsHandler : IRequestHandler<PublishResultsCommand, MediatR.Unit>
    {
        private readonly IAssessmentEngine _engine;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;
        private readonly ILogger<PublishResultsHandler> _logger;

        public PublishResultsHandler(
            IAssessmentEngine engine,
            SMS.Domain.Interfaces.ICurrentUserService currentUser,
            ILogger<PublishResultsHandler> logger)
        {
            _engine = engine;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(PublishResultsCommand request, CancellationToken cancellationToken)
        {
            await _engine.PublishUnitResultsAsync(request.UnitId, request.CourseOfferingId, _currentUser.Username, cancellationToken);

            _logger.LogInformation("Results for unit {UnitId} published by {User}", request.UnitId, _currentUser.Username);

            return MediatR.Unit.Value;
        }
    }
}