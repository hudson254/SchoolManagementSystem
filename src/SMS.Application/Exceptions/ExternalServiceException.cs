using System;

namespace SMS.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when an external service call fails.
    /// </summary>
    public class ExternalServiceException : Exception
    {
        /// <summary>
        /// Gets the name of the external service that failed.
        /// </summary>
        public string ServiceName { get; }

        /// <summary>
        /// Gets the error code for the external service exception.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalServiceException"/> class.
        /// </summary>
        public ExternalServiceException()
            : base("An external service is currently unavailable. Please try again later.")
        {
            ServiceName = "Unknown";
            ErrorCode = "EXTERNAL_SERVICE_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalServiceException"/> class with a specified service name.
        /// </summary>
        public ExternalServiceException(string serviceName)
            : base($"The external service '{serviceName}' is currently unavailable. Please try again later.")
        {
            ServiceName = serviceName;
            ErrorCode = "EXTERNAL_SERVICE_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalServiceException"/> class with a specified service name and error code.
        /// </summary>
        public ExternalServiceException(string serviceName, string errorCode)
            : base($"The external service '{serviceName}' is currently unavailable. Please try again later.")
        {
            ServiceName = serviceName;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalServiceException"/> class with a specified service name and inner exception.
        /// </summary>
        public ExternalServiceException(string serviceName, Exception innerException)
            : base($"The external service '{serviceName}' is currently unavailable. Please try again later.", innerException)
        {
            ServiceName = serviceName;
            ErrorCode = "EXTERNAL_SERVICE_ERROR";
        }
    }
}
