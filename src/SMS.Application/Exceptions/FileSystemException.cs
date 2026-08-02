using System;

namespace SMS.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when a file system operation fails.
    /// </summary>
    public class FileSystemException : Exception
    {
        /// <summary>
        /// Gets the error code for the file system exception.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemException"/> class.
        /// </summary>
        public FileSystemException()
            : base("A file operation failed. Please try again or contact support.")
        {
            ErrorCode = "FILE_SYSTEM_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemException"/> class with a specified error message.
        /// </summary>
        public FileSystemException(string message)
            : base(message)
        {
            ErrorCode = "FILE_SYSTEM_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemException"/> class with a specified error message and error code.
        /// </summary>
        public FileSystemException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemException"/> class with a specified error message and inner exception.
        /// </summary>
        public FileSystemException(string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = "FILE_SYSTEM_ERROR";
        }
    }
}
