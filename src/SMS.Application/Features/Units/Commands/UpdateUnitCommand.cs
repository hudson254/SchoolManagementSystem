using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Units.Commands
{
    public class UpdateUnitCommand : IRequest<UnitDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; } = 3;
        public int ContactHours { get; set; } = 3;
        public int Semester { get; set; } = 1;
        public Guid CourseId { get; set; }
        public Guid? PrerequisiteUnitId { get; set; }
        public string? LearningOutcomes { get; set; }
        public string? AssessmentMethods { get; set; }
        public string? RecommendedTextbooks { get; set; }
        public bool IsActive { get; set; } = true;
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

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Unit code is required")
                .MaximumLength(20)
                .Matches(@"^[A-Z0-9]+$").WithMessage("Unit code must contain only uppercase letters and numbers");

            RuleFor(x => x.Credits)
                .GreaterThan(0).WithMessage("Credits must be greater than 0")
                .LessThanOrEqualTo(6).WithMessage("Credits cannot exceed 6");

            RuleFor(x => x.ContactHours)
                .GreaterThan(0).WithMessage("Contact hours must be greater than 0");

            RuleFor(x => x.Semester)
                .GreaterThan(0).WithMessage("Semester must be greater than 0");

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
            // Get existing unit
            var unit = await _unitRepository.GetUnitWithDetailsAsync(request.Id, cancellationToken);
            if (unit == null)
            {
                throw new NotFoundException("Unit", request.Id);
            }

            // Check if code is being changed and if it already exists
            if (unit.Code != request.Code)
            {
                var existingUnit = await _unitRepository.GetByCodeAsync(request.Code, cancellationToken);
                if (existingUnit != null && existingUnit.Id != request.Id)
                {
                    throw new ConflictException("Unit", "Code", request.Code);
                }
            }

            // Validate course exists
            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null)
            {
                throw new NotFoundException("Course", request.CourseId);
            }

            // Validate prerequisite unit exists if provided
            if (request.PrerequisiteUnitId.HasValue)
            {
                var prerequisite = await _unitRepository.GetByIdAsync(request.PrerequisiteUnitId.Value, cancellationToken);
                if (prerequisite == null)
                {
                    throw new NotFoundException("Prerequisite Unit", request.PrerequisiteUnitId.Value);
                }
            }

            // Update the unit
            unit.Name = request.Name;
            unit.Code = request.Code;
            unit.Description = request.Description;
            unit.Credits = request.Credits;
            unit.ContactHours = request.ContactHours;
            unit.Semester = request.Semester;
            unit.CourseId = request.CourseId;
            unit.PrerequisiteUnitId = request.PrerequisiteUnitId;
            unit.LearningOutcomes = request.LearningOutcomes;
            unit.AssessmentMethods = request.AssessmentMethods;
            unit.RecommendedTextbooks = request.RecommendedTextbooks;
            unit.IsActive = request.IsActive;
            unit.UpdatedAt = DateTime.UtcNow;

            await _unitRepository.UpdateAsync(unit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log the update
            await _auditService.LogAsync("UpdateUnit", unit.Id.ToString(), $"Unit updated: {unit.Code}");

            _logger.LogInformation("Unit updated: {UnitCode}", unit.Code);

            // Return the DTO
            return new UnitDto
            {
                Id = unit.Id,
                Name = unit.Name,
                Code = unit.Code,
                Description = unit.Description,
                Credits = unit.Credits,
                ContactHours = unit.ContactHours,
                IsActive = unit.IsActive,
                Semester = unit.Semester,
                CourseId = unit.CourseId,
                CourseName = course.Name,
                CourseCode = course.Code,
                PrerequisiteUnitId = unit.PrerequisiteUnitId,
                CreatedDate = unit.CreatedAt
            };
        }
    }
}

