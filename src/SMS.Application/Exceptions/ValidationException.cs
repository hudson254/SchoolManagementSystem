using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when validation fails
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IDictionary<string, string[]> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        public ValidationException()
            : base("One or more validation failures have occurred.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ValidationException(string message)
            : base(message)
        {
            Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with validation errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        public ValidationException(IEnumerable<ValidationError> errors)
            : base("One or more validation failures have occurred.")
        {
            Errors = errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a dictionary of errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        public ValidationException(IDictionary<string, string[]> errors)
            : base("One or more validation failures have occurred.")
        {
            Errors = errors;
        }
    }

    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class.
        /// </summary>
        public ValidationError()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="errorMessage">The error message.</param>
        public ValidationError(string propertyName, string errorMessage)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }
    }
}