using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.Queries;
using SMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Locks all assessments in a unit once grading has begun. Weighting and mark
    /// edits are rejected while locked; only an Administrator may unlock.
    /// </summary>
    public class LockUnitHandler : IRequestHandler<LockUnitCommand, MediatR.Unit>
    {
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;
        private readonly ILogger<LockUnitHandler> _logger;

        public LockUnitHandler(
            IAssessmentRepository assessmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Domain.Interfaces.ICurrentUserService currentUser,
            ILogger<LockUnitHandler> logger)
        {
            _assessmentRepository = assessmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(LockUnitCommand request, CancellationToken cancellationToken)
        {
            var assessments = (await _assessmentRepository.GetByUnitAsync(request.UnitId, cancellationToken))
                .Where(a => !a.IsDeleted)
                .ToList();

            if (assessments.Count == 0)
                throw new NotFoundException("Assessments", request.UnitId);

            foreach (var assessment in assessments)
            {
                assessment.IsWeightLocked = true;
                assessment.WeightLockedDate = DateTime.UtcNow;
                assessment.WeightLockedBy = _currentUser.Username;
                await _assessmentRepository.UpdateAsync(assessment, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("UnitWeightingLocked", "Assessment", request.UnitId.ToString(),
                $"Unit {request.UnitId} locked by {_currentUser.Username}. Reason: {request.Reason}");

            _logger.LogInformation("Unit {UnitId} weightings locked by {User}", request.UnitId, _currentUser.Username);

            return MediatR.Unit.Value;
        }
    }

    /// <summary>
    /// Unlocks assessments in a unit (Administrator only).
    /// </summary>
    public class UnlockUnitHandler : IRequestHandler<UnlockUnitCommand, MediatR.Unit>
    {
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;
        private readonly ILogger<UnlockUnitHandler> _logger;

        public UnlockUnitHandler(
            IAssessmentRepository assessmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Domain.Interfaces.ICurrentUserService currentUser,
            ILogger<UnlockUnitHandler> logger)
        {
            _assessmentRepository = assessmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(UnlockUnitCommand request, CancellationToken cancellationToken)
        {
            var assessments = (await _assessmentRepository.GetByUnitAsync(request.UnitId, cancellationToken))
                .Where(a => !a.IsDeleted)
                .ToList();

            if (assessments.Count == 0)
                throw new NotFoundException("Assessments", request.UnitId);

            foreach (var assessment in assessments)
            {
                assessment.IsWeightLocked = false;
                assessment.WeightLockedDate = null;
                assessment.WeightLockedBy = null;
                await _assessmentRepository.UpdateAsync(assessment, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("UnitWeightingUnlocked", "Assessment", request.UnitId.ToString(),
                $"Unit {request.UnitId} unlocked by {_currentUser.Username}. Reason: {request.Reason}");

            _logger.LogInformation("Unit {UnitId} weightings unlocked by {User}", request.UnitId, _currentUser.Username);

            return MediatR.Unit.Value;
        }
    }
}