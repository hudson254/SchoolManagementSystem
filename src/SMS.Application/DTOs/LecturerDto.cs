using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    public class LecturerDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string DisplayName => BuildDisplayName();
        public string Email { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public string? UserId { get; set; }
        public int CourseCount { get; set; }
        public int UnitCount { get; set; }
        public int StudentCount { get; set; }
        public string? Specialization { get; set; }
        public string? Qualifications { get; set; }
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// National ID or Passport Number. Alphanumeric, preserves leading zeros.
        /// Only visible to authorized personnel.
        /// </summary>
        public string? NationalIdPassport { get; set; }

        /// <summary>
        /// Tracks the registration approval lifecycle.
        /// </summary>
        public string RegistrationStatus { get; set; } = "PendingCourseSelection";

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

    public class LecturerDetailsDto : LecturerDto
    {
        public List<UnitSummaryDto> AssignedUnits { get; set; } = new List<UnitSummaryDto>();
    }
}
