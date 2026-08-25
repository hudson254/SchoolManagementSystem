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
    public class CrossTenantIsolationTests
    {
        private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        [Fact]
        public async Task TenantA_CannotRead_TenantB_Students()
        {
            var dbName = $"XRead_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName).Options;
            await SeedStudentAsync(options, TenantA, "STU-A-001");
            await SeedStudentAsync(options, TenantB, "STU-B-001");
            await using var ctxA = CreateContext(options, TenantA);
            var students = await ctxA.Students.ToListAsync();
            students.Should().HaveCount(1);
            students.Should().ContainSingle(s => s.StudentNumber == "STU-A-001");
            students.Should().NotContain(s => s.StudentNumber == "STU-B-001");
        }

        [Fact]
        public async Task TenantB_CannotRead_TenantA_Students()
        {
            var dbName = $"XReadB_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName).Options;
            await SeedStudentAsync(options, TenantA, "STU-A-002");
            await SeedStudentAsync(options, TenantB, "STU-B-002");
            await using var ctxB = CreateContext(options, TenantB);
            var students = await ctxB.Students.ToListAsync();
            students.Should().HaveCount(1);
            students.Should().ContainSingle(s => s.StudentNumber == "STU-B-002");
            students.Should().NotContain(s => s.StudentNumber == "STU-A-002");
        }

        [Fact]
        public async Task TenantA_CannotWrite_ToTenantB()
        {
            var dbName = $"XWrite_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName).Options;
            await SeedStudentAsync(options, TenantA, "STU-A-W");
            await using var ctxB = CreateContext(options, TenantB);
            var malicious = new Student
            {
                Id = Guid.NewGuid(), StudentNumber = "STU-B-W",
                Email = "b@test.com", FirstName = "Mal", LastName = "Write",
                UserId = Guid.NewGuid().ToString(), TenantId = TenantA, IsDeleted = false
            };
            ctxB.Students.Add(malicious); await ctxB.SaveChangesAsync();
            await using (var ctxB2 = CreateContext(options, TenantB))
            {
                var list = await ctxB2.Students.ToListAsync();
                list.Should().ContainSingle(s => s.StudentNumber == "STU-B-W");
                list.Single(s => s.StudentNumber == "STU-B-W").TenantId.Should().Be(TenantB);
            }
            await using (var ctxA2 = CreateContext(options, TenantA))
            {
                var list = await ctxA2.Students.ToListAsync();
                list.Should().NotContain(s => s.StudentNumber == "STU-B-W");
            }
        }

        [Fact]
        public async Task TenantA_CannotDelete_TenantB_Data()
        {
            var dbName = $"XDel_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName).Options;
            await SeedStudentAsync(options, TenantA, "STU-A-D");
            await SeedStudentAsync(options, TenantB, "STU-B-D");
            await using (var ctxA = CreateContext(options, TenantA))
            {
                var b = await ctxA.Students.FirstOrDefaultAsync(s => s.StudentNumber == "STU-B-D");
                b.Should().BeNull();
            }
            await using (var ctxB = CreateContext(options, TenantB))
            {
                var b = await ctxB.Students.FirstOrDefaultAsync(s => s.StudentNumber == "STU-B-D");
                b.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task CrossTenant_EntityIdEnumeration_ShouldNotLeakData()
        {
            var dbName = $"XEnum_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName).Options;
            var studentBId = Guid.NewGuid();
            await using (var ctxB = CreateContext(options, TenantB))
            {
                ctxB.Students.Add(new Student
                {
                    Id = studentBId, StudentNumber = "STU-B-E", Email = "b@test.com",
                    FirstName = "Enum", LastName = "Test", UserId = Guid.NewGuid().ToString(),
                    TenantId = TenantB, IsDeleted = false
                });
                await ctxB.SaveChangesAsync();
            }
            await using (var ctxA = CreateContext(options, TenantA))
            {
                var student = await ctxA.Students.FindAsync(studentBId);
                student.Should().BeNull();
            }
        }

        [Fact]
        public async Task CrossTenant_Courses_AreIsolated()
        {
            var dbName = $"XCourses_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName).Options;
            await SeedCourseAsync(options, TenantA, "CS-A", "Course A");
            await SeedCourseAsync(options, TenantB, "CS-B", "Course B");
            await using var ctxA = CreateContext(options, TenantA);
            var courses = await ctxA.Courses.ToListAsync();
            courses.Should().HaveCount(1);
            courses.Should().ContainSingle(c => c.Code == "CS-A");
            courses.Should().NotContain(c => c.Code == "CS-B");
        }

        [Fact]
        public async Task CrossTenant_Units_AreIsolated()
        {
            var dbName = $"XUnits_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName).Options;
            await SeedUnitAsync(options, TenantA, "U-A", "Unit A");
            await SeedUnitAsync(options, TenantB, "U-B", "Unit B");
            await using var ctxA = CreateContext(options, TenantA);
            var units = await ctxA.Units.ToListAsync();
            units.Should().HaveCount(1);
            units.Should().ContainSingle(u => u.Code == "U-A");
            units.Should().NotContain(u => u.Code == "U-B");
        }

        private static ApplicationDbContext CreateContext(DbContextOptions<ApplicationDbContext> options, Guid tenantGuid)
        {
            var mockTenant = new Mock<ITenantContext>();
            mockTenant.SetupGet(x => x.TenantId).Returns(tenantGuid.ToString());
            return new ApplicationDbContext(options, new Mock<ICurrentUserService>().Object, mockTenant.Object);
        }

        private static async Task SeedStudentAsync(DbContextOptions<ApplicationDbContext> options, Guid tenantId, string number)
        {
            await using var ctx = CreateContext(options, tenantId);
            ctx.Students.Add(new Student
            {
                Id = Guid.NewGuid(), StudentNumber = number, Email = $"{number}@test.com",
                FirstName = "Test", LastName = "Student", UserId = Guid.NewGuid().ToString(),
                TenantId = tenantId, IsDeleted = false
            });
            await ctx.SaveChangesAsync();
        }

        private static async Task SeedCourseAsync(DbContextOptions<ApplicationDbContext> options, Guid tenantId, string code, string name)
        {
            await using var ctx = CreateContext(options, tenantId);
            ctx.Courses.Add(new Course
            {
                Id = Guid.NewGuid(), Code = code, Name = name,
                Description = "Test", Credits = 40, Duration = 4,
                TenantId = tenantId, IsDeleted = false
            });
            await ctx.SaveChangesAsync();
        }

        private static async Task SeedUnitAsync(DbContextOptions<ApplicationDbContext> options, Guid tenantId, string code, string name)
        {
            await using var ctx = CreateContext(options, tenantId);
            ctx.Units.Add(new Unit
            {
                Id = Guid.NewGuid(), Code = code, Name = name,
                Credits = 3, CourseId = Guid.NewGuid(),
                TenantId = tenantId, IsDeleted = false
            });
            await ctx.SaveChangesAsync();
        }
    }
}
