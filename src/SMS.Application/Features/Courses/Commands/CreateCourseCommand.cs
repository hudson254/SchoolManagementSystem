using FluentValidation;
using SMS.Shared.DTOs;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Courses.Commands
{
    public class CreateCourseCommand : IRequest<CourseDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Duration { get; set; } = 48;
        public int TotalCredits { get; set; }
        public Guid DepartmentId { get; set; }
        public string? AdmissionRequirements { get; set; }
        public string? Objectives { get; set; }
    }

    public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
    {
        public CreateCourseCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Course name is required")
                .MaximumLength(100);

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Course code is required")
                .MaximumLength(20)
                .Matches(@"^[A-Z0-9]+$").WithMessage("Course code must contain only uppercase letters and numbers");

            RuleFor(x => x.Duration)
                .GreaterThan(0).WithMessage("Duration must be greater than 0");

            RuleFor(x => x.TotalCredits)
                .GreaterThan(0).WithMessage("Total credits must be greater than 0");

            RuleFor(x => x.DepartmentId)
                .NotEmpty().WithMessage("Department ID is required");
        }
    }

    public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CourseDto>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateCourseCommandHandler> _logger;

        public CreateCourseCommandHandler(
            ICourseRepository courseRepository,
            IDepartmentRepository departmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateCourseCommandHandler> logger)
        {
            _courseRepository = courseRepository;
            _departmentRepository = departmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CourseDto> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
        {
            var existingCourse = await _courseRepository.GetByCodeAsync(request.Code, cancellationToken);
            if (existingCourse != null)
            {
                throw new ConflictException("Course", "Code", request.Code);
            }

            var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
            if (department == null)
            {
                throw new NotFoundException("Department", request.DepartmentId);
            }

            var course = new Course
            {
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                Duration = request.Duration,
                TotalCredits = request.TotalCredits,
                DepartmentId = request.DepartmentId,
                AdmissionRequirements = request.AdmissionRequirements,
                Objectives = request.Objectives,
                IsActive = true
            };

            await _courseRepository.AddAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Course", "Create", course.Id.ToString());

            _logger.LogInformation("Course created: {CourseCode}", course.Code);

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





