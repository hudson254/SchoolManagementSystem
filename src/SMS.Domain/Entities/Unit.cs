using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    [Table("units")]
    public class Unit : BaseEntity, ITenantAwareEntity
    {
        [Column("name")]
        [MaxLength(200)]
        [Required]
        public string Name { get; set; } = string.Empty;

        [Column("code")]
        [MaxLength(50)]
        [Required]
        public string Code { get; set; } = string.Empty;

        [Column("description")]
        [MaxLength(1000)]
        public string? Description { get; set; }

        [Column("credits")]
        public int Credits { get; set; }

        [Column("contact_hours")]
        public int ContactHours { get; set; }

        [Column("semester")]
        public int Semester { get; set; }

        [Column("course_id")]
        public Guid CourseId { get; set; }

        [Column("prerequisite_unit_id")]
        public Guid? PrerequisiteUnitId { get; set; }

        [Column("learning_outcomes")]
        public string? LearningOutcomes { get; set; }

        [Column("assessment_methods")]
        public string? AssessmentMethods { get; set; }

        [Column("recommended_textbooks")]
        public string? RecommendedTextbooks { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey(nameof(CourseId))]
        public virtual Course? Course { get; set; }

        [ForeignKey(nameof(PrerequisiteUnitId))]
        public virtual Unit? Prerequisite { get; set; }

        public virtual ICollection<Enrollment>? Enrollments { get; set; }
        public virtual ICollection<Assignment>? Assignments { get; set; }
        public virtual ICollection<Grade>? Grades { get; set; }
    }
}

