using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Common;

namespace SMS.Persistence.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>
    {
        private readonly ITenantResolver _tenantResolver;
        private readonly ICurrentUserService _currentUserService;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ITenantResolver tenantResolver,
            ICurrentUserService currentUserService)
            : base(options)
        {
            _tenantResolver = tenantResolver;
            _currentUserService = currentUserService;
        }

        // DbSets
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Programme> Programmes { get; set; }
        public DbSet<ProgrammeUnit> ProgrammeUnits { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<UnitAllocation> UnitAllocations { get; set; }
        public DbSet<StudentEnrollment> StudentEnrollments { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Timetable> Timetables { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<LectureNote> LectureNotes { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Block> Blocks { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<AccommodationAssignment> AccommodationAssignments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure all entities
            ConfigureTenant(modelBuilder);
            ConfigureUser(modelBuilder);
            ConfigureStudent(modelBuilder);
            ConfigureLecturer(modelBuilder);
            ConfigureCourse(modelBuilder);
            ConfigureDepartment(modelBuilder);
            ConfigureProgramme(modelBuilder);
            ConfigureProgrammeUnit(modelBuilder);
            ConfigureUnit(modelBuilder);
            ConfigureUnitAllocation(modelBuilder);
            ConfigureStudentEnrollment(modelBuilder);
            ConfigureSemester(modelBuilder);
            ConfigureAcademicYear(modelBuilder);
            ConfigureAssignment(modelBuilder);
            ConfigureAssignmentSubmission(modelBuilder);
            ConfigureGrade(modelBuilder);
            ConfigureAttendance(modelBuilder);
            ConfigureLectureNote(modelBuilder);
            ConfigureBuilding(modelBuilder);
            ConfigureBlock(modelBuilder);
            ConfigureRoom(modelBuilder);
            ConfigureAccommodationAssignment(modelBuilder);
            ConfigureNotification(modelBuilder);
            ConfigureAuditLog(modelBuilder);

            // Apply global query filters for multi-tenancy and soft delete
            ApplyGlobalFilters(modelBuilder);
        }

        private void ConfigureTenant(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Subdomain).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Organization).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Subdomain).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.LogoUrl).HasMaxLength(500);
                entity.Property(e => e.ThemeColor).HasMaxLength(7);
                
                // Seed default tenant
                entity.HasData(new Tenant
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Default Tenant",
                    Organization = "Main School",
                    Subdomain = "main",
                    PhoneNumber = "+254711000000",
                    Email = "admin@school.com",
                    IsActive = true,
                    MaxStudents = 1000,
                    MaxLecturers = 100,
                    MaxStorageMB = 10240,
                    ThemeColor = "#576426",
                    CreatedBy = "SYSTEM",
                    CreatedDate = DateTime.UtcNow
                });
            });
        }

        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Organization).HasMaxLength(200);
                entity.Property(e => e.RefreshToken).HasMaxLength(500);
                entity.Property(e => e.LastLoginIP).HasMaxLength(45);

                entity.HasOne(e => e.Tenant)
                    .WithMany(t => t.Users)
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Student)
                    .WithOne(s => s.User)
                    .HasForeignKey<Student>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Lecturer)
                    .WithOne(l => l.User)
                    .HasForeignKey<Lecturer>(l => l.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureStudent(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.StudentNumber }).IsUnique();
                entity.Property(e => e.StudentNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DateOfBirth).IsRequired();
                entity.Property(e => e.Gender).HasMaxLength(10);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.AcademicStatus).HasMaxLength(20);
                entity.Property(e => e.EmergencyContactName).HasMaxLength(100);
                entity.Property(e => e.EmergencyContactPhone).HasMaxLength(20);
                entity.Property(e => e.EmergencyContactRelation).HasMaxLength(50);

                entity.HasOne(e => e.Programme)
                    .WithMany(p => p.Students)
                    .HasForeignKey(e => e.ProgrammeId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.CurrentSemester)
                    .WithMany()
                    .HasForeignKey(e => e.CurrentSemesterId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureLecturer(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lecturer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.EmployeeNumber }).IsUnique();
                entity.Property(e => e.EmployeeNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Specialization).HasMaxLength(100);
                entity.Property(e => e.Qualifications).HasMaxLength(500);
                entity.Property(e => e.Biography).HasMaxLength(1000);
                entity.Property(e => e.OfficeLocation).HasMaxLength(100);
            });
        }

        private void ConfigureCourse(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.AdmissionRequirements).HasMaxLength(1000);
                entity.Property(e => e.Objectives).HasMaxLength(2000);

                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Courses)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureDepartment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.HeadOfDepartment).HasMaxLength(100);
            });
        }

        private void ConfigureProgramme(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Programme>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Programmes)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureProgrammeUnit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProgrammeUnit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ProgrammeId, e.UnitId, e.SemesterNumber }).IsUnique();

                entity.HasOne(e => e.Programme)
                    .WithMany(p => p.ProgrammeUnits)
                    .HasForeignKey(e => e.ProgrammeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Unit)
                    .WithMany(u => u.ProgrammeUnits)
                    .HasForeignKey(e => e.UnitId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureUnit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Unit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.LearningOutcomes).HasMaxLength(2000);
                entity.Property(e => e.AssessmentMethods).HasMaxLength(500);
                entity.Property(e => e.RecommendedTextbooks).HasMaxLength(500);

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Units)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Prerequisite)
                    .WithMany()
                    .HasForeignKey(e => e.PrerequisiteUnitId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureUnitAllocation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UnitAllocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.LecturerId, e.UnitId, e.SemesterId }).IsUnique();
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.Notes).HasMaxLength(500);

                entity.HasOne(e => e.Lecturer)
                    .WithMany(l => l.UnitAllocations)
                    .HasForeignKey(e => e.LecturerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Unit)
                    .WithMany(u => u.Allocations)
                    .HasForeignKey(e => e.UnitId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Semester)
                    .WithMany(s => s.UnitAllocations)
                    .HasForeignKey(e => e.SemesterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureStudentEnrollment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StudentEnrollment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.StudentId, e.UnitId, e.SemesterId }).IsUnique();
                entity.Property(e => e.Status).HasMaxLength(20);

                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Enrollments)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Unit)
                    .WithMany(u => u.Enrollments)
                    .HasForeignKey(e => e.UnitId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Semester)
                    .WithMany(s => s.Enrollments)
                    .HasForeignKey(e => e.SemesterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureSemester(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Semester>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);

                entity.HasOne(e => e.AcademicYear)
                    .WithMany(a => a.Semesters)
                    .HasForeignKey(e => e.AcademicYearId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureAcademicYear(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AcademicYear>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            });
        }

        private void ConfigureAssignment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Assignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Status).HasMaxLength(20);

                entity.HasOne(e => e.Unit)
                    .WithMany(u => u.Assignments)
                    .HasForeignKey(e => e.UnitId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Lecturer)
                    .WithMany(l => l.Assignments)
                    .HasForeignKey(e => e.LecturerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Semester)
                    .WithMany(s => s.Assignments)
                    .HasForeignKey(e => e.SemesterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureAssignmentSubmission(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AssignmentSubmission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.AssignmentId, e.StudentId }).IsUnique();
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.Feedback).HasMaxLength(1000);
                entity.Property(e => e.FilePath).HasMaxLength(500);

                entity.HasOne(e => e.Assignment)
                    .WithMany(a => a.Submissions)
                    .HasForeignKey(e => e.AssignmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Submissions)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureGrade(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Grade>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.GradeValue).HasMaxLength(2);
                entity.Property(e => e.Remarks).HasMaxLength(500);

                entity.HasOne(e => e.Enrollment)
                    .WithMany(e => e.Grades)
                    .HasForeignKey(e => e.EnrollmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Grades)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureAttendance(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasMaxLength(20);

                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Attendances)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Class)
                    .WithMany(c => c.Attendances)
                    .HasForeignKey(e => e.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureLectureNote(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LectureNote>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ContentType).HasMaxLength(100);

                entity.HasOne(e => e.Unit)
                    .WithMany(u => u.LectureNotes)
                    .HasForeignKey(e => e.UnitId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Lecturer)
                    .WithMany(l => l.LectureNotes)
                    .HasForeignKey(e => e.LecturerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureBuilding(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Building>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(200);
            });
        }

        private void ConfigureBlock(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Block>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

                entity.HasOne(e => e.Building)
                    .WithMany(b => b.Blocks)
                    .HasForeignKey(e => e.BuildingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureRoom(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.RoomNumber, e.BlockId }).IsUnique();
                entity.Property(e => e.RoomNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.RoomType).HasMaxLength(20);

                entity.HasOne(e => e.Block)
                    .WithMany(b => b.Rooms)
                    .HasForeignKey(e => e.BlockId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureAccommodationAssignment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccommodationAssignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.StudentId, e.SemesterId }).IsUnique();
                entity.Property(e => e.Status).HasMaxLength(20);

                entity.HasOne(e => e.Student)
                    .WithOne(s => s.AccommodationAssignment)
                    .HasForeignKey<AccommodationAssignment>(a => a.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Room)
                    .WithMany(r => r.AssignmentHistory)
                    .HasForeignKey(e => e.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Semester)
                    .WithMany(s => s.AccommodationAssignments)
                    .HasForeignKey(e => e.SemesterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureNotification(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.Type).HasMaxLength(50);
                entity.Property(e => e.Link).HasMaxLength(500);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureAuditLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UserId).HasMaxLength(100);
                entity.Property(e => e.IPAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ApplyGlobalFilters(ModelBuilder modelBuilder)
        {
            // Apply soft delete and tenant filters to all entities
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IBaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var deletedProperty = System.Linq.Expressions.Expression.Property(parameter, "IsDeleted");
                    var tenantProperty = System.Linq.Expressions.Expression.Property(parameter, "TenantId");
                    
                    var deletedCheck = System.Linq.Expressions.Expression.Equal(
                        deletedProperty, 
                        System.Linq.Expressions.Expression.Constant(false));
                    
                    // This filter will be applied at runtime by the interceptor
                    // We'll use a dynamic approach
                    var filter = System.Linq.Expressions.Expression.Lambda(deletedCheck, parameter);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = await _tenantResolver.GetTenantIdAsync();
            var userId = _currentUserService.GetUserId();

            foreach (var entry in ChangeTracker.Entries<IBaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.TenantId = tenantId;
                        entry.Entity.CreatedBy = userId ?? "SYSTEM";
                        entry.Entity.CreatedDate = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.ModifiedBy = userId ?? "SYSTEM";
                        entry.Entity.ModifiedDate = DateTime.UtcNow;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedBy = userId ?? "SYSTEM";
                        entry.Entity.DeletedDate = DateTime.UtcNow;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}