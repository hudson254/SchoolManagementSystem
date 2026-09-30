using System;
using System.Linq;
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
    /// Fixture for the student course-selection API tests.
    ///
    /// <para>
    /// Unlike <see cref="StudentIdorFixture"/>, this fixture needs a REAL bearer
    /// token carrying the "Student" role, because the whole point of the tests is
    /// to prove the <c>StudentAccess</c> policy admits a Student while
    /// <c>ModeratorAccess</c> still rejects one. The mocked
    /// <c>ICurrentUserService</c> supplies the handler-side identity (email), and
    /// the real login supplies the policy-side role claims.
    /// </para>
    /// </summary>
    public class StudentCourseSelectionFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid SelectableCourseId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid SelectableUnitId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        private const string StudentPassword = "Test123!@#Xyz";

        /// <summary>Email of the student the mocked current user reports.</summary>
        public string CurrentUserEmail { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var appCurrentUserServiceType = typeof(SMS.Application.Common.Interfaces.ICurrentUserService);
                RemoveServiceDescriptors(appCurrentUserServiceType, services);
                var mockCurrentUser = new Mock<SMS.Application.Common.Interfaces.ICurrentUserService>();
                mockCurrentUser.Setup(x => x.UserId).Returns(() => CurrentUserId);
                mockCurrentUser.Setup(x => x.Email).Returns(() => CurrentUserEmail);
                mockCurrentUser.Setup(x => x.Username).Returns("student");
                mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
                mockCurrentUser.Setup(x => x.Roles).Returns(new[] { "Student" });
                services.AddScoped(_ => mockCurrentUser.Object);

                RemoveServiceDescriptors(typeof(SMS.Domain.Interfaces.ITenantContext), services);
                var mockDomainTenant = new Mock<SMS.Domain.Interfaces.ITenantContext>();
                mockDomainTenant.Setup(x => x.TenantId).Returns(DefaultTenantId.ToString());
                services.AddScoped(_ => mockDomainTenant.Object);

                RemoveServiceDescriptors(typeof(SMS.Multitenancy.Interfaces.ITenantContext), services);
                var mockMultiTenant = new Mock<SMS.Multitenancy.Interfaces.ITenantContext>();
                mockMultiTenant.Setup(x => x.TenantId).Returns(DefaultTenantId.ToString());
                mockMultiTenant.Setup(x => x.TenantName).Returns("Test Tenant");
                services.AddScoped(_ => mockMultiTenant.Object);

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
            using var initClient = base.CreateClient();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                await TestDatabaseMigrator.MigrateAsync(Services);
            }
            else
            {
                await db.Database.EnsureCreatedAsync();
            }

            if (!await db.Tenants.AnyAsync(t => t.Id == DefaultTenantId))
            {
                db.Tenants.Add(new Tenant
                {
                    Id = DefaultTenantId,
                    Name = "Default Tenant",
                    Organization = "Default Organization",
                    Subdomain = "default",
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            foreach (var roleName in new[] { "Administrator", "Student" })
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

            // Seed a selectable course with a unit so both new endpoints have
            // real, tenant-scoped data to return.
            if (!await db.Courses.AnyAsync(c => c.Id == SelectableCourseId))
            {
                db.Courses.Add(new Course
                {
                    Id = SelectableCourseId,
                    Name = "Student Selectable Course",
                    Code = "SSC101",
                    Description = "Course used by the course-selection API tests",
                    Credits = 3,
                    Duration = 1,
                    IsActive = true,
                    TenantId = DefaultTenantId
                });
                await db.SaveChangesAsync();

                db.Units.Add(new Unit
                {
                    Id = SelectableUnitId,
                    Code = "SSC101U1",
                    Name = "Selectable Unit One",
                    Credits = 3,
                    CourseId = SelectableCourseId,
                    IsActive = true,
                    TenantId = DefaultTenantId
                });
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Seeds a real student (User + Student record) with a persisted
        /// SelectedCourseId - the post-fix state of a student who just
        /// registered - and returns an authenticated client whose token really
        /// carries the Student role.
        /// </summary>
        public async Task<HttpClient> CreateStudentClientAsync(Guid? selectedCourseId = null)
        {
            return await CreateStudentClientCoreAsync(
                selectedCourseId ?? SelectableCourseId);
        }

        /// <summary>
        /// Same as <see cref="CreateStudentClientAsync"/> but with NO persisted
        /// course selection - the pre-fix state, and the state of a student whose
        /// selection could not be backfilled unambiguously.
        /// </summary>
        public async Task<HttpClient> CreateStudentClientWithoutSelectionAsync()
        {
            return await CreateStudentClientCoreAsync(null);
        }

        private async Task<HttpClient> CreateStudentClientCoreAsync(Guid? selectedCourseId)
        {
            string email;
            string userId;

            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

                var unique = Guid.NewGuid().ToString("N")[..10];
                email = $"course-sel-{unique}@school.com";
                var userName = $"coursesel{unique}";

                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = userName,
                    Email = email,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    NormalizedEmail = email.ToUpperInvariant(),
                    FirstName = "Course",
                    LastName = "Selection",
                    Organization = "Test Org",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                    RefreshToken = string.Empty,
                    TenantId = DefaultTenantId
                };

                var created = await userManager.CreateAsync(user, StudentPassword);
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create student user: {string.Join(", ", created.Errors.Select(e => e.Description))}");
                }

                await userManager.AddToRoleAsync(user, "Student");
                userId = user.Id;

                db.Students.Add(new Student
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    StudentNumber = $"CS-{unique}",
                    FirstName = "Course",
                    LastName = "Selection",
                    Email = email,
                    AcademicStatus = "Active",
                    IsActive = true,
                    // Not yet enrolled: the student is still pending approval.
                    IsEnrolled = false,
                    RegistrationStatus = SMS.Domain.Enums.RegistrationStatus.PendingCourseSelection,
                    SelectedCourseId = selectedCourseId,
                    TenantId = DefaultTenantId
                });

                await db.SaveChangesAsync();
            }

            CurrentUserEmail = email;
            CurrentUserId = userId;

            var client = base.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");

            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { email, password = StudentPassword, rememberMe = true });

            var token = ExtractCookieValue(loginResponse, "access_token");
            token.Should().NotBeNullOrWhiteSpace("the student login must return an access token");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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

        public new Task DisposeAsync() => Task.CompletedTask;
    }
}
