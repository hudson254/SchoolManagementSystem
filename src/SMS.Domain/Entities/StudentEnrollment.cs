using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class StudentEnrollment : BaseEntity
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid UnitId { get; set; }

        [Required]
        public Guid SemesterId { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Status { get; set; } = "Enrolled";

        public DateTime? DropDate { get; set; }

        public virtual Student? Student { get; set; }
        public virtual Unit? Unit { get; set; }
        public virtual Semester? Semester { get; set; }
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}