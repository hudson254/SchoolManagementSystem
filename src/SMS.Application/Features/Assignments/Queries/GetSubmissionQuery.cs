using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Assignments.Queries
{
    public class GetSubmissionQuery : IRequest<AssignmentSubmissionDto>
    {
        public Guid SubmissionId { get; set; }
    }

    public class GetSubmissionQueryHandler : IRequestHandler<GetSubmissionQuery, AssignmentSubmissionDto>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILogger<GetSubmissionQueryHandler> _logger;

        public GetSubmissionQueryHandler(
            IAssignmentRepository assignmentRepository,
            ILogger<GetSubmissionQueryHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _logger = logger;
        }

        public async Task<AssignmentSubmissionDto> Handle(GetSubmissionQuery request, CancellationToken cancellationToken)
        {
            var submission = await _assignmentRepository.GetSubmissionWithDetailsAsync(request.SubmissionId, cancellationToken);
            if (submission == null)
                throw new NotFoundException("Submission", request.SubmissionId);

            return new AssignmentSubmissionDto
            {
                Id = submission.Id,
                AssignmentId = submission.AssignmentId,
                StudentId = submission.StudentId,
                StudentName = submission.Student != null ? $"{submission.Student.FirstName} {submission.Student.LastName}" : string.Empty,
                StudentNumber = submission.Student?.StudentNumber ?? string.Empty,
                AssignmentTitle = submission.Assignment?.Title ?? string.Empty,
                MaxScore = submission.Assignment?.MaxScore ?? 0,
                SubmissionDate = submission.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                FilePath = submission.FilePath,
                FileName = submission.FileName,
                Comments = submission.Comments,
                Score = submission.Score ?? 0,
                GradedScore = submission.Score,
                Feedback = submission.Feedback,
                Status = submission.Status ?? "Submitted",
                IsLate = submission.IsLate,
                SubmittedAt = submission.SubmittedAt,
                GradedAt = submission.GradedDate,
                GraderName = submission.GraderName,
                GradedDate = submission.GradedDate?.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
    }
}
