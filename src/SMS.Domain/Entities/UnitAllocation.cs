using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    public class UnitAllocation : BaseEntity
    {
        [Required]
        public Guid LecturerId { get; set; }

        [Required]
        public Guid UnitId { get; set; }

        [Required]
        public Guid SemesterId { get; set; }

        public Guid? CourseOfferingId { get; set; }
        public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        [MaxLength(500)]
        public string? Notes { get; set; }

        public bool IsPrimary { get; set; } = true;

        public virtual Lecturer? Lecturer { get; set; }
        public virtual Unit? Unit { get; set; }
        public virtual Semester? Semester { get; set; }
        public virtual CourseOffering? CourseOffering { get; set; }
    }
}
