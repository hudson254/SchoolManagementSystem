using MediatR;
using SMS.Application.Features.Moderation.DTOs;
using SMS.Application.Features.Moderation.Queries;
using SMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Lists moderation records awaiting review (Coordinator / Administrator).
    /// </summary>
    public class GetPendingModerationHandler : IRequestHandler<GetPendingModerationQuery, IEnumerable<ModerationRecordDto>>
    {
        private readonly IModerationRecordRepository _moderationRepository;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IStudentRepository _studentRepository;

        public GetPendingModerationHandler(
            IModerationRecordRepository moderationRepository,
            IAssessmentRepository assessmentRepository,
            IStudentRepository studentRepository)
        {
            _moderationRepository = moderationRepository;
            _assessmentRepository = assessmentRepository;
            _studentRepository = studentRepository;
        }

        public async Task<IEnumerable<ModerationRecordDto>> Handle(GetPendingModerationQuery request, CancellationToken cancellationToken)
        {
            var records = await _moderationRepository.GetPendingAsync(cancellationToken);
            var dtos = new List<ModerationRecordDto>();

            foreach (var r in records)
            {
                var assessment = r.AssessmentId != null
                    ? await _assessmentRepository.GetByIdAsync(r.AssessmentId, cancellationToken)
                    : null;
                var student = r.StudentId != null
                    ? await _studentRepository.GetByIdAsync(r.StudentId.Value, cancellationToken)
                    : null;

                dtos.Add(new ModerationRecordDto
                {
                    Id = r.Id,
                    AssessmentId = r.AssessmentId,
                    AssessmentName = assessment?.Title ?? string.Empty,
                    StudentId = r.StudentId ?? Guid.Empty,
                    StudentName = student != null ? $"{student.FirstName} {student.LastName}".Trim() : string.Empty,
                    MarkId = r.MarkId,
                    OriginalScore = r.OriginalScore ?? 0m,
                    RevisedScore = r.RevisedScore,
                    Status = r.Status,
                    Comments = r.Comments,
                    ModeratedBy = r.ModeratedBy,
                    ModeratedDate = r.ModeratedDate,
                    ReviewerComments = r.ReviewerComments
                });
            }

            return dtos;
        }
    }
}
