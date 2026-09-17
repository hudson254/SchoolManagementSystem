using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using SMS.Application.DTOs;
using SMS.Domain.Enums;
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
    /// End-to-end API tests for the document upload pipeline (assignment
    /// question documents + study materials) and the personal dashboards.
    /// These tests exist because a missing DI registration for
    /// IUploadService/IUploadRepository shipped to production undetected: the
    /// handlers failed at ACTIVATION time (HTTP 500), which only surfaces when
    /// an upload/list endpoint is actually exercised.
    ///
    /// Dedicated WebApplicationFactory with a mocked ICurrentUserService
    /// (mutable email/roles), real bearer-token authentication, and the shared
    /// PostgreSQL test database.
    /// </summary>
    public class DocumentUploadFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private const string AdminEmail = "admin@school.com";
        private const string AdminPassword = "Admin123!@#q1";

        // Mutable mocked identity. Tests set these before creating a client.
        // The fixture overrides CurrentUserEmail in InitializeAsync with a
        // unique per-instance value because the shared PostgreSQL test database
        // enforces the unique Lecturers.Email index across runs.
        public string CurrentUserEmail { get; set; } = "lecturer.upload@school.com";
        public string CurrentUserId { get; set; } = "lecturer-upload-user-id";
        public string[] CurrentUserRoles { get; set; } = new[] { "Lecturer" };

        // Seeded graph shared by the tests (unique GUIDs per fixture instance).
        public Guid CourseId { get; private set; }
        public Guid SemesterId { get; private set; }
        public Guid UnitId { get; private set; }
        public Guid OtherUnitId { get; private set; }
        public Guid LecturerId { get; private set; }
        public Guid AssignmentId { get; private set; }
        public Guid OfferingId { get; private set; }
        public Guid StudentId { get; private set; }
        public Guid LaneId { get; private set; }
        public Guid HouseId { get; private set; }
        public Guid AccommodationAssignmentId { get; private set; }
        public Guid StudentAccommodationAssignmentId { get; private set; }
        public string SeededStudentEmail { get; private set; } = string.Empty;
        public Guid SeededStudentUserId { get; private set; }
        public string LecturerEmail { get; private set; } = string.Empty;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            // Keep uploaded test files out of the repository/source tree.
            builder.UseSetting("FileStorage:Path",
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sms-test-uploads", Guid.NewGuid().ToString("N")));

            builder.ConfigureServices(services =>
            {
                var appCurrentUserServiceType = typeof(SMS.Application.Common.Interfaces.ICurrentUserService);
                RemoveServiceDescriptors(appCurrentUserServiceType, services);
                var mockCurrentUser = new Mock<SMS.Application.Common.Interfaces.ICurrentUserService>();
                mockCurrentUser.Setup(x => x.UserId).Returns(() => CurrentUserId);
                mockCurrentUser.Setup(x => x.Email).Returns(() => CurrentUserEmail);
                mockCurrentUser.Setup(x => x.Username).Returns(() => CurrentUserEmail.Split('@')[0]);
                mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
                mockCurrentUser.Setup(x => x.Roles).Returns(() => CurrentUserRoles);
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
            var pending = await db.Database.GetPendingMigrationsAsync();
            if (pending.Any())
                await db.Database.MigrateAsync();

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

            // ---- Academic graph for the lecturer under test ----
            var suffix = Guid.NewGuid().ToString("N")[..8];
            LecturerEmail = $"lecturer.upload.{suffix}@school.com";
            CurrentUserEmail = LecturerEmail;
            CourseId = Guid.NewGuid();
            db.Courses.Add(new Course
            {
                Id = CourseId,
                Name = $"Upload Test Course {suffix}",
                Code = $"UTC-{suffix}",
                Credits = 3,
                Duration = 1,
                IsActive = true,
                TenantId = DefaultTenantId
            });

            SemesterId = Guid.NewGuid();
            db.Semesters.Add(new Semester
            {
                Id = SemesterId,
                Name = $"Upload Test Semester {suffix}",
                SemesterNumber = 1,
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow.AddMonths(5),
                IsActive = true,
                TenantId = DefaultTenantId
            });

            UnitId = Guid.NewGuid();
            OtherUnitId = Guid.NewGuid();
            db.Units.Add(new Unit
            {
                Id = UnitId,
                Name = $"Taught Unit {suffix}",
                Code = $"UTU-{suffix}",
                Credits = 3,
                ContactHours = 30,
                CourseId = CourseId,
                IsActive = true,
                TenantId = DefaultTenantId
            });
            db.Units.Add(new Unit
            {
                Id = OtherUnitId,
                Name = $"Untaught Unit {suffix}",
                Code = $"UTX-{suffix}",
                Credits = 3,
                ContactHours = 30,
                CourseId = CourseId,
                IsActive = true,
                TenantId = DefaultTenantId
            });

            LecturerId = Guid.NewGuid();
            db.Lecturers.Add(new Lecturer
            {
                Id = LecturerId,
                FirstName = "Upload",
                LastName = "Lecturer",
                Email = CurrentUserEmail,
                EmployeeNumber = $"EMP-{suffix}",
                IsActive = true,
                TenantId = DefaultTenantId
            });

            db.UnitAllocations.Add(new UnitAllocation
            {
                Id = Guid.NewGuid(),
                LecturerId = LecturerId,
                UnitId = UnitId,
                SemesterId = SemesterId,
                AllocationDate = DateTime.UtcNow,
                Status = "Active",
                IsPrimary = true,
                TenantId = DefaultTenantId
            });

            AssignmentId = Guid.NewGuid();
            db.Assignments.Add(new Assignment
            {
                Id = AssignmentId,
                Title = $"Upload Test Assignment {suffix}",
                Description = "Assignment used by the document upload integration tests",
                UnitId = UnitId,
                LecturerId = LecturerId,
                MaxScore = 100,
                DueDate = DateTime.UtcNow.AddDays(14),
                IsActive = true,
                Status = "Published",
                TenantId = DefaultTenantId
            });

            // ---- Course offering machinery for the dashboard tests ----
            OfferingId = Guid.NewGuid();
            db.CourseOfferings.Add(new CourseOffering
            {
                Id = OfferingId,
                OfferingCode = $"OFF-{suffix}",
                CourseId = CourseId,
                AcademicYearName = "2026/2027",
                SemesterName = "Semester 1",
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow.AddMonths(4),
                IsActive = true,
                Status = CourseOfferingStatus.Active,
                TenantId = DefaultTenantId
            });
            db.CourseOfferingUnits.Add(new CourseOfferingUnit
            {
                Id = Guid.NewGuid(),
                CourseOfferingId = OfferingId,
                UnitId = UnitId,
                Name = $"Taught Unit {suffix}",
                Code = $"UTU-{suffix}",
                Credits = 3,
                ContactHours = 30,
                Order = 1,
                IsActive = true,
                TenantId = DefaultTenantId
            });
            db.CourseOfferingLecturers.Add(new CourseOfferingLecturer
            {
                Id = Guid.NewGuid(),
                CourseOfferingId = OfferingId,
                LecturerId = LecturerId,
                // A confirmed teaching assignment. The production lifecycle is
                // PendingConfirmation -> (ConfirmTeachingAssignmentCommand)
                // -> Status="Active" (see ConfirmTeachingAssignmentCommand); the
                // dashboard's GetActiveByLecturerAsync filters Status=="Active"
                // and no production code path ever writes "Confirmed" here.
                Status = "Active",
                ConfirmationStatus = ConfirmationStatus.Confirmed,
                IsActive = true,
                IsPrimary = true,
                TenantId = DefaultTenantId
            });

            // ---- Accommodation hierarchy + lecturer assignment ----
            LaneId = Guid.NewGuid();
            db.Lanes.Add(new Lane
            {
                Id = LaneId,
                LaneName = $"Lane {suffix}",
                IsActive = true,
                TenantId = DefaultTenantId
            });
            HouseId = Guid.NewGuid();
            db.Houses.Add(new House
            {
                Id = HouseId,
                LaneId = LaneId,
                HouseNumber = $"H-{suffix}",
                HouseNumberNumeric = 1,
                Capacity = 2,
                IsEnabled = true,
                IsAvailable = true,
                Status = HouseStatus.Vacant,
                TenantId = DefaultTenantId
            });
            AccommodationAssignmentId = Guid.NewGuid();
            db.AccommodationAssignments.Add(new AccommodationAssignment
            {
                Id = AccommodationAssignmentId,
                LecturerId = LecturerId,
                HouseId = HouseId,
                LaneId = LaneId,
                SemesterId = SemesterId,
                AssignedDate = DateTime.UtcNow,
                Status = "Active",
                TenantId = DefaultTenantId
            });

            // ---- Student + enrollment for the student dashboard test ----
            var studentEmail = $"student.upload.{suffix}@school.com";
            var studentUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = studentEmail,
                Email = studentEmail,
                NormalizedUserName = studentEmail.ToUpperInvariant(),
                NormalizedEmail = studentEmail.ToUpperInvariant(),
                FirstName = "Upload",
                LastName = "Student",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                RefreshToken = string.Empty,
                TenantId = DefaultTenantId
            };
            await userManager.CreateAsync(studentUser, "Student123!@#xyz");
            SeededStudentEmail = studentEmail;
            SeededStudentUserId = Guid.Parse(studentUser.Id);

            StudentId = Guid.NewGuid();
            db.Students.Add(new Student
            {
                Id = StudentId,
                UserId = studentUser.Id,
                StudentNumber = $"SN-{suffix}",
                FirstName = "Upload",
                LastName = "Student",
                Email = studentEmail,
                AcademicStatus = "Active",
                IsActive = true,
                IsEnrolled = true,
                TenantId = DefaultTenantId
            });
            db.CourseOfferingEnrollments.Add(new CourseOfferingEnrollment
            {
                Id = Guid.NewGuid(),
                CourseOfferingId = OfferingId,
                StudentId = StudentId,
                Status = "Active",
                IsActive = true,
                AttemptNumber = 1,
                TenantId = DefaultTenantId
            });
            StudentAccommodationAssignmentId = Guid.NewGuid();
            db.AccommodationAssignments.Add(new AccommodationAssignment
            {
                Id = StudentAccommodationAssignmentId,
                StudentId = StudentId,
                HouseId = HouseId,
                LaneId = LaneId,
                SemesterId = SemesterId,
                AssignedDate = DateTime.UtcNow,
                Status = "Active",
                TenantId = DefaultTenantId
            });

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Authenticated client (bearer token from the seeded admin login) with
        /// the tenant header. The mocked ICurrentUserService identity is what
        /// the APPLICATION-layer authorization checks observe.
        /// </summary>
        public HttpClient CreateAuthenticatedClient()
        {
            var client = base.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");

            var loginResponse = client.PostAsJsonAsync("/api/v1/auth/login",
                new { email = AdminEmail, password = AdminPassword, rememberMe = true }).GetAwaiter().GetResult();

            var token = ExtractCookieValue(loginResponse, "access_token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        public new Task DisposeAsync() => Task.CompletedTask;

        /// <summary>Switches the mocked identity to the seeded lecturer.</summary>
        public void UseLecturerIdentity()
        {
            CurrentUserEmail = LecturerEmail;
            CurrentUserRoles = new[] { "Lecturer" };
            CurrentUserId = "lecturer-upload-user-id";
        }

        /// <summary>Switches the mocked identity to the seeded enrolled student.</summary>
        public void UseStudentIdentity()
        {
            CurrentUserEmail = SeededStudentEmail;
            CurrentUserRoles = new[] { "Student" };
            CurrentUserId = SeededStudentUserId.ToString();
        }

        /// <summary>Switches the mocked identity to an unknown profile-less user.</summary>
        public void UseUnknownIdentity()
        {
            CurrentUserEmail = $"nobody.{Guid.NewGuid():N}@school.com";
            CurrentUserRoles = new[] { "Lecturer" };
            CurrentUserId = "unknown-user-id";
        }

        /// <summary>Multipart form content carrying a valid PDF.</summary>
        public static MultipartFormDataContent PdfUploadContent(
            string fileName, string unitField = null, string title = "E2E Material",
            string description = null, string specifiedLecturerId = null)
        {
            var form = new MultipartFormDataContent();
            var bytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%test-document\n");
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(fileContent, "file", fileName);
            if (title != null) form.Add(new StringContent(title), "title");
            if (description != null) form.Add(new StringContent(description), "description");
            if (specifiedLecturerId != null) form.Add(new StringContent(specifiedLecturerId), "specifiedLecturerId");
            return form;
        }

        /// <summary>Multipart form content whose bytes do NOT look like any allowed document.</summary>
        public static MultipartFormDataContent ExecutableUploadContent(string fileName)
        {
            var form = new MultipartFormDataContent();
            var bytes = Encoding.ASCII.GetBytes("MZ\x90\x00 fake executable payload");
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent("Malicious upload attempt"), "title");
            return form;
        }

        private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
        {
            var cookies = response.Headers.GetValues("Set-Cookie")
                .SelectMany(header => header.Split(';'))
                .Select(part => part.Trim())
                .FirstOrDefault(part => part.StartsWith(cookieName + "=", StringComparison.OrdinalIgnoreCase));

            return cookies?.Substring(cookieName.Length + 1) ?? string.Empty;
        }
    }

    /// <summary>
    /// Regression tests for the document upload pipeline and personal
    /// dashboards. The first run of this class caught the production incident
    /// where IUploadService/IUploadRepository were not registered in the DI
    /// container (HTTP 500 on every upload), proving the value of exercising
    /// the endpoints end-to-end.
    /// </summary>
    public class DocumentUploadApiTests : IClassFixture<DocumentUploadFixture>
    {
        private readonly DocumentUploadFixture _fixture;

        // The API serializes enums as strings (JsonStringEnumConverter in
        // Program.cs) with camelCase property names (default ASP.NET Core
        // JsonOptions). The default ReadFromJsonAsync options use neither, so
        // OccupantType deserialization fails and camelCase members may not bind.
        private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public DocumentUploadApiTests(DocumentUploadFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Lecturer_UploadsStudyMaterial_ThenListsDownloadsAndDeletes()
        {
            _fixture.UseLecturerIdentity();
            var client = _fixture.CreateAuthenticatedClient();

            using var form = DocumentUploadFixture.PdfUploadContent(
                "lecture-notes.pdf",
                title: "Week 1 Notes",
                description: "Integration test material");

            var uploadResponse = await client.PostAsync(
                $"/api/v1/study-materials/unit/{_fixture.UnitId}", form);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created,
                because: "a lecturer teaching the unit must be able to upload study materials");

            var material = await uploadResponse.Content.ReadFromJsonAsync<StudyMaterialDto>();
            material.Should().NotBeNull();
            material!.Id.Should().NotBe(Guid.Empty);
            material.UnitId.Should().Be(_fixture.UnitId);
            material.OriginalFileName.Should().Be("lecture-notes.pdf");

            var listResponse = await client.GetAsync($"/api/v1/study-materials/unit/{_fixture.UnitId}");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var materials = await listResponse.Content.ReadFromJsonAsync<List<StudyMaterialDto>>();
            materials.Should().Contain(m => m.Id == material.Id);

            var downloadResponse = await client.GetAsync($"/api/v1/study-materials/{material.Id}/download");
            downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            downloadResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
            (await downloadResponse.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();

            var deleteResponse = await client.DeleteAsync(
                $"/api/v1/study-materials/{material.Id}?unitId={_fixture.UnitId}");
            deleteResponse.IsSuccessStatusCode.Should().BeTrue();

            var listAfter = await client.GetAsync($"/api/v1/study-materials/unit/{_fixture.UnitId}");
            var after = await listAfter.Content.ReadFromJsonAsync<List<StudyMaterialDto>>();
            after.Should().NotContain(m => m.Id == material.Id);
        }

        [Fact]
        public async Task Lecturer_UploadsAssignmentDocument_ThenListsDownloadsAndDeletes()
        {
            _fixture.UseLecturerIdentity();
            var client = _fixture.CreateAuthenticatedClient();

            using var form = new MultipartFormDataContent();
            var bytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%assignment-brief\n");
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(fileContent, "file", "assignment-brief.pdf");
            form.Add(new StringContent("E2E brief"), "description");

            var uploadResponse = await client.PostAsync(
                $"/api/v1/assignments/{_fixture.AssignmentId}/documents", form);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK,
                because: "the owning lecturer must be able to attach question documents");

            var document = await uploadResponse.Content.ReadFromJsonAsync<AssignmentDocumentDto>();
            document.Should().NotBeNull();
            document!.Id.Should().NotBe(Guid.Empty);
            document.OriginalFileName.Should().Be("assignment-brief.pdf");

            var listResponse = await client.GetAsync($"/api/v1/assignments/{_fixture.AssignmentId}/documents");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var documents = await listResponse.Content.ReadFromJsonAsync<List<AssignmentDocumentDto>>();
            documents.Should().Contain(d => d.Id == document.Id);

            var downloadResponse = await client.GetAsync(
                $"/api/v1/assignments/{_fixture.AssignmentId}/documents/{document.Id}/download");
            downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            downloadResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

            var deleteResponse = await client.DeleteAsync(
                $"/api/v1/assignments/{_fixture.AssignmentId}/documents/{document.Id}");
            deleteResponse.IsSuccessStatusCode.Should().BeTrue();

            var listAfter = await client.GetAsync($"/api/v1/assignments/{_fixture.AssignmentId}/documents");
            var after = await listAfter.Content.ReadFromJsonAsync<List<AssignmentDocumentDto>>();
            after.Should().NotContain(d => d.Id == document.Id);
        }

        [Fact]
        public async Task Upload_ExecutableFile_ShouldBeRejected()
        {
            _fixture.UseLecturerIdentity();
            var client = _fixture.CreateAuthenticatedClient();

            using var form = DocumentUploadFixture.ExecutableUploadContent("payload.exe");
            var response = await client.PostAsync(
                $"/api/v1/study-materials/unit/{_fixture.UnitId}", form);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                because: "executable files must be rejected server-side regardless of authorization");
        }

        [Fact]
        public async Task Lecturer_UploadingToUnitTheyDoNotTeach_ShouldBeForbidden()
        {
            _fixture.UseLecturerIdentity();
            var client = _fixture.CreateAuthenticatedClient();

            using var form = DocumentUploadFixture.PdfUploadContent("notes.pdf");
            var response = await client.PostAsync(
                $"/api/v1/study-materials/unit/{_fixture.OtherUnitId}", form);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                because: "authorization must be enforced server-side from the lecturer-unit relationship");
        }

        [Fact]
        public async Task Student_UploadStudyMaterial_ShouldBeForbidden()
        {
            // Student identity (enrolled in the offering) attempts an upload.
            _fixture.UseStudentIdentity();
            
            var client = _fixture.CreateAuthenticatedClient();

            using var form = DocumentUploadFixture.PdfUploadContent("notes.pdf");
            var response = await client.PostAsync(
                $"/api/v1/study-materials/unit/{_fixture.UnitId}", form);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                because: "students must never be able to create study materials");
        }

        [Fact]
        public async Task Student_EnrolledInOffering_CanAccessUnitMaterials()
        {
            var client = _fixture.CreateAuthenticatedClient();

            // Lecturer uploads a published material.
            _fixture.UseLecturerIdentity();
            using (var form = DocumentUploadFixture.PdfUploadContent("reading.pdf", title: "Reading"))
            {
                var upload = await client.PostAsync($"/api/v1/study-materials/unit/{_fixture.UnitId}", form);
                upload.StatusCode.Should().Be(HttpStatusCode.Created);
                var material = await upload.Content.ReadFromJsonAsync<StudyMaterialDto>();

                // Switch to the enrolled student and access the material.
                _fixture.UseStudentIdentity();
                using var studentClient = _fixture.CreateAuthenticatedClient();

                var list = await studentClient.GetAsync($"/api/v1/study-materials/unit/{_fixture.UnitId}");
                list.StatusCode.Should().Be(HttpStatusCode.OK);
                var materials = await list.Content.ReadFromJsonAsync<List<StudyMaterialDto>>();
                materials.Should().Contain(m => m.Id == material!.Id);

                var download = await studentClient.GetAsync($"/api/v1/study-materials/{material!.Id}/download");
                download.StatusCode.Should().Be(HttpStatusCode.OK);

                // Unrelated unit remains inaccessible.
                var unrelated = await studentClient.GetAsync(
                    $"/api/v1/study-materials/unit/{_fixture.OtherUnitId}");
                unrelated.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                    because: "students must not access materials of units outside their enrollment");
            }
        }

        [Fact]
        public async Task LecturerDashboard_ReturnsRealCoursesAndAccommodation()
        {
            _fixture.UseLecturerIdentity();
            
            var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync("/api/v1/dashboard/lecturer-me");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var dashboard = await response.Content.ReadFromJsonAsync<MyLecturerDashboardDto>(JsonOptions);
            dashboard.Should().NotBeNull();
            dashboard!.LecturerId.Should().Be(_fixture.LecturerId);

            // Feature C: the real course offering taught via CourseOfferingLecturer.
            dashboard.Courses.Should().Contain(c => c.CourseOfferingId == _fixture.OfferingId);
            var course = dashboard.Courses.First(c => c.CourseOfferingId == _fixture.OfferingId);
            course.CourseId.Should().Be(_fixture.CourseId);
            course.CourseName.Should().NotBeEmpty();
            course.AcademicYearName.Should().Be("2026/2027");
            course.Units.Should().Contain(u => u.UnitId == _fixture.UnitId);

            // Direct unit allocation is also surfaced.
            dashboard.UnitAllocations.Should().Contain(u => u.UnitId == _fixture.UnitId);

            // Feature E: accommodation assignment from the accommodation module.
            dashboard.Accommodation.Should().NotBeNull();
            dashboard.Accommodation!.HouseId.Should().Be(_fixture.HouseId);
        }

        [Fact]
        public async Task StudentDashboard_ReturnsRealEnrollmentAndAccommodation()
        {
            _fixture.UseStudentIdentity();
            
            var client = _fixture.CreateAuthenticatedClient();

            var response = await client.GetAsync("/api/v1/dashboard/student-me");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var dashboard = await response.Content.ReadFromJsonAsync<MyStudentDashboardDto>(JsonOptions);
            dashboard.Should().NotBeNull();
            dashboard!.StudentId.Should().Be(_fixture.StudentId);

            // Feature D: the real active course-offering enrollment.
            dashboard.Enrollments.Should().Contain(e => e.CourseOfferingId == _fixture.OfferingId);
            var enrollment = dashboard.Enrollments.First(e => e.CourseOfferingId == _fixture.OfferingId);
            enrollment.CourseId.Should().Be(_fixture.CourseId);
            enrollment.CourseName.Should().NotBeEmpty();
            enrollment.Units.Should().Contain(u => u.UnitId == _fixture.UnitId);

            // Feature E: accommodation assignment from the accommodation module.
            dashboard.Accommodation.Should().NotBeNull();
            dashboard.Accommodation!.HouseId.Should().Be(_fixture.HouseId);
        }

        [Fact]
        public async Task Dashboards_WithoutMatchingProfile_ReturnNotFound()
        {
            // Identity that has neither a lecturer nor a student profile.
            _fixture.UseUnknownIdentity();
            var client = _fixture.CreateAuthenticatedClient();

            var lecturerResponse = await client.GetAsync("/api/v1/dashboard/lecturer-me");
            lecturerResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            _fixture.CurrentUserRoles = new[] { "Student" };
            var studentResponse = await client.GetAsync("/api/v1/dashboard/student-me");
            studentResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
