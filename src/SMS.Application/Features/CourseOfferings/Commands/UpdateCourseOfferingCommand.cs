using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Commands
{
    public class UpdateCourseOfferingCommand : IRequest<CourseOfferingDto>
    {
        public Guid Id { get; set; }
        public string? Intake { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }
        public CourseOfferingStatus Status { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class UpdateCourseOfferingCommandValidator : AbstractValidator<UpdateCourseOfferingCommand>
    {
        public UpdateCourseOfferingCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Course Offering ID is required");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required")
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");
        }
    }

    public class UpdateCourseOfferingCommandHandler : IRequestHandler<UpdateCourseOfferingCommand, CourseOfferingDto>
    {
        private readonly ICourseOfferingRepository _courseOfferingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateCourseOfferingCommandHandler> _logger;

        public UpdateCourseOfferingCommandHandler(
            ICourseOfferingRepository courseOfferingRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateCourseOfferingCommandHandler> logger)
        {
            _courseOfferingRepository = courseOfferingRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CourseOfferingDto> Handle(UpdateCourseOfferingCommand request, CancellationToken cancellationToken)
        {
            var offering = await _courseOfferingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (offering == null)
                throw new NotFoundException("CourseOffering", request.Id);

            offering.Intake = request.Intake;
            offering.StartDate = request.StartDate;
            offering.EndDate = request.EndDate;
            offering.RegistrationStartDate = request.RegistrationStartDate;
            offering.RegistrationEndDate = request.RegistrationEndDate;
            offering.Status = request.Status;
            offering.IsActive = request.IsActive;
            offering.Notes = request.Notes;

            await _courseOfferingRepository.UpdateAsync(offering, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("CourseOffering", "Update", offering.Id.ToString());

            _logger.LogInformation("Course offering updated: {OfferingCode}", offering.OfferingCode);

            return new CourseOfferingDto
            {
                Id = offering.Id,
                OfferingCode = offering.OfferingCode,
                CourseId = offering.CourseId,
                CourseName = offering.Course?.Name,
                CourseCode = offering.Course?.Code,
                AcademicYearId = offering.AcademicYearId,
                AcademicYearName = offering.AcademicYear?.Name,
                SemesterId = offering.SemesterId,
                SemesterName = offering.Semester?.Name,
                Intake = offering.Intake,
                StartDate = offering.StartDate,
                EndDate = offering.EndDate,
                RegistrationStartDate = offering.RegistrationStartDate,
                RegistrationEndDate = offering.RegistrationEndDate,
                Status = offering.Status,
                IsActive = offering.IsActive,
                Notes = offering.Notes,
                TotalUnits = offering.Units?.Count ?? 0,
                TotalEnrollments = offering.Enrollments?.Count ?? 0,
                TotalLecturers = offering.Lecturers?.Count ?? 0,
                CreatedDate = offering.CreatedDate ?? DateTime.UtcNow
            };
        }
    }
}
