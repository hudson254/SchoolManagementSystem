using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    public class Lecturer : BaseEntity, ITenantAwareEntity
    {
        public string? UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;

        /// <summary>
        /// National ID or Passport Number. Alphanumeric, preserves leading zeros.
        /// </summary>
        public string? NationalIdPassport { get; set; }
        public Guid? DepartmentId { get; set; }
        public DateTime HireDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Tracks the registration approval lifecycle.
        /// New registrations start as PendingCourseSelection.
        /// </summary>
        public RegistrationStatus RegistrationStatus { get; set; } = RegistrationStatus.PendingCourseSelection;

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Department Department { get; set; }
        public virtual ICollection<Timetable> Timetables { get; set; } = new List<Timetable>();

        // Accommodation navigation properties
        public virtual ICollection<Accommodation> Accommodations { get; set; } = new List<Accommodation>();
        public virtual ICollection<AccommodationAssignment> AccommodationAssignments { get; set; } = new List<AccommodationAssignment>();
        public virtual ICollection<House> Houses { get; set; } = new List<House>();
    }
}
