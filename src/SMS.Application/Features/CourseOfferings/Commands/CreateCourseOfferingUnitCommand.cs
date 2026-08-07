using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Commands
{
    public class CreateCourseOfferingUnitCommand : IRequest<CourseOfferingUnitDto>
    {
        public Guid CourseOfferingId { get; set; }
        public Guid? UnitId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; }
        public int ContactHours { get; set; }
        public int Order { get; set; }
        public string? LearningOutcomes { get; set; }
        public string? AssessmentMethods { get; set; }
        public string? AssessmentWeighting { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CreateCourseOfferingUnitCommandValidator : AbstractValidator<CreateCourseOfferingUnitCommand>
    {
        public CreateCourseOfferingUnitCommandValidator()
        {
            RuleFor(x => x.CourseOfferingId)
                .NotEmpty().WithMessage("Course Offering ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Unit name is required")
                .MaximumLength(200).WithMessage("Unit name must not exceed 200 characters");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Unit code is required")
                .MaximumLength(50).WithMessage("Unit code must not exceed 50 characters");

            RuleFor(x => x.Credits)
                .GreaterThanOrEqualTo(0).WithMessage("Credits must be zero or greater");

            RuleFor(x => x.ContactHours)
                .GreaterThanOrEqualTo(0).WithMessage("Contact hours must be zero or greater");
        }
    }

    public class CreateCourseOfferingUnitCommandHandler
        : IRequestHandler<CreateCourseOfferingUnitCommand, CourseOfferingUnitDto>
    {
        private readonly ICourseOfferingUnitRepository _courseOfferingUnitRepository;
        private readonly ICourseOfferingRepository _courseOfferingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateCourseOfferingUnitCommandHandler> _logger;

        public CreateCourseOfferingUnitCommandHandler(
            ICourseOfferingUnitRepository courseOfferingUnitRepository,
            ICourseOfferingRepository courseOfferingRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateCourseOfferingUnitCommandHandler> logger)
        {
            _courseOfferingUnitRepository = courseOfferingUnitRepository;
            _courseOfferingRepository = courseOfferingRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CourseOfferingUnitDto> Handle(
            CreateCourseOfferingUnitCommand request,
            CancellationToken cancellationToken)
        {
            var offering = await _courseOfferingRepository.GetByIdAsync(request.CourseOfferingId, cancellationToken);
            if (offering == null)
                throw new NotFoundException("CourseOffering", request.CourseOfferingId);

            var unit = new CourseOfferingUnit
            {
                CourseOfferingId = request.CourseOfferingId,
                UnitId = request.UnitId,
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                Credits = request.Credits,
                ContactHours = request.ContactHours,
                Order = request.Order,
                LearningOutcomes = request.LearningOutcomes,
                AssessmentMethods = request.AssessmentMethods,
                AssessmentWeighting = request.AssessmentWeighting,
                IsActive = request.IsActive
            };

            await _courseOfferingUnitRepository.AddAsync(unit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("CourseOfferingUnit", "Create", unit.Id.ToString());

            _logger.LogInformation("Unit {UnitCode} added to offering {OfferingCode}", unit.Code, offering.OfferingCode);

            return new CourseOfferingUnitDto
            {
                Id = unit.Id,
                CourseOfferingId = unit.CourseOfferingId,
                UnitId = unit.UnitId,
                Name = unit.Name,
                Code = unit.Code,
                Description = unit.Description,
                Credits = unit.Credits,
                ContactHours = unit.ContactHours,
                Order = unit.Order,
                LearningOutcomes = unit.LearningOutcomes,
                AssessmentMethods = unit.AssessmentMethods,
                AssessmentWeighting = unit.AssessmentWeighting,
                IsActive = unit.IsActive
            };
        }
    }
}
