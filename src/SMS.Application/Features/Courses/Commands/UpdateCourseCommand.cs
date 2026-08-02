using FluentValidation;
using SMS.Shared.DTOs;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Courses.Commands
{
    public class UpdateCourseCommand : IRequest<CourseDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Duration { get; set; }
        public int TotalCredits { get; set; }
        public Guid DepartmentId { get; set; }
        public string? AdmissionRequirements { get; set; }
        public string? Objectives { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
    {
        public UpdateCourseCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Course ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Course name is required")
                .MaximumLength(100);

            RuleFor(x => x.Duration)
                .GreaterThan(0).WithMessage("Duration must be greater than 0");

            RuleFor(x => x.TotalCredits)
                .GreaterThan(0).WithMessage("Total credits must be greater than 0");

            RuleFor(x => x.DepartmentId)
                .NotEmpty().WithMessage("Department ID is required");
        }
    }

    public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, CourseDto>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateCourseCommandHandler> _logger;

        public UpdateCourseCommandHandler(
            ICourseRepository courseRepository,
            IDepartmentRepository departmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateCourseCommandHandler> logger)
        {
            _courseRepository = courseRepository;
            _departmentRepository = departmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CourseDto> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
        {
            var course = await _courseRepository.GetByIdAsync(request.Id, cancellationToken);
            if (course == null)
            {
                throw new NotFoundException("Course", request.Id);
            }

            var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
            if (department == null)
            {
                throw new NotFoundException("Department", request.DepartmentId);
            }

            course.Name = request.Name;
            course.Description = request.Description;
            course.Duration = request.Duration;
            course.TotalCredits = request.TotalCredits;
            course.DepartmentId = request.DepartmentId;
            course.AdmissionRequirements = request.AdmissionRequirements;
            course.Objectives = request.Objectives;
            course.IsActive = request.IsActive;

            await _courseRepository.UpdateAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("Course", "Update", course.Id.ToString(), "Update-Course");

            _logger.LogInformation("Course updated: {CourseCode}", course.Code);

            return new CourseDto
            {
                Id = course.Id,
                Name = course.Name,
                Code = course.Code,
                Description = course.Description,
                Duration = course.Duration,
                TotalCredits = course.TotalCredits,
                IsActive = course.IsActive,
                DepartmentId = course.DepartmentId,
                DepartmentName = department.Name,
                DepartmentCode = department.Code,
                CreatedDate = course.CreatedDate ?? DateTime.UtcNow
            };
        }
    }
}





