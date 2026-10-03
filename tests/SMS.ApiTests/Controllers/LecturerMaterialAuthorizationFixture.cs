using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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
    /// Fixture for the D4/D5 security regressions: the approval gate on teaching
    /// material management, and object-level authorization when deleting another
    /// lecturer's material.
    /// <para>
    /// One course offering with THREE units. Lecturer A and Lecturer B are both
    /// approved and BOTH appointed to Unit 1, so "teaches the unit" cannot be
    /// mistaken for "owns the material". Lecturer P is deliberately given an ACTIVE
    /// unit allocation too - the worst case - while still being PendingApproval, so
    /// the approval gate is proven independently of the allocation status.
    /// </para>
    /// </summary>
    public class LecturerMaterialAuthorizationFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private const string AdminEmail = "admin@school.com";
        private const string AdminPassword = "Admin123!@#q1";

        public string CurrentUserEmail { get; set; } = "nobody@school.com";
        public string CurrentUserId { get; set; } = "nobody-user-id";
        public string[] CurrentUserRoles { get; set; } = new[] { "Lecturer" };

        public Guid CourseId { get; private set; }
        public Guid OfferingId { get; private set; }
        public Guid SemesterId { get; private set; }
        public Guid UnitOneId { get; private set; }
        public Guid UnitTwoId { get; private set; }
        public Guid UnitThreeId { get; private set; }
        public Guid LecturerAId { get; private set; }
        public Guid LecturerBId { get; private set; }
        public Guid PendingLecturerId { get; private set; }
        public string LecturerAEmail { get; private set; } = string.Empty;
        public string LecturerBEmail { get; private set; } = string.Empty;
        public string PendingLecturerEmail { get; private set; } = string.Empty;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
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
                mockTenantStore.Setup(x => x.GetTenantAsync(It.IsAny<string>()))
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
            await TestDatabaseMigrator.MigrateAsync(Services);

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

            foreach (var roleName in new[] { "Administrator", "Lecturer" })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new Role { Name = roleName });
            }

            if (!await userManager.Users.AnyAsync(u => u.Email == AdminEmail))
            {
                var admin = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true,
                    FirstName = "Verification",
                    LastName = "Administrator",
                    IsActive = true
                };
                await userManager.CreateAsync(admin, AdminPassword);
                await userManager.AddToRoleAsync(admin, "Administrator");
            }

            var suffix = Guid.NewGuid().ToString("N")[..8];
            SemesterId = Guid.NewGuid();
            db.Semesters.Add(new Semester
            {
                Id = SemesterId,
                Name = $"Semester 1 {suffix}",
                IsActive = true,
                TenantId = DefaultTenantId
            });

            CourseId = Guid.NewGuid();
            db.Courses.Add(new Course
            {
                Id = CourseId,
                Name = $"Authorization Verification Course {suffix}",
                Code = $"AVC-{suffix}",
                IsActive = true,
                TenantId = DefaultTenantId
            });

            UnitOneId = Guid.NewGuid();
            UnitTwoId = Guid.NewGuid();
            UnitThreeId = Guid.NewGuid();
            foreach (var (unitId, code) in new[]
            {
                (UnitOneId, $"U1{suffix}"), (UnitTwoId, $"U2{suffix}"), (UnitThreeId, $"U3{suffix}")
            })
            {
                db.Units.Add(new Unit
                {
                    Id = unitId,
                    Code = code,
                    Name = $"Verification Unit {code}",
                    Credits = 3,
                    ContactHours = 30,
                    CourseId = CourseId,
                    IsActive = true,
                    TenantId = DefaultTenantId
                });
            }

            OfferingId = Guid.NewGuid();
            db.CourseOfferings.Add(new CourseOffering
            {
                Id = OfferingId,
                OfferingCode = $"AVC-{suffix}-2026-S1-001",
                CourseId = CourseId,
                AcademicYearName = "2026/2027",
                SemesterName = "Semester 1",
                SemesterId = SemesterId,
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow.AddMonths(4),
                IsActive = true,
                Status = CourseOfferingStatus.Active,
                TenantId = DefaultTenantId
            });

            // The shared offering snapshot deliberately contains all three units.
            foreach (var (unitId, code, order) in new[]
            {
                (UnitOneId, $"U1{suffix}", 1), (UnitTwoId, $"U2{suffix}", 2), (UnitThreeId, $"U3{suffix}", 3)
            })
            {
                db.CourseOfferingUnits.Add(new CourseOfferingUnit
                {
                    Id = Guid.NewGuid(),
                    CourseOfferingId = OfferingId,
                    UnitId = unitId,
                    Code = code,
                    Name = $"Verification Unit {code}",
                    Credits = 3,
                    Order = order,
                    IsActive = true,
                    TenantId = DefaultTenantId
                });
            }

            LecturerAId = await SeedLecturerAsync(db, userManager, "A", RegistrationStatus.Approved, UnitOneId, "Active");
            LecturerBId = await SeedLecturerAsync(db, userManager, "B", RegistrationStatus.Approved, UnitOneId, "Active");
            // Worst case for D4: an ACTIVE allocation while still PendingApproval.
            PendingLecturerId = await SeedLecturerAsync(
                db, userManager, "P", RegistrationStatus.PendingApproval, UnitOneId, "Active");

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds a lecturer with an ACTIVE unit allocation and an ACTIVE offering
        /// teaching assignment, so the only variable is <paramref name="status"/>.
        /// </summary>
        private async Task<Guid> SeedLecturerAsync(
            ApplicationDbContext db, UserManager<User> userManager,
            string tag, RegistrationStatus status, Guid unitId, string allocationStatus)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var email = $"lecturer.{tag.ToLowerInvariant()}.{suffix}@school.com";

            // Lecturers.UserId is a real FK to AspNetUsers, so each profile needs a
            // genuine Identity account - exactly as registration would create.
            var identityUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = $"Verification {tag}",
                LastName = "Lecturer",
                IsActive = true
            };
            var created = await userManager.CreateAsync(identityUser, "Lecturer123!@#abc");
            if (!created.Succeeded)
                throw new InvalidOperationException(
                    "Could not seed the lecturer identity user: " +
                    string.Join("; ", created.Errors.Select(e => e.Description)));

            var lecturer = new Lecturer
            {
                Id = Guid.NewGuid(),
                FirstName = $"Verification {tag}",
                LastName = "Lecturer",
                Email = email,
                EmployeeNumber = $"EMP-{tag}-{suffix}",
                IsActive = true,
                RegistrationStatus = status,
                UserId = identityUser.Id,
                TenantId = DefaultTenantId
            };
            db.Lecturers.Add(lecturer);

            db.UnitAllocations.Add(new UnitAllocation
            {
                Id = Guid.NewGuid(),
                LecturerId = lecturer.Id,
                UnitId = unitId,
                SemesterId = SemesterId,
                CourseOfferingId = OfferingId,
                AllocationDate = DateTime.UtcNow,
                Status = allocationStatus,
                IsPrimary = true,
                TenantId = DefaultTenantId
            });

            db.CourseOfferingLecturers.Add(new CourseOfferingLecturer
            {
                Id = Guid.NewGuid(),
                CourseOfferingId = OfferingId,
                LecturerId = lecturer.Id,
                AssignmentDate = DateTime.UtcNow,
                // Mirrors production: approval is what flips this to "Active".
                Status = status == RegistrationStatus.Approved ? "Active" : "PendingConfirmation",
                ConfirmationStatus = status == RegistrationStatus.Approved
                    ? ConfirmationStatus.Confirmed
                    : ConfirmationStatus.Pending,
                IsActive = true,
                IsPrimary = false,
                TenantId = DefaultTenantId
            });

            switch (tag)
            {
                case "A": LecturerAEmail = email; break;
                case "B": LecturerBEmail = email; break;
                default: PendingLecturerEmail = email; break;
            }

            await Task.CompletedTask;
            return lecturer.Id;
        }

        /// <summary>Reads a material straight from PostgreSQL, bypassing the API.</summary>
        public async Task<(bool Exists, bool IsDeleted)> ReadMaterialAsync(Guid materialId)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var note = await db.LectureNotes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(n => n.Id == materialId);

            return (note != null, note?.IsDeleted ?? false);
        }

        public void UseLecturerAIdentity()
        {
            CurrentUserEmail = LecturerAEmail;
            CurrentUserId = LecturerAId.ToString();
            CurrentUserRoles = new[] { "Lecturer" };
        }

        public void UseLecturerBIdentity()
        {
            CurrentUserEmail = LecturerBEmail;
            CurrentUserId = LecturerBId.ToString();
            CurrentUserRoles = new[] { "Lecturer" };
        }

        public void UsePendingLecturerIdentity()
        {
            CurrentUserEmail = PendingLecturerEmail;
            CurrentUserId = PendingLecturerId.ToString();
            CurrentUserRoles = new[] { "Lecturer" };
        }

        public HttpClient CreateAuthenticatedClient()
        {
            var client = base.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");

            var loginResponse = client.PostAsJsonAsync("/api/v1/auth/login",
                new { email = AdminEmail, password = AdminPassword, rememberMe = true })
                .GetAwaiter().GetResult();

            var token = ExtractCookieValue(loginResponse, "access_token");
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        public new Task DisposeAsync() => Task.CompletedTask;

        public static MultipartFormDataContent PdfUploadContent(string fileName, string title)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4\n%authorization-test\n"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(title), "title");
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
}
