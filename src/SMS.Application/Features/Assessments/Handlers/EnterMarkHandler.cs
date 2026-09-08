using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Enters a mark for a student on an assessment. Marks are restricted to the
    /// assessment maximum (authoritative range check). Draft marks are stored
    /// without triggering result recalculation; final marks flow through the
    /// centralized assessment engine (duplicate grading is prevented there).
    /// </summary>
    public class EnterMarkHandler : IRequestHandler<EnterMarkCommand, StudentAssessmentMarkDto>
    {
        private readonly IAssessmentEngine _engine;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly ILogger<EnterMarkHandler> _logger;

        public EnterMarkHandler(
            IAssessmentEngine engine,
            IAssessmentRepository assessmentRepository,
            ILogger<EnterMarkHandler> logger)
        {
            _engine = engine;
            _assessmentRepository = assessmentRepository;
            _logger = logger;
        }

        public async Task<StudentAssessmentMarkDto> Handle(EnterMarkCommand request, CancellationToken cancellationToken)
        {
            var assessment = await _assessmentRepository.GetByIdAsync(request.AssessmentId, cancellationToken);
            if (assessment == null || assessment.IsDeleted)
                throw new NotFoundException("Assessment", request.AssessmentId);

            if (request.Score < 0 || request.Score > assessment.MaxScore)
                throw new InvalidOperationException($"Score must be between 0 and {assessment.MaxScore}.");

            var mark = request.IsDraft
                ? await _engine.SaveDraftMarkAsync(request.AssessmentId, request.StudentId, request.Score, request.Feedback, cancellationToken)
                : await _engine.CalculateAndSaveMarkAsync(request.AssessmentId, request.StudentId, request.Score, cancellationToken);

            _logger.LogInformation("Mark entered for student {StudentId} on assessment {AssessmentId}",
                request.StudentId, request.AssessmentId);

            return Map(mark, assessment);
        }

        internal static StudentAssessmentMarkDto Map(StudentAssessmentMark m, Assessment a) => new()
        {
            Id = m.Id,
            AssessmentId = m.AssessmentId,
            AssessmentName = a.Title,
            StudentId = m.StudentId,
            CourseOfferingId = m.CourseOfferingId,
            Score = m.Mark,
            MaxScore = a.MaxScore,
            Percentage = m.Percentage,
            WeightedScore = m.WeightedScore,
            Weight = a.Weight,
            IsDraft = m.IsDraft,
            IsModerated = m.IsModerated,
            ModeratedDate = m.ModeratedDate,
            ModeratedBy = m.ModeratedBy,
            ModerationComment = m.ModerationComment,
            OriginalMark = m.OriginalMark,
            RevisedMark = m.RevisedMark,
            EntrySource = m.EntrySource,
            ImportBatchReference = m.ImportBatchReference,
            IsExempt = m.IsExempt,
            ExemptionReason = m.ExemptionReason,
            Feedback = m.Feedback,
            FeedbackPublished = m.FeedbackPublished,
            GradedBy = m.GradedBy?.ToString(),
            GradedDate = m.GradedDate,
            PublicationStatus = a.PublicationStatus,
            ModerationStatus = a.ModerationStatus
        };
    }
}