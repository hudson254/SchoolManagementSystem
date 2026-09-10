using System;

namespace SMS.Application.DTOs
{
    /// <summary>
    /// Public API DTO for a Class (specific unit taught by a lecturer in a
    /// semester, with the recurring day/time schedule). Uses the existing
    /// SMS.Domain.Entities.Class domain entity — no parallel model is created.
    /// </summary>
    public class ClassDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }

        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;

        public Guid LecturerId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public string LecturerEmail { get; set; } = string.Empty;

        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;

        public int MaxCapacity { get; set; }
        public int CurrentEnrollment { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        /// <summary>Recurring day of week (e.g. "Monday").</summary>
        public string? ScheduleDay { get; set; }

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}