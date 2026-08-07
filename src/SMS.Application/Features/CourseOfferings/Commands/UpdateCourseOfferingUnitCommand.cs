using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Commands
{
    public class UpdateCourseOfferingUnitCommand : IRequest<CourseOfferingUnitDto>
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public int? Credits { get; set; }
        public int? ContactHours { get; set; }
        public int? Order { get; set; }
        public string? LearningOutcomes { get; set; }
        public string? AssessmentMethods { get; set; }
        public string? AssessmentWeighting { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateCourseOfferingUnitCommandValidator : AbstractValidator<UpdateCourseOfferingUnitCommand>
    {
        public UpdateCourseOfferingUnitCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Course Offering Unit ID is required");

            When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
            {
                RuleFor(x => x.Name).MaximumLength(200).WithMessage("Unit name must not exceed 200 characters");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Code), () =>
            {
                RuleFor(x => x.Code).MaximumLength(50).WithMessage("Unit code must not exceed 50 characters");
            });
        }
    }

    public class UpdateCourseOfferingUnitCommandHandler
        : IRequestHandler<UpdateCourseOfferingUnitCommand, CourseOfferingUnitDto>
    {
        private readonly ICourseOfferingUnitRepository _courseOfferingUnitRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateCourseOfferingUnitCommandHandler> _logger;

        public UpdateCourseOfferingUnitCommandHandler(
            ICourseOfferingUnitRepository courseOfferingUnitRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateCourseOfferingUnitCommandHandler> logger)
        {
            _courseOfferingUnitRepository = courseOfferingUnitRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CourseOfferingUnitDto> Handle(
            UpdateCourseOfferingUnitCommand request,
            CancellationToken cancellationToken)
        {
            var unit = await _courseOfferingUnitRepository.GetByIdAsync(request.Id, cancellationToken);
            if (unit == null)
                throw new NotFoundException("CourseOfferingUnit", request.Id);

            if (request.Name != null) unit.Name = request.Name;
            if (request.Code != null) unit.Code = request.Code;
            if (request.Description != null) unit.Description = request.Description;
            if (request.Credits.HasValue) unit.Credits = request.Credits.Value;
            if (request.ContactHours.HasValue) unit.ContactHours = request.ContactHours.Value;
            if (request.Order.HasValue) unit.Order = request.Order.Value;
            if (request.LearningOutcomes != null) unit.LearningOutcomes = request.LearningOutcomes;
            if (request.AssessmentMethods != null) unit.AssessmentMethods = request.AssessmentMethods;
            if (request.AssessmentWeighting != null) unit.AssessmentWeighting = request.AssessmentWeighting;
            unit.IsActive = request.IsActive;

            await _courseOfferingUnitRepository.UpdateAsync(unit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("CourseOfferingUnit", "Update", unit.Id.ToString());

            _logger.LogInformation("Unit {UnitCode} updated for offering {OfferingId}", unit.Code, unit.CourseOfferingId);

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
