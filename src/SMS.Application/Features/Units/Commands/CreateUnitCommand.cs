using MediatR;
using FluentValidation;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Units.Commands
{
    public class CreateUnitCommand : IRequest<UnitDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; } = 3;
        public int ContactHours { get; set; } = 3;
        public Guid CourseId { get; set; }
        public Guid? PrerequisiteUnitId { get; set; }
        public string? LearningOutcomes { get; set; }
        public string? AssessmentMethods { get; set; }
        public string? RecommendedTextbooks { get; set; }
    }

    public class CreateUnitCommandValidator : AbstractValidator<CreateUnitCommand>
    {
        public CreateUnitCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Unit name is required")
                .MaximumLength(100);

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Unit code is required")
                .MaximumLength(20)
                .Matches(@"^[A-Z0-9]+$").WithMessage("Unit code must contain only uppercase letters and numbers");

            RuleFor(x => x.Credits)
                .GreaterThan(0).WithMessage("Credits must be greater than 0")
                .LessThanOrEqualTo(6).WithMessage("Credits cannot exceed 6");

            RuleFor(x => x.ContactHours)
                .GreaterThan(0).WithMessage("Contact hours must be greater than 0");

            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Course ID is required");
        }
    }

    public class CreateUnitCommandHandler : IRequestHandler<CreateUnitCommand, UnitDto>
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateUnitCommandHandler> _logger;

        public CreateUnitCommandHandler(
            IUnitRepository unitRepository,
            ICourseRepository courseRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateUnitCommandHandler> logger)
        {
            _unitRepository = unitRepository;
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<UnitDto> Handle(CreateUnitCommand request, CancellationToken cancellationToken)
        {
            var existingUnit = await _unitRepository.GetByCodeAsync(request.Code, cancellationToken);
            if (existingUnit != null)
            {
                throw new ConflictException("Unit", "Code", request.Code);
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

            var unit = new Unit
            {
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                Credits = request.Credits,
                ContactHours = request.ContactHours,
                CourseId = request.CourseId,
                PrerequisiteUnitId = request.PrerequisiteUnitId,
                LearningOutcomes = request.LearningOutcomes,
                AssessmentMethods = request.AssessmentMethods,
                RecommendedTextbooks = request.RecommendedTextbooks,
                IsActive = true
            };

            await _unitRepository.AddAsync(unit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Unit", "Create", unit.Id, null, $"Unit: {unit.Code}");

            _logger.LogInformation("Unit created: {UnitCode}", unit.Code);

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