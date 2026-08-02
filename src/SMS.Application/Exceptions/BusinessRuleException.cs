namespace SMS.Application.Exceptions
{
    public class BusinessRuleException : Exception
    {
        public string RuleName { get; }

        public BusinessRuleException(string ruleName, string message)
            : base($"Business rule '{ruleName}' violated: {message}")
        {
            RuleName = ruleName;
        }

        public BusinessRuleException(string ruleName, string message, Exception innerException)
            : base($"Business rule '{ruleName}' violated: {message}", innerException)
        {
            RuleName = ruleName;
        }

        public BusinessRuleException(string message)
            : base(message)
        {
            RuleName = "BusinessRule";
        }
    }
}