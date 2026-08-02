using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    public class LecturerDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
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
    }

    public class LecturerDetailsDto : LecturerDto
    {
        public List<UnitSummaryDto> AssignedUnits { get; set; } = new List<UnitSummaryDto>();
    }
}
