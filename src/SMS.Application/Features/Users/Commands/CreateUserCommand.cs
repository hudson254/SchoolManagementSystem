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
        public string LastName { get; set; } = string.Empty;
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

        public CreateUserCommandHandler(
            IUserManagerService userManager,
            IAuditService auditService,
            ILogger<CreateUserCommandHandler> logger)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                throw new ConflictException("User with this email already exists");

            var username = $"{request.FirstName.ToLower()}.{request.LastName.ToLower()}{new Random().Next(100, 999)}";
            var user = await _userManager.CreateUserAsync(username, request.Email, request.Password, request.Role);

            if (user == null)
                throw new ExternalServiceException("User creation service returned null");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            await _userManager.UpdateUserAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            await _auditService.LogAsync("Create", "User", $"User created: {user.Email}");

            _logger.LogInformation("User created: {Email} with role {Role}", user.Email, request.Role);

            return new UserDto
            {
                Id = Guid.Parse(user.Id),
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
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

