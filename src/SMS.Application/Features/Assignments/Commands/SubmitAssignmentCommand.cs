using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;

namespace SMS.Application.Features.Assignments.Commands
{
    public class SubmitAssignmentCommand : IRequest<AssignmentSubmissionDto>
    {
        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
        public string? Comments { get; set; }
    }

    public class SubmitAssignmentCommandHandler : IRequestHandler<SubmitAssignmentCommand, AssignmentSubmissionDto>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<SubmitAssignmentCommandHandler> _logger;

        public SubmitAssignmentCommandHandler(
            IAssignmentRepository assignmentRepository,
            IStudentRepository studentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<SubmitAssignmentCommandHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _studentRepository = studentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AssignmentSubmissionDto> Handle(SubmitAssignmentCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _assignmentRepository.GetAssignmentWithDetailsAsync(request.AssignmentId, cancellationToken);
            if (assignment == null)
                throw new NotFoundException("Assignment", request.AssignmentId);

            if (assignment.Status != "Published" && assignment.Status != "Open")
                throw new BusinessRuleException("Assignment is not open for submissions");

            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student == null)
                throw new NotFoundException("Student", request.StudentId);

            var isEnrolled = await _assignmentRepository.IsStudentEnrolledAsync(request.StudentId, assignment.UnitId, cancellationToken);
            if (!isEnrolled)
                throw new BusinessRuleException("Student is not enrolled in this unit");

            var existingSubmission = await _assignmentRepository.GetSubmissionsAsync(assignment.Id, cancellationToken);
            if (existingSubmission.Any())
                throw new ConflictException("Submission", "Student-Assignment", $"{request.StudentId}-{request.AssignmentId}");

            var isLate = DateTime.UtcNow > assignment.DueDate;
            var status = isLate && !assignment.AllowLateSubmission ? "Late" : "Submitted";

            var submission = new AssignmentSubmission
            {
                AssignmentId = request.AssignmentId,
                StudentId = request.StudentId,
                SubmittedAt = DateTime.UtcNow,
                FilePath = request.FilePath,
                FileName = request.FileName,
                FileSize = request.FileSize,
                Comments = request.Comments,
                Status = status,
                IsLate = isLate
            };

            await _assignmentRepository.AddSubmissionAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("AssignmentSubmission", "Submit", submission.Id.ToString(), request.AssignmentId.ToString());

            _logger.LogInformation("Assignment submitted by student {StudentNumber} for assignment {AssignmentId}",
                student.StudentNumber, request.AssignmentId);

            return new AssignmentSubmissionDto
            {
                Id = submission.Id,
                AssignmentId = submission.AssignmentId,
                StudentId = submission.StudentId,
                SubmissionDate = submission.SubmittedAt.ToString("O"),
                FilePath = submission.FilePath,
                FileName = submission.FileName,
                FileSize = submission.FileSize,
                Comments = submission.Comments,
                Score = (int)(submission.Score ?? 0),
                Feedback = submission.Feedback,
                Status = submission.Status,
                IsLate = submission.IsLate,
                GradedDate = submission.GradedDate?.ToString("O"),
                StudentName = (student.FirstName ?? "") + " " + (student.LastName ?? ""),
                StudentNumber = student.StudentNumber,
                AssignmentTitle = assignment.Title,
                MaxScore = assignment.MaxScore
            };
        }
    }
}

