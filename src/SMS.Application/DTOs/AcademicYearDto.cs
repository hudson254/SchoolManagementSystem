using System;

namespace SMS.Application.DTOs
{
    /// <summary>
    /// Public API DTO for AcademicYear.
    /// Id is the UUID public identifier (BaseEntity.Id) — never a sequential integer.
    /// </summary>
    public class AcademicYearDto
    {
        /// <summary>UUID public identifier for the academic year.</summary>
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
