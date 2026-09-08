using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Application.Features.Moderation.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Approves moderation for a student's mark (Coordinator / Administrator).
    /// The mark is marked moderated so it can proceed through the publication
    /// workflow. The complete modification history is retained.
    /// </summary>
    public class ApproveMarksHandler : IRequestHandler<ApproveMarksCommand, MediatR.Unit>
    {
        private readonly IStudentAssessmentMarkRepository _markRepository;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IModerationRecordRepository _moderationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;
        private readonly ILogger<ApproveMarksHandler> _logger;

        public ApproveMarksHandler(
            IStudentAssessmentMarkRepository markRepository,
            IAssessmentRepository assessmentRepository,
            IModerationRecordRepository moderationRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Domain.Interfaces.ICurrentUserService currentUser,
            ILogger<ApproveMarksHandler> logger)
        {
            _markRepository = markRepository;
            _assessmentRepository = assessmentRepository;
            _moderationRepository = moderationRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(ApproveMarksCommand request, CancellationToken cancellationToken)
        {
            var mark = await _markRepository.GetByAssessmentAndStudentAsync(request.AssessmentId, request.StudentId, cancellationToken);
            if (mark == null || mark.IsDeleted)
                throw new NotFoundException("StudentAssessmentMark", request.StudentId);

            var assessment = await _assessmentRepository.GetByIdAsync(request.AssessmentId, cancellationToken);
            if (assessment == null || assessment.IsDeleted)
                throw new NotFoundException("Assessment", request.AssessmentId);

            var existing = (await _moderationRepository.GetByAssessmentAsync(request.AssessmentId, cancellationToken))
                .Where(r => r.StudentId == request.StudentId)
                .OrderByDescending(r => r.ModeratedDate)
                .FirstOrDefault();

            if (existing == null)
            {
                existing = new ModerationRecord
                {
                    AssessmentId = request.AssessmentId,
                    CourseOfferingId = assessment.CourseOfferingId,
                    UnitId = assessment.UnitId,
                    StudentId = request.StudentId,
                    MarkId = mark.Id,
                    OriginalScore = mark.Mark,
                    RevisedScore = mark.Mark,
                    Status = ModerationStatus.Approved,
                    Comments = request.Comments,
                    ModeratedBy = _currentUser.Username,
                    ModeratedDate = DateTime.UtcNow,
                    ReviewerComments = request.Comments
                };
                await _moderationRepository.AddAsync(existing, cancellationToken);
            }
            else
            {
                existing.Status = ModerationStatus.Approved;
                existing.ApprovedBy = _currentUser.Username;
                existing.ApprovedDate = DateTime.UtcNow;
                existing.ReviewerComments = request.Comments;
                await _moderationRepository.UpdateAsync(existing, cancellationToken);
            }

            mark.IsModerated = true;
            mark.ModeratedBy = _currentUser.Username;
            mark.ModeratedDate = DateTime.UtcNow;
            mark.ModerationComment = request.Comments;
            await _markRepository.UpdateAsync(mark, cancellationToken);

            assessment.ModerationStatus = ModerationStatus.Approved;
            await _assessmentRepository.UpdateAsync(assessment, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("MarksApproved", "ModerationRecord", existing.Id.ToString(),
                $"Assessment: {request.AssessmentId}, Student: {request.StudentId}, By: {_currentUser.Username}");

            _logger.LogInformation("Mark for student {StudentId} on assessment {AssessmentId} approved",
                request.StudentId, request.AssessmentId);

            return MediatR.Unit.Value;
        }
    }
}