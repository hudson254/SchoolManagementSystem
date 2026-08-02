using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SMS.API.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers using indexer (avoids duplicate key exception)
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
            context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";

            // HSTS (RISK-25): Strict-Transport-Security forces browsers to use
            // HTTPS only. The header is emitted ONLY when the effective
            // transport is provably HTTPS — either the request is directly
            // HTTPS, or the app sits behind a TLS-terminating reverse proxy
            // that forwards "X-Forwarded-Proto: https". This keeps local dev
            // (plain HTTP to localhost) and TestServer calls free of the
            // header, so clients are never instructed to upgrade from a
            // non-existent HTTPS endpoint. includeSubDomains lets subdomains
            // inherit the policy.
            var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString();
            var effectiveTransportIsHttps = context.Request.IsHttps
                || forwardedProto.Contains("https", System.StringComparison.OrdinalIgnoreCase);

            if (effectiveTransportIsHttps)
            {
                context.Response.Headers["Strict-Transport-Security"] =
                    "max-age=31536000; includeSubDomains";
            }

            // Add Content Security Policy (strict, no unsafe-inline/unsafe-eval)
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' data:; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'";

            await _next(context);
        }
    }
}
