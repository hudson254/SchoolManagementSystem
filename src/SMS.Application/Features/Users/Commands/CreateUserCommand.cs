using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Commands
{
    public class CreateUserCommand : IRequest<UserDto>
    {
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = "User";
    }

    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required");
        }
    }

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
    {
        private readonly IUserManagerService _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateUserCommandHandler> _logger;
        private readonly INameParser _nameParser;
        private readonly IUsernameGenerator _usernameGenerator;

        public CreateUserCommandHandler(
            IUserManagerService userManager,
            IAuditService auditService,
            ILogger<CreateUserCommandHandler> logger,
            INameParser nameParser,
            IUsernameGenerator usernameGenerator)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
            _nameParser = nameParser;
            _usernameGenerator = usernameGenerator;
        }

        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                throw new ConflictException("User with this email already exists");

            // Parse the full name to extract any title and normalize name parts.
            // This ensures titles like "Dr" are stored separately and never leak
            // into usernames or file names.
            var fullName = $"{request.FirstName} {request.LastName}".Trim();
            var parsed = _nameParser.ParseName(fullName);

            if (!parsed.IsValid)
                throw new SMS.Application.Exceptions.ValidationException(parsed.ErrorMessage ?? "Invalid name format");

            var title = request.Title ?? parsed.Title;

            var username = await _usernameGenerator.GenerateUsernameAsync(parsed.FirstName, parsed.LastName);
            var user = await _userManager.CreateUserAsync(username, request.Email, request.Password, request.Role);

            if (user == null)
                throw new ExternalServiceException("User creation service returned null");

            user.FirstName = parsed.FirstName;
            user.LastName = parsed.LastName;
            user.MiddleName = parsed.MiddleName;
            user.Title = title;
            user.PhoneNumber = request.PhoneNumber;
            await _userManager.UpdateUserAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            await _auditService.LogAsync("Create", "User", $"User created: {user.Email}");

            _logger.LogInformation("User created: {Email} with role {Role}", user.Email, request.Role);

            return new UserDto
            {
                Id = Guid.Parse(user.Id),
                FirstName = user.FirstName ?? string.Empty,
                MiddleName = user.MiddleName,
                LastName = user.LastName ?? string.Empty,
                Title = user.Title,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                Organization = user.Organization ?? string.Empty,
                TenantId = user.TenantId,
                Roles = roles?.ToList() ?? new List<string>()
            };
        }
    }
}

