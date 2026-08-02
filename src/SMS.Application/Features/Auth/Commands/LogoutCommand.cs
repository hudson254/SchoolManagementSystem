using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Auth.Commands
{
    /// <summary>
    /// Logs a user out by revoking their refresh token (so it cannot be used
    /// to obtain new access tokens) and adding the current access token's
    /// identifier (jti) to a short-lived deny-list so the access token itself
    /// is rejected for the remainder of its lifetime.
    ///
    /// This replaces the previous no-op implementation (RISK-05) which did
    /// nothing, allowing a stolen token to remain valid until natural expiry.
    /// </summary>
    public class LogoutCommand : IRequest<MediatR.Unit>
    {
        public Guid UserId { get; set; }

        /// <summary>
        /// The JWT identifier (jti claim) of the access token being
        /// logged out. Used to add the token to the deny-list.
        /// </summary>
        public string? AccessTokenJti { get; set; }
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, MediatR.Unit>
    {
        private readonly IUserManagerService _userManager;
        private readonly ITokenRevocationService _tokenRevocation;
        private readonly IAuditService _auditService;
        private readonly ILogger<LogoutCommandHandler> _logger;

        public LogoutCommandHandler(
            IUserManagerService userManager,
            ITokenRevocationService tokenRevocation,
            IAuditService auditService,
            ILogger<LogoutCommandHandler> logger)
        {
            _userManager = userManager;
            _tokenRevocation = tokenRevocation;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            // 1. Revoke the stored refresh token so it can no longer be used
            //    to mint new access tokens. RevokeRefreshTokenAsync now
            //    persists the revocation (nulls out the stored token + expiry)
            //    rather than being a no-op.
            var revoked = await _userManager.RevokeRefreshTokenAsync(request.UserId.ToString());
            if (!revoked)
            {
                _logger.LogWarning("Logout: refresh token revocation reported failure for user {UserId}", request.UserId);
            }

            // 2. Add the current access token's jti to the short-lived
            //    deny-list so the access token is rejected for the rest of
            //    its lifetime. This closes the window where a stolen access
            //    token remains valid after the user logs out.
            if (!string.IsNullOrWhiteSpace(request.AccessTokenJti))
            {
                await _tokenRevocation.RevokeAccessTokenAsync(request.AccessTokenJti);
            }

            // 3. Audit the logout for traceability.
            await _auditService.LogAsync("Logout", request.UserId.ToString(), "User logged out; refresh token revoked and access token deny-listed");

            _logger.LogInformation("User {UserId} logged out successfully", request.UserId);

            return MediatR.Unit.Value;
        }
    }
}
