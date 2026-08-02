using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;

namespace SMS.Application.Features.Assignments.Commands
{
    public class GradeAssignmentCommand : IRequest<AssignmentSubmissionDto>
    {
        public Guid SubmissionId { get; set; }
        public int Score { get; set; }
        public string? Feedback { get; set; }
    }

    public class GradeAssignmentCommandHandler : IRequestHandler<GradeAssignmentCommand, AssignmentSubmissionDto>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<GradeAssignmentCommandHandler> _logger;

        public GradeAssignmentCommandHandler(
            IAssignmentRepository assignmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<GradeAssignmentCommandHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AssignmentSubmissionDto> Handle(GradeAssignmentCommand request, CancellationToken cancellationToken)
        {
            var submission = await _assignmentRepository.GetSubmissionWithDetailsAsync(request.SubmissionId, cancellationToken);
            if (submission == null)
                throw new NotFoundException("Submission", request.SubmissionId);

            if (submission.Score.HasValue)
                throw new BusinessRuleException("Submission has already been graded");

            var assignment = await _assignmentRepository.GetAssignmentWithDetailsAsync(submission.AssignmentId, cancellationToken);
            if (assignment == null)
                throw new NotFoundException("Assignment", submission.AssignmentId);

            if (request.Score > assignment.MaxScore)
                throw new FluentValidation.ValidationException("Score cannot exceed maximum score of " + assignment.MaxScore);

            submission.Score = request.Score;
            submission.Feedback = request.Feedback;
            submission.Status = "Graded";
            submission.GradedDate = DateTime.UtcNow;
            submission.GraderName = "System";

            await _assignmentRepository.UpdateSubmission(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var allSubmissions = await _assignmentRepository.GetSubmissionsAsync(assignment.Id, cancellationToken);
            var allGraded = allSubmissions.All(s => s.Status == "Graded");
            if (allGraded && allSubmissions.Any())
            {
                assignment.IsGraded = true;
                await _assignmentRepository.UpdateAsync(assignment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _auditService.LogActivityAsync("AssignmentSubmission", "Grade", submission.Id.ToString(), request.SubmissionId.ToString());

            _logger.LogInformation("Assignment graded: Submission {SubmissionId} scored {Score}", submission.Id, request.Score);

            return new AssignmentSubmissionDto
            {
                Id = submission.Id,
                AssignmentId = submission.AssignmentId,
                StudentId = submission.StudentId,
                SubmissionDate = submission.SubmissionDate,
                FilePath = submission.FilePath,
                FileName = submission.FileName,
                FileSize = submission.FileSize,
                Comments = submission.Comments,
                Score = (int)(submission.Score ?? 0),
                Feedback = submission.Feedback,
                Status = submission.Status,
                IsLate = submission.IsLate,
                GradedDate = submission.GradedDate?.ToString("O"),
                StudentName = (submission.Student?.FirstName ?? "") + " " + (submission.Student?.LastName ?? ""),
                StudentNumber = submission.Student?.StudentNumber ?? string.Empty,
                AssignmentTitle = assignment.Title,
                MaxScore = assignment.MaxScore
            };
        }
    }
}
