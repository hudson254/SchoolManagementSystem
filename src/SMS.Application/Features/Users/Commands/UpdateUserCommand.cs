using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Users.Commands
{
    public class UpdateUserCommand : IRequest<UserDto>
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100);
        }
    }

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
    {
        private readonly IUserManagerService _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(
            IUserManagerService userManager,
            IAuditService auditService,
            ILogger<UpdateUserCommandHandler> logger)
        {
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.Id.ToString());
            if (user == null)
                throw new NotFoundException("User", request.Id);

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;

            await _userManager.UpdateUserAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            await _auditService.LogAsync("Update", "User", $"User updated: {user.Email}");

            _logger.LogInformation("User updated: {Email}", user.Email);

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

