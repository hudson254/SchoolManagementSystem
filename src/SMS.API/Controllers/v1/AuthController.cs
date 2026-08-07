using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Auth.Commands;
using SMS.Application.Features.Auth.Queries;
using SMS.Application.Features.Courses.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    public class AuthController : BaseApiController
    {
        // RISK-08: tokens are stored in httpOnly, SameSite cookies so
        // JavaScript cannot read them, eliminating the localStorage XSS
        // exposure. A non-httpOnly XSRF-TOKEN cookie (double-submit pattern)
        // is managed by CsrfProtectionMiddleware.
        private const string AccessTokenCookieName = "access_token";
        private const string RefreshTokenCookieName = "refresh_token";

        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns whether the effective transport is HTTPS, honoring the
        /// X-Forwarded-Proto header set by the nginx TLS-terminating reverse
        /// proxy. The Secure flag must be set on cookies when served over
        /// the public HTTPS endpoint even though the backend may see plain HTTP.
        /// </summary>
        private bool IsHttpsRequest =>
            Request.IsHttps ||
            Request.Headers["X-Forwarded-Proto"].ToString().Contains("https", StringComparison.OrdinalIgnoreCase);

        private void SetAuthCookies(string accessToken, string refreshToken)
        {
            Response.Cookies.Append(AccessTokenCookieName, accessToken, new CookieOptions
            {
                HttpOnly = true,      // JS cannot read the token
                Secure = IsHttpsRequest,
                SameSite = SameSiteMode.Lax,   // sent on same-site top-level navigations; blocks most CSRF
                IsEssential = true,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(1)   // matches JwtSettings AccessTokenExpirationMinutes (60)
            });

            Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = IsHttpsRequest,
                SameSite = SameSiteMode.Strict,  // only same-origin requests carry the refresh token
                IsEssential = true,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
        }

        private void ClearAuthCookies()
        {
            foreach (var name in new[] { AccessTokenCookieName, RefreshTokenCookieName })
            {
                if (Request.Cookies.ContainsKey(name))
                {
                    Response.Cookies.Delete(name, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = IsHttpsRequest,
                        SameSite = SameSiteMode.Lax,
                        Path = "/"
                    });
                }
            }
        }

        /// <summary>
        /// Strips the token fields from the DTO before it is serialized to the
        /// client. Tokens now live exclusively in httpOnly cookies; they must
        /// never appear in the JSON response body.
        /// </summary>
        private static AuthResponseDto SanitizeAuthResponse(AuthResponseDto dto)
        {
            dto.AccessToken = string.Empty;
            dto.RefreshToken = string.Empty;
            dto.TokenType = string.Empty;
            return dto;
        }

        private static string ExtractBearerToken(HttpRequest request)
        {
            var authHeader = request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) &&
                authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
            return request.Cookies[AccessTokenCookieName] ?? string.Empty;
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

            SetAuthCookies(result.AccessToken, result.RefreshToken);
            return Ok(SanitizeAuthResponse(result));
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

            SetAuthCookies(result.AccessToken, result.RefreshToken);
            return Created("", SanitizeAuthResponse(result));
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
        {
            // RISK-08: tokens live in httpOnly cookies. The refresh token is
            // read from the refresh_token cookie and the (expired) access token
            // from the access_token cookie or Authorization header. The frontend
            // never sends tokens in the request body.
            var refreshToken = Request.Cookies[RefreshTokenCookieName];
            var accessToken = ExtractBearerToken(Request);

            if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(accessToken))
            {
                ClearAuthCookies();
                return Unauthorized();
            }

            try
            {
                var command = new RefreshTokenCommand
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };

                var result = await Mediator.Send(command, cancellationToken);

                SetAuthCookies(result.AccessToken, result.RefreshToken);
                return Ok(SanitizeAuthResponse(result));
            }
            catch
            {
                // Invalid/expired refresh token — clear the stale auth cookies
                // so the browser does not keep retrying with garbage.
                ClearAuthCookies();
                throw;
            }
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Extract the JWT identifier (jti) so the LogoutCommand can
                // add the current access token to the deny-list, closing the
                // window where a stolen access token remains valid after
                // logout (RISK-05 fix).
                var jti = User.FindFirst("jti")?.Value;
                var command = new LogoutCommand { UserId = Guid.Parse(userId), AccessTokenJti = jti };
                await Mediator.Send(command, cancellationToken);
            }

            // RISK-08: clear the httpOnly auth cookies on the client.
            ClearAuthCookies();

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

        [HttpGet("active-courses")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CourseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveCourses(CancellationToken cancellationToken)
        {
            var query = new GetActiveCoursesForRegistrationQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("username-availability")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UsernameAvailabilityDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckUsernameAvailability(
            [FromQuery] string username,
            CancellationToken cancellationToken)
        {
            var query = new CheckUsernameAvailabilityQuery { Username = username };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
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
