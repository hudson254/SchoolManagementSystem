using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
using SMS.Application.Common;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Assignments.Commands
{
    public class CreateAssignmentCommand : IRequest<AssignmentDto>
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid UnitId { get; set; }
        public Guid LecturerId { get; set; }
        public Guid? SemesterId { get; set; }
        public int MaxScore { get; set; } = 100;
        public int Weight { get; set; } = 20;
        public DateTime DueDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public string? Instructions { get; set; }
        public string? Attachments { get; set; }
        public bool AllowLateSubmission { get; set; } = false;
        public int LatePenaltyPercent { get; set; } = 10;
    }

    public class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
    {
        public CreateAssignmentCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Assignment title is required")
                .MaximumLength(200);

            RuleFor(x => x.UnitId)
                .NotEmpty().WithMessage("Unit ID is required");

            RuleFor(x => x.LecturerId)
                .NotEmpty().WithMessage("Lecturer ID is required");

            RuleFor(x => x.SemesterId)
                .Must(x => !x.HasValue || x.Value != Guid.Empty)
                .WithMessage("Semester ID is required");

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

    public class CreateAssignmentCommandHandler : IRequestHandler<CreateAssignmentCommand, AssignmentDto>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateAssignmentCommandHandler> _logger;

        public CreateAssignmentCommandHandler(
            IAssignmentRepository assignmentRepository,
            IUnitRepository unitRepository,
            ILecturerRepository lecturerRepository,
            ISemesterRepository semesterRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateAssignmentCommandHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _unitRepository = unitRepository;
            _lecturerRepository = lecturerRepository;
            _semesterRepository = semesterRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AssignmentDto> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null)
            {
                throw new NotFoundException("Unit", request.UnitId);
            }

            // The client may send the identity user id of the current lecturer
            // (the frontend assignment form uses user?.id). Fall back to the
            // lecturer profile by its linked user id.
            var lecturer = await _lecturerRepository.GetByIdAsync(request.LecturerId, cancellationToken);
            if (lecturer == null)
            {
                lecturer = await _lecturerRepository.GetByUserIdAsync(request.LecturerId, cancellationToken);
            }
            if (lecturer == null)
            {
                throw new NotFoundException("Lecturer", request.LecturerId);
            }

            // Resolve the semester client-side when not supplied (the web form
            // has no semester picker): prefer the current/default semester.
            Guid? semesterId = null;
            if (request.SemesterId.HasValue)
            {
                semesterId = request.SemesterId.Value;
            }
            else
            {
                var currentSemester = await _semesterRepository.GetCurrentOrDefaultAsync(cancellationToken);
                semesterId = currentSemester?.Id;
            }

            // Normalize to UTC so PostgreSQL 'timestamp with time zone' columns
            // accept the values (web forms submit datetimes without an offset).
            var dueDate = DateTimeUtc.From(request.DueDate);
            var closingDate = DateTimeUtc.From(request.ClosingDate);

            var assignment = new Assignment
            {
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                UnitId = request.UnitId,
                LecturerId = lecturer.Id,
                SemesterId = semesterId,
                MaxScore = request.MaxScore,
                Weight = request.Weight,
                DueDate = dueDate.Value,
                PublishedDate = DateTime.UtcNow,
                ClosingDate = closingDate,
                Instructions = request.Instructions,
                Attachments = request.Attachments,
                AllowLateSubmission = request.AllowLateSubmission,
                LatePenaltyPercent = request.LatePenaltyPercent,
                Status = "Published",
                IsGraded = false
            };

            await _assignmentRepository.AddAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("Assignment", "Create", assignment.Id.ToString(), "create");

            _logger.LogInformation("Assignment created: {Title} for unit {UnitCode}", assignment.Title, unit.Code);

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
                UnitName = unit.Name,
                UnitCode = unit.Code,
                LecturerName = lecturer.User != null
                    ? lecturer.User.FullName
                    : $"{lecturer.FirstName} {lecturer.LastName}".Trim(),
                SemesterName = assignment.Semester?.Name ?? string.Empty,
                SubmissionCount = 0,
                GradedCount = 0
            };
        }
    }
}




