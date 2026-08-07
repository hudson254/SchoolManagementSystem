using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;


namespace SMS.Application.Features.Auth.Commands
{
    public class RegisterCommand : IRequest<AuthResponseDto>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public string? Organization { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Username { get; set; }
        public Guid? CourseId { get; set; }
        public string? Specialization { get; set; }
    }

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email is required")
                .MaximumLength(200).WithMessage("Email must not exceed 200 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(12).WithMessage("Password must be at least 12 characters")
                .Must(p => p.Any(char.IsUpper)).WithMessage("Password must contain an uppercase letter")
                .Must(p => p.Any(char.IsLower)).WithMessage("Password must contain a lowercase letter")
                .Must(p => p.Any(char.IsDigit)).WithMessage("Password must contain a number")
                .Must(p => p.Any(ch => !char.IsLetterOrDigit(ch))).WithMessage("Password must contain a special character");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.Password).WithMessage("Passwords do not match");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required")
                .Must(r => r == "Student" || r == "Lecturer").WithMessage("Role must be either Student or Lecturer");

            RuleFor(x => x.Organization)
                .NotEmpty().WithMessage("Organization / Institution is required")
                .MaximumLength(200).WithMessage("Organization must not exceed 200 characters");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^[+]?[0-9\s\-\(\)]{7,20}$").WithMessage("A valid phone number is required")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.Username)
                .Matches(@"^[a-z0-9]+$").WithMessage("Username may only contain lowercase letters and numbers")
                .MaximumLength(50).WithMessage("Username must not exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Username));

            // Course is required for students; specialization is required for lecturers.
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Please select a course")
                .When(x => x.Role == "Student");

            RuleFor(x => x.Specialization)
                .NotEmpty().WithMessage("Specialization is required")
                .MaximumLength(200).WithMessage("Specialization must not exceed 200 characters")
                .When(x => x.Role == "Lecturer");
        }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
    {
        /// <summary>
        /// The only roles ever assigned during public self-registration.
        /// Client-supplied roles are validated against this allow-list to
        /// prevent privilege escalation (previously an attacker could register
        /// with Role="Administrator").
        /// </summary>
        private static readonly HashSet<string> AllowedSelfRegistrationRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Student",
            "Lecturer"
        };

        private readonly IUserManagerService _userManagerService;
        private readonly IJwtService _jwtService;
        private readonly IAuditService _auditService;
        private readonly ILogger<RegisterCommandHandler> _logger;
        private readonly SMS.Application.Common.Interfaces.IUsernameGenerator _usernameGenerator;
        private readonly IStudentRepository _studentRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IUnitAllocationRepository _unitAllocationRepository;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterCommandHandler(
            IUserManagerService userManagerService,
            IJwtService jwtService,
            IAuditService auditService,
            ILogger<RegisterCommandHandler> logger,
            SMS.Application.Common.Interfaces.IUsernameGenerator usernameGenerator,
            IStudentRepository studentRepository,
            ILecturerRepository lecturerRepository,
            ICourseRepository courseRepository,
            IUnitRepository unitRepository,
            IUnitAllocationRepository unitAllocationRepository,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext,
            IUnitOfWork unitOfWork)
        {
            _userManagerService = userManagerService;
            _jwtService = jwtService;
            _auditService = auditService;
            _logger = logger;
            _usernameGenerator = usernameGenerator;
            _studentRepository = studentRepository;
            _lecturerRepository = lecturerRepository;
            _courseRepository = courseRepository;
            _unitRepository = unitRepository;
            _unitAllocationRepository = unitAllocationRepository;
            _tenantContext = tenantContext;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            // Validate passwords match
            if (request.Password != request.ConfirmPassword)
            {
                throw new SMS.Application.Exceptions.ValidationException("Passwords do not match");
            }

            // Validate role is in the allow-list (server-side security check).
            if (!AllowedSelfRegistrationRoles.Contains(request.Role))
            {
                throw new SMS.Application.Exceptions.ValidationException("Invalid registration role");
            }

            // Check if user already exists
            var existingUser = await _userManagerService.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ConflictException("User with this email already exists");
            }

            // Generate a unique username if one was not supplied.
            var username = request.Username;
            if (string.IsNullOrWhiteSpace(username))
            {
                username = await _usernameGenerator.GenerateUsernameAsync(request.FirstName, request.LastName);
            }
            else
            {
                // Re-validate the manually supplied username is unique.
                var isAvailable = await _usernameGenerator.IsUsernameAvailableAsync(username);
                if (!isAvailable)
                {
                    throw new ConflictException("Username is already taken");
                }
            }

            // Create user with the validated, allow-listed role.
            var user = await _userManagerService.CreateUserAsync(
                username,
                request.Email,
                request.Password,
                request.Role);

            if (user == null)
            {
                throw new ExternalServiceException("User creation service returned null");
            }

            var typedUser = (User)user;

            // Sync the User's profile fields with the Student/Lecturer entity.
            typedUser.FirstName = request.FirstName;
            typedUser.LastName = request.LastName;
            typedUser.PhoneNumber = request.PhoneNumber;
            typedUser.Organization = request.Organization;
            await _userManagerService.UpdateUserAsync(typedUser);

            // Create the role-specific record (Student or Lecturer).
            if (request.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                await CreateStudentRecord(request, typedUser, cancellationToken);
            }
            else if (request.Role.Equals("Lecturer", StringComparison.OrdinalIgnoreCase))
            {
                await CreateLecturerRecord(request, typedUser, cancellationToken);
            }

            // Get user roles
            var roles = await _userManagerService.GetRolesAsync(typedUser);
            var rolesList = roles?.ToList() ?? new List<string>();

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(typedUser.Id.ToString(), typedUser.Email ?? typedUser.UserName, rolesList);
            var refreshToken = await _userManagerService.GenerateRefreshTokenAsync(typedUser.Id.ToString());

            // Log successful registration
            await _auditService.LogAsync("Register", typedUser.Id.ToString(), $"User registered successfully as {request.Role}");

            _logger.LogInformation("User registered successfully: {Email} as {Role}", request.Email, request.Role);

            // Return response
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600,
                TokenType = "Bearer",
                UserId = typedUser.Id.ToString(),
                Email = typedUser.Email,
                Username = typedUser.UserName,
                Roles = rolesList
            };
        }

        private async Task CreateStudentRecord(RegisterCommand request, User user, CancellationToken cancellationToken)
        {
            // Validate the selected course exists and is active.
            if (!request.CourseId.HasValue)
                throw new SMS.Application.Exceptions.ValidationException("Course selection is required for student registration");

            var course = await _courseRepository.GetByIdAsync(request.CourseId.Value, cancellationToken);
            if (course == null || !course.IsActive)
                throw new NotFoundException("Course", request.CourseId.Value);

            // Create the student record.
            var student = new Student
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                StudentNumber = $"STU{DateTime.UtcNow:yyyyMMdd}{new Random().Next(1000, 9999)}",
                UserId = user.Id,
                ProgrammeId = course.ProgrammeId,
                IsActive = true,
                IsEnrolled = true,
                EnrollmentDate = DateTime.UtcNow,
                TenantId = Guid.Parse(_tenantContext.TenantId)
            };

            await _studentRepository.AddAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Automatically enroll the student in all active units of the course.
            var units = await _unitRepository.GetUnitsByCourseIdAsync(course.Id, cancellationToken);
            foreach (var unit in units.Where(u => u.IsActive))
            {
                var enrollment = new Enrollment
                {
                    StudentId = student.Id,
                    CourseId = course.Id,
                    UnitId = unit.Id,
                    SemesterId = course.SemesterId,
                    EnrollmentDate = DateTime.UtcNow,
                    Status = "Active",
                    IsActive = true,
                    TenantId = Guid.Parse(_tenantContext.TenantId)
                };
                // Enrollment is added via the Student's collection to preserve
                // referential integrity and avoid duplicate-unit enforcement.
                student.Enrollments.Add(enrollment);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Enroll", student.Id.ToString(), $"Student enrolled in course {course.Code} with {student.Enrollments.Count} units");
        }

        private async Task CreateLecturerRecord(RegisterCommand request, User user, CancellationToken cancellationToken)
        {
            // Specialization is required for lecturers.
            if (string.IsNullOrWhiteSpace(request.Specialization))
                throw new SMS.Application.Exceptions.ValidationException("Specialization is required for lecturer registration");

            // Create the lecturer record.
            var lecturer = new Lecturer
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                EmployeeNumber = $"LEC{DateTime.UtcNow:yyyyMMdd}{new Random().Next(1000, 9999)}",
                IsActive = true,
                UserId = user.Id.ToString(),
                HireDate = DateTime.UtcNow,
                TenantId = Guid.Parse(_tenantContext.TenantId)
            };

            await _lecturerRepository.AddAsync(lecturer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // If a course was selected, automatically assign the lecturer to
            // all active units of that course (teaching assignment).
            if (request.CourseId.HasValue)
            {
                var course = await _courseRepository.GetByIdAsync(request.CourseId.Value, cancellationToken);
                if (course != null && course.IsActive)
                {
                    var units = await _unitRepository.GetUnitsByCourseIdAsync(course.Id, cancellationToken);
                    foreach (var unit in units.Where(u => u.IsActive))
                    {
                        var allocation = new UnitAllocation
                        {
                            LecturerId = lecturer.Id,
                            UnitId = unit.Id,
                            SemesterId = course.SemesterId ?? Guid.Empty,
                            AllocationDate = DateTime.UtcNow,
                            Status = "Active",
                            IsPrimary = true
                        };
                        await _unitAllocationRepository.AddAsync(allocation, cancellationToken);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    await _auditService.LogAsync("Allocate", lecturer.Id.ToString(), $"Lecturer assigned to {units.Count(u => u.IsActive)} units of course {course.Code}");
                }
            }
        }
    }
}
