using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Commands
{
    public class CreateCourseOfferingCommand : IRequest<CourseOfferingDto>
    {
        public Guid CourseId { get; set; }
        public Guid AcademicYearId { get; set; }
        public Guid SemesterId { get; set; }
        public string? Intake { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }
        public CourseOfferingStatus Status { get; set; } = CourseOfferingStatus.Draft;
        public string? Notes { get; set; }
    }

    public class CreateCourseOfferingCommandValidator : AbstractValidator<CreateCourseOfferingCommand>
    {
        public CreateCourseOfferingCommandValidator()
        {
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Course ID is required");

            RuleFor(x => x.AcademicYearId)
                .NotEmpty().WithMessage("Academic Year ID is required");

            RuleFor(x => x.SemesterId)
                .NotEmpty().WithMessage("Semester ID is required");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required")
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");
        }
    }

    public class CreateCourseOfferingCommandHandler : IRequestHandler<CreateCourseOfferingCommand, CourseOfferingDto>
    {
        private readonly ICourseOfferingRepository _courseOfferingRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateCourseOfferingCommandHandler> _logger;

        public CreateCourseOfferingCommandHandler(
            ICourseOfferingRepository courseOfferingRepository,
            ICourseRepository courseRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateCourseOfferingCommandHandler> logger)
        {
            _courseOfferingRepository = courseOfferingRepository;
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CourseOfferingDto> Handle(CreateCourseOfferingCommand request, CancellationToken cancellationToken)
        {
            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null)
                throw new NotFoundException("Course", request.CourseId);

            // Generate the next sequence number for this course in the given academic year/semester
            var sequence = await _courseOfferingRepository.GetNextSequenceForCourseAsync(
                request.CourseId,
                request.StartDate.Year,
                request.SemesterId.GetHashCode(),
                cancellationToken);

            var offeringCode = await _courseOfferingRepository.GenerateOfferingCodeAsync(
                course.Code,
                request.StartDate.Year,
                1,
                sequence,
                cancellationToken);

            var offering = new CourseOffering
            {
                // Explicitly assign a new Guid to guarantee a non-empty Id.
                // BaseEntity initialises Id with Guid.NewGuid(), but we make it
                // explicit here so the returned DTO (and any subsequent requests
                // that reference this offering) always has a valid identifier.
                Id = Guid.NewGuid(),
                OfferingCode = offeringCode,
                CourseId = request.CourseId,
                AcademicYearId = request.AcademicYearId,
                SemesterId = request.SemesterId,
                Intake = request.Intake,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                RegistrationStartDate = request.RegistrationStartDate,
                RegistrationEndDate = request.RegistrationEndDate,
                Status = request.Status,
                IsActive = true,
                Notes = request.Notes
            };

            await _courseOfferingRepository.AddAsync(offering, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("CourseOffering", "Create", offering.Id.ToString());

            _logger.LogInformation("Course offering created: Code {OfferingCode} for Course {CourseId}", offeringCode, request.CourseId);

            return new CourseOfferingDto
            {
                Id = offering.Id,
                OfferingCode = offering.OfferingCode,
                CourseId = offering.CourseId,
                AcademicYearId = offering.AcademicYearId,
                SemesterId = offering.SemesterId,
                Intake = offering.Intake,
                StartDate = offering.StartDate,
                EndDate = offering.EndDate,
                RegistrationStartDate = offering.RegistrationStartDate,
                RegistrationEndDate = offering.RegistrationEndDate,
                Status = offering.Status,
                IsActive = offering.IsActive,
                Notes = offering.Notes,
                CreatedDate = offering.CreatedDate ?? DateTime.UtcNow
            };
        }
    }
}
