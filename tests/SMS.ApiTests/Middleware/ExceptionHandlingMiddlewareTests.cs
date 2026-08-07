using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SMS.API.Middleware;
using SMS.API.Models;
using SMS.Application.Common;
using SMS.Application.Exceptions;
using Xunit;

namespace SMS.ApiTests.Middleware
{
    /// <summary>
    /// Regression tests for the enterprise error handling contract:
    /// - No stack traces or internal details are ever exposed in production.
    /// - The standardized envelope { success, code, message, severity, category } is honored.
    /// - Severity/category classification is correct.
    /// </summary>
    public class ExceptionHandlingMiddlewareTests
    {
        private static DefaultHttpContext CreateContext(Microsoft.Extensions.Hosting.IHostEnvironment env)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/v1/test";
            context.Request.Method = "GET";
            context.Items["X-Correlation-ID"] = "test-correlation-id";
            context.RequestServices = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();
            return context;
        }

        private static ExceptionHandlingMiddleware CreateMiddleware(Microsoft.Extensions.Hosting.IHostEnvironment env)
        {
            return new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("Test exception with sensitive details: password=secret connectionString=Server=db"),
                NullLogger<ExceptionHandlingMiddleware>.Instance,
                env);
        }

        private static async Task<(int statusCode, JsonElement body)> InvokeAndRead(ExceptionHandlingMiddleware middleware, DefaultHttpContext context)
        {
            context.Response.Body = new MemoryStream();
            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            var doc = JsonDocument.Parse(json);
            return (context.Response.StatusCode, doc.RootElement.Clone());
        }

        [Fact]
        public async Task InvokeAsync_Production_DoesNotExposeStackTrace()
        {
            // Arrange — production environment
            var env = HostEnvironmentProduction();
            var context = CreateContext(env);
            var middleware = CreateMiddleware(env);

            // Act
            var (statusCode, body) = await InvokeAndRead(middleware, context);

            // Assert
            Assert.Equal(500, statusCode);
            Assert.False(body.TryGetProperty("details", out var details) && details.GetString() is not null,
                "Production responses must never include the 'details' (stack trace) field.");
            Assert.DoesNotContain("password", body.GetRawText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("connectionString", body.GetRawText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("at SMS.", body.GetRawText(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task InvokeAsync_Production_HonorsStandardEnvelope()
        {
            // Arrange
            var env = HostEnvironmentProduction();
            var context = CreateContext(env);
            var middleware = CreateMiddleware(env);

            // Act
            var (_, body) = await InvokeAndRead(middleware, context);

            // Assert — standardized envelope fields
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("INTERNAL_ERROR", body.GetProperty("code").GetString());
            Assert.False(string.IsNullOrEmpty(body.GetProperty("message").GetString()));
            Assert.Equal(500, body.GetProperty("statusCode").GetInt32());
            Assert.Equal(ErrorSeverity.High.ToString(), body.GetProperty("severity").GetString());
            Assert.Equal(ErrorCategory.Unknown.ToString(), body.GetProperty("category").GetString());
            Assert.Equal("test-correlation-id", body.GetProperty("correlationId").GetString());
        }

        [Fact]
        public async Task InvokeAsync_ValidationException_ClassifiesAsLowValidation()
        {
            // Arrange
            var env = HostEnvironmentProduction();
            var context = CreateContext(env);
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new ValidationException("One or more validation failures have occurred."),
                NullLogger<ExceptionHandlingMiddleware>.Instance,
                env);

            // Act
            var (statusCode, body) = await InvokeAndRead(middleware, context);

            // Assert
            Assert.Equal(400, statusCode);
            Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
            Assert.Equal(ErrorSeverity.Low.ToString(), body.GetProperty("severity").GetString());
            Assert.Equal(ErrorCategory.Validation.ToString(), body.GetProperty("category").GetString());
        }

        [Fact]
        public async Task InvokeAsync_DatabaseException_ClassifiesAsHighDatabase()
        {
            // Arrange
            var env = HostEnvironmentProduction();
            var context = CreateContext(env);
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new DatabaseException("A database error occurred."),
                NullLogger<ExceptionHandlingMiddleware>.Instance,
                env);

            // Act
            var (statusCode, body) = await InvokeAndRead(middleware, context);

            // Assert
            Assert.Equal(500, statusCode);
            Assert.Equal("DB_ERROR", body.GetProperty("code").GetString());
            Assert.Equal(ErrorSeverity.High.ToString(), body.GetProperty("severity").GetString());
            Assert.Equal(ErrorCategory.Database.ToString(), body.GetProperty("category").GetString());
        }

        [Fact]
        public async Task InvokeAsync_UnauthorizedException_ClassifiesAsMediumAuthentication()
        {
            // Arrange
            var env = HostEnvironmentProduction();
            var context = CreateContext(env);
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new UnauthorizedException("Unauthorized."),
                NullLogger<ExceptionHandlingMiddleware>.Instance,
                env);

            // Act
            var (statusCode, body) = await InvokeAndRead(middleware, context);

            // Assert
            Assert.Equal(401, statusCode);
            Assert.Equal("UNAUTHORIZED", body.GetProperty("code").GetString());
            Assert.Equal(ErrorSeverity.Medium.ToString(), body.GetProperty("severity").GetString());
            Assert.Equal(ErrorCategory.Authentication.ToString(), body.GetProperty("category").GetString());
        }

        [Fact]
        public async Task InvokeAsync_ForbiddenException_ClassifiesAsMediumAuthorization()
        {
            // Arrange
            var env = HostEnvironmentProduction();
            var context = CreateContext(env);
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new ForbiddenException("Forbidden."),
                NullLogger<ExceptionHandlingMiddleware>.Instance,
                env);

            // Act
            var (statusCode, body) = await InvokeAndRead(middleware, context);

            // Assert
            Assert.Equal(403, statusCode);
            Assert.Equal("FORBIDDEN", body.GetProperty("code").GetString());
            Assert.Equal(ErrorSeverity.Medium.ToString(), body.GetProperty("severity").GetString());
            Assert.Equal(ErrorCategory.Authorization.ToString(), body.GetProperty("category").GetString());
        }

        [Fact]
        public async Task InvokeAsync_NotFoundException_ClassifiesAsLowValidation()
        {
            // Arrange
            var env = HostEnvironmentProduction();
            var context = CreateContext(env);
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new NotFoundException("Record not found."),
                NullLogger<ExceptionHandlingMiddleware>.Instance,
                env);

            // Act
            var (statusCode, body) = await InvokeAndRead(middleware, context);

            // Assert
            Assert.Equal(404, statusCode);
            Assert.Equal("NOT_FOUND", body.GetProperty("code").GetString());
            Assert.Equal(ErrorSeverity.Low.ToString(), body.GetProperty("severity").GetString());
            Assert.Equal(ErrorCategory.Validation.ToString(), body.GetProperty("category").GetString());
        }

        [Fact]
        public async Task InvokeAsync_Development_IncludesDetails()
        {
            // Arrange — development environment
            var env = HostEnvironmentDevelopment();
            var context = CreateContext(env);
            var middleware = CreateMiddleware(env);

            // Act
            var (_, body) = await InvokeAndRead(middleware, context);

            // Assert — details are only present in development
            Assert.True(body.TryGetProperty("details", out var details) && !string.IsNullOrEmpty(details.GetString()),
                "Development environments may include the 'details' field.");
        }

        private static Microsoft.Extensions.Hosting.IHostEnvironment HostEnvironmentProduction()
        {
            return new HostEnvironment { EnvironmentName = Environments.Production };
        }

        private static Microsoft.Extensions.Hosting.IHostEnvironment HostEnvironmentDevelopment()
        {
            return new HostEnvironment { EnvironmentName = Environments.Development };
        }

        private class HostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Production;
            public string ApplicationName { get; set; } = "SMS.API";
            public string ContentRootPath { get; set; } = "";
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        }
    }
}
