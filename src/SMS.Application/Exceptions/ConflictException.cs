using System;

namespace SMS.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when a conflict occurs (e.g., duplicate data, version mismatch)
    /// </summary>
    public class ConflictException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class.
        /// </summary>
        public ConflictException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ConflictException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified entity name and property.
        /// </summary>
        /// <param name="name">The name of the entity.</param>
        /// <param name="property">The property that caused the conflict.</param>
        /// <param name="value">The value that caused the conflict.</param>
        public ConflictException(string name, string property, object value)
            : base($"Entity '{name}' with '{property}' = '{value}' already exists.")
        {
        }
    }
}