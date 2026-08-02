using System;

namespace SMS.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when a database operation fails.
    /// </summary>
    public class DatabaseException : Exception
    {
        /// <summary>
        /// Gets the error code for the database exception.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseException"/> class.
        /// </summary>
        public DatabaseException()
            : base("A database error occurred while processing your request.")
        {
            ErrorCode = "DB_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseException"/> class with a specified error message.
        /// </summary>
        public DatabaseException(string message)
            : base(message)
        {
            ErrorCode = "DB_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseException"/> class with a specified error message and error code.
        /// </summary>
        public DatabaseException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseException"/> class with a specified error message and inner exception.
        /// </summary>
        public DatabaseException(string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = "DB_ERROR";
        }
    }
}
