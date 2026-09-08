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
    /// Reviews a student's mark for an assessment (Coordinator / Administrator).
    /// Keeps an immutable ModerationRecord capturing original / revised scores
    /// and moderator comments. Returning for correction flags the mark so it can
    /// be edited by the lecturer before re-submission.
    /// </summary>
    public class ReviewMarksHandler : IRequestHandler<ReviewMarksCommand, MediatR.Unit>
    {
        private readonly IStudentAssessmentMarkRepository _markRepository;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IModerationRecordRepository _moderationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;
        private readonly ILogger<ReviewMarksHandler> _logger;

        public ReviewMarksHandler(
            IStudentAssessmentMarkRepository markRepository,
            IAssessmentRepository assessmentRepository,
            IModerationRecordRepository moderationRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Domain.Interfaces.ICurrentUserService currentUser,
            ILogger<ReviewMarksHandler> logger)
        {
            _markRepository = markRepository;
            _assessmentRepository = assessmentRepository;
            _moderationRepository = moderationRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(ReviewMarksCommand request, CancellationToken cancellationToken)
        {
            var mark = await _markRepository.GetByAssessmentAndStudentAsync(request.AssessmentId, request.StudentId, cancellationToken);
            if (mark == null || mark.IsDeleted)
                throw new NotFoundException("StudentAssessmentMark", request.StudentId);

            var assessment = await _assessmentRepository.GetByIdAsync(request.AssessmentId, cancellationToken);
            if (assessment == null || assessment.IsDeleted)
                throw new NotFoundException("Assessment", request.AssessmentId);

            var status = request.ReturnForCorrection
                ? ModerationStatus.ReturnedForCorrection
                : ModerationStatus.PendingReview;

            var record = new ModerationRecord
            {
                AssessmentId = request.AssessmentId,
                CourseOfferingId = assessment.CourseOfferingId,
                UnitId = assessment.UnitId,
                StudentId = request.StudentId,
                MarkId = mark.Id,
                OriginalScore = mark.Mark,
                RevisedScore = request.ReturnForCorrection ? null : mark.Mark,
                Status = status,
                Comments = request.Comments,
                ModeratedBy = _currentUser.Username,
                ModeratedDate = DateTime.UtcNow,
                ReturnedReason = request.ReturnForCorrection ? request.Comments : null,
                ReturnedDate = request.ReturnForCorrection ? DateTime.UtcNow : null,
                ReviewerComments = request.Comments
            };

            await _moderationRepository.AddAsync(record, cancellationToken);

            mark.IsModerated = false;
            mark.ModeratedBy = _currentUser.Username;
            mark.ModeratedDate = DateTime.UtcNow;
            mark.ModerationComment = request.Comments;
            await _markRepository.UpdateAsync(mark, cancellationToken);

            assessment.ModerationStatus = status;
            await _assessmentRepository.UpdateAsync(assessment, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("MarksReviewed", "ModerationRecord", record.Id.ToString(),
                $"Assessment: {request.AssessmentId}, Student: {request.StudentId}, Status: {status}, By: {_currentUser.Username}");

            _logger.LogInformation("Mark for student {StudentId} on assessment {AssessmentId} reviewed: {Status}",
                request.StudentId, request.AssessmentId, status);

            return MediatR.Unit.Value;
        }
    }
}