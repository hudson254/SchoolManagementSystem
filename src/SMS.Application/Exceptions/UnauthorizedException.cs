using System;

namespace SMS.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when a user is not authorized to perform an action
    /// </summary>
    public class UnauthorizedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
        /// </summary>
        public UnauthorizedException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnauthorizedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnauthorizedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a specified action.
        /// </summary>
        /// <param name="action">The action the user was trying to perform.</param>
        /// <param name="resource">The resource the user was trying to access.</param>
        public UnauthorizedException(string action, string resource)
            : base($"User is not authorized to '{action}' on '{resource}'.")
        {
        }
    }
}