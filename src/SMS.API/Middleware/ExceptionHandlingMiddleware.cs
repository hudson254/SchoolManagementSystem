using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SMS.API.Models;
using SMS.Application.Exceptions;
using System;
using System.Data.Common;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SMS.API.Middleware
{
    /// <summary>
    /// Enterprise-grade exception handling middleware that provides consistent,
    /// structured error responses with correlation IDs, error codes, and
    /// user-friendly messages. Does not expose internal implementation details.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = context.GetCorrelationId();

            // Log the exception with full details (server-side only)
            _logger.LogError(exception, "An unhandled exception occurred: {Message}. CorrelationId: {CorrelationId}",
                exception.Message, correlationId);

            var (statusCode, errorCode, message, details) = ClassifyException(exception, _env.IsDevelopment());

            var errorResponse = new ErrorResponse
            {
                StatusCode = (int)statusCode,
                ErrorCode = errorCode,
                Message = message,
                CorrelationId = correlationId,
                Path = context.Request.Path,
                Details = details
            };

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/problem+json";

            var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Classifies an exception into a structured error response.
        /// </summary>
        private static (HttpStatusCode statusCode, string errorCode, string message, string? details) ClassifyException(
            Exception exception, bool isDevelopment)
        {
            // Custom application exceptions
            switch (exception)
            {
                case ValidationException validationException:
                    return (HttpStatusCode.BadRequest, "VALIDATION_ERROR",
                        "One or more validation failures have occurred. Please check your input and try again.",
                        isDevelopment ? FormatValidationDetails(validationException) : null);

                // FluentValidation.ValidationException thrown by ValidationBehavior pipeline
                case FluentValidation.ValidationException fluentValidationException:
                    return (HttpStatusCode.BadRequest, "VALIDATION_ERROR",
                        "One or more validation failures have occurred. Please check your input and try again.",
                        isDevelopment ? string.Join(Environment.NewLine, fluentValidationException.Errors.Select(e => $"  - {e.PropertyName}: {e.ErrorMessage}")) : null);

                case NotFoundException notFoundException:
                    return (HttpStatusCode.NotFound, "NOT_FOUND",
                        notFoundException.Message,
                        isDevelopment ? exception.ToString() : null);

                case UnauthorizedException unauthorizedException:
                    return (HttpStatusCode.Unauthorized, "UNAUTHORIZED",
                        "You are not authorized to perform this action. Please log in and try again.",
                        isDevelopment ? exception.ToString() : null);

                case ForbiddenException forbiddenException:
                    return (HttpStatusCode.Forbidden, "FORBIDDEN",
                        "Access denied. You do not have permission to perform this action.",
                        isDevelopment ? exception.ToString() : null);

                case ConflictException conflictException:
                    return (HttpStatusCode.Conflict, "CONFLICT",
                        conflictException.Message,
                        isDevelopment ? exception.ToString() : null);

                case BusinessRuleException businessRuleException:
                    return (HttpStatusCode.BadRequest, "BUSINESS_RULE_VIOLATION",
                        businessRuleException.Message,
                        isDevelopment ? exception.ToString() : null);

                // Database exceptions
                case DatabaseException databaseException:
                    return (HttpStatusCode.InternalServerError, databaseException.ErrorCode,
                        "A database error occurred. The system is unable to process your request at this time. Please try again later.",
                        isDevelopment ? exception.ToString() : null);

                case DbException dbException:
                    return (HttpStatusCode.InternalServerError, "DATABASE_UNAVAILABLE",
                        "The database is currently unavailable. Please try again later.",
                        isDevelopment ? exception.ToString() : null);

                // External service exceptions
                case ExternalServiceException externalServiceException:
                    return (HttpStatusCode.BadGateway, externalServiceException.ErrorCode,
                        externalServiceException.Message,
                        isDevelopment ? exception.ToString() : null);

                case HttpRequestException httpRequestException:
                    return (HttpStatusCode.BadGateway, "EXTERNAL_SERVICE_ERROR",
                        "An external service is currently unavailable. Please try again later.",
                        isDevelopment ? exception.ToString() : null);

                // File system exceptions
                case FileSystemException fileSystemException:
                    return (HttpStatusCode.InternalServerError, fileSystemException.ErrorCode,
                        "A file operation failed. Please try again or contact support.",
                        isDevelopment ? exception.ToString() : null);

                case IOException ioException:
                    return (HttpStatusCode.InternalServerError, "FILE_SYSTEM_ERROR",
                        "A file operation failed. Please try again or contact support.",
                        isDevelopment ? exception.ToString() : null);

                case UnauthorizedAccessException unauthorizedAccessException:
                    return (HttpStatusCode.Forbidden, "ACCESS_DENIED",
                        "Access denied. You do not have permission to access this resource.",
                        isDevelopment ? exception.ToString() : null);

                // Network exceptions
                case NetworkException networkException:
                    return (HttpStatusCode.BadGateway, networkException.ErrorCode,
                        "A network error occurred. Please check your connection and try again.",
                        isDevelopment ? exception.ToString() : null);

                // Timeout exceptions (use fully qualified name to avoid ambiguity with System.TimeoutException)
                case SMS.Application.Exceptions.TimeoutException timeoutException:
                    return (HttpStatusCode.RequestTimeout, timeoutException.ErrorCode,
                        "The operation timed out. Please try again.",
                        isDevelopment ? exception.ToString() : null);

                case TaskCanceledException taskCanceledException when taskCanceledException.InnerException is not null:
                    return (HttpStatusCode.RequestTimeout, "TIMEOUT_ERROR",
                        "The request timed out. Please try again.",
                        isDevelopment ? exception.ToString() : null);

                // Background job exceptions
                case BackgroundJobException backgroundJobException:
                    return (HttpStatusCode.InternalServerError, backgroundJobException.ErrorCode,
                        "A background job failed. The system will retry automatically.",
                        isDevelopment ? exception.ToString() : null);

                // Authentication token expired
                case SecurityTokenExpiredException:
                    return (HttpStatusCode.Unauthorized, "TOKEN_EXPIRED",
                        "Your session has expired. Please log in again to continue.",
                        null);

                // Default: unhandled exception
                default:
                    return (HttpStatusCode.InternalServerError, "INTERNAL_ERROR",
                        "An error occurred while processing your request. Please try again later or contact support.",
                        isDevelopment ? exception.ToString() : null);
            }
        }

        private static string FormatValidationDetails(ValidationException ex)
        {
            if (ex.Errors == null || ex.Errors.Count == 0)
                return ex.Message;

            var details = new System.Text.StringBuilder();
            details.AppendLine(ex.Message);
            foreach (var error in ex.Errors)
            {
                details.AppendLine($"  - {error.Key}: {string.Join(", ", error.Value)}");
            }
            return details.ToString();
        }
    }
}
