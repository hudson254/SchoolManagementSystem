using System;

namespace SMS.Application.DTOs
{
    /// <summary>
    /// Public API DTO for Programme.
    /// Id is the UUID public identifier (BaseEntity.Id) — never a sequential integer.
    /// </summary>
    public class ProgrammeDto
    {
        /// <summary>UUID public identifier for the programme.</summary>
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Duration { get; set; }
        public bool IsActive { get; set; }
    }
}
