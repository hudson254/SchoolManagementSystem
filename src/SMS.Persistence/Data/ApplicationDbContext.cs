using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SMS.Certificates.Domain.Entities;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Persistence.Data
{
    public partial class ApplicationDbContext : IdentityDbContext<User>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITenantContext _tenantContext;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentUserService currentUserService,
            ITenantContext tenantContext)
            : base(options)
        {
            _currentUserService = currentUserService;
            _tenantContext = tenantContext;
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Lane> Lanes { get; set; }
        public DbSet<House> Houses { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<Timetable> Timetables { get; set; }
        public DbSet<Accommodation> Accommodations { get; set; }
        public DbSet<AccommodationAssignment> AccommodationAssignments { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Programme> Programmes { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
        public DbSet<Block> Blocks { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<LectureNote> LectureNotes { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        public DbSet<ProgrammeUnit> ProgrammeUnits { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<StudentEnrollment> StudentEnrollments { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<UnitAllocation> UnitAllocations { get; set; }
        // UserRoles is already defined on IdentityDbContext<User> base class.
        // The derived UserRole type is mapped via TPH using AspNetUserRoles table.
        // Do NOT add a separate DbSet<UserRole> here - it's inherited.
        public DbSet<ReportVerification> ReportVerifications { get; set; }
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
        public DbSet<CourseOffering> CourseOfferings { get; set; }
        public DbSet<CourseOfferingUnit> CourseOfferingUnits { get; set; }
        public DbSet<CourseOfferingEnrollment> CourseOfferingEnrollments { get; set; }
        public DbSet<CourseOfferingLecturer> CourseOfferingLecturers { get; set; }
        public DbSet<AssignmentIssueReport> AssignmentIssueReports { get; set; }
        public DbSet<Title> Titles { get; set; }
        public DbSet<UploadFile> UploadFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Title entity
            modelBuilder.Entity<Title>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Code).IsRequired().HasMaxLength(50);
                entity.Property(t => t.DisplayText).IsRequired().HasMaxLength(100);
                entity.Property(t => t.Language).IsRequired().HasMaxLength(10);
                entity.Property(t => t.Category).HasMaxLength(50);
                entity.Property(t => t.NormalizedCode).HasMaxLength(50);
                entity.HasIndex(t => new { t.Code, t.Language }).IsUnique();
                entity.HasIndex(t => t.NormalizedCode);
                entity.HasIndex(t => t.IsActive);
            });

            // Configure Lane entity
            modelBuilder.Entity<Lane>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.LaneName).IsRequired().HasMaxLength(100);
                entity.Property(l => l.Description).HasMaxLength(500);
                entity.Property(l => l.NumberingFormat).HasMaxLength(20);
                entity.HasIndex(l => new { l.LaneName, l.TenantId }).IsUnique();
                entity.HasMany(l => l.Houses)
                    .WithOne(h => h.Lane)
                    .HasForeignKey(h => h.LaneId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasQueryFilter(l => !l.IsDeleted);
            });

            // Configure House entity
            modelBuilder.Entity<House>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.HouseNumber).IsRequired().HasMaxLength(20);
                entity.Property(h => h.Status).IsRequired().HasMaxLength(30);
                entity.Property(h => h.Notes).HasMaxLength(500);
                entity.HasIndex(h => new { h.LaneId, h.HouseNumber }).IsUnique();

                entity.HasOne(h => h.Lane)
                    .WithMany(l => l.Houses)
                    .HasForeignKey(h => h.LaneId)
                    .OnDelete(DeleteBehavior.Cascade);

                // FIX: Explicitly configure Occupant as Student and LecturerOccupant as Lecturer
                // using separate FK columns to avoid the shadow FK conflict.
                // House.OccupantId is the FK for Student (nullable, one-to-many)
                entity.HasOne(h => h.Occupant)
                    .WithMany(s => s.Houses)
                    .HasForeignKey(h => h.OccupantId)
                    .OnDelete(DeleteBehavior.SetNull);

                // House.LecturerOccupant maps to Lecturer with the SAME OccupantId column
                // This is intentionally using the same column since a house can only have
                // one occupant type at a time. The OccupantType discriminator determines
                // which navigation property is valid.
                entity.HasOne(h => h.LecturerOccupant)
                    .WithMany(l => l.Houses)
                    .HasForeignKey(h => h.OccupantId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(h => h.Semester)
                    .WithMany()
                    .HasForeignKey(h => h.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(h => !h.IsDeleted);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.FirstName).HasMaxLength(100);
                entity.Property(u => u.LastName).HasMaxLength(100);
                entity.Property(u => u.MiddleName).HasMaxLength(100);
                entity.Property(u => u.Title).HasMaxLength(50);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasQueryFilter(u => !u.IsDeleted);
            });

            // FIX: Explicitly configure UserRole entity to avoid shadow FK UserId1/RoleId1
            // IdentityUserRole<string> already defines UserId (string) and RoleId (string)
            // with the composite key on the base IdentityUserRole<string> type.
            // The UserRole class adds navigation properties User and Role.
            // We need to explicitly tell EF Core to use the inherited UserId/RoleId properties
            // from IdentityUserRole<string> as the FKs for the navigation properties.
            // NOTE: We do NOT call HasKey() here because the key is already configured
            // on IdentityUserRole<string> by the Identity framework. Configuring a key
            // on a derived type would cause EF Core error.
            modelBuilder.Entity<UserRole>(entity =>
            {
                // Explicitly map User navigation to UserId FK (inherited from IdentityUserRole<string>)
                entity.HasOne(ur => ur.User)
                    .WithMany()
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Explicitly map Role navigation to RoleId FK (inherited from IdentityUserRole<string>)
                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Student entity
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.StudentNumber).IsUnique();
                entity.HasIndex(s => s.Email).IsUnique();

                entity.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(s => s.LastName).IsRequired().HasMaxLength(100);
                entity.Property(s => s.MiddleName).HasMaxLength(100);
                entity.Property(s => s.Title).HasMaxLength(50);
                entity.Property(s => s.Email).IsRequired().HasMaxLength(200);
                entity.Property(s => s.StudentNumber).IsRequired().HasMaxLength(50);
                entity.Property(s => s.PhoneNumber).HasMaxLength(20);
                entity.Property(s => s.Address).HasMaxLength(500);

                entity.HasOne(s => s.User)
                    .WithOne(u => u.Student)
                    .HasForeignKey<Student>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // FIX: Explicitly configure Programme relationship using ProgrammeId
                // Use .WithMany(p => p.Students) to match the inverse navigation in Programme
                entity.HasOne(s => s.Programme)
                    .WithMany(p => p.Students)
                    .HasForeignKey(s => s.ProgrammeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.CurrentSemester)
                    .WithMany()
                    .HasForeignKey(s => s.CurrentSemesterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(s => s.Enrollments)
                    .WithOne(e => e.Student)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(s => s.Grades)
                    .WithOne(g => g.Student)
                    .HasForeignKey(g => g.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(s => s.Houses)
                    .WithOne(h => h.Occupant)
                    .HasForeignKey(h => h.OccupantId)
                    .OnDelete(DeleteBehavior.SetNull);

                // FIX: AccommodationAssignments - one-to-many with Student
                // AccommodationAssignment.StudentId is the FK for this relationship.
                // The singular AccommodationAssignment navigation is removed from hte model
                // to avoid conflict - both can't use the same inverse navigation Student.
                // Use only the plural colleciton for the one-to-many.
                entity.HasMany(s => s.AccommodationAssignments)
                    .WithOne(aa => aa.Student)
                    .HasForeignKey(aa => aa.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(s => !s.IsDeleted);
            });

            // Configure Lecturer entity
            modelBuilder.Entity<Lecturer>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(l => l.LastName).IsRequired().HasMaxLength(100);
                entity.Property(l => l.MiddleName).HasMaxLength(100);
                entity.Property(l => l.Title).HasMaxLength(50);
                entity.Property(l => l.Email).IsRequired().HasMaxLength(200);
                entity.Property(l => l.PhoneNumber).HasMaxLength(20);
                entity.Property(l => l.EmployeeNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(l => l.EmployeeNumber).IsUnique();
                entity.HasIndex(l => l.Email).IsUnique();

                // FIX: Add Department relationship configuration
                entity.HasOne(l => l.Department)
                    .WithMany(d => d.Lecturers)
                    .HasForeignKey(l => l.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // FIX: AccommodationAssignments - one-to-many with Lecturer
                // AccommodationAssignment.LecturerId is the FK for this relationship.
                // The singular AccommodationAssignment navigation on Lecturer is removed
                // to avoid conflict with the plural collection using the same inverse navigation.
                entity.HasMany(l => l.AccommodationAssignments)
                    .WithOne(aa => aa.Lecturer)
                    .HasForeignKey(aa => aa.LecturerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure AccommodationAssignment entity
            modelBuilder.Entity<AccommodationAssignment>(entity =>
            {
                entity.HasKey(aa => aa.Id);
                entity.Property(aa => aa.Status).IsRequired().HasMaxLength(50);

                // FIX: Both Student and Lecturer relationships are already configured
                // from the Student and Lecturer side to avoid ambiguous FK mapping.
                // Only configure the House, Lane, Room, and Semester relationships here.
                entity.HasOne(aa => aa.House)
                    .WithMany(h => h.AccommodationAssignments)
                    .HasForeignKey(aa => aa.HouseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(aa => aa.Lane)
                    .WithMany()
                    .HasForeignKey(aa => aa.LaneId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(aa => aa.Room)
                    .WithMany()
                    .HasForeignKey(aa => aa.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(aa => aa.Semester)
                    .WithMany()
                    .HasForeignKey(aa => aa.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(aa => !aa.IsDeleted);
            });

            // Configure Accommodation entity
            modelBuilder.Entity<Accommodation>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Status).IsRequired().HasMaxLength(50);

                entity.HasOne(a => a.Student)
                    .WithMany(s => s.Accommodations)
                    .HasForeignKey(a => a.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Lecturer)
                    .WithMany(l => l.Accommodations)
                    .HasForeignKey(a => a.LecturerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.House)
                    .WithMany(h => h.Accommodations)
                    .HasForeignKey(a => a.HouseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Lane)
                    .WithMany()
                    .HasForeignKey(a => a.LaneId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Room)
                    .WithMany(r => r.Accommodations)
                    .HasForeignKey(a => a.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(a => !a.IsDeleted);
            });

            // Configure Course entity relationships
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasOne(c => c.Programme)
                    .WithMany(p => p.Courses)
                    .HasForeignKey(c => c.ProgrammeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Department)
                    .WithMany(d => d.Courses)
                    .HasForeignKey(c => c.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Semester)
                    .WithMany()
                    .HasForeignKey(c => c.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(c => c.Programmes)
                    .WithMany()
                    .UsingEntity<Dictionary<string, object>>(
                        "CourseProgrammes",
                        j => j.HasOne<Programme>().WithMany().HasForeignKey("ProgrammeId"),
                        j => j.HasOne<Course>().WithMany().HasForeignKey("CourseId"));
            });

            // Configure Room entity
            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasIndex(r => r.RoomNumber).IsUnique();
                entity.Property(r => r.RoomNumber).IsRequired().HasMaxLength(20);
                entity.Property(r => r.RoomType).HasMaxLength(50);
                entity.Property(r => r.Facilities).HasMaxLength(500);
                entity.HasMany(r => r.Accommodations)
                    .WithOne(a => a.Room)
                    .HasForeignKey(a => a.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasQueryFilter(r => !r.IsDeleted);
            });

            // Configure Enrollment entity indexes (RISK-18)
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasIndex(e => e.StudentId);
                entity.HasIndex(e => e.CourseId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.StudentId, e.CourseId });
                entity.HasIndex(e => e.EnrollmentDate);
            });

            // Configure Grade entity indexes (RISK-18)
            modelBuilder.Entity<Grade>(entity =>
            {
                entity.HasIndex(g => g.StudentId);
                entity.HasIndex(g => g.UnitId);
                entity.HasIndex(g => new { g.StudentId, g.UnitId });
                entity.HasIndex(g => g.SemesterId);
            });

            // Configure Notification entity indexes (RISK-18)
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => new { n.UserId, n.IsRead });
                entity.HasIndex(n => n.Type);
                entity.HasIndex(n => n.CreatedDate);
            });

            // Configure LoginHistory entity indexes (RISK-18)
            modelBuilder.Entity<LoginHistory>(entity =>
            {
                entity.HasIndex(h => h.UserId);
                entity.HasIndex(h => new { h.UserId, h.LoginTime });
                entity.HasIndex(h => h.LoginTime);
                entity.HasIndex(h => new { h.IsSuccessful, h.LoginTime });
            });

            // Configure AuditLog entity indexes (RISK-18)
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => a.Timestamp);
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.Action);
                entity.HasIndex(a => a.EntityName);
                entity.HasIndex(a => new { a.UserId, a.Timestamp });
            });

            // Configure ReportVerification entity
            modelBuilder.Entity<ReportVerification>(entity =>
            {
                entity.HasKey(rv => rv.Id);
                entity.HasIndex(rv => rv.ReportId).IsUnique();
                entity.HasIndex(rv => rv.VerificationToken).IsUnique();
                entity.HasIndex(rv => rv.ReportType);
                entity.HasIndex(rv => rv.GeneratedDate);
                entity.HasIndex(rv => rv.Status);

                entity.Property(rv => rv.ReportId).IsRequired().HasMaxLength(50);
                entity.Property(rv => rv.VerificationToken).IsRequired().HasMaxLength(128);
                entity.Property(rv => rv.ReportType).IsRequired().HasMaxLength(100);
                entity.Property(rv => rv.ReportName).IsRequired().HasMaxLength(500);
                entity.Property(rv => rv.GeneratedByUserId).IsRequired().HasMaxLength(100);
                entity.Property(rv => rv.GeneratedByUserName).HasMaxLength(200);
                entity.Property(rv => rv.SHA256Hash).IsRequired().HasMaxLength(128);
                entity.Property(rv => rv.HashAlgorithm).IsRequired().HasMaxLength(50);
                entity.Property(rv => rv.Status).HasConversion<string>().HasMaxLength(50);
                entity.Property(rv => rv.RevocationReason).HasMaxLength(500);
                entity.Property(rv => rv.RevokedBy).HasMaxLength(100);
                entity.Property(rv => rv.Remarks).HasMaxLength(2000);
                entity.Property(rv => rv.WatermarkText).HasMaxLength(500);

                entity.HasOne(rv => rv.GeneratedByUser)
                    .WithMany()
                    .HasForeignKey(rv => rv.GeneratedByUserId)
                    .HasPrincipalKey(u => u.Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(rv => !rv.IsDeleted);
            });

            // Configure Certificate entities
            modelBuilder.Entity<Certificate>(entity =>
            {
                entity.ToTable("Certificates");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.CertificateNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.VerificationToken)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.FinalGrade)
                    .HasMaxLength(5);

                entity.Property(e => e.Classification)
                    .HasMaxLength(50);

                entity.Property(e => e.PdfPath)
                    .HasMaxLength(500);

                entity.Property(e => e.QrCodePath)
                    .HasMaxLength(500);

                entity.Property(e => e.RevocationReason)
                    .HasMaxLength(500);

                entity.HasIndex(e => e.CertificateNumber)
                    .IsUnique();

                entity.HasIndex(e => e.VerificationToken)
                    .IsUnique();

                entity.HasIndex(e => e.StudentId);
                entity.HasIndex(e => e.CourseOfferingId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.IssueDate);

                entity.HasOne(e => e.Template)
                    .WithMany()
                    .HasForeignKey(e => e.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CertificateTemplate>(entity =>
            {
                entity.ToTable("CertificateTemplates");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.Property(e => e.Version)
                    .HasMaxLength(20);

                entity.Property(e => e.FilePath)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.FieldMappings)
                    .HasMaxLength(5000);

                entity.Property(e => e.LogoPath)
                    .HasMaxLength(500);

                entity.Property(e => e.WatermarkPath)
                    .HasMaxLength(500);

                entity.HasIndex(e => e.Name)
                    .IsUnique();

                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.IsDefault);
            });

            modelBuilder.Entity<DigitalSignature>(entity =>
            {
                entity.ToTable("DigitalSignatures");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ImagePath)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.HasIndex(e => new { e.Name, e.Type })
                    .IsUnique();
            });

            modelBuilder.Entity<CertificateAuditLog>(entity =>
            {
                entity.ToTable("CertificateAuditLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.CertificateId)
                    .IsRequired();

                entity.Property(e => e.CertificateNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.UserRole)
                    .HasMaxLength(50);

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(45);

                entity.Property(e => e.SessionId)
                    .HasMaxLength(100);

                entity.Property(e => e.Outcome)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Timestamp)
                    .IsRequired();

                entity.HasIndex(e => e.CertificateId);
                entity.HasIndex(e => e.CertificateNumber);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
            });

            // FIX: Configure Class entity relationships
            modelBuilder.Entity<Class>(entity =>
            {
                entity.HasOne(c => c.Lecturer)
                    .WithMany()
                    .HasForeignKey(c => c.LecturerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Unit)
                    .WithMany()
                    .HasForeignKey(c => c.UnitId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Semester)
                    .WithMany()
                    .HasForeignKey(c => c.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // FIX: Configure LectureNote entity relationships
            modelBuilder.Entity<LectureNote>(entity =>
            {
                entity.HasOne(ln => ln.Lecturer)
                    .WithMany()
                    .HasForeignKey(ln => ln.LecturerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ln => ln.Unit)
                    .WithMany()
                    .HasForeignKey(ln => ln.UnitId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // FIX: Configure UnitAllocation entity relationships
            modelBuilder.Entity<UnitAllocation>(entity =>
            {
                entity.HasOne(ua => ua.Lecturer)
                    .WithMany()
                    .HasForeignKey(ua => ua.LecturerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ua => ua.Unit)
                    .WithMany()
                    .HasForeignKey(ua => ua.UnitId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ua => ua.Semester)
                    .WithMany()
                    .HasForeignKey(ua => ua.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ua => ua.CourseOffering)
                    .WithMany()
                    .HasForeignKey(ua => ua.CourseOfferingId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // FIX: Configure StudentEnrollment entity relationships
            modelBuilder.Entity<StudentEnrollment>(entity =>
            {
                entity.HasOne(se => se.Student)
                    .WithMany()
                    .HasForeignKey(se => se.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(se => se.Unit)
                    .WithMany()
                    .HasForeignKey(se => se.UnitId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(se => se.Semester)
                    .WithMany()
                    .HasForeignKey(se => se.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // FIX: Configure ProgrammeUnit entity relationships
            modelBuilder.Entity<ProgrammeUnit>(entity =>
            {
                entity.HasKey(pu => new { pu.ProgrammeId, pu.UnitId });

                entity.HasOne(pu => pu.Programme)
                    .WithMany()
                    .HasForeignKey(pu => pu.ProgrammeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pu => pu.Unit)
                    .WithMany()
                    .HasForeignKey(pu => pu.UnitId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // FIX: Configure CourseOffering entity relationships
            // The CourseOffering entity uses [Table("course_offerings")]
            // and maps to Course, AcademicYear, and Semester
            modelBuilder.Entity<CourseOffering>(entity =>
            {
                entity.HasKey(co => co.Id);
                entity.Property(co => co.OfferingCode).IsRequired().HasMaxLength(50);
                entity.Property(co => co.Intake).HasMaxLength(100);
                entity.Property(co => co.Notes).HasMaxLength(1000);
                entity.Property(co => co.Status).HasConversion<int>();
                entity.HasIndex(co => co.OfferingCode).IsUnique();

                entity.HasOne(co => co.Course)
                    .WithMany()
                    .HasForeignKey(co => co.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(co => co.AcademicYear)
                    .WithMany()
                    .HasForeignKey(co => co.AcademicYearId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(co => co.Semester)
                    .WithMany()
                    .HasForeignKey(co => co.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(co => co.Units)
                    .WithOne(cou => cou.CourseOffering)
                    .HasForeignKey(cou => cou.CourseOfferingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(co => co.Enrollments)
                    .WithOne(coe => coe.CourseOffering)
                    .HasForeignKey(coe => coe.CourseOfferingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(co => co.Lecturers)
                    .WithOne(col => col.CourseOffering)
                    .HasForeignKey(col => col.CourseOfferingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasQueryFilter(co => !co.IsDeleted);
            });

            // Apply global tenant query filters.
            //
            // RISK-04 FIX: The previous implementation captured a Guid constant
            // value (resolved from _tenantContext.TenantId) at model-build time.
            // Since OnModelCreating is called once and the model is cached, the
            // resolved Guid (which could be Guid.Empty if the tenant context was
            // unset at first use) was permanently baked into the cached model
            // for ALL subsequent requests, causing cross-tenant data leakage.
            //
            // The fix captures the DbContext instance itself (via `this`) in
            // the expression tree, so the tenant Guid is resolved per-query via
            // the CurrentTenantGuid property below. Because the DbContext is
            // scoped per request, the filter is re-evaluated for each request
            // with the correct tenant context.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var tenantIdProperty = entityType.FindProperty("TenantId");

                // Apply the tenant filter ONLY to entities that explicitly
                // implement ITenantAwareEntity. SaveChangesAsync also only
                // assigns TenantId to ITenantAwareEntity entities, so entities
                // with a TenantId column that are NOT tenant-aware (e.g.
                // PasswordResetRequest, AuditLog) would otherwise be saved with
                // Guid.Empty and become invisible to every tenant-scoped
                // query. This mismatch caused the
                // GetPendingAsync_ReturnsOnlyPendingRequests integration test
                // failure (Expected 2, Actual 0).
                var isTenantAware = typeof(ITenantAwareEntity).IsAssignableFrom(entityType.ClrType);
                if (tenantIdProperty != null && tenantIdProperty.ClrType == typeof(Guid) && isTenantAware)
                {
                    if (entityType.ClrType == typeof(User) || entityType.ClrType == typeof(Role) || entityType.ClrType == typeof(UserRole))
                        continue;

                    // Skip derived entity types in TPH hierarchies (e.g., UserRole is derived
                    // from IdentityUserRole<string>) because query filters can only be applied
                    // to root entity types in a TPH hierarchy.
                    if (entityType.BaseType != null)
                        continue;

                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var property = System.Linq.Expressions.Expression.Property(parameter, "TenantId");

                    // Capture `this` (the scoped DbContext instance) so
                    // CurrentTenantGuid is evaluated at query-execution time,
                    // not at model-build time.
                    var thisExpr = System.Linq.Expressions.Expression.Constant(this);
                    var tenantGuidProperty = System.Linq.Expressions.Expression.Property(thisExpr, nameof(CurrentTenantGuid));
                    var condition = System.Linq.Expressions.Expression.Equal(property, tenantGuidProperty);
                    var lambda = System.Linq.Expressions.Expression.Lambda(condition, parameter);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }

        /// <summary>
        /// Resolves the current tenant Guid from the scoped ITenantContext at
        /// query-execution time. Used by the global tenant query filter
        /// expression so the filter is evaluated per-request rather than being
        /// baked into the cached model at startup (RISK-04 fix).
        ///
        /// If the tenant context is unavailable or the TenantId cannot be
        /// parsed, this returns Guid.Empty. Callers that require tenant
        /// isolation should verify the returned value is not Guid.Empty before
        /// executing tenant-scoped queries.
        /// </summary>
        public Guid CurrentTenantGuid
        {
            get
            {
                if (_tenantContext == null || string.IsNullOrWhiteSpace(_tenantContext.TenantId))
                    return Guid.Empty;

                return Guid.TryParse(_tenantContext.TenantId, out var guid) ? guid : Guid.Empty;
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.UtcNow;
                        entry.Entity.CreatedBy = _currentUserService?.UserId;
                        if (entry.Entity is ITenantAwareEntity tenantAware)
                        {
                            if (_tenantContext != null && Guid.TryParse(_tenantContext.TenantId, out var tenantId))
                            {
                                tenantAware.TenantId = tenantId;
                            }
                        }
                        break;

                    case EntityState.Modified:
                        entry.Entity.ModifiedDate = DateTime.UtcNow;
                        entry.Entity.ModifiedBy = _currentUserService?.UserId;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedDate = DateTime.UtcNow;
                        entry.Entity.DeletedBy = _currentUserService?.UserId;
                        break;
                }
            }

            foreach (var entry in ChangeTracker.Entries<User>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        if (_tenantContext != null && Guid.TryParse(_tenantContext.TenantId, out var userTenantId))
                        {
                            entry.Entity.TenantId = userTenantId;
                        }
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = DateTime.UtcNow;
                        break;
                }
            }

            foreach (var entry in ChangeTracker.Entries<UserRole>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (_tenantContext != null && Guid.TryParse(_tenantContext.TenantId, out var urTenantId))
                        {
                            entry.Entity.TenantId = urTenantId;
                        }
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
