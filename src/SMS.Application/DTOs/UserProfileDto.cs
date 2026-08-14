using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string DisplayName => BuildDisplayName();
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

        private string BuildDisplayName()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Title))
                parts.Add(Title);
            parts.Add(FirstName);
            if (!string.IsNullOrWhiteSpace(MiddleName))
                parts.Add(MiddleName);
            parts.Add(LastName);
            return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        }
    }
}
