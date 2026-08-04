using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SMS.API;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Regression tests for the RISK-09 security fix (IDOR on student data
    /// endpoints). The StudentController previously allowed any caller with
    /// the "Student" role to read/update ANY student's record (details,
    /// enrollments, grades, transcript) by guessing/iterating the student id.
    /// The controller now verifies ownership: a Student-role caller may only
    /// access their OWN record; staff roles (Administrator, Moderator,
    /// Lecturer, Receptionist) retain full access.
    ///
    /// This fixture uses a DEDICATED WebApplicationFactory so the
    /// ICurrentUserService mock can be configured to report a Student role
    /// WITHOUT leaking static state into the shared ApiTestFixture used by
    /// other test classes.
    /// </summary>
    public class StudentIdorFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private const string AdminEmail = "admin@school.com";
        private const string AdminPassword = "Admin123!";

        // Controls the mocked current user. Tests set these before creating a client.
        public string CurrentUserId { get; set; } = "test-user-id";
        public string[] CurrentUserRoles { get; set; } = new[] { "Student" };

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                RemoveServiceDescriptors(typeof(ApplicationDbContext), services);
                RemoveServiceDescriptors(typeof(DbContextOptions<ApplicationDbContext>), services);
                RemoveServiceDescriptors(typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<ApplicationDbContext>), services);

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("StudentIdorTestDb");
                }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);

                // Mock ICurrentUserService — the controller resolves the
                // APPLICATION interface (SMS.Application.Common.Interfaces),
                // NOT the Domain one. Register the mock for the Application
                // interface so the StudentController ownership check uses it.
                var appCurrentUserServiceType = typeof(SMS.Application.Common.Interfaces.ICurrentUserService);
                RemoveServiceDescriptors(appCurrentUserServiceType, services);
                var mockCurrentUser = new Mock<SMS.Application.Common.Interfaces.ICurrentUserService>();
                mockCurrentUser.Setup(x => x.UserId).Returns(() => CurrentUserId);
                mockCurrentUser.Setup(x => x.Email).Returns("student@school.com");
                mockCurrentUser.Setup(x => x.Username).Returns("student");
                mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
                mockCurrentUser.Setup(x => x.Roles).Returns(() => CurrentUserRoles);
                services.AddScoped(_ => mockCurrentUser.Object);

                // Mock Domain ITenantContext
                RemoveServiceDescriptors(typeof(SMS.Domain.Interfaces.ITenantContext), services);
                var mockDomainTenant = new Mock<SMS.Domain.Interfaces.ITenantContext>();
                mockDomainTenant.Setup(x => x.TenantId).Returns(DefaultTenantId.ToString());
                services.AddScoped(_ => mockDomainTenant.Object);

                // Mock Multitenancy ITenantContext
                RemoveServiceDescriptors(typeof(SMS.Multitenancy.Interfaces.ITenantContext), services);
                var mockMultiTenant = new Mock<SMS.Multitenancy.Interfaces.ITenantContext>();
                mockMultiTenant.Setup(x => x.TenantId).Returns(DefaultTenantId.ToString());
                mockMultiTenant.Setup(x => x.TenantName).Returns("Test Tenant");
                services.AddScoped(_ => mockMultiTenant.Object);

                // Mock ITenantStore used by TenantResolutionMiddleware
                RemoveServiceDescriptors(typeof(ITenantStore), services);
                var mockTenantStore = new Mock<ITenantStore>();
                mockTenantStore
                    .Setup(x => x.GetTenantAsync(It.IsAny<string>()))
                    .ReturnsAsync(new Tenant
                    {
                        Id = DefaultTenantId,
                        Name = "Default Tenant",
                        Organization = "Default Organization",
                        Subdomain = "default",
                        IsActive = true
                    });
                services.AddScoped(_ => mockTenantStore.Object);
            });
        }

        private static void RemoveServiceDescriptors(Type serviceType, IServiceCollection services)
        {
            var descriptors = services.Where(d => d.ServiceType == serviceType).ToList();
            foreach (var d in descriptors)
                services.Remove(d);
        }

        public async Task InitializeAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            foreach (var roleName in new[] { "Administrator", "Lecturer", "Student", "Moderator" })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new Role
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant(),
                        IsActive = true
                    });
                }
            }

            var adminUser = await userManager.FindByEmailAsync(AdminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    NormalizedUserName = AdminEmail.ToUpperInvariant(),
                    NormalizedEmail = AdminEmail.ToUpperInvariant(),
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                    RefreshToken = string.Empty,
                    TenantId = DefaultTenantId
                };
                await userManager.CreateAsync(adminUser, AdminPassword);
            }

            if (!await userManager.IsInRoleAsync(adminUser, "Administrator"))
                await userManager.AddToRoleAsync(adminUser, "Administrator");
        }

        public new Task DisposeAsync() => Task.CompletedTask;

        /// <summary>
        /// Returns an authenticated client using the admin bearer token (so the
        /// request passes the [Authorize] checks). The mocked ICurrentUserService
        /// still reports the Student role, which is what the ownership check uses.
        /// </summary>
        public HttpClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");

            // Login as admin to obtain a valid access token (Set-Cookie)
            var loginResponse = client.PostAsJsonAsync("/api/v1/auth/login",
                new { email = AdminEmail, password = AdminPassword, rememberMe = true }).GetAwaiter().GetResult();

            var token = ExtractCookieValue(loginResponse, "access_token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                foreach (var header in values)
                {
                    var firstPart = header.Split(';')[0].Trim();
                    var eq = firstPart.IndexOf('=');
                    if (eq > 0 && firstPart.Substring(0, eq).Trim() == cookieName)
                        return firstPart.Substring(eq + 1).Trim();
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Seeds a Student linked to a real User and returns the student id
        /// together with the user id (GUID string). The mock ICurrentUserService
        /// must report this user id for the "own data" tests to pass, because the
        /// ownership check compares student.UserId with the current user id.
        /// </summary>
        public (Guid StudentId, string UserId) SeedStudent(string emailPrefix)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Create a linked User entity so the GetStudentQueryHandler can
            // populate student.User (it dereferences User.FirstName/LastName).
            var user = new User
            {
                // ASP.NET Identity User.Id is a string, not a Guid
                Id = Guid.NewGuid().ToString(),
                UserName = $"{emailPrefix}-{Guid.NewGuid():N}",
                Email = $"{emailPrefix}-{Guid.NewGuid():N}@school.com",
                NormalizedUserName = $"{emailPrefix}-{Guid.NewGuid():N}".ToUpperInvariant(),
                NormalizedEmail = $"{emailPrefix}-{Guid.NewGuid():N}@school.com".ToUpperInvariant(),
                FirstName = "Test",
                LastName = "Student",
                PhoneNumber = "555-0000",
                Organization = "Test Org",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                RefreshToken = string.Empty,
                TenantId = DefaultTenantId
            };
            db.Users.Add(user);

            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id.ToString(),
                StudentNumber = $"SN-{Guid.NewGuid():N}"[..12],
                FirstName = "Test",
                LastName = "Student",
                Email = user.Email,
                AcademicStatus = "Active",
                IsActive = true,
                IsEnrolled = true,
                TenantId = DefaultTenantId
            };

            db.Students.Add(student);
            db.SaveChanges();
            return (student.Id, user.Id.ToString());
        }
    }

    public class StudentAuthorizationTests : IClassFixture<StudentIdorFixture>
    {
        private readonly StudentIdorFixture _fixture;

        // A distinct user id that does NOT own any seeded student record. Used
        // as the "attacker" to verify the IDOR block.
        private const string AttackerUserId = "attacker-student-user-id";

        public StudentAuthorizationTests(StudentIdorFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Student_AccessingAnotherStudentsData_ShouldReturnForbidden()
        {
            var (victimStudentId, _) = _fixture.SeedStudent("owner");
            _fixture.CurrentUserId = AttackerUserId;
            _fixture.CurrentUserRoles = new[] { "Student" };
            using var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync($"/api/v1/students/{victimStudentId}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Student_AccessingOwnData_ShouldReturnOk()
        {
            var (ownStudentId, ownerUserId) = _fixture.SeedStudent("owner");
            _fixture.CurrentUserId = ownerUserId;
            _fixture.CurrentUserRoles = new[] { "Student" };
            using var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync($"/api/v1/students/{ownStudentId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Student_AccessingAnotherStudentsEnrollments_ShouldReturnForbidden()
        {
            var (victimStudentId, _) = _fixture.SeedStudent("owner");
            _fixture.CurrentUserId = AttackerUserId;
            _fixture.CurrentUserRoles = new[] { "Student" };
            using var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync($"/api/v1/students/{victimStudentId}/enrollments");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Student_AccessingAnotherStudentsGrades_ShouldReturnForbidden()
        {
            var (victimStudentId, _) = _fixture.SeedStudent("owner");
            _fixture.CurrentUserId = AttackerUserId;
            _fixture.CurrentUserRoles = new[] { "Student" };
            using var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync($"/api/v1/students/{victimStudentId}/grades");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Student_AccessingAnotherStudentsTranscript_ShouldReturnForbidden()
        {
            var (victimStudentId, _) = _fixture.SeedStudent("owner");
            _fixture.CurrentUserId = AttackerUserId;
            _fixture.CurrentUserRoles = new[] { "Student" };
            using var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync($"/api/v1/students/{victimStudentId}/transcript");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Moderator_AccessingAnyStudentData_ShouldReturnOk()
        {
            var (anyStudentId, _) = _fixture.SeedStudent("owner");
            _fixture.CurrentUserId = "moderator-user-id";
            _fixture.CurrentUserRoles = new[] { "Moderator" };
            using var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync($"/api/v1/students/{anyStudentId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Lecturer_AccessingAnyStudentData_ShouldReturnOk()
        {
            var (anyStudentId, _) = _fixture.SeedStudent("owner");
            _fixture.CurrentUserId = "lecturer-user-id";
            _fixture.CurrentUserRoles = new[] { "Lecturer" };
            using var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync($"/api/v1/students/{anyStudentId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
