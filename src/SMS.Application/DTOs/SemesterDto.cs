using System;

namespace SMS.Application.DTOs
{
    /// <summary>
    /// Public API DTO for Semester.
    /// Id is the UUID public identifier (BaseEntity.Id) — never a sequential integer.
    /// </summary>
    public class SemesterDto
    {
        /// <summary>UUID public identifier for the semester.</summary>
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
