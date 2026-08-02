using System.Text;

namespace SMS.Application.Exceptions
{
    public class ValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException()
            : base("One or more validation failures have occurred.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(string message)
            : base(message)
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(string propertyName, string errorMessage)
            : base(FormatErrorMessage(propertyName, errorMessage))
        {
            Errors = new Dictionary<string, string[]>
            {
                { propertyName, new[] { errorMessage } }
            };
        }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("One or more validation failures have occurred.")
        {
            Errors = errors;
        }

        public ValidationException(IEnumerable<(string PropertyName, string ErrorMessage)> errors)
            : base("One or more validation failures have occurred.")
        {
            Errors = errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }

        private static string FormatErrorMessage(string propertyName, string errorMessage)
        {
            return $"Validation failed for '{propertyName}': {errorMessage}";
        }

        public override string ToString()
        {
            if (Errors == null || Errors.Count == 0)
                return base.ToString();

            var sb = new StringBuilder();
            sb.AppendLine(base.Message);
            sb.AppendLine("Validation Errors:");

            foreach (var error in Errors)
            {
                sb.AppendLine($"  {error.Key}: {string.Join(", ", error.Value)}");
            }

            return sb.ToString();
        }
    }
}