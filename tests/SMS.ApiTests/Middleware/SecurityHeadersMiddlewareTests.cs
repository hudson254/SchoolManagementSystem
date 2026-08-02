using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SMS.API.Middleware;
using Xunit;

namespace SMS.ApiTests.Middleware
{
    /// <summary>
    /// Regression tests for RISK-25 (HSTS / Strict-Transport-Security) and the
    /// core security headers emitted by SecurityHeadersMiddleware.
    ///
    /// HSTS must be emitted ONLY when the effective transport is provably
    /// HTTPS (direct TLS or X-Forwarded-Proto: https from a TLS-terminating
    /// reverse proxy). Over plain HTTP the header must be absent so clients
    /// are never instructed to upgrade from a non-existent HTTPS endpoint.
    /// </summary>
    public class SecurityHeadersMiddlewareTests
    {
        private static DefaultHttpContext CreateContext(
            bool https = false,
            string? forwardedProto = null)
        {
            var context = new DefaultHttpContext();
            if (https)
            {
                context.Request.Scheme = "https";
            }

            if (!string.IsNullOrEmpty(forwardedProto))
            {
                context.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
            }

            return context;
        }

        private static SecurityHeadersMiddleware CreateMiddleware()
        {
            // A no-op next delegate; this middleware only sets headers.
            return new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        }

        [Fact]
        public async Task InvokeAsync_OverPlainHttp_DoesNotEmitHstsHeader()
        {
            // Arrange
            var context = CreateContext(); // default scheme is http
            var middleware = CreateMiddleware();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
        }

        [Fact]
        public async Task InvokeAsync_OverHttps_EmitsHstsHeader()
        {
            // Arrange
            var context = CreateContext(https: true);
            var middleware = CreateMiddleware();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(
                "max-age=31536000; includeSubDomains",
                context.Response.Headers["Strict-Transport-Security"].ToString());
        }

        [Fact]
        public async Task InvokeAsync_WithHttpsForwardedProto_EmitsHstsHeader()
        {
            // Arrange — simulates the app behind a TLS-terminating reverse proxy
            var context = CreateContext(forwardedProto: "https");
            var middleware = CreateMiddleware();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(
                "max-age=31536000; includeSubDomains",
                context.Response.Headers["Strict-Transport-Security"].ToString());
        }

        [Fact]
        public async Task InvokeAsync_AlwaysEmitsCoreSecurityHeaders()
        {
            // Arrange
            var context = CreateContext();
            var middleware = CreateMiddleware();

            // Act
            await middleware.InvokeAsync(context);

            // Assert — the existing core headers must not regress
            Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
            Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
            Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"].ToString());
            Assert.False(string.IsNullOrEmpty(context.Response.Headers["Content-Security-Policy"].ToString()));
            Assert.False(string.IsNullOrEmpty(context.Response.Headers["Permissions-Policy"].ToString()));
            Assert.Equal("same-origin", context.Response.Headers["Cross-Origin-Opener-Policy"].ToString());
            Assert.Equal("same-origin", context.Response.Headers["Cross-Origin-Resource-Policy"].ToString());
        }
    }
}
