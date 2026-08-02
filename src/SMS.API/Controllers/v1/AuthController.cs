using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Auth.Commands;
using SMS.Application.Features.Auth.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    public class AuthController : BaseApiController
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            [FromBody] LoginCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return Created("", result);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken(
            [FromBody] RefreshTokenCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var command = new LogoutCommand { UserId = Guid.Parse(userId) };
                await Mediator.Send(command, cancellationToken);
            }
            return NoContent();
        }

        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordCommand command,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
                command.UserId = Guid.Parse(userId);
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordCommand command,
            CancellationToken cancellationToken)
        {
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordCommand command,
            CancellationToken cancellationToken)
        {
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpGet("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail(
            [FromQuery] string userId,
            [FromQuery] string token,
            CancellationToken cancellationToken)
        {
            var command = new VerifyEmailCommand { UserId = userId, Token = token };
            await Mediator.Send(command, cancellationToken);
            return Ok(new { Message = "Email verified successfully" });
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var query = new GetCurrentUserQuery { UserId = Guid.Parse(userId) };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
