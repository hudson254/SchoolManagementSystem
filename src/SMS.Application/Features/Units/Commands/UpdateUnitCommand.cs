using MediatR;
using FluentValidation;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Units.Commands
{
    public class UpdateUnitCommand : IRequest<UnitDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; }
        public int ContactHours { get; set; }
        public Guid CourseId { get; set; }
        public Guid? PrerequisiteUnitId { get; set; }
        public string? LearningOutcomes { get; set; }
        public string? AssessmentMethods { get; set; }
        public string? RecommendedTextbooks { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateUnitCommandValidator : AbstractValidator<UpdateUnitCommand>
    {
        public UpdateUnitCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Unit ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Unit name is required")
                .MaximumLength(100);

            RuleFor(x => x.Credits)
                .GreaterThan(0).WithMessage("Credits must be greater than 0")
                .LessThanOrEqualTo(6).WithMessage("Credits cannot exceed 6");

            RuleFor(x => x.ContactHours)
                .GreaterThan(0).WithMessage("Contact hours must be greater than 0");

            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Course ID is required");
        }
    }

    public class UpdateUnitCommandHandler : IRequestHandler<UpdateUnitCommand, UnitDto>
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateUnitCommandHandler> _logger;

        public UpdateUnitCommandHandler(
            IUnitRepository unitRepository,
            ICourseRepository courseRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateUnitCommandHandler> logger)
        {
            _unitRepository = unitRepository;
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<UnitDto> Handle(UpdateUnitCommand request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetUnitWithDetailsAsync(request.Id, cancellationToken);
            if (unit == null)
            {
                throw new NotFoundException("Unit", request.Id);
            }

            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null)
            {
                throw new NotFoundException("Course", request.CourseId);
            }

            if (request.PrerequisiteUnitId.HasValue)
            {
                var prerequisite = await _unitRepository.GetByIdAsync(request.PrerequisiteUnitId.Value, cancellationToken);
                if (prerequisite == null)
                {
                    throw new NotFoundException("Prerequisite Unit", request.PrerequisiteUnitId.Value);
                }
            }

            unit.Name = request.Name;
            unit.Description = request.Description;
            unit.Credits = request.Credits;
            unit.ContactHours = request.ContactHours;
            unit.CourseId = request.CourseId;
            unit.PrerequisiteUnitId = request.PrerequisiteUnitId;
            unit.LearningOutcomes = request.LearningOutcomes;
            unit.AssessmentMethods = request.AssessmentMethods;
            unit.RecommendedTextbooks = request.RecommendedTextbooks;
            unit.IsActive = request.IsActive;

            _unitRepository.Update(unit);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Unit", "Update", unit.Id, null, $"Unit: {unit.Code}");

            _logger.LogInformation("Unit updated: {UnitCode}", unit.Code);

            return new UnitDto
            {
                Id = unit.Id,
                Name = unit.Name,
                Code = unit.Code,
                Description = unit.Description,
                Credits = unit.Credits,
                ContactHours = unit.ContactHours,
                IsActive = unit.IsActive,
                CourseId = unit.CourseId,
                CourseName = course.Name,
                CourseCode = course.Code,
                PrerequisiteUnitId = unit.PrerequisiteUnitId,
                PrerequisiteCode = unit.Prerequisite?.Code,
                PrerequisiteName = unit.Prerequisite?.Name,
                CreatedDate = unit.CreatedDate
            };
        }
    }
}