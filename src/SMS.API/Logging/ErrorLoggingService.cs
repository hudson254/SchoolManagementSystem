using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;

namespace SMS.API.Logging
{
    /// <summary>
    /// Centralized logging pipeline for the enterprise error handling framework.
    /// Every component logs through this service to ensure consistent,
    /// structured, correlated, and enriched logging. Sensitive data is always
    /// masked. Full technical details are captured for the PRIVATE error layer.
    /// </summary>
    public interface IErrorLoggingService
    {
        Task LogExceptionAsync(HttpContext context, Exception exception, ErrorCategory category, ErrorSeverity severity);
        Task LogExceptionAsync(ErrorLogContext logContext);
        Task LogAsync(string message, LogLevel level, Dictionary<string, object>? extraContext = null);
    }

    /// <summary>
    /// Default implementation of <see cref="IErrorLoggingService"/>.
    /// </summary>
    public class ErrorLoggingService : IErrorLoggingService
    {
        private readonly ILogger<ErrorLoggingService> _logger;

        private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "token", "secret", "apikey", "api_key", "access_token",
            "refresh_token", "authorization", "cookie", "set-cookie", "x-csrf-token",
            "x-xsrf-token", "connectionstring", "cardnumber", "cvv", "pin"
        };

        private static readonly HashSet<string> SensitiveQueryParams = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "token", "secret", "apiKey", "api_key", "access_token", "refresh_token"
        };

        public ErrorLoggingService(ILogger<ErrorLoggingService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Captures and logs a full exception diagnostic context.
        /// </summary>
        public Task LogExceptionAsync(HttpContext context, Exception exception, ErrorCategory category, ErrorSeverity severity)
        {
            var logContext = ErrorLogContext.FromHttpContext(context);
            logContext.ExceptionType = exception.GetType().FullName;
            logContext.ExceptionMessage = exception.Message;
            logContext.InnerException = exception.InnerException?.ToString();
            logContext.FullStackTrace = exception.StackTrace;
            logContext.Category = category;
            logContext.Severity = severity;
            logContext.RootCause = exception.GetBaseException().Message;
            logContext.SourceFile = exception.TargetSite?.DeclaringType?.Assembly?.GetName()?.Name;
            logContext.Assembly = exception.TargetSite?.DeclaringType?.Assembly?.GetName()?.Name;
            logContext.Namespace = exception.TargetSite?.DeclaringType?.Namespace;
            logContext.Method = exception.TargetSite?.Name;
            logContext.ThreadId = Environment.CurrentManagedThreadId.ToString();
            logContext.MemoryUsageBytes = GC.GetTotalMemory(false);

            return LogExceptionAsync(logContext);
        }

        /// <summary>
        /// Logs a pre-built error diagnostic context.
        /// </summary>
        public Task LogExceptionAsync(ErrorLogContext logContext)
        {
            var level = logContext.Severity switch
            {
                ErrorSeverity.Critical => LogLevel.Critical,
                ErrorSeverity.High => LogLevel.Error,
                ErrorSeverity.Medium => LogLevel.Warning,
                ErrorSeverity.Low => LogLevel.Information,
                _ => LogLevel.Information
            };

            var masked = MaskSensitiveData(logContext);

            using (_logger.BeginScope(masked))
            {
                _logger.Log(level,
                    "Error logged: {ExceptionType} | {ExceptionMessage} | Category: {Category} | Severity: {Severity} | CorrelationId: {CorrelationId}",
                    masked["ExceptionType"], masked["ExceptionMessage"], masked["Category"], masked["Severity"], masked["CorrelationId"]);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs a general structured message.
        /// </summary>
        public Task LogAsync(string message, LogLevel level, Dictionary<string, object>? extraContext = null)
        {
            if (extraContext != null)
            {
                using (_logger.BeginScope(MaskSensitiveData(extraContext)))
                {
                    _logger.Log(level, "{Message}", message);
                }
            }
            else
            {
                _logger.Log(level, "{Message}", message);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Masks sensitive values in the diagnostic context before logging.
        /// </summary>
        private static Dictionary<string, object> MaskSensitiveData(ErrorLogContext ctx)
        {
            var dict = new Dictionary<string, object>
            {
                ["TimestampUtc"] = ctx.TimestampUtc,
                ["RequestId"] = ctx.RequestId ?? string.Empty,
                ["CorrelationId"] = ctx.CorrelationId ?? string.Empty,
                ["SessionId"] = ctx.SessionId ?? string.Empty,
                ["UserId"] = ctx.UserId ?? string.Empty,
                ["Username"] = ctx.Username ?? string.Empty,
                ["UserRole"] = ctx.UserRole ?? string.Empty,
                ["TenantId"] = ctx.TenantId ?? string.Empty,
                ["IpAddress"] = ctx.IpAddress ?? string.Empty,
                ["UserAgent"] = ctx.UserAgent ?? string.Empty,
                ["Device"] = ctx.Device ?? string.Empty,
                ["Browser"] = ctx.Browser ?? string.Empty,
                ["OperatingSystem"] = ctx.OperatingSystem ?? string.Empty,
                ["Route"] = ctx.Route ?? string.Empty,
                ["Controller"] = ctx.Controller ?? string.Empty,
                ["Endpoint"] = ctx.Endpoint ?? string.Empty,
                ["HttpMethod"] = ctx.HttpMethod ?? string.Empty,
                ["ExceptionType"] = ctx.ExceptionType ?? string.Empty,
                ["ExceptionMessage"] = ctx.ExceptionMessage ?? string.Empty,
                ["InnerException"] = ctx.InnerException ?? string.Empty,
                ["FullStackTrace"] = ctx.FullStackTrace ?? string.Empty,
                ["SourceFile"] = ctx.SourceFile ?? string.Empty,
                ["LineNumber"] = ctx.LineNumber?.ToString() ?? string.Empty,
                ["Namespace"] = ctx.Namespace ?? string.Empty,
                ["Assembly"] = ctx.Assembly ?? string.Empty,
                ["Method"] = ctx.Method ?? string.Empty,
                ["Category"] = ctx.Category.ToString(),
                ["Severity"] = ctx.Severity.ToString(),
                ["RootCause"] = ctx.RootCause ?? string.Empty,
                ["RequestDurationMs"] = ctx.RequestDurationMs?.ToString() ?? string.Empty,
                ["DatabaseDurationMs"] = ctx.DatabaseDurationMs?.ToString() ?? string.Empty,
                ["ApiDurationMs"] = ctx.ApiDurationMs?.ToString() ?? string.Empty,
                ["MemoryUsageBytes"] = ctx.MemoryUsageBytes?.ToString() ?? string.Empty,
                ["ThreadId"] = ctx.ThreadId ?? string.Empty,
                ["SqlCommand"] = "REDACTED",
                ["DatabaseProvider"] = ctx.DatabaseProvider ?? string.Empty,
                ["TransactionId"] = ctx.TransactionId ?? string.Empty,
                ["RetryCount"] = ctx.RetryCount?.ToString() ?? string.Empty,
                ["ConnectionStatus"] = ctx.ConnectionStatus ?? string.Empty
            };

            // Mask request body & form data
            if (ctx.RequestBody != null)
            {
                dict["RequestBody"] = "REDACTED";
            }
            else
            {
                dict["RequestBody"] = string.Empty;
            }

            if (ctx.QueryParameters != null)
            {
                dict["QueryParameters"] = MaskDictionary(ctx.QueryParameters);
            }
            else
            {
                dict["QueryParameters"] = new Dictionary<string, string>();
            }

            return dict;
        }

        private static Dictionary<string, object> MaskSensitiveData(Dictionary<string, object> context)
        {
            var masked = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in context)
            {
                masked[kvp.Key] = SensitiveProperties.Contains(kvp.Key)
                    ? "[REDACTED]"
                    : kvp.Value;
            }
            return masked;
        }

        private static Dictionary<string, string> MaskDictionary(IDictionary<string, string> source)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in source)
            {
                result[kvp.Key] = SensitiveQueryParams.Contains(kvp.Key)
                    ? "[REDACTED]"
                    : kvp.Value;
            }
            return result;
        }
    }
}
