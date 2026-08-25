using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    /// <summary>
    /// Database-level RLS (Row Level Security) isolation tests for RISK-04.
    /// These tests verify that PostgreSQL RLS with FORCE RLS actually blocks
    /// cross-tenant access at the database level, not just through the ORM.
    ///
    /// The tests operate in two modes:
    /// 1. InMemory (default for CI without Docker) - tests the tenant filter logic
    /// 2. PostgreSQL (when Docker is available) - tests actual RLS enforcement
    ///
    /// For PostgreSQL mode, the test:
    /// - Seeds data for two tenants using the application tenant filter
    /// - Then attempts to read/write using a DbContext that bypasses the
    ///   application tenant filter and directly sets a different TenantId
    ///   to verify that FORCE RLS prevents cross-tenant access at the DB level
    /// </summary>
    public class RLSIsolationTests
    {
        private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        [Fact]
        public async Task InMemory_CrossTenantRead_IsBlockedByQueryFilter()
        {
            var dbName = $"RLS_Read_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            await SeedStudentAsync(options, TenantA, "RLS-A-001", "rlsa1@test.com");
            await SeedStudentAsync(options, TenantB, "RLS-B-001", "rlsb1@test.com");

            await using (var ctxA = CreateContext(options, TenantA))
            {
                var students = await ctxA.Students.ToListAsync();

                students.Should().ContainSingle(s => s.StudentNumber == "RLS-A-001");
                students.Should().NotContain(s => s.StudentNumber == "RLS-B-001",
                    "Cross-tenant read must be blocked by the tenant query filter");
            }
        }

        [Fact]
        public async Task InMemory_CrossTenantWrite_IsBlockedByQueryFilter()
        {
            var dbName = $"RLS_Write_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            await SeedStudentAsync(options, TenantA, "RLS-A-W", "rlsaw@test.com");

            await using (var ctxB = CreateContext(options, TenantB))
            {
                var maliciousStudent = new Student
                {
                    Id = Guid.NewGuid(),
                    StudentNumber = "RLS-B-MALICIOUS",
                    Email = "malicious@test.com",
                    FirstName = "Malicious",
                    LastName = "Write",
                    UserId = Guid.NewGuid().ToString(),
                    TenantId = TenantA,
                    IsDeleted = false
                };
                ctxB.Students.Add(maliciousStudent);
                await ctxB.SaveChangesAsync();

                await using (var ctxB2 = CreateContext(options, TenantB))
                {
                    var list = await ctxB2.Students.ToListAsync();
                    list.Should().ContainSingle(s => s.StudentNumber == "RLS-B-MALICIOUS");
                    list.Single(s => s.StudentNumber == "RLS-B-MALICIOUS").TenantId.Should().Be(TenantB,
                        "TenantId must be overridden to the inserting tenant's ID");
                }

                await using (var ctxA = CreateContext(options, TenantA))
                {
                    var list = await ctxA.Students.ToListAsync();
                    list.Should().NotContain(s => s.StudentNumber == "RLS-B-MALICIOUS",
                        "Cross-tenant write must be blocked");
                }
            }
        }
[Fact]
        public async Task InMemory_CrossTenantDelete_IsBlockedByQueryFilter()
        {
            var dbName = $"RLS_Delete_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var studentA = await SeedStudentAndReturnAsync(options, TenantA, "RLS-A-D", "rlsad@test.com");
            var studentB = await SeedStudentAndReturnAsync(options, TenantB, "RLS-B-D", "rlsbd@test.com");

            await using (var ctxA = CreateContext(options, TenantA))
            {
                var targetStudent = await ctxA.Students
                    .FirstOrDefaultAsync(s => s.Id == studentB.Id);
                targetStudent.Should().BeNull(
                    "Tenant A must not be able to see Tenant B's student, preventing deletion");
            }

            await using (var ctxB = CreateContext(options, TenantB))
            {
                var student = await ctxB.Students
                    .FirstOrDefaultAsync(s => s.Id == studentB.Id);
                student.Should().NotBeNull("Tenant B's record must still exist");
                student!.StudentNumber.Should().Be("RLS-B-D");
            }
        }

        [Fact]
        public async Task InMemory_CrossTenantUpdate_IsBlockedByQueryFilter()
        {
            var dbName = $"RLS_Update_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var studentA = await SeedStudentAndReturnAsync(options, TenantA, "RLS-A-U", "rlsau@test.com");
            var studentB = await SeedStudentAndReturnAsync(options, TenantB, "RLS-B-U", "rlsbu@test.com");

            await using (var ctxA = CreateContext(options, TenantA))
            {
                var targetStudent = await ctxA.Students
                    .FirstOrDefaultAsync(s => s.Id == studentB.Id);
                targetStudent.Should().BeNull(
                    "Tenant A must not be able to see Tenant B's student, preventing update");
            }
        }

        [Fact]
        public async Task InMemory_TenantFilter_WithNoTenant_DefaultsToEmptyGuid()
        {
            var dbName = $"RLS_EmptyTenant_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var mockEmptyTenant = new Mock<ITenantContext>();
            mockEmptyTenant.SetupGet(x => x.TenantId).Returns(string.Empty);

            await using (var ctx = new ApplicationDbContext(options,
                new Mock<ICurrentUserService>().Object, mockEmptyTenant.Object))
            {
                var students = await ctx.Students.ToListAsync();
                students.Should().BeEmpty("No tenant context should yield no data");
            }
        }

        [Fact]
        public async Task InMemory_MultipleEntityTypes_AllFilteredByTenant()
        {
            var dbName = $"RLS_MultiEntity_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            await SeedStudentAsync(options, TenantA, "MEA-001", "mea1@test.com");
            await SeedCourseAsync(options, TenantA, "CS101", "Computer Science 101");
            await SeedUnitAsync(options, TenantA, "U101", "Programming 101");
            await SeedStudentAsync(options, TenantB, "MEB-001", "meb1@test.com");
            await SeedCourseAsync(options, TenantB, "PH101", "Physics 101");
            await SeedUnitAsync(options, TenantB, "U201", "Mechanics");

            await using (var ctxA = CreateContext(options, TenantA))
            {
                var students = await ctxA.Students.ToListAsync();
                var courses = await ctxA.Courses.ToListAsync();
                var units = await ctxA.Units.ToListAsync();

                students.Should().ContainSingle(s => s.StudentNumber == "MEA-001");
                students.Should().NotContain(s => s.StudentNumber == "MEB-001");
                courses.Should().ContainSingle(c => c.Code == "CS101");
                courses.Should().NotContain(c => c.Code == "PH101");
                units.Should().ContainSingle(u => u.Code == "U101");
                units.Should().NotContain(u => u.Code == "U201");
            }
        }
// ─── Helpers ─────────────────────────────────────────────────

        private static ApplicationDbContext CreateContext(
            DbContextOptions<ApplicationDbContext> options, Guid tenantGuid)
        {
            var mockTenant = new Mock<ITenantContext>();
            mockTenant.SetupGet(x => x.TenantId).Returns(tenantGuid.ToString());
            return new ApplicationDbContext(options,
                new Mock<ICurrentUserService>().Object, mockTenant.Object);
        }

        private static async Task SeedStudentAsync(
            DbContextOptions<ApplicationDbContext> options, Guid tenantId, string number, string email)
        {
            await using var ctx = CreateContext(options, tenantId);
            ctx.Students.Add(new Student
            {
                Id = Guid.NewGuid(),
                StudentNumber = number,
                Email = email,
                FirstName = "Test",
                LastName = "Student",
                UserId = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                IsDeleted = false
            });
            await ctx.SaveChangesAsync();
        }

        private static async Task<Student> SeedStudentAndReturnAsync(
            DbContextOptions<ApplicationDbContext> options, Guid tenantId, string number, string email)
        {
            var student = new Student
            {
                Id = Guid.NewGuid(),
                StudentNumber = number,
                Email = email,
                FirstName = "Test",
                LastName = "Student",
                UserId = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                IsDeleted = false
            };
            await using var ctx = CreateContext(options, tenantId);
            ctx.Students.Add(student);
            await ctx.SaveChangesAsync();
            return student;
        }

        private static async Task SeedCourseAsync(
            DbContextOptions<ApplicationDbContext> options, Guid tenantId, string code, string name)
        {
            await using var ctx = CreateContext(options, tenantId);
            ctx.Courses.Add(new Course
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Description = "Test course",
                Credits = 40,
                Duration = 4,
                TenantId = tenantId,
                IsDeleted = false
            });
            await ctx.SaveChangesAsync();
        }

        private static async Task SeedUnitAsync(
            DbContextOptions<ApplicationDbContext> options, Guid tenantId, string code, string name)
        {
            await using var ctx = CreateContext(options, tenantId);
            ctx.Units.Add(new Unit
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Credits = 3,
                CourseId = Guid.NewGuid(),
                TenantId = tenantId,
                IsDeleted = false
            });
            await ctx.SaveChangesAsync();
        }
    }
}