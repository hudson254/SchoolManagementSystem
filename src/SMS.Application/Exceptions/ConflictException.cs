namespace SMS.Application.Exceptions
{
    public class ConflictException : Exception
    {
        public ConflictException()
            : base("A conflict occurred while processing your request.")
        {
        }

        public ConflictException(string message)
            : base(message)
        {
        }

        public ConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ConflictException(string entityName, string property, string value)
            : base($"A record with '{property}' = '{value}' already exists in '{entityName}'.")
        {
        }
    }
}