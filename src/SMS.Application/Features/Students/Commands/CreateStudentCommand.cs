using MediatR;
using FluentValidation;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Identity.Services;

namespace SMS.Application.Features.Students.Commands
{
    public class CreateStudentCommand : IRequest<StudentDto>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public Guid? ProgrammeId { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
    }

    public class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
    {
        public CreateStudentCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .MaximumLength(20);

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required")
                .LessThan(DateTime.UtcNow.AddYears(-16))
                .WithMessage("Student must be at least 16 years old");
        }
    }

    public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, StudentDto>
    {
        private readonly IUserManagerService _userManager;
        private readonly IStudentRepository _studentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateStudentCommandHandler> _logger;

        public CreateStudentCommandHandler(
            IUserManagerService userManager,
            IStudentRepository studentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateStudentCommandHandler> logger)
        {
            _userManager = userManager;
            _studentRepository = studentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<StudentDto> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ConflictException("Student", "Email", request.Email);
            }

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.Email,
                PhoneNumber = request.PhoneNumber,
                Organization = request.Organization,
                IsActive = true,
                IsEmailVerified = false
            };

            var createResult = await _userManager.CreateUserAsync(user, "DefaultPassword123!");
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new ValidationException($"User creation failed: {errors}");
            }

            await _userManager.AddToRoleAsync(user, "Student");

            var student = new Student
            {
                UserId = user.Id,
                StudentNumber = GenerateStudentNumber(),
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Address = request.Address,
                ProgrammeId = request.ProgrammeId,
                EnrollmentDate = DateTime.UtcNow,
                IsEnrolled = true,
                AcademicStatus = "Active",
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone,
                EmergencyContactRelation = request.EmergencyContactRelation
            };

            await _studentRepository.AddAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Student", "Create", student.Id, null, $"Student Number: {student.StudentNumber}");

            _logger.LogInformation("Student created: {StudentNumber} - {Email}", student.StudentNumber, request.Email);

            return new StudentDto
            {
                Id = student.Id,
                UserId = user.Id,
                StudentNumber = student.StudentNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                Address = student.Address,
                EnrollmentDate = student.EnrollmentDate,
                ProgrammeId = student.ProgrammeId,
                AcademicStatus = student.AcademicStatus,
                IsEnrolled = student.IsEnrolled,
                CreatedDate = student.CreatedDate
            };
        }

        private string GenerateStudentNumber()
        {
            var year = DateTime.UtcNow.Year;
            var sequence = DateTime.UtcNow.Ticks.ToString().Substring(10, 5);
            return $"STU-{year}-{sequence}";
        }
    }
}