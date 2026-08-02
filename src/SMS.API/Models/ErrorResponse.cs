using System;
using System.Collections.Generic;

namespace SMS.API.Models
{
    /// <summary>
    /// Represents a structured error response returned by the API.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the error code for programmatic handling.
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user-friendly error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp of the error (UTC).
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
        /// Gets or sets the validation errors, if any.
        /// </summary>
        public IDictionary<string, string[]>? Errors { get; set; }

        /// <summary>
        /// Gets or sets additional error details (only in development).
        /// </summary>
        public string? Details { get; set; }
    }
}
