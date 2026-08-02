using System;

namespace SMS.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when an operation times out.
    /// </summary>
    public class TimeoutException : Exception
    {
        /// <summary>
        /// Gets the error code for the timeout exception.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutException"/> class.
        /// </summary>
        public TimeoutException()
            : base("The operation timed out. Please try again.")
        {
            ErrorCode = "TIMEOUT_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutException"/> class with a specified error message.
        /// </summary>
        public TimeoutException(string message)
            : base(message)
        {
            ErrorCode = "TIMEOUT_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutException"/> class with a specified error message and error code.
        /// </summary>
        public TimeoutException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutException"/> class with a specified error message and inner exception.
        /// </summary>
        public TimeoutException(string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = "TIMEOUT_ERROR";
        }
    }
}
