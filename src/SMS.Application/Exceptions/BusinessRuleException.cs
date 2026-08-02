using System;

namespace SMS.Application.Exceptions
{
    /// <summary>
    /// Exception thrown when a business rule is violated
    /// </summary>
    public class BusinessRuleException : Exception
    {
        /// <summary>
        /// Gets the error code associated with the business rule violation.
        /// </summary>
        public string? ErrorCode { get; }

        /// <summary>
        /// Gets the rule name that was violated.
        /// </summary>
        public string? RuleName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessRuleException"/> class.
        /// </summary>
        public BusinessRuleException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessRuleException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public BusinessRuleException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessRuleException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public BusinessRuleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessRuleException"/> class with a specified rule name and details.
        /// </summary>
        /// <param name="ruleName">The name of the violated business rule.</param>
        /// <param name="details">Additional details about the violation.</param>
        public BusinessRuleException(string ruleName, string details)
            : base($"Business rule '{ruleName}' was violated: {details}")
        {
            RuleName = ruleName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessRuleException"/> class with a specified error message and error code.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The error code associated with the business rule violation.</param>
        /// <param name="ruleName">The name of the violated business rule (optional).</param>
        public BusinessRuleException(string message, string errorCode, string? ruleName = null)
            : base(message)
        {
            ErrorCode = errorCode;
            RuleName = ruleName;
        }
    }
}
