using System;

namespace SMS.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when a background job fails.
    /// </summary>
    public class BackgroundJobException : Exception
    {
        /// <summary>
        /// Gets the error code for the background job exception.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobException"/> class.
        /// </summary>
        public BackgroundJobException()
            : base("A background job failed. The system will retry automatically.")
        {
            ErrorCode = "BACKGROUND_JOB_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobException"/> class with a specified error message.
        /// </summary>
        public BackgroundJobException(string message)
            : base(message)
        {
            ErrorCode = "BACKGROUND_JOB_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobException"/> class with a specified error message and error code.
        /// </summary>
        public BackgroundJobException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobException"/> class with a specified error message and inner exception.
        /// </summary>
        public BackgroundJobException(string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = "BACKGROUND_JOB_ERROR";
        }
    }
}
