namespace SMS.Application.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException()
            : base("You do not have permission to access this resource.")
        {
        }

        public ForbiddenException(string message)
            : base(message)
        {
        }

        public ForbiddenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ForbiddenException(string resource, string action)
            : base($"You do not have permission to {action} '{resource}'.")
        {
        }
    }
}