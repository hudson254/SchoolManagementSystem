using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SMS.API.Middleware
{
    /// <summary>
    /// Double-submit cookie CSRF protection for cookie-authenticated requests.
    ///
    /// RISK-10: The API now authenticates browsers via httpOnly cookies
    /// (access_token / refresh_token set by AuthController). Because cookies
    /// are sent automatically by the browser, state-changing requests are
    /// vulnerable to CSRF unless protected. This middleware implements the
    /// standard double-submit cookie pattern:
    ///
    ///  - On every request, if the XSRF-TOKEN cookie is absent, a random
    ///    32-byte token is generated and set as a non-httpOnly cookie (the
    ///    frontend reads it and echoes it back in the X-CSRF-TOKEN header).
    ///  - For state-changing methods (POST/PUT/PATCH/DELETE) that are
    ///    authenticated via the access_token cookie, the X-CSRF-TOKEN header
    ///    must match the XSRF-TOKEN cookie value; otherwise 403 is returned.
    ///  - Requests authenticated via the Authorization Bearer header are
    ///    skipped entirely — a cross-origin attacker cannot set that header
    ///    without a preflight, so Bearer-token auth is inherently CSRF-safe.
    ///    (This also keeps Swagger and API tests working.)
    ///  - Anonymous state-changing requests (login, register, refresh-token)
    ///    have no cookie session to protect, so they are not blocked.
    /// </summary>
    public class CsrfProtectionMiddleware
    {
        private const string CsrfCookieName = "XSRF-TOKEN";
        private const string CsrfHeaderName = "X-CSRF-TOKEN";
        private const string AccessTokenCookieName = "access_token";
        private static readonly string[] SafeMethods = { "GET", "HEAD", "OPTIONS", "TRACE" };

        // Anonymous auth endpoints are exempt from CSRF validation. These
        // establish/refresh the session (login/register/refresh-token) so there
        // is no cookie-authenticated session to protect yet — enforcing CSRF
        // here would break the flow (e.g. a duplicate register on a client that
        // already holds an access_token cookie from the first register).
        private static readonly string[] CsrfExemptPaths =
        {
            "/api/v1/auth/login",
            "/api/v1/auth/register",
            "/api/v1/auth/refresh-token",
            "/api/v1/auth/forgot-password",
            "/api/v1/auth/reset-password",
            "/api/v1/auth/verify-email",
            // Logout is low-risk: a CSRF-forced logout is at most a minor
            // denial-of-service (the session is revoked anyway). Exempting it
            // keeps the final logout call simple for clients.
            "/api/v1/auth/logout"
        };

        private readonly RequestDelegate _next;

        public CsrfProtectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var method = context.Request.Method?.ToUpperInvariant() ?? "GET";
            var isStateChanging = Array.IndexOf(SafeMethods, method) < 0;

            // Anonymous auth endpoints are exempt from CSRF validation.
            var path = context.Request.Path.Value ?? string.Empty;
            var isExemptPath = Array.Exists(CsrfExemptPaths, p =>
                path.Equals(p, StringComparison.OrdinalIgnoreCase));

            // Ensure the CSRF cookie exists so the frontend can read it
            // (for safe methods and for the first state-changing request).
            if (!context.Request.Cookies.ContainsKey(CsrfCookieName))
            {
                var token = GenerateToken();
                context.Response.Cookies.Append(CsrfCookieName, token, new CookieOptions
                {
                    HttpOnly = false,  // frontend JS must read this
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    Path = "/"
                });
            }

            if (isStateChanging && !isExemptPath)
            {
                // Only enforce CSRF when the request is authenticated via the
                // access_token cookie (the browser flow). Bearer-token requests
                // (API tests, Swagger, scripts) are not blocked.
                var hasAccessTokenCookie = context.Request.Cookies.ContainsKey(AccessTokenCookieName);
                var authHeader = context.Request.Headers["Authorization"].ToString();
                var isBearerAuth = !string.IsNullOrEmpty(authHeader) &&
                                   authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

                if (hasAccessTokenCookie && !isBearerAuth)
                {
                    var cookieToken = context.Request.Cookies[CsrfCookieName];
                    var headerToken = context.Request.Headers[CsrfHeaderName].ToString();

                    if (string.IsNullOrEmpty(cookieToken) || string.IsNullOrEmpty(headerToken))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(
                            "{\"error\":\"CSRF validation failed: missing token\"}");
                        return;
                    }

                    // Constant-time comparison to avoid timing attacks.
                    if (!CryptographicOperations.FixedTimeEquals(
                            System.Text.Encoding.UTF8.GetBytes(cookieToken),
                            System.Text.Encoding.UTF8.GetBytes(headerToken)))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(
                            "{\"error\":\"CSRF validation failed: token mismatch\"}");
                        return;
                    }
                }
            }

            await _next(context);
        }

        private static string GenerateToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
