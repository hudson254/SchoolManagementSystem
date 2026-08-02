using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
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
        public Guid SemesterId { get; set; }
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
                .NotEmpty().WithMessage("Semester ID is required");

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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateAssignmentCommandHandler> _logger;

        public CreateAssignmentCommandHandler(
            IAssignmentRepository assignmentRepository,
            IUnitRepository unitRepository,
            ILecturerRepository lecturerRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateAssignmentCommandHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _unitRepository = unitRepository;
            _lecturerRepository = lecturerRepository;
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

            var lecturer = await _lecturerRepository.GetByIdAsync(request.LecturerId, cancellationToken);
            if (lecturer == null)
            {
                throw new NotFoundException("Lecturer", request.LecturerId);
            }

            var assignment = new Assignment
            {
                Title = request.Title,
                Description = request.Description,
                UnitId = request.UnitId,
                LecturerId = request.LecturerId,
                SemesterId = request.SemesterId,
                MaxScore = request.MaxScore,
                Weight = request.Weight,
                DueDate = request.DueDate,
                PublishedDate = DateTime.UtcNow,
                ClosingDate = request.ClosingDate,
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
                LecturerName = lecturer.User.FullName,
                SemesterName = assignment.Semester?.Name ?? string.Empty,
                SubmissionCount = 0,
                GradedCount = 0
            };
        }
    }
}




