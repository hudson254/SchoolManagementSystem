using System;

namespace SMS.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when a network operation fails.
    /// </summary>
    public class NetworkException : Exception
    {
        /// <summary>
        /// Gets the error code for the network exception.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkException"/> class.
        /// </summary>
        public NetworkException()
            : base("A network error occurred. Please check your connection and try again.")
        {
            ErrorCode = "NETWORK_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkException"/> class with a specified error message.
        /// </summary>
        public NetworkException(string message)
            : base(message)
        {
            ErrorCode = "NETWORK_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkException"/> class with a specified error message and error code.
        /// </summary>
        public NetworkException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkException"/> class with a specified error message and inner exception.
        /// </summary>
        public NetworkException(string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = "NETWORK_ERROR";
        }
    }
}
