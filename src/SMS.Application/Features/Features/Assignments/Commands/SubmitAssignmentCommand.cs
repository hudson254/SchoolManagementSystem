using MediatR;
using FluentValidation;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

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

    public class SubmitAssignmentCommandValidator : AbstractValidator<SubmitAssignmentCommand>
    {
        public SubmitAssignmentCommandValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty().WithMessage("Assignment ID is required");

            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("Student ID is required");

            RuleFor(x => x.FilePath)
                .NotEmpty().WithMessage("File path is required")
                .MaximumLength(500);
        }
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
            {
                throw new NotFoundException("Assignment", request.AssignmentId);
            }

            if (assignment.Status != "Published" && assignment.Status != "Open")
            {
                throw new BusinessRuleException("Cannot submit", "Assignment is not open for submissions");
            }

            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException("Student", request.StudentId);
            }

            // Check if student is enrolled in the unit
            var isEnrolled = await _assignmentRepository.IsStudentEnrolledAsync(request.StudentId, assignment.UnitId, cancellationToken);
            if (!isEnrolled)
            {
                throw new BusinessRuleException("Cannot submit", "Student is not enrolled in this unit");
            }

            // Check for existing submission
            var existingSubmission = await _assignmentRepository.GetSubmissionAsync(
                request.AssignmentId,
                request.StudentId,
                cancellationToken);

            if (existingSubmission != null)
            {
                throw new ConflictException("Submission", "Student-Assignment", $"{request.StudentId}-{request.AssignmentId}");
            }

            var isLate = DateTime.UtcNow > assignment.DueDate;
            var status = isLate && !assignment.AllowLateSubmission ? "Late" : "Submitted";

            var submission = new AssignmentSubmission
            {
                AssignmentId = request.AssignmentId,
                StudentId = request.StudentId,
                SubmissionDate = DateTime.UtcNow,
                FilePath = request.FilePath,
                FileName = request.FileName,
                FileSize = request.FileSize,
                ContentType = request.ContentType,
                Comments = request.Comments,
                Status = status,
                IsLate = isLate
            };

            await _assignmentRepository.AddSubmissionAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("AssignmentSubmission", "Submit", submission.Id, null, $"Student: {student.StudentNumber}");

            _logger.LogInformation("Assignment submitted by student {StudentNumber} for assignment {AssignmentId}",
                student.StudentNumber, request.AssignmentId);

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
                Score = submission.Score,
                Feedback = submission.Feedback,
                Status = submission.Status,
                IsLate = submission.IsLate,
                GradedDate = submission.GradedDate,
                StudentName = student.User.FullName,
                StudentNumber = student.StudentNumber,
                AssignmentTitle = assignment.Title,
                MaxScore = assignment.MaxScore
            };
        }
    }
}