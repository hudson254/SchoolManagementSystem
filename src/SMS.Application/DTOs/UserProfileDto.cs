using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? LastLoginIP { get; set; }
        public string? Organization { get; set; }
        public Guid TenantId { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}