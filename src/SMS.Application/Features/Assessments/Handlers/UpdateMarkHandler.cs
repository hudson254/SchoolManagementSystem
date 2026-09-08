using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Updates an existing mark. All changes are routed through the centralized
    /// assessment engine, which records an immutable GradeChangeHistory entry,
    /// recalculates the unit result / grade / certificate eligibility, and audits.
    /// </summary>
    public class UpdateMarkHandler : IRequestHandler<UpdateMarkCommand, StudentAssessmentMarkDto>
    {
        private readonly IAssessmentEngine _engine;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;
        private readonly ILogger<UpdateMarkHandler> _logger;

        public UpdateMarkHandler(
            IAssessmentEngine engine,
            IAssessmentRepository assessmentRepository,
            SMS.Domain.Interfaces.ICurrentUserService currentUser,
            ILogger<UpdateMarkHandler> logger)
        {
            _engine = engine;
            _assessmentRepository = assessmentRepository;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<StudentAssessmentMarkDto> Handle(UpdateMarkCommand request, CancellationToken cancellationToken)
        {
            var mark = await _engine.UpdateMarkAsync(
                request.MarkId,
                request.Score,
                request.Reason,
                request.Feedback,
                request.IsDraft,
                _currentUser.Username,
                cancellationToken);

            var assessment = await _assessmentRepository.GetByIdAsync(mark.AssessmentId, cancellationToken);
            if (assessment == null)
                throw new SMS.Application.Exceptions.NotFoundException("Assessment", mark.AssessmentId);

            _logger.LogInformation("Mark {MarkId} updated by {User}", request.MarkId, _currentUser.Username);

            return EnterMarkHandler.Map(mark, assessment);
        }
    }
}