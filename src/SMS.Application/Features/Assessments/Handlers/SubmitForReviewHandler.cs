using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Application.Features.Assessments.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Submits all draft unit results to Pending Review. The weighting total must
    /// equal exactly 100% before results can be submitted for review.
    /// </summary>
    public class SubmitForReviewHandler : IRequestHandler<SubmitForReviewCommand, MediatR.Unit>
    {
        private readonly IAssessmentEngine _engine;
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;
        private readonly ILogger<SubmitForReviewHandler> _logger;

        public SubmitForReviewHandler(
            IAssessmentEngine engine,
            IUnitResultRepository unitResultRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Domain.Interfaces.ICurrentUserService currentUser,
            ILogger<SubmitForReviewHandler> logger)
        {
            _engine = engine;
            _unitResultRepository = unitResultRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(SubmitForReviewCommand request, CancellationToken cancellationToken)
        {
            if (!await _engine.ValidateWeightTotalAsync(request.UnitId, request.CourseOfferingId, cancellationToken))
                throw new InvalidOperationException("Total assessment weighting must equal exactly 100% before results can be submitted for review.");

            var results = (await _unitResultRepository.GetByUnitAsync(request.UnitId, cancellationToken))
                .Where(r => r.PublicationStatus == ResultPublicationStatus.Draft)
                .ToList();

            if (results.Count == 0)
                throw new InvalidOperationException("No draft results are available to submit for this unit.");

            foreach (var result in results)
            {
                result.PublicationStatus = ResultPublicationStatus.PendingReview;
                result.ModerationStatus = ModerationStatus.PendingReview;
                await _unitResultRepository.UpdateAsync(result, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("ResultsSubmittedForReview", "UnitResult", request.UnitId.ToString(),
                $"Unit: {request.UnitId}, Count: {results.Count}, Submitted by: {_currentUser.Username}. Comments: {request.Comments}");

            _logger.LogInformation("Submitted {Count} results for unit {UnitId} for review", results.Count, request.UnitId);

            return MediatR.Unit.Value;
        }
    }
}