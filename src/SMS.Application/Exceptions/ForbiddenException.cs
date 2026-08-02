using System;

namespace SMS.Application.Exceptions
{
    public class ForbiddenException : Exception
    {
        public string? Resource { get; }

        public ForbiddenException() : base() { }
        public ForbiddenException(string message) : base(message) { }
        public ForbiddenException(string resource, string userId) 
            : base($"User '{userId}' does not have permission to access '{resource}'.") 
        {
            Resource = resource;
        }
    }
}
