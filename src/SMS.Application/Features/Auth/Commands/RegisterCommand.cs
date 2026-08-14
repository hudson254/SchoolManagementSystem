using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Application.Services;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
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
        public string? Title { get; set; }
        public string Role { get; set; } = "Student";

        public string? Organization { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Username { get; set; }
        public Guid? CourseId { get; set; }
        public string? Specialization { get; set; }

        /// <summary>
        /// Staff ID / Establishment Number. Preserves leading zeros.
        /// </summary>
        public string? StaffIdEstNo { get; set; }

        /// <summary>
        /// National ID or Passport Number. Alphanumeric, preserves leading zeros.
        /// </summary>
        public string? NationalIdPassport { get; set; }
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

            // Staff Id / Est No. validation
            RuleFor(x => x.StaffIdEstNo)
                .MaximumLength(50).WithMessage("Staff Id / Est No. must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9\-\/]+$").WithMessage("Staff Id / Est No. contains invalid characters")
                .When(x => !string.IsNullOrEmpty(x.StaffIdEstNo));

            // National ID / Passport No. validation
            RuleFor(x => x.NationalIdPassport)
                .MaximumLength(50).WithMessage("National ID / Passport No. must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9]+$").WithMessage("National ID / Passport No. must be alphanumeric")
                .When(x => !string.IsNullOrEmpty(x.NationalIdPassport));
        }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
    {
        private static readonly HashSet<string> AllowedSelfRegistrationRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Student",
            "Lecturer"
        };

        private readonly IUserManagerService _userManagerService;
        private readonly IJwtService _jwtService;
        private readonly IAuditService _auditService;
        private readonly ILogger<RegisterCommandHandler> _logger;
        private readonly IUsernameGenerator _usernameGenerator;
        private readonly INameParser _nameParser;
        private readonly IStudentRepository _studentRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IUnitAllocationRepository _unitAllocationRepository;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordPolicyService _passwordPolicyService;

        public RegisterCommandHandler(
            IUserManagerService userManagerService,
            IJwtService jwtService,
            IAuditService auditService,
            ILogger<RegisterCommandHandler> logger,
            IUsernameGenerator usernameGenerator,
            INameParser nameParser,
            IStudentRepository studentRepository,
            ILecturerRepository lecturerRepository,
            ICourseRepository courseRepository,
            IUnitRepository unitRepository,
            IUnitAllocationRepository unitAllocationRepository,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext,
            IUnitOfWork unitOfWork,
            IPasswordPolicyService passwordPolicyService)
        {
            _userManagerService = userManagerService;
            _jwtService = jwtService;
            _auditService = auditService;
            _logger = logger;
            _usernameGenerator = usernameGenerator;
            _nameParser = nameParser;
            _studentRepository = studentRepository;
            _lecturerRepository = lecturerRepository;
            _courseRepository = courseRepository;
            _unitRepository = unitRepository;
            _unitAllocationRepository = unitAllocationRepository;
            _tenantContext = tenantContext;
            _unitOfWork = unitOfWork;
            _passwordPolicyService = passwordPolicyService;
        }

        public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (request.Password != request.ConfirmPassword)
                throw new SMS.Application.Exceptions.ValidationException("Passwords do not match");

            if (!AllowedSelfRegistrationRoles.Contains(request.Role))
                throw new SMS.Application.Exceptions.ValidationException("Invalid registration role");

            // Server-side password policy enforcement (authoritative).
            var policyErrors = _passwordPolicyService.Validate(
                request.Password,
                new PasswordPolicyContext
                {
                    Email = request.Email,
                    Username = request.Username,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Organization = request.Organization
                });
            if (policyErrors.Count > 0)
                throw new SMS.Application.Exceptions.ValidationException(policyErrors.First());

            var existingUser = await _userManagerService.FindByEmailAsync(request.Email);
            if (existingUser != null)
                throw new ConflictException("User with this email already exists");

            // Parse the full name to extract any title and normalize name parts.
            var fullName = $"{request.FirstName} {request.LastName}".Trim();
            var parsed = _nameParser.ParseName(fullName);

            var title = request.Title ?? parsed.Title;

            if (!parsed.IsValid)
                throw new SMS.Application.Exceptions.ValidationException(parsed.ErrorMessage ?? "Invalid name format");

            var username = request.Username;
            if (string.IsNullOrWhiteSpace(username))
            {
                username = await _usernameGenerator.GenerateUsernameAsync(parsed.FirstName, parsed.LastName);
            }
            else
            {
                var isAvailable = await _usernameGenerator.IsUsernameAvailableAsync(username);
                if (!isAvailable)
                    throw new ConflictException("Username is already taken");
            }

            var user = await _userManagerService.CreateUserAsync(username, request.Email, request.Password, request.Role);
            if (user == null)
                throw new ExternalServiceException("User creation service returned null");

            var typedUser = (User)user;
            typedUser.FirstName = parsed.FirstName;
            typedUser.LastName = parsed.LastName;
            typedUser.MiddleName = parsed.MiddleName;
            typedUser.Title = title;
            typedUser.PhoneNumber = request.PhoneNumber;
            typedUser.Organization = request.Organization;
            await _userManagerService.UpdateUserAsync(typedUser);

            if (request.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
                await CreateStudentRecord(request, typedUser, parsed, title, cancellationToken);
            else if (request.Role.Equals("Lecturer", StringComparison.OrdinalIgnoreCase))
                await CreateLecturerRecord(request, typedUser, parsed, title, cancellationToken);

            var roles = await _userManagerService.GetRolesAsync(typedUser);
            var rolesList = roles?.ToList() ?? new List<string>();

            var accessToken = _jwtService.GenerateAccessToken(typedUser.Id.ToString(), typedUser.Email ?? typedUser.UserName, rolesList);
            var refreshToken = await _userManagerService.GenerateRefreshTokenAsync(typedUser.Id.ToString());

            await _auditService.LogAsync("Register", typedUser.Id.ToString(), $"User registered successfully as {request.Role} (pending course selection)");
            _logger.LogInformation("User registered successfully: {Email} as {Role} (pending course selection)", request.Email, request.Role);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600,
                TokenType = "Bearer",
                UserId = typedUser.Id.ToString(),
                Email = typedUser.Email,
                Username = typedUser.UserName,
                FullName = string.IsNullOrWhiteSpace(title)
                    ? $"{typedUser.FirstName} {typedUser.LastName}".Trim()
                    : $"{title} {typedUser.FirstName} {typedUser.LastName}".Trim(),
                FirstName = typedUser.FirstName,
                LastName = typedUser.LastName,
                Title = title,
                Roles = rolesList,
                RegistrationStatus = RegistrationStatus.PendingCourseSelection.ToString()
            };
        }

        private async Task CreateStudentRecord(RegisterCommand request, User user, NameParseResult parsed, string? title, CancellationToken cancellationToken)
        {
            // Validate course selection (stored for later use during course selection workflow)
            if (!request.CourseId.HasValue)
                throw new SMS.Application.Exceptions.ValidationException("Course selection is required for student registration");

            var course = await _courseRepository.GetByIdAsync(request.CourseId.Value, cancellationToken);
            if (course == null || !course.IsActive)
                throw new NotFoundException("Course", request.CourseId.Value);

            var student = new Student
            {
                FirstName = parsed.FirstName,
                LastName = parsed.LastName,
                MiddleName = parsed.MiddleName,
                Title = title,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                StaffIdEstNo = request.StaffIdEstNo,
                NationalIdPassport = request.NationalIdPassport,
                StudentNumber = $"STU{DateTime.UtcNow:yyyyMMdd}{new Random().Next(1000, 9999)}",
                UserId = user.Id,
                ProgrammeId = course.ProgrammeId,
                IsActive = true,
                IsEnrolled = false,  // Not enrolled until course selection + approval
                EnrollmentDate = DateTime.UtcNow,
                TenantId = Guid.Parse(_tenantContext.TenantId),
                RegistrationStatus = RegistrationStatus.PendingCourseSelection
            };

            await _studentRepository.AddAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Register", student.Id.ToString(), $"Student account created (pending course selection for course {course.Code})");
        }

        private async Task CreateLecturerRecord(RegisterCommand request, User user, NameParseResult parsed, string? title, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Specialization))
                throw new SMS.Application.Exceptions.ValidationException("Specialization is required for lecturer registration");

            var lecturer = new Lecturer
            {
                FirstName = parsed.FirstName,
                LastName = parsed.LastName,
                MiddleName = parsed.MiddleName,
                Title = title,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                NationalIdPassport = request.NationalIdPassport,
                EmployeeNumber = $"LEC{DateTime.UtcNow:yyyyMMdd}{new Random().Next(1000, 9999)}",
                IsActive = true,
                UserId = user.Id.ToString(),
                HireDate = DateTime.UtcNow,
                TenantId = Guid.Parse(_tenantContext.TenantId),
                RegistrationStatus = RegistrationStatus.PendingCourseSelection
            };

            await _lecturerRepository.AddAsync(lecturer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Register", lecturer.Id.ToString(), $"Lecturer account created (pending course/unit selection)");
        }
    }
}
