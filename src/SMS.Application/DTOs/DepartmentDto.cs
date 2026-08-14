using System;

namespace SMS.Application.DTOs
{
    /// <summary>
    /// Public API DTO for Department.
    /// Id is the UUID public identifier (BaseEntity.Id) — never a sequential integer.
    /// </summary>
    public class DepartmentDto
    {
        /// <summary>UUID public identifier for the department.</summary>
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
