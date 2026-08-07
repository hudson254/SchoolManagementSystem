using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SMS.Application.Common;

namespace SMS.API.Models
{
    /// <summary>
    /// Represents a structured, standardized API error response.
    /// Follows the enterprise envelope contract:
    /// <code>
    /// {
    ///   "success": false,
    ///   "code": "VALIDATION_ERROR",
    ///   "message": "Please correct the highlighted fields.",
    ///   "errors": { "Email": ["Email is required."] }
    /// }
    /// </code>
    /// Never contains stack traces, SQL, file paths, or internal implementation details.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Indicates the operation failed.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the machine-readable error code for programmatic handling.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user-friendly, actionable error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp of the error.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the correlation ID for request tracing.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the request path that caused the error.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the field-level validation errors, if any.
        /// </summary>
        public IDictionary<string, string[]>? Errors { get; set; }

        /// <summary>
        /// Gets or sets the error severity classification.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Medium;

        /// <summary>
        /// Gets or sets the error category classification.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ErrorCategory Category { get; set; } = ErrorCategory.Unknown;

        // Backward-compatible aliases (kept for existing consumers/tests):
        /// <summary>
        /// Backward-compatible alias for <see cref="Code"/>.
        /// </summary>
        public string ErrorCode
        {
            get => Code;
            set => Code = value;
        }

        /// <summary>
        /// Gets or sets additional error details (only populated in development).
        /// </summary>
        public string? Details { get; set; }
    }
}
