using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Persistence.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
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
        public new DbSet<UserRole> UserRoles { get; set; }
        public DbSet<ReportVerification> ReportVerifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                entity.HasOne(h => h.Occupant)
                    .WithMany(s => s.Houses)
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
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasQueryFilter(u => !u.IsDeleted);
            });

            // Configure Student entity
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.StudentNumber).IsUnique();
                entity.HasIndex(s => s.Email).IsUnique();

                entity.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(s => s.LastName).IsRequired().HasMaxLength(100);
                entity.Property(s => s.Email).IsRequired().HasMaxLength(200);
                entity.Property(s => s.StudentNumber).IsRequired().HasMaxLength(50);
                entity.Property(s => s.PhoneNumber).HasMaxLength(20);
                entity.Property(s => s.Address).HasMaxLength(500);

                entity.HasOne(s => s.User)
                    .WithOne(u => u.Student)
                    .HasForeignKey<Student>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Programme)
                    .WithMany()
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

                entity.HasQueryFilter(s => !s.IsDeleted);
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

            // Configure AccommodationAssignment entity
            modelBuilder.Entity<AccommodationAssignment>(entity =>
            {
                entity.HasKey(aa => aa.Id);
                entity.Property(aa => aa.Status).IsRequired().HasMaxLength(50);

                entity.HasOne(aa => aa.Student)
                    .WithOne(s => s.AccommodationAssignment)
                    .HasForeignKey<AccommodationAssignment>(aa => aa.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

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

                entity.HasQueryFilter(aa => !aa.IsDeleted);
            });

            // Configure Course entity relationships
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasOne(c => c.Programme)
                    .WithMany(p => p.Courses)
                    .HasForeignKey(c => c.ProgrammeId)
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

            // Apply global tenant query filters
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var tenantIdProperty = entityType.FindProperty("TenantId");
                if (tenantIdProperty != null && tenantIdProperty.ClrType == typeof(Guid))
                {
                    if (entityType.ClrType == typeof(User) || entityType.ClrType == typeof(Role))
                        continue;

                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var property = System.Linq.Expressions.Expression.Property(parameter, "TenantId");

                    Guid tenantGuid = Guid.Empty;
                    if (_tenantContext != null && Guid.TryParse(_tenantContext.TenantId, out var parsedGuid))
                    {
                        tenantGuid = parsedGuid;
                    }

                    var tenantIdValue = System.Linq.Expressions.Expression.Constant(tenantGuid);
                    var condition = System.Linq.Expressions.Expression.Equal(property, tenantIdValue);
                    var lambda = System.Linq.Expressions.Expression.Lambda(condition, parameter);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
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

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
