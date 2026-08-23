using FluentValidation;
using MediatR;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Students.Commands
{
    public class CreateStudentCommand : IRequest<StudentDto>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? Title { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Guid? ProgrammeId { get; set; }
        public Guid? CurrentSemesterId { get; set; }
        public string StudentNumber { get; set; }
        public string Password { get; set; }
    }

    public class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
    {
        public CreateStudentCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email is required");

            RuleFor(x => x.Password)
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .When(x => !string.IsNullOrEmpty(x.Password));

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required")
                .LessThan(DateTime.UtcNow).WithMessage("Date of birth must be in the past");
        }
    }

    public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, StudentDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IUserManagerService _userManager;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;
        private readonly IAuditService _auditService;
        private readonly INameParser _nameParser;
        private readonly IUsernameGenerator _usernameGenerator;

        public CreateStudentCommandHandler(
            IStudentRepository studentRepository,
            IUserManagerService userManager,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext,
            IAuditService auditService,
            INameParser nameParser,
            IUsernameGenerator usernameGenerator)
        {
            _studentRepository = studentRepository;
            _userManager = userManager;
            _tenantContext = tenantContext;
            _auditService = auditService;
            _nameParser = nameParser;
            _usernameGenerator = usernameGenerator;
        }

        public async Task<StudentDto> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
        {
            // Check if email already exists
            var existingUser = await _userManager.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ConflictException("User with this email already exists");
            }

            // Parse the full name to extract any title and normalize name parts.
            // This ensures titles like "Dr" are stored separately and never leak
            // into usernames or file names.
            var fullName = $"{request.FirstName} {request.LastName}".Trim();
            var parsed = _nameParser.ParseName(fullName);

            if (!parsed.IsValid)
                throw new SMS.Application.Exceptions.ValidationException(parsed.ErrorMessage ?? "Invalid name format");

            var title = request.Title ?? parsed.Title;

            // Create user account. If no password was supplied (administrative
            // student creation), generate a random secure default password so
            // the user account can be created successfully. The user can then
            // use the forgot-password flow to set their own password.
            var password = string.IsNullOrWhiteSpace(request.Password)
                ? GenerateDefaultPassword()
                : request.Password;

            var username = await _usernameGenerator.GenerateUsernameAsync(parsed.FirstName, parsed.LastName);
            var user = await _userManager.CreateUserAsync(username, request.Email, password, "Student");
            if (user == null)
                throw new ExternalServiceException("Failed to create user account", "USER_CREATION_FAILED");

            // Sync the User's name fields with the Student's names. UserManagerService
            // creates users with empty FirstName/LastName, but GetStudent/GetCurrentUser
            // read names from the User entity, so without this sync the returned
            // names would be blank.
            if (user != null)
            {
                user.FirstName = parsed.FirstName;
                user.LastName = parsed.LastName;
                user.MiddleName = parsed.MiddleName;
                user.Title = title;
                user.PhoneNumber = request.PhoneNumber;
                user.Email = request.Email;
                await _userManager.UpdateUserAsync(user);
            }

            // Create student
            var student = new Student
            {
                FirstName = parsed.FirstName,
                LastName = parsed.LastName,
                MiddleName = parsed.MiddleName,
                Title = title,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                DateOfBirth = request.DateOfBirth,
                ProgrammeId = request.ProgrammeId,
                CurrentSemesterId = request.CurrentSemesterId,
                StudentNumber = request.StudentNumber ?? $"STU{DateTime.UtcNow:yyyyMMdd}{new Random().Next(1000, 9999)}",
                UserId = user.Id,
                IsActive = true,
                TenantId = Guid.Parse(_tenantContext.TenantId)
            };

            var createdStudent = await _studentRepository.AddAsync(student, cancellationToken);

            await _auditService.LogAsync("Create", "Student", $"Student created: {createdStudent.StudentNumber}");

            return new StudentDto
            {
                Id = createdStudent.Id,
                UserId = createdStudent.UserId,
                StudentNumber = createdStudent.StudentNumber,
                FirstName = createdStudent.FirstName,
                LastName = createdStudent.LastName,
                MiddleName = createdStudent.MiddleName,
                Title = createdStudent.Title,
                Email = createdStudent.Email,
                PhoneNumber = createdStudent.PhoneNumber,
                Address = createdStudent.Address,
                ProgrammeId = createdStudent.ProgrammeId,
                IsActive = createdStudent.IsActive
            };
        }

        /// <summary>
        /// Generates a cryptographically strong random default password
        /// (e.g., for administrative student creation when no password is supplied).
        /// The generated password satisfies the Identity password policy
        /// (digit, lowercase, uppercase, non-alphanumeric, min 12 characters).
        /// </summary>
        private static string GenerateDefaultPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%^&*";
            const string all = upper + lower + digits + special;

            var bytes = RandomNumberGenerator.GetBytes(12);
            var chars = new char[12];

            // Ensure at least one char from each required category so the
            // generated password passes the Identity password policy
            // (RequireDigit, RequireUpper, RequireLower, RequireNonAlphanumeric).
            chars[0] = upper[bytes[0] % upper.Length];
            chars[1] = lower[bytes[1] % lower.Length];
            chars[2] = digits[bytes[2] % digits.Length];
            chars[3] = special[bytes[3] % special.Length];

            for (int i = 4; i < chars.Length; i++)
            {
                chars[i] = all[bytes[i] % all.Length];
            }

            // Shuffle to avoid a predictable prefix pattern.
            // Type argument specified explicitly: char[] -> Span<char> is an
            // implicit conversion, which the C# compiler does not use for
            // generic type inference (CS0411 otherwise).
            RandomNumberGenerator.Shuffle<char>(chars);
            return new string(chars);
        }
    }
}
