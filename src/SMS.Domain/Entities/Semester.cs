using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class Semester : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public DateTime? RegistrationStart { get; set; }
        public DateTime? RegistrationEnd { get; set; }

        [Required]
        public Guid AcademicYearId { get; set; }

        public int SemesterNumber { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsRegistrationOpen { get; set; } = false;

        public virtual AcademicYear? AcademicYear { get; set; }
        public virtual ICollection<UnitAllocation> UnitAllocations { get; set; } = new List<UnitAllocation>();
        public virtual ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();
        public virtual ICollection<Timetable> Timetables { get; set; } = new List<Timetable>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<AccommodationAssignment> AccommodationAssignments { get; set; } = new List<AccommodationAssignment>();
    }
}