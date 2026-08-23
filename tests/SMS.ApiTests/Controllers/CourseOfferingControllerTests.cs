using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SMS.API;
using SMS.Application.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// HTTP-integration tests for the Course Offering feature:
    ///   - CourseOfferingController (CRUD, units, student/lecturer assignment)
    ///   - CourseOfferingAssignmentController (assignment)
    ///   - ConfirmationController (enrollment/teaching confirmation + issue reporting)
    ///
    /// A dedicated WebApplicationFactory is used so the shared ApiTestFixture
    /// (used by other test classes) is not polluted. The InMemory database is
    /// seeded with roles, an admin user, a course, a student and a lecturer.
    /// </summary>
    public class CourseOfferingApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private const string AdminEmail = "admin@school.com";
        // Must satisfy Identity PasswordOptions: RequiredLength=12, RequireDigit,
        // RequireLowercase, RequireUppercase, RequireNonAlphanumeric, RequiredUniqueChars=4.
        private const string AdminPassword = "Admin123!@#q1";

        public Guid SeededCourseId { get; private set; }
        public Guid SeededStudentId { get; private set; }
        public Guid SeededLecturerId { get; private set; }
        public Guid SeededAcademicYearId { get; private set; }
        public Guid SeededSemesterId { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Mock ICurrentUserService (Application interface)
                var appCurrentUserServiceType = typeof(SMS.Application.Common.Interfaces.ICurrentUserService);
                RemoveServiceDescriptors(appCurrentUserServiceType, services);
                var mockCurrentUser = new Mock<SMS.Application.Common.Interfaces.ICurrentUserService>();
                mockCurrentUser.Setup(x => x.UserId).Returns("test-user-id");
                mockCurrentUser.Setup(x => x.Email).Returns("test@test.com");
                mockCurrentUser.Setup(x => x.Username).Returns("testuser");
                mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
                mockCurrentUser.Setup(x => x.Roles).Returns(new[] { "Administrator" });
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

                // Mock ITenantStore
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
            // Create a client first to ensure the server is built (Services becomes available)
            using var initClient = base.CreateClient();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // Use MigrateAsync since we're now using PostgreSQL (same as ApiTestFixture).
            // EnsureCreatedAsync does not create the full Identity schema.
            // Only migrate if there are pending migrations
            if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                var pending = await db.Database.GetPendingMigrationsAsync();
                if (pending.Any())
                    await db.Database.MigrateAsync();
            }
            else
            {
                await db.Database.EnsureCreatedAsync();
            }

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            foreach (var roleName in new[] { "Administrator", "Lecturer", "Student", "Coordinator" })
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

            // Seed required reference data (IDEMPOTENT: InitializeAsync is
            // invoked by xUnit before EVERY test in the fixture, so each
            // entity must be created only if it does not already exist,
            // otherwise the unique indexes (Email, StudentNumber,
            // EmployeeNumber, Code) throw
            // "An item with the same key has already been added" on the
            // second and subsequent tests.
            var academicYear = await db.AcademicYears
                .FirstOrDefaultAsync(x => x.Name == "2026" && x.TenantId == DefaultTenantId);
            if (academicYear == null)
            {
                academicYear = new AcademicYear
                {
                    Id = Guid.NewGuid(),
                    Name = "2026",
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    IsCurrent = true,
                    TenantId = DefaultTenantId
                };
                db.AcademicYears.Add(academicYear);
            }
            SeededAcademicYearId = academicYear.Id;

            var semester = await db.Semesters
                .FirstOrDefaultAsync(x => x.Name == "Semester 1" && x.AcademicYearId == SeededAcademicYearId);
            if (semester == null)
            {
                semester = new Semester
                {
                    Id = Guid.NewGuid(),
                    Name = "Semester 1",
                    SemesterNumber = 1,
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    IsCurrent = true,
                    AcademicYearId = SeededAcademicYearId,
                    TenantId = DefaultTenantId
                };
                db.Semesters.Add(semester);
            }
            SeededSemesterId = semester.Id;

            var department = await db.Departments
                .FirstOrDefaultAsync(x => x.Code == "CS" && x.TenantId == DefaultTenantId);
            if (department == null)
            {
                department = new Department
                {
                    Id = Guid.NewGuid(),
                    Name = "Computer Science",
                    Code = "CS",
                    TenantId = DefaultTenantId
                };
                db.Departments.Add(department);
            }

            var course = await db.Courses
                .FirstOrDefaultAsync(x => x.Code == "WM101" && x.TenantId == DefaultTenantId);
            if (course == null)
            {
                course = new Course
                {
                    Id = Guid.NewGuid(),
                    Name = "Wildlife Management",
                    Code = "WM101",
                    Description = "Intro to wildlife management",
                    Credits = 3,
                    Duration = 1,
                    DepartmentId = department.Id,
                    IsActive = true,
                    TenantId = DefaultTenantId
                };
                db.Courses.Add(course);
            }
            SeededCourseId = course.Id;

            // Seed a student (only if not already present)
            var studentUser = await userManager.FindByEmailAsync("student@school.com");
            if (studentUser == null)
            {
                studentUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "student@school.com",
                    Email = "student@school.com",
                    NormalizedUserName = "STUDENT@SCHOOL.COM",
                    NormalizedEmail = "STUDENT@SCHOOL.COM",
                    FirstName = "Test",
                    LastName = "Student",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                    RefreshToken = string.Empty,
                    TenantId = DefaultTenantId
                };
                // Use UserManager so Identity stores (AspNetUsers) are consistent
                // and the user is properly hashed/validated. Direct db.Users.Add
                // bypasses Identity and can cause duplicate-key issues when the
                // same user is re-added across test runs.
                var createStudentUserResult = await userManager.CreateAsync(studentUser);
                if (!createStudentUserResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed creating student user: {string.Join(", ", createStudentUserResult.Errors.Select(e => e.Description))}");
                }
            }

            var student = await db.Students
                .FirstOrDefaultAsync(x => x.StudentNumber == "SN-TEST-001" && x.TenantId == DefaultTenantId);
            if (student == null)
            {
                student = new Student
                {
                    Id = Guid.NewGuid(),
                    UserId = studentUser.Id,
                    StudentNumber = "SN-TEST-001",
                    FirstName = "Test",
                    LastName = "Student",
                    Email = "student@school.com",
                    AcademicStatus = "Active",
                    IsActive = true,
                    IsEnrolled = true,
                    TenantId = DefaultTenantId
                };
                db.Students.Add(student);
            }
            SeededStudentId = student.Id;

            // Seed a lecturer (only if not already present)
            var lecturerUser = await userManager.FindByEmailAsync("lecturer@school.com");
            if (lecturerUser == null)
            {
                lecturerUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "lecturer@school.com",
                    Email = "lecturer@school.com",
                    NormalizedUserName = "LECTURER@SCHOOL.COM",
                    NormalizedEmail = "LECTURER@SCHOOL.COM",
                    FirstName = "Test",
                    LastName = "Lecturer",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                    RefreshToken = string.Empty,
                    TenantId = DefaultTenantId
                };
                // Use UserManager for consistent Identity store handling.
                var createLecturerUserResult = await userManager.CreateAsync(lecturerUser);
                if (!createLecturerUserResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed creating lecturer user: {string.Join(", ", createLecturerUserResult.Errors.Select(e => e.Description))}");
                }
            }

            var lecturer = await db.Lecturers
                .FirstOrDefaultAsync(x => x.EmployeeNumber == "EMP-001" && x.TenantId == DefaultTenantId);
            if (lecturer == null)
            {
                lecturer = new Lecturer
                {
                    Id = Guid.NewGuid(),
                    UserId = lecturerUser.Id,
                    FirstName = "Test",
                    LastName = "Lecturer",
                    Email = "lecturer@school.com",
                    EmployeeNumber = "EMP-001",
                    DepartmentId = department.Id,
                    IsActive = true,
                    TenantId = DefaultTenantId
                };
                db.Lecturers.Add(lecturer);
            }
            SeededLecturerId = lecturer.Id;

            await db.SaveChangesAsync();
        }

        public new Task DisposeAsync() => Task.CompletedTask;

        public HttpClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");

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
    }

    public class CourseOfferingControllerTests : IClassFixture<CourseOfferingApiFixture>
    {
        private readonly CourseOfferingApiFixture _fixture;

        // The API serializes enums as strings (JsonStringEnumConverter is configured
        // in Program.cs) and uses camelCase property names (default ASP.NET Core
        // JsonOptions). The test client must use the same converter and case-insensitive
        // matching when reading response bodies, otherwise:
        //   - deserializing CourseOfferingStatus fails (missing JsonStringEnumConverter)
        //   - camelCase "id" fails to bind to PascalCase "Id", leaving Guid.Empty,
        //     which cascades into "CourseOfferingId is required" / NotFound failures
        //     on dependent operations (delete, assign, confirm, report).
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public CourseOfferingControllerTests(CourseOfferingApiFixture fixture)
        {
            _fixture = fixture;
        }

        // ===== Course Offering CRUD =====

        [Fact]
        public async Task CreateCourseOffering_ValidRequest_ReturnsCreatedWithId()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var request = new
            {
                courseId = _fixture.SeededCourseId,
                academicYearId = _fixture.SeededAcademicYearId,
                semesterId = _fixture.SeededSemesterId,
                intake = "2026 Intake A",
                startDate = "2026-01-15T00:00:00Z",
                endDate = "2026-06-30T00:00:00Z",
                registrationOpenDate = "2025-11-01T00:00:00Z",
                registrationCloseDate = "2026-01-10T00:00:00Z",
                status = "Draft"
            };

            var response = await client.PostAsJsonAsync("/api/v1/courseoffering", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var dto = await response.Content.ReadFromJsonAsync<CourseOfferingDto>(JsonOptions);
            dto.Should().NotBeNull();
            dto!.Id.Should().NotBeEmpty();
            dto.CourseId.Should().Be(_fixture.SeededCourseId);
        }

        [Fact]
        public async Task CreateCourseOffering_MissingCourseId_ReturnsBadRequest()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var request = new
            {
                academicYearId = _fixture.SeededAcademicYearId,
                semesterId = _fixture.SeededSemesterId,
                intake = "2026 Intake A",
                startDate = "2026-01-15T00:00:00Z",
                endDate = "2026-06-30T00:00:00Z",
                registrationOpenDate = "2025-11-01T00:00:00Z",
                registrationCloseDate = "2026-01-10T00:00:00Z",
                status = "Draft"
            };

            var response = await client.PostAsJsonAsync("/api/v1/courseoffering", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetCourseOfferings_ReturnsList()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync("/api/v1/courseoffering");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await response.Content.ReadFromJsonAsync<CourseOfferingDto[]>(JsonOptions);
            results.Should().NotBeNull();
        }

        [Fact]
        public async Task GetCourseOffering_NonExistentId_ReturnsNotFound()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync($"/api/v1/courseoffering/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteCourseOffering_ExistingOffering_ReturnsNoContent()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            // First create an offering
            var createRequest = new
            {
                courseId = _fixture.SeededCourseId,
                academicYearId = _fixture.SeededAcademicYearId,
                semesterId = _fixture.SeededSemesterId,
                intake = "2026 Intake B",
                startDate = "2026-01-15T00:00:00Z",
                endDate = "2026-06-30T00:00:00Z",
                registrationOpenDate = "2025-11-01T00:00:00Z",
                registrationCloseDate = "2026-01-10T00:00:00Z",
                status = "Draft"
            };
            var createResponse = await client.PostAsJsonAsync("/api/v1/courseoffering", createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<CourseOfferingDto>(JsonOptions);

            var deleteResponse = await client.DeleteAsync($"/api/v1/courseoffering/{created!.Id}");

            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // ===== Recreation / Reopening a course =====

        [Fact]
        public async Task ReopenCourse_CreatesNewDistinctOffering()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            // Create two offerings of the SAME course (simulating reopening)
            var first = await CreateOfferingAsync(client, "2026 Intake First", "Active");
            var second = await CreateOfferingAsync(client, "2026 Intake Second", "Active");

            second.Should().NotBeNull();
            first.Should().NotBeNull();
            second!.Id.Should().NotBe(first!.Id);
            second.CourseId.Should().Be(first.CourseId);
            second.OfferingCode.Should().NotBe(first.OfferingCode);
        }

        private async Task<CourseOfferingDto?> CreateOfferingAsync(HttpClient client, string intake, string status)
        {
            var request = new
            {
                courseId = _fixture.SeededCourseId,
                academicYearId = _fixture.SeededAcademicYearId,
                semesterId = _fixture.SeededSemesterId,
                intake,
                startDate = "2026-01-15T00:00:00Z",
                endDate = "2026-06-30T00:00:00Z",
                registrationOpenDate = "2025-11-01T00:00:00Z",
                registrationCloseDate = "2026-01-10T00:00:00Z",
                status
            };
            var response = await client.PostAsJsonAsync("/api/v1/courseoffering", request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return await response.Content.ReadFromJsonAsync<CourseOfferingDto>(JsonOptions);
        }

        // ===== Course Offering Units =====

        [Fact]
        public async Task AddUnitToOffering_ValidRequest_ReturnsCreated()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var offering = await CreateOfferingAsync(client, "2026 Intake Units", "Draft");
            offering.Should().NotBeNull();

            var request = new
            {
                name = "Unit 1: Introduction",
                code = "WM101U1",
                description = "First unit",
                order = 1,
                credits = 1
            };

            var response = await client.PostAsJsonAsync($"/api/v1/courseoffering/{offering!.Id}/units", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var dto = await response.Content.ReadFromJsonAsync<CourseOfferingUnitDto>(JsonOptions);
            dto.Should().NotBeNull();
            dto!.CourseOfferingId.Should().Be(offering.Id);
        }

        [Fact]
        public async Task GetUnits_ExistingOffering_ReturnsList()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var offering = await CreateOfferingAsync(client, "2026 Intake Units2", "Draft");
            offering.Should().NotBeNull();

            var response = await client.GetAsync($"/api/v1/courseoffering/{offering!.Id}/units");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await response.Content.ReadFromJsonAsync<CourseOfferingUnitDto[]>(JsonOptions);
            results.Should().NotBeNull();
        }

        // ===== Assignment =====

        [Fact]
        public async Task AssignStudentToOffering_ValidRequest_ReturnsCreated()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var offering = await CreateOfferingAsync(client, "2026 Intake Assign S", "Active");
            offering.Should().NotBeNull();

            var request = new { studentId = _fixture.SeededStudentId };

            var response = await client.PostAsJsonAsync($"/api/v1/courseoffering/{offering!.Id}/students", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var dto = await response.Content.ReadFromJsonAsync<CourseOfferingEnrollmentDto>(JsonOptions);
            dto.Should().NotBeNull();
            dto!.StudentId.Should().Be(_fixture.SeededStudentId);
        }

        [Fact]
        public async Task AssignLecturerToOffering_ValidRequest_ReturnsCreated()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var offering = await CreateOfferingAsync(client, "2026 Intake Assign L", "Active");
            offering.Should().NotBeNull();

            var request = new { lecturerId = _fixture.SeededLecturerId };

            var response = await client.PostAsJsonAsync($"/api/v1/courseoffering/{offering!.Id}/lecturers", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var dto = await response.Content.ReadFromJsonAsync<CourseOfferingLecturerDto>(JsonOptions);
            dto.Should().NotBeNull();
            dto!.LecturerId.Should().Be(_fixture.SeededLecturerId);
        }

        // ===== Confirmation Workflow =====

        [Fact]
        public async Task ConfirmEnrollment_PendingEnrollment_ReturnsOkAndUpdatesStatus()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            // Create offering + assign student -> creates pending enrollment
            var offering = await CreateOfferingAsync(client, "2026 Intake Confirm", "Active");
            offering.Should().NotBeNull();

            var assignRequest = new { studentId = _fixture.SeededStudentId };
            var assignResponse = await client.PostAsJsonAsync($"/api/v1/courseoffering/{offering!.Id}/students", assignRequest);
            assignResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var enrollment = await assignResponse.Content.ReadFromJsonAsync<CourseOfferingEnrollmentDto>(JsonOptions);

            // Confirm the enrollment
            var confirmRequest = new { confirm = true };
            var confirmResponse = await client.PostAsJsonAsync($"/api/v1/confirmation/enrollments/{enrollment!.Id}/confirm", confirmRequest);

            confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var confirmed = await confirmResponse.Content.ReadFromJsonAsync<CourseOfferingEnrollmentDto>(JsonOptions);
            confirmed.Should().NotBeNull();
            confirmed!.ConfirmationStatus.Should().Be(ConfirmationStatus.Confirmed);
        }

        [Fact]
        public async Task ReportIssue_ValidRequest_ReturnsCreated()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var offering = await CreateOfferingAsync(client, "2026 Intake Issue", "Active");
            offering.Should().NotBeNull();

            var request = new
            {
                courseOfferingId = offering!.Id,
                assignmentType = "Enrollment",
                reason = "I was assigned to the wrong course offering.",
                reporterUserId = Guid.NewGuid()
            };

            var response = await client.PostAsJsonAsync("/api/v1/confirmation/issues", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var dto = await response.Content.ReadFromJsonAsync<AssignmentIssueReportDto>(JsonOptions);
            dto.Should().NotBeNull();
            dto!.CourseOfferingId.Should().Be(offering.Id);
        }

        [Fact]
        public async Task ReportIssue_InvalidAssignmentType_ReturnsBadRequest()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var offering = await CreateOfferingAsync(client, "2026 Intake Issue Invalid", "Active");
            offering.Should().NotBeNull();

            var request = new
            {
                courseOfferingId = offering!.Id,
                assignmentType = "InvalidType",
                reason = "Test details",
                reporterUserId = Guid.NewGuid()
            };

            var response = await client.PostAsJsonAsync("/api/v1/confirmation/issues", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ===== Authorization Enforcement =====

        [Fact]
        public async Task UnauthenticatedRequest_ReturnsUnauthorized()
        {
            var client = _fixture.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");

            var response = await client.GetAsync("/api/v1/courseoffering");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetPendingEnrollments_ReturnsOkForStudent()
        {
            using var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync($"/api/v1/confirmation/enrollments/pending/{_fixture.SeededStudentId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await response.Content.ReadFromJsonAsync<CourseOfferingEnrollmentDto[]>(JsonOptions);
            results.Should().NotBeNull();
        }
    }
}
