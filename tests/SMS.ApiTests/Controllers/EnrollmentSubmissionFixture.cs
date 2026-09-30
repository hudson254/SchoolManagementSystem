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
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Fixture for the enrollment-submission API regression tests.
    ///
    /// <para>
    /// Exercises the REAL HTTP pipeline (real routing, real authorization
    /// policies, real MediatR handlers, real repositories) against the REAL
    /// PostgreSQL test database, with a real bearer token whose claims carry
    /// the "Student" role.
    /// </para>
    ///
    /// <para>
    /// It seeds two kinds of student: a new student in
    /// <c>PendingCourseSelection</c> (POST /api/v1/enrollment/submit-enrollment)
    /// and a returning student in <c>Approved</c>
    /// (POST /api/v1/returning-user/enroll), because the same entity-state
    /// defect existed in both handlers.
    /// </para>
    /// </summary>
    public class EnrollmentSubmissionFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid CourseId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid UnitOneId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid UnitTwoId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        private const string StudentPassword = "Test123!@#Xyz";
        private const string SemesterName = "Enrollment Submission Semester";

        public string CurrentUserEmail { get; private set; } = string.Empty;
        public string CurrentUserId { get; private set; } = string.Empty;

        public static Guid SemesterId { get; private set; }

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

            // The returning-student path stores a SemesterId that is a real
            // foreign key, so a real semester must exist.
            if (!await db.Semesters.AnyAsync(s => s.Name == SemesterName))
            {
                var academicYear = new AcademicYear
                {
                    Name = "2026/2027",
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2027, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    IsCurrent = true,
                    TenantId = DefaultTenantId
                };
                db.AcademicYears.Add(academicYear);
                await db.SaveChangesAsync();

                var semester = new Semester
                {
                    Name = SemesterName,
                    SemesterNumber = 1,
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    IsCurrent = true,
                    AcademicYearId = academicYear.Id,
                    TenantId = DefaultTenantId
                };
                db.Semesters.Add(semester);
                await db.SaveChangesAsync();
                SemesterId = semester.Id;
            }
            else
            {
                SemesterId = (await db.Semesters.FirstAsync(s => s.Name == SemesterName)).Id;
            }

            // A selectable course with two active units, so a submission has to
            // insert more than one Enrollment row.
            if (!await db.Courses.AnyAsync(c => c.Id == CourseId))
            {
                db.Courses.Add(new Course
                {
                    Id = CourseId,
                    Name = "Enrollment Submission Course",
                    Code = "ESC900",
                    Description = "Course used by the enrollment submission API tests",
                    Credits = 3,
                    Duration = 1,
                    IsActive = true,
                    TenantId = DefaultTenantId
                });
                await db.SaveChangesAsync();

                db.Units.AddRange(
                    new Unit
                    {
                        Id = UnitOneId,
                        Code = "ESC900U1",
                        Name = "Submission Unit One",
                        Credits = 3,
                        CourseId = CourseId,
                        IsActive = true,
                        TenantId = DefaultTenantId
                    },
                    new Unit
                    {
                        Id = UnitTwoId,
                        Code = "ESC900U2",
                        Name = "Submission Unit Two",
                        Credits = 3,
                        CourseId = CourseId,
                        IsActive = true,
                        TenantId = DefaultTenantId
                    });
                await db.SaveChangesAsync();
            }
        }

        public new Task DisposeAsync() => Task.CompletedTask;

        /// <summary>
        /// A student in <c>PendingCourseSelection</c> with a persisted course
        /// selection - the post-registration state the wizard starts from.
        /// </summary>
        public Task<HttpClient> CreateNewStudentClientAsync()
            => CreateStudentClientAsync(RegistrationStatus.PendingCourseSelection);

        /// <summary>
        /// A returning student in <c>Approved</c> - the entry state for the
        /// returning-user enrollment path, which carried the same defect.
        /// </summary>
        public Task<HttpClient> CreateReturningStudentClientAsync()
            => CreateStudentClientAsync(RegistrationStatus.Approved);

        private async Task<HttpClient> CreateStudentClientAsync(RegistrationStatus status)
        {
            string email;
            string userId;

            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

                var unique = Guid.NewGuid().ToString("N")[..10];
                email = $"enroll-sub-{unique}@school.com";
                var userName = $"enrollsub{unique}";

                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = userName,
                    Email = email,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    NormalizedEmail = email.ToUpperInvariant(),
                    FirstName = "Enroll",
                    LastName = "Submit",
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
                    StudentNumber = $"ES-{unique}",
                    FirstName = "Enroll",
                    LastName = "Submit",
                    Email = email,
                    AcademicStatus = "Active",
                    IsActive = true,
                    IsEnrolled = false,
                    RegistrationStatus = status,
                    SelectedCourseId = CourseId,
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


        /// <summary>
        /// A brand new client performing a brand new login for the SAME student
        /// that the current mocked identity describes - i.e. logout followed by
        /// login, which must still show the persisted enrollment.
        /// </summary>
        public async Task<HttpClient> CreateReloginClientAsync()
        {
            var client = base.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");

            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { email = CurrentUserEmail, password = StudentPassword, rememberMe = true });

            var token = ExtractCookieValue(loginResponse, "access_token");
            token.Should().NotBeNullOrWhiteSpace("the re-login must return an access token");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return client;
        }
        /// <summary>
        /// Resolves the student id the handler acted on, so the database can be
        /// inspected directly afterwards.
        /// </summary>
        public async Task<Guid> GetStudentIdAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var student = await db.Students.FirstAsync(s => s.Email == CurrentUserEmail);
            return student.Id;
        }

        /// <summary>
        /// Reads the persisted enrollment rows straight from the database.
        /// </summary>
        public async Task<System.Collections.Generic.List<(Guid UnitId, string Status, bool IsActive, Guid TenantId)>>
            ReadEnrollmentsDirectlyAsync(Guid studentId)
        {
            var rows = new System.Collections.Generic.List<(Guid, string, bool, Guid)>();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Open through Entity Framework rather than calling
            // connection.OpenAsync() directly. OpenConnectionAsync() is what
            // raises DbConnectionInterceptor.ConnectionOpened, which is where
            // the tenant context is published for the session. Opening the
            // raw DbConnection bypasses that entirely, and once row level
            // security is on the policy would evaluate against whatever the
            // pooled connection last carried - returning zero rows. This is
            // the same call the production number generators use.
            await db.Database.OpenConnectionAsync();
            try
            {
                var connection = db.Database.GetDbConnection();

                await using var command = connection.CreateCommand();
                command.CommandText =
                    "select \"UnitId\", \"Status\", \"IsActive\", \"tenant_id\" " +
                    "from \"Enrollments\" where \"StudentId\" = @student";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "student";
                parameter.Value = studentId;
                command.Parameters.Add(parameter);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add((reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2), reader.GetGuid(3)));
                }

                return rows;
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }
}
