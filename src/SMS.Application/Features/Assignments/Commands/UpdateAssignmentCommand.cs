using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Assignments.Commands
{
    public class UpdateAssignmentCommand : IRequest<AssignmentDto>
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxScore { get; set; }
        public int Weight { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public string? Instructions { get; set; }
        public string? Attachments { get; set; }
        public bool AllowLateSubmission { get; set; }
        public int LatePenaltyPercent { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateAssignmentCommandValidator : AbstractValidator<UpdateAssignmentCommand>
    {
        public UpdateAssignmentCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Assignment ID is required");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Assignment title is required")
                .MaximumLength(200);

            RuleFor(x => x.MaxScore)
                .GreaterThan(0).WithMessage("Maximum score must be greater than 0");

            RuleFor(x => x.Weight)
                .GreaterThan(0).WithMessage("Weight must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Weight cannot exceed 100");

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future");

            RuleFor(x => x.ClosingDate)
                .GreaterThan(x => x.DueDate)
                .When(x => x.ClosingDate.HasValue)
                .WithMessage("Closing date must be after due date");

            RuleFor(x => x.LatePenaltyPercent)
                .InclusiveBetween(0, 100).WithMessage("Late penalty must be between 0 and 100");
        }
    }

    public class UpdateAssignmentCommandHandler : IRequestHandler<UpdateAssignmentCommand, AssignmentDto>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IBusinessEventNotifier _notifier;
        private readonly ILogger<UpdateAssignmentCommandHandler> _logger;

        public UpdateAssignmentCommandHandler(
            IAssignmentRepository assignmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IBusinessEventNotifier notifier,
            ILogger<UpdateAssignmentCommandHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<AssignmentDto> Handle(UpdateAssignmentCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _assignmentRepository.GetAssignmentWithDetailsAsync(request.Id, cancellationToken);
            if (assignment == null)
            {
                throw new NotFoundException("Assignment", request.Id);
            }

            // Check if assignment has submissions before allowing changes
            var hasSubmissions = await _assignmentRepository.HasSubmissionsAsync(request.Id, cancellationToken);
            if (hasSubmissions && request.Status != "Closed" && request.Status != "Archived")
            {
                _logger.LogWarning("Assignment has submissions, changes limited for ID: {AssignmentId}", request.Id);
            }

            // Captured BEFORE the mutation below so the notification can describe what
            // actually changed rather than restating the new state.
            var previousDueDate = assignment.DueDate;
            var previousStatus = assignment.Status;

            assignment.Title = request.Title;
            assignment.Description = request.Description ?? string.Empty;
            assignment.MaxScore = request.MaxScore;
            assignment.Weight = request.Weight;
            assignment.DueDate = DateTimeUtc.From(request.DueDate).Value;
            assignment.ClosingDate = DateTimeUtc.From(request.ClosingDate);
            assignment.Instructions = request.Instructions;
            assignment.Attachments = request.Attachments;
            assignment.AllowLateSubmission = request.AllowLateSubmission;
            assignment.LatePenaltyPercent = request.LatePenaltyPercent;
            assignment.Status = request.Status;

            await _assignmentRepository.UpdateAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("Assignment", "Update", assignment.Id.ToString(), "Update-Assignment");

            _logger.LogInformation("Assignment updated: {Title}", assignment.Title);

            var submissions = await _assignmentRepository.GetSubmissionsAsync(request.Id, cancellationToken);

            // AFTER the commit. Students enrolled in the unit learn that the
            // assignment they are working towards has materially changed (a moved due
            // date, a new status). Students NOT enrolled in this unit are never
            // included, so a unit's assessment is not leaked to a sibling unit.
            var dueDateChanged = Math.Abs((assignment.DueDate - previousDueDate).TotalSeconds) > 1;
            var statusChanged = !string.Equals(previousStatus, assignment.Status, StringComparison.OrdinalIgnoreCase);
            if (dueDateChanged || statusChanged)
            {
                var detail = dueDateChanged
                    ? $"The due date is now {assignment.DueDate:yyyy-MM-dd HH:mm} UTC."
                    : $"Its status is now \"{assignment.Status}\".";

                await _notifier.NotifyAssignmentPublishedAsync(
                    assignment.Id,
                    assignment.UnitId,
                    $"Assignment Updated: {assignment.Title}",
                    $"\"{assignment.Title}\" has changed. {detail}",
                    includeLecturer: true,
                    cancellationToken);
            }

            return new AssignmentDto
            {
                Id = assignment.Id,
                Title = assignment.Title,
                Description = assignment.Description,
                UnitId = assignment.UnitId,
                LecturerId = assignment.LecturerId,
                SemesterId = assignment.SemesterId,
                MaxScore = assignment.MaxScore,
                Weight = assignment.Weight,
                DueDate = assignment.DueDate,
                PublishedDate = assignment.PublishedDate,
                ClosingDate = assignment.ClosingDate,
                Instructions = assignment.Instructions,
                Attachments = assignment.Attachments,
                Status = assignment.Status,
                IsGraded = assignment.IsGraded,
                AllowLateSubmission = assignment.AllowLateSubmission,
                LatePenaltyPercent = assignment.LatePenaltyPercent,
                UnitName = assignment.Unit?.Name ?? string.Empty,
                UnitCode = assignment.Unit?.Code ?? string.Empty,
                LecturerName = assignment.Lecturer?.User.FullName ?? string.Empty,
                SemesterName = assignment.Semester?.Name ?? string.Empty,
                SubmissionCount = submissions.Count(),
                GradedCount = submissions.Count(s => s.Status == "Graded")
            };
        }
    }
}






