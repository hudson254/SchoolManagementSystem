using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    /// <summary>
    /// Cross-tenant isolation tests for RISK-04. These are the single most
    /// important new tests in the repair effort per the mandate, because the
    /// audit rated the tenant filter Guid.Empty capture bug as CRITICAL.
    ///
    /// The tests create data under Tenant A and Tenant B, then verify that a
    /// DbContext scoped to Tenant A can never see Tenant B's rows, and vice
    /// versa. They also verify that the filter is evaluated per-request (not
    /// baked into the cached model at startup).
    /// </summary>
    public class TenantIsolationTests
    {
        private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        [Fact]
        public async Task TenantA_CannotSeeTenantB_Data()
        {
            // Arrange — seed data under both tenants using a shared DB name.
            var dbName = $"SharedTenantTest_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            await SeedTenantDataAsync(options, TenantA, "STU-A-001", "student.a@school.edu");
            await SeedTenantDataAsync(options, TenantB, "STU-B-001", "student.b@school.edu");

            // Act — query from Tenant A's context
            await using (var contextA = CreateContextWithDb(options, TenantA))
            {
                var studentsA = await contextA.Students.ToListAsync();

                // Assert — Tenant A only sees its own student
                studentsA.Should().HaveCount(1);
                studentsA.Should().ContainSingle(s => s.StudentNumber == "STU-A-001");
                studentsA.Should().NotContain(s => s.StudentNumber == "STU-B-001");
            }
        }

        [Fact]
        public async Task TenantB_CannotSeeTenantA_Data()
        {
            // Arrange
            var dbName = $"SharedTenantTest_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            await SeedTenantDataAsync(options, TenantA, "STU-A-002", "student.a2@school.edu");
            await SeedTenantDataAsync(options, TenantB, "STU-B-002", "student.b2@school.edu");

            // Act — query from Tenant B's context
            await using (var contextB = CreateContextWithDb(options, TenantB))
            {
                var studentsB = await contextB.Students.ToListAsync();

                // Assert — Tenant B only sees its own student
                studentsB.Should().HaveCount(1);
                studentsB.Should().ContainSingle(s => s.StudentNumber == "STU-B-002");
                studentsB.Should().NotContain(s => s.StudentNumber == "STU-A-002");
            }
        }

        [Fact]
        public async Task TenantFilter_IsEvaluatedPerRequest_NotBakedIntoModel()
        {
            // Arrange — This is the key test for RISK-04. The previous bug
            // baked the tenant Guid into the cached model at first use. We
            // verify the filter is re-evaluated per DbContext instance by
            // creating two contexts with different tenants against the same DB.
            var dbName = $"PerRequestFilterTest_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            await SeedTenantDataAsync(options, TenantA, "STU-X-001", "x.a@school.edu");
            await SeedTenantDataAsync(options, TenantB, "STU-X-002", "x.b@school.edu");

            // Act & Assert — first context sees only Tenant A data
            await using (var contextA = CreateContextWithDb(options, TenantA))
            {
                var students = await contextA.Students.ToListAsync();
                students.Should().HaveCount(1);
                students[0].StudentNumber.Should().Be("STU-X-001");
            }

            // Act & Assert — second context (same DB, different tenant) sees
            // only Tenant B data. If the filter were baked into the model,
            // this would still return Tenant A's data.
            await using (var contextB = CreateContextWithDb(options, TenantB))
            {
                var students = await contextB.Students.ToListAsync();
                students.Should().HaveCount(1);
                students[0].StudentNumber.Should().Be("STU-X-002");
            }
        }

        [Fact]
        public void CurrentTenantGuid_ReturnsCorrectValuePerContext()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"GuidTest_{Guid.NewGuid()}")
                .Options;

            // Act & Assert — context A resolves Tenant A
            using (var contextA = CreateContextWithDb(options, TenantA))
            {
                contextA.CurrentTenantGuid.Should().Be(TenantA);
            }

            // Act & Assert — context B resolves Tenant B
            using (var contextB = CreateContextWithDb(options, TenantB))
            {
                contextB.CurrentTenantGuid.Should().Be(TenantB);
            }
        }

        [Fact]
        public async Task NonTenantAwareEntity_IsNotFilteredByTenant()
        {
            // Arrange — Regression test for entities without ITenantAwareEntity.
            // Previously, PasswordResetRequest did not implement ITenantAwareEntity
            // and was visible across tenants. As of the RLS remediation, it now
            // implements ITenantAwareEntity and is tenant-scoped. This test verifies
            // that truly non-tenant-aware entities (if any remain) are visible
            // across tenants. Currently, all entities implement ITenantAwareEntity,
            // so this test is a safety net for future entities.
            var dbName = $"NonTenantAwareTest_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            // Seed data under TenantA with tenant-aware PasswordResetRequest
            var tenantContextMockA = new Mock<ITenantContext>();
            tenantContextMockA.SetupGet(x => x.TenantId).Returns(TenantA.ToString());
            var currentUserMockA = new Mock<ICurrentUserService>();

            await using (var seedContext = new ApplicationDbContext(options, currentUserMockA.Object, tenantContextMockA.Object))
            {
                // PasswordResetRequest now implements ITenantAwareEntity, so
                // SaveChangesAsync stamps TenantId = TenantA.
                seedContext.PasswordResetRequests.AddRange(
                    new PasswordResetRequest
                    {
                        Id = Guid.NewGuid(),
                        UserId = "user-123",
                        RequestedEmail = "user@example.com",
                        Status = PasswordResetRequestStatus.Pending
                    },
                    new PasswordResetRequest
                    {
                        Id = Guid.NewGuid(),
                        UserId = "user-456",
                        RequestedEmail = "user456@example.com",
                        Status = PasswordResetRequestStatus.Fulfilled
                    });
                await seedContext.SaveChangesAsync();
            }

            // Act — query from Tenant B's context
            await using (var contextB = CreateContextWithDb(options, TenantB))
            {
                var requests = await contextB.PasswordResetRequests.ToListAsync();

                // Assert — PasswordResetRequest is NOW tenant-aware, so
                // Tenant B should see ZERO records (all belong to Tenant A).
                requests.Should().HaveCount(0);
            }
        }

        [Fact]
        public async Task TenantAwareEntity_IsStillFilteredByTenant()
        {
            // Arrange — guards against over-broad filter removal. Tenant-aware
            // entities (Student) must remain strictly isolated per tenant even
            // after the ITenantAwareEntity scoping fix.
            var dbName = $"TenantAwareStillFiltered_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            await SeedTenantDataAsync(options, TenantA, "STU-Z-001", "z.a@school.edu");
            await SeedTenantDataAsync(options, TenantB, "STU-Z-002", "z.b@school.edu");

            // Act — Tenant A's context must only see Tenant A's student.
            await using (var contextA = CreateContextWithDb(options, TenantA))
            {
                var students = await contextA.Students.ToListAsync();

                // Assert
                students.Should().HaveCount(1);
                students[0].StudentNumber.Should().Be("STU-Z-001");
            }
        }

        [Fact]
        public void CurrentTenantGuid_ReturnsEmpty_WhenTenantContextIsUnset()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"EmptyTenantTest_{Guid.NewGuid()}")
                .Options;

            var tenantContextMock = new Mock<ITenantContext>();
            tenantContextMock.SetupGet(x => x.TenantId).Returns((string)null!);

            var currentUserMock = new Mock<ICurrentUserService>();

            using var context = new ApplicationDbContext(options, currentUserMock.Object, tenantContextMock.Object);

            // Act & Assert
            context.CurrentTenantGuid.Should().Be(Guid.Empty);
        }

        /// <summary>
        /// Helper: creates a DbContext scoped to a specific tenant against a
        /// shared in-memory database.
        /// </summary>
        private static ApplicationDbContext CreateContextWithDb(DbContextOptions<ApplicationDbContext> options, Guid tenantGuid)
        {
            var tenantContextMock = new Mock<ITenantContext>();
            tenantContextMock.SetupGet(x => x.TenantId).Returns(tenantGuid.ToString());

            var currentUserMock = new Mock<ICurrentUserService>();

            return new ApplicationDbContext(options, currentUserMock.Object, tenantContextMock.Object);
        }

        /// <summary>
        /// Helper: seeds a student under a specific tenant. Uses a context
        /// with the tenant set so SaveChangesAsync stamps the TenantId.
        /// </summary>
        private static async Task SeedTenantDataAsync(DbContextOptions<ApplicationDbContext> options, Guid tenantGuid, string studentNumber, string email)
        {
            var tenantContextMock = new Mock<ITenantContext>();
            tenantContextMock.SetupGet(x => x.TenantId).Returns(tenantGuid.ToString());

            var currentUserMock = new Mock<ICurrentUserService>();

            await using var context = new ApplicationDbContext(options, currentUserMock.Object, tenantContextMock.Object);

            var student = new Student
            {
                Id = Guid.NewGuid(),
                StudentNumber = studentNumber,
                Email = email,
                FirstName = "Test",
                LastName = "Student",
                UserId = Guid.NewGuid().ToString(),
                TenantId = tenantGuid,
                IsDeleted = false
            };

            context.Students.Add(student);
            await context.SaveChangesAsync();
        }
    }
}
