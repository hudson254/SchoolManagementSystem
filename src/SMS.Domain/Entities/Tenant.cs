using System.ComponentModel.DataAnnotations;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents a tenant (school/organization) in the multi-tenant system
    /// </summary>
    public class Tenant : BaseEntity
    {
        /// <summary>
        /// Name of the tenant/school
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Organization/company name
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Organization { get; set; } = string.Empty;

        /// <summary>
        /// Subdomain for tenant-specific access (e.g., school1.school.com)
        /// </summary>
        [MaxLength(50)]
        public string? Subdomain { get; set; }

        /// <summary>
        /// Contact phone number
        /// </summary>
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Contact email address
        /// </summary>
        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        /// <summary>
        /// Physical address
        /// </summary>
        [MaxLength(500)]
        public string? Address { get; set; }

        /// <summary>
        /// Logo URL or path
        /// </summary>
        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        /// <summary>
        /// Whether the tenant is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Subscription expiry date
        /// </summary>
        public DateTime? SubscriptionExpiry { get; set; }

        /// <summary>
        /// Maximum number of students allowed
        /// </summary>
        public int MaxStudents { get; set; } = 100;

        /// <summary>
        /// Maximum number of lecturers allowed
        /// </summary>
        public int MaxLecturers { get; set; } = 50;

        /// <summary>
        /// Maximum storage space in MB
        /// </summary>
        public int MaxStorageMB { get; set; } = 10240;

        /// <summary>
        /// Theme color (primary color)
        /// </summary>
        [MaxLength(7)]
        public string? ThemeColor { get; set; } = "#576426";

        /// <summary>
        /// Navigation property for users
        /// </summary>
        public virtual ICollection<User> Users { get; set; } = new List<User>();

        /// <summary>
        /// Navigation property for students
        /// </summary>
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();

        /// <summary>
        /// Navigation property for lecturers
        /// </summary>
        public virtual ICollection<Lecturer> Lecturers { get; set; } = new List<Lecturer>();

        /// <summary>
        /// Navigation property for courses
        /// </summary>
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

        /// <summary>
        /// Navigation property for departments
        /// </summary>
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

        /// <summary>
        /// Navigation property for programmes
        /// </summary>
        public virtual ICollection<Programme> Programmes { get; set; } = new List<Programme>();

        /// <summary>
        /// Navigation property for academic years
        /// </summary>
        public virtual ICollection<AcademicYear> AcademicYears { get; set; } = new List<AcademicYear>();

        /// <summary>
        /// Navigation property for buildings
        /// </summary>
        public virtual ICollection<Building> Buildings { get; set; } = new List<Building>();
    }
}