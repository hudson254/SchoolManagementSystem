using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.API;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.ApiTests.Controllers
{
    /// <summary>
    /// Fixture for the Accommodation API contract tests.
    ///
    /// <para>
    /// Unlike <see cref="ApiTestFixture"/>, this fixture pins the resolved tenant
    /// to <see cref="TenantA"/> and additionally seeds a second tenant
    /// (<see cref="TenantB"/>) directly into the shared PostgreSQL database. That
    /// lets a single HTTP client prove the tenant-isolation invariant: a request
    /// issued as Tenant A must never surface a row that belongs to Tenant B,
    /// while <see cref="CreateTenantDb"/> proves the foreign row really exists.
    /// </para>
    ///
    /// <para>
    /// The tenant/identity mocks below are the same ones
    /// <see cref="ApiTestFixture"/> uses and 164 tests already pass with them, so
    /// the only new variable in these tests is the Accommodation data itself.
    /// </para>
    /// </summary>
    public class AccommodationApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        /// <summary>The tenant every HTTP client created here authenticates against.</summary>
        public static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

        /// <summary>
        /// A second, fully populated tenant that is never the resolved tenant of
        /// any client produced by this fixture.
        /// </summary>
        public static readonly Guid TenantB = Guid.Parse("99999999-9999-9999-9999-999999999999");

        private const string UserPassword = "Accom123!@#qX";
        private const string AdminEmail = "admin@school.com";
        private const string AdminPassword = "Admin123!@#q1";

        // Shared PostgreSQL database: migrate exactly once across fixture instances.
        private static readonly SemaphoreSlim InitLock = new(1, 1);
        private static int _userCounter;
        private bool _initialized;
        private string? _cachedAdminToken;

        // ===== Seeded identifiers (per fixture instance; names are GUID-suffixed) =====

        /// <summary>Tenant A lane that owns three houses (occupied / vacant / maintenance).</summary>
        public Guid OccupiedLaneId { get; private set; }
        public string OccupiedLaneName { get; private set; } = string.Empty;

        /// <summary>Tenant A lane that exists but owns zero houses.</summary>
        public Guid EmptyLaneId { get; private set; }

        /// <summary>Tenant B lane. Reaching it through a Tenant A client must be impossible.</summary>
        public Guid ForeignLaneId { get; private set; }
        public string ForeignLaneName { get; private set; } = string.Empty;
        public string ForeignStudentNumber { get; private set; } = string.Empty;
        public string ForeignEmployeeNumber { get; private set; } = string.Empty;

        /// <summary>
        /// A free Tenant B house. It must be seeded as genuinely available, or
        /// the "available houses never leak" assertion would pass trivially
        /// because the house is filtered out for being occupied.
        /// </summary>
        public string ForeignHouseNumber { get; private set; } = string.Empty;

        /// <summary>Tenant A house that carries an active student assignment.</summary>
        public Guid OccupiedHouseId { get; private set; }
        public Guid VacantHouseId { get; private set; }
        public Guid MaintenanceHouseId { get; private set; }

        /// <summary>
        /// A free house owned by the calling tenant. Used as the positive
        /// control in the available-houses test: it must be returned, so the
        /// absence of the foreign house is attributable to tenant scope and not
        /// to the endpoint simply returning nothing.
        /// </summary>
        public string OwnFreeHouseNumber { get; private set; } = string.Empty;

        public Guid StudentId { get; private set; }
        public string StudentNumber { get; private set; } = string.Empty;
        public Guid LecturerId { get; private set; }
        public string EmployeeNumber { get; private set; } = string.Empty;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var appCurrentUserServiceType = typeof(SMS.Application.Common.Interfaces.ICurrentUserService);
                RemoveServiceDescriptors(appCurrentUserServiceType, services);
                var mockCurrentUser = new Mock<SMS.Application.Common.Interfaces.ICurrentUserService>();
                mockCurrentUser.Setup(x => x.UserId).Returns("accommodation-test-user");
                mockCurrentUser.Setup(x => x.Email).Returns("accommodation@test.com");
                mockCurrentUser.Setup(x => x.Username).Returns("accommodation");
                mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
                mockCurrentUser.Setup(x => x.Roles).Returns(new[] { "Administrator" });
                services.AddScoped(_ => mockCurrentUser.Object);

                RemoveServiceDescriptors(typeof(SMS.Domain.Interfaces.ITenantContext), services);
                var mockDomainTenant = new Mock<SMS.Domain.Interfaces.ITenantContext>();
                mockDomainTenant.Setup(x => x.TenantId).Returns(TenantA.ToString());
                services.AddScoped(_ => mockDomainTenant.Object);

                RemoveServiceDescriptors(typeof(SMS.Multitenancy.Interfaces.ITenantContext), services);
                var mockMultiTenant = new Mock<SMS.Multitenancy.Interfaces.ITenantContext>();
                mockMultiTenant.Setup(x => x.TenantId).Returns(TenantA.ToString());
                mockMultiTenant.Setup(x => x.TenantName).Returns("Accommodation Tenant A");
                services.AddScoped(_ => mockMultiTenant.Object);

                RemoveServiceDescriptors(typeof(SMS.Application.Common.Interfaces.IUsernameGenerator), services);
                var mockUsernameGenerator = new Mock<SMS.Application.Common.Interfaces.IUsernameGenerator>();
                mockUsernameGenerator
                    .Setup(x => x.GenerateUsernameAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(() =>
                    {
                        var counter = Interlocked.Increment(ref _userCounter);
                        return $"accomuser{counter}_{Guid.NewGuid().ToString("N")[..6]}";
                    });
                mockUsernameGenerator
                    .Setup(x => x.IsUsernameAvailableAsync(It.IsAny<string>()))
                    .ReturnsAsync(true);
                services.AddScoped(_ => mockUsernameGenerator.Object);

                RemoveServiceDescriptors(typeof(ITenantStore), services);
                var mockTenantStore = new Mock<ITenantStore>();
                mockTenantStore
                    .Setup(x => x.GetTenantAsync(It.IsAny<string>()))
                    .ReturnsAsync(new Tenant
                    {
                        Id = TenantA,
                        Name = "Accommodation Tenant A",
                        Organization = "Accommodation Organization A",
                        Subdomain = "accommodation-a",
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

        /// <summary>
        /// Builds a UTC-kind date. Every accommodation column is
        /// <c>timestamp with time zone</c>, and Npgsql refuses to write a
        /// DateTime whose <see cref="DateTimeKind"/> is Unspecified - so the
        /// usual <c>new DateTime(2026, 1, 1)</c> shorthand fails on save.
        /// </summary>
        private static DateTime Date(int year, int month, int day) =>
            new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

        public async Task InitializeAsync() => await EnsureSeedDataAsync();

        public new Task DisposeAsync()
        {
            _initialized = false;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Builds a DbContext bound to PostgreSQL for the supplied tenant, outside
        /// the HTTP request pipeline. Used to seed the foreign tenant and to prove
        /// that a row filtered out of an HTTP response really is present in the
        /// database - without this, "the endpoint returned 404" would be
        /// indistinguishable from "the row was never created".
        /// </summary>
        public ApplicationDbContext CreateTenantDb(Guid tenantId)
        {
            var connectionString = Services.GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection");

            var tenantContext = new Mock<SMS.Domain.Interfaces.ITenantContext>();
            tenantContext.SetupGet(x => x.TenantId).Returns(tenantId.ToString());
            tenantContext.SetupGet(x => x.TenantName).Returns($"Tenant {tenantId:N}");
            tenantContext.SetupGet(x => x.ConnectionString).Returns(string.Empty);

            // The tenant interceptor MUST be attached here, exactly as the
            // runtime DI root attaches it. Without it this context has no
            // app.tenant_id on the PostgreSQL session, so once row level
            // security is enabled every read returns zero rows - and the
            // "prove the foreign row really exists" guard these isolation
            // tests depend on would fail for the wrong reason (or, worse,
            // pass vacuously).
            //
            // Attaching it also keeps this fixture honest: it now reads the
            // database through the same tenant context mechanism that
            // production uses, rather than through a privileged back door.
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString)
                .AddInterceptors(new TenantContextDbInterceptor(
                    tenantContext.Object,
                    NullLogger<TenantContextDbInterceptor>.Instance))
                .Options;

            return new ApplicationDbContext(
                options,
                new Mock<SMS.Domain.Interfaces.ICurrentUserService>().Object,
                tenantContext.Object);
        }

        /// <summary>An authenticated client whose user carries the supplied role in Tenant A.</summary>
        public async Task<HttpClient> CreateClientWithRoleAsync(string role)
        {
            if (role == "Administrator")
                return await CreateAdminClientAsync();

            var token = await CreateUserAndLoginAsync(role);
            return CreateClientWithToken(token);
        }

        public async Task<HttpClient> CreateAdminClientAsync()
        {
            var token = await EnsureAdminTokenAsync();
            return CreateClientWithToken(token);
        }

        private HttpClient CreateClientWithToken(string token)
        {
            var client = base.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", TenantA.ToString());
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<string> EnsureAdminTokenAsync()
        {
            if (!string.IsNullOrWhiteSpace(_cachedAdminToken))
                return _cachedAdminToken!;

            var client = base.CreateClient();
            try
            {
                var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                    new { email = AdminEmail, password = AdminPassword, rememberMe = true });
                response.EnsureSuccessStatusCode();
                _cachedAdminToken = ExtractCookieValue(response, "access_token");
            }
            finally
            {
                client.Dispose();
            }
            return _cachedAdminToken!;
        }

        /// <summary>
        /// Creates a Tenant A user in the supplied role and returns its access
        /// token. Emails are GUID-suffixed so repeated runs never collide.
        /// </summary>
        private async Task<string> CreateUserAndLoginAsync(string role)
        {
            var unique = Guid.NewGuid().ToString("N")[..8];
            var email = $"accom-{role.ToLowerInvariant()}-{unique}@test.local";
            var userName = email.Split('@')[0];

            using var scope = Services.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                FirstName = "Accommodation",
                LastName = role,
                Organization = "Test Org",
                EmailConfirmed = true,
                PhoneNumber = "0700000000",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                RefreshToken = string.Empty,
                TenantId = TenantA
            };

            var created = await users.CreateAsync(user, UserPassword);
            created.Succeeded.Should().BeTrue(
                $"the {role} test user must be created: {string.Join(", ", created.Errors.Select(e => e.Description))}");
            await users.AddToRoleAsync(user, role);

            var client = base.CreateClient();
            try
            {
                var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                    new { email, password = UserPassword, rememberMe = true });
                response.EnsureSuccessStatusCode();
                return ExtractCookieValue(response, "access_token");
            }
            finally
            {
                client.Dispose();
            }
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

        private async Task EnsureSeedDataAsync()
        {
            if (_initialized) return;

            await InitLock.WaitAsync();
            try
            {
                if (_initialized) return;

                using (var scope = Services.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await TestDatabaseMigrator.MigrateAsync(Services);
                    await EnsureTenantsAsync(db);
                    await EnsureRolesAndAdminAsync(scope.ServiceProvider);
                }

                await SeedTenantAAsync();
                await SeedTenantBAsync();

                _initialized = true;
            }
            catch
            {
                _initialized = false;
                throw;
            }
            finally
            {
                InitLock.Release();
            }
        }

        private static async Task EnsureTenantsAsync(ApplicationDbContext db)
        {
            var existing = await db.Tenants.IgnoreQueryFilters()
                .Select(t => t.Id)
                .ToListAsync();

            if (!existing.Contains(TenantA))
                db.Tenants.Add(new Tenant
                {
                    Id = TenantA,
                    Name = "Accommodation Tenant A",
                    Organization = "Accommodation Organization A",
                    Subdomain = "accommodation-a",
                    IsActive = true
                });

            if (!existing.Contains(TenantB))
                db.Tenants.Add(new Tenant
                {
                    Id = TenantB,
                    Name = "Accommodation Tenant B",
                    Organization = "Accommodation Organization B",
                    Subdomain = "accommodation-b",
                    IsActive = true
                });

            await db.SaveChangesAsync();
        }

        private async Task EnsureRolesAndAdminAsync(IServiceProvider provider)
        {
            var roleManager = provider.GetRequiredService<RoleManager<Role>>();
            foreach (var role in new[] { "Administrator", "Coordinator", "Receptionist", "Lecturer", "Student" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new Role { Name = role, DisplayName = role });
            }

            var userManager = provider.GetRequiredService<UserManager<User>>();
            var admin = await userManager.FindByEmailAsync(AdminEmail);
            if (admin is null)
            {
                admin = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = AdminEmail.Split('@')[0],
                    NormalizedUserName = AdminEmail.ToUpperInvariant(),
                    Email = AdminEmail,
                    NormalizedEmail = AdminEmail.ToUpperInvariant(),
                    FirstName = "Test",
                    LastName = "Admin",
                    Organization = "Test Org",
                    EmailConfirmed = true,
                    PhoneNumber = "0700000000",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                    RefreshToken = string.Empty,
                    TenantId = TenantA
                };
                var created = await userManager.CreateAsync(admin, AdminPassword);
                created.Succeeded.Should().BeTrue(
                    $"the admin test user must be created: {string.Join(", ", created.Errors.Select(e => e.Description))}");
            }

            var adminRoles = await userManager.GetRolesAsync(admin);
            if (!adminRoles.Contains("Administrator"))
                await userManager.AddToRoleAsync(admin, "Administrator");
        }

        /// <summary>
        /// Seeds the tenant the HTTP clients authenticate as: a lane with three
        /// houses covering every occupancy state, a lane with no houses at all,
        /// and one active student plus one active lecturer assignment.
        /// </summary>
        private async Task SeedTenantAAsync()
        {
            var tag = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

            using var db = CreateTenantDb(TenantA);

            var semester = new Semester
            {
                Name = $"ACC-S1-{tag}",
                SemesterNumber = 1,
                StartDate = Date(2026, 1, 1),
                EndDate = Date(2026, 12, 31),
                IsActive = true,
                IsCurrent = true
            };
            db.Semesters.Add(semester);

            var lane = new Lane
            {
                LaneName = $"A-OCC-{tag}",
                Description = "Tenant A lane with houses in every occupancy state",
                IsActive = true
            };
            db.Lanes.Add(lane);
            await db.SaveChangesAsync();

            OccupiedLaneId = lane.Id;
            OccupiedLaneName = lane.LaneName;

            var occupied = new House
            {
                LaneId = lane.Id,
                HouseNumber = $"A-{tag}-1",
                HouseName = "Tenant A occupied house",
                HouseNumberNumeric = 1,
                Capacity = 2,
                OccupiedCount = 1,
                Status = HouseStatus.Occupied,
                IsOccupied = true
            };
            var vacant = new House
            {
                LaneId = lane.Id,
                HouseNumber = $"A-{tag}-2",
                HouseName = "Tenant A vacant house",
                HouseNumberNumeric = 2,
                Capacity = 3,
                OccupiedCount = 0,
                Status = HouseStatus.Vacant
            };
            var maintenance = new House
            {
                LaneId = lane.Id,
                HouseNumber = $"A-{tag}-3",
                HouseName = "Tenant A maintenance house",
                HouseNumberNumeric = 3,
                Capacity = 1,
                OccupiedCount = 0,
                Status = HouseStatus.Maintenance,
                IsEnabled = false,
                IsAvailable = false
            };
            db.Houses.AddRange(occupied, vacant, maintenance);

            // Positive control for the "available houses" test: a house that
            // satisfies the availability predicate AND belongs to the calling
            // tenant, so its presence proves the endpoint is not simply empty.
            var freeHouse = new House
            {
                LaneId = lane.Id,
                HouseNumber = $"A-{tag}-5",
                HouseName = "Tenant A free house",
                HouseNumberNumeric = 5,
                Capacity = 2,
                OccupiedCount = 0,
                Status = HouseStatus.Vacant
            };
            db.Houses.Add(freeHouse);

            var emptyLane = new Lane
            {
                LaneName = $"A-EMPTY-{tag}",
                Description = "Tenant A lane that owns no houses",
                IsActive = true
            };
            db.Lanes.Add(emptyLane);
            await db.SaveChangesAsync();

            EmptyLaneId = emptyLane.Id;
            OccupiedHouseId = occupied.Id;
            VacantHouseId = vacant.Id;
            MaintenanceHouseId = maintenance.Id;
            OwnFreeHouseNumber = freeHouse.HouseNumber;

            var student = new Student
            {
                StudentNumber = $"ASTU{tag}",
                FirstName = "Amina",
                MiddleName = "Bakari",
                LastName = "Samatta",
                Email = $"astu{tag}@tenant-a.test",
                PhoneNumber = "0711111111",
                Gender = "Female",
                DateOfBirth = Date(2005, 5, 5),
                EnrollmentDate = Date(2026, 1, 5),
                AcademicStatus = "Active",
                IsActive = true,
                IsEnrolled = true
            };
            db.Students.Add(student);

            var lecturer = new Lecturer
            {
                EmployeeNumber = $"AEMP{tag}",
                FirstName = "Baraka",
                MiddleName = "C",
                LastName = "Mushi",
                Title = "Dr.",
                Email = $"aemp{tag}@tenant-a.test",
                PhoneNumber = "0722222222",
                HireDate = Date(2020, 2, 2),
                IsActive = true
            };
            db.Lecturers.Add(lecturer);
            await db.SaveChangesAsync();

            StudentId = student.Id;
            StudentNumber = student.StudentNumber;
            LecturerId = lecturer.Id;
            EmployeeNumber = lecturer.EmployeeNumber;

            db.AccommodationAssignments.Add(new AccommodationAssignment
            {
                StudentId = student.Id,
                OccupantType = OccupantType.Student,
                HouseId = occupied.Id,
                LaneId = lane.Id,
                SemesterId = semester.Id,
                Status = "Active",
                Remarks = "Seeded by AccommodationApiFixture"
            });

            // The lecturer shares the occupied lane but sits in a different house,
            // so the "lane with no houses" lane stays genuinely house-free.
            db.AccommodationAssignments.Add(new AccommodationAssignment
            {
                LecturerId = lecturer.Id,
                OccupantType = OccupantType.Lecturer,
                HouseId = vacant.Id,
                LaneId = lane.Id,
                SemesterId = semester.Id,
                Status = "Active",
                Remarks = "Seeded by AccommodationApiFixture"
            });
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds an unrelated tenant with the same shape of data. None of these
        /// rows may ever appear in a Tenant A response.
        /// </summary>
        private async Task SeedTenantBAsync()
        {
            var tag = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

            using var db = CreateTenantDb(TenantB);

            var semester = new Semester
            {
                Name = $"ACC-B1-{tag}",
                SemesterNumber = 1,
                StartDate = Date(2026, 1, 1),
                EndDate = Date(2026, 12, 31),
                IsActive = true,
                IsCurrent = true
            };
            db.Semesters.Add(semester);

            var lane = new Lane
            {
                LaneName = $"B-OCC-{tag}",
                Description = "Tenant B lane - must never be visible to Tenant A",
                IsActive = true
            };
            db.Lanes.Add(lane);
            await db.SaveChangesAsync();

            var house = new House
            {
                LaneId = lane.Id,
                HouseNumber = $"B-{tag}-1",
                HouseName = "Tenant B occupied house",
                HouseNumberNumeric = 1,
                Capacity = 2,
                OccupiedCount = 2,
                Status = HouseStatus.Occupied,
                IsOccupied = true
            };
            db.Houses.Add(house);

            // A free Tenant B house. Seeding this makes the "available houses"
            // isolation test meaningful: the house genuinely satisfies the
            // availability predicate, so it can only be excluded by tenant scope.
            var foreignFreeHouse = new House
            {
                LaneId = lane.Id,
                HouseNumber = $"B-{tag}-2",
                HouseName = "Tenant B free house",
                HouseNumberNumeric = 2,
                Capacity = 2,
                OccupiedCount = 0,
                Status = HouseStatus.Vacant
            };
            db.Houses.Add(foreignFreeHouse);

            var student = new Student
            {
                StudentNumber = $"BSTU{tag}",
                FirstName = "Fatuma",
                MiddleName = "D",
                LastName = "Nuru",
                Email = $"bstu{tag}@tenant-b.test",
                PhoneNumber = "0733333333",
                Gender = "Female",
                DateOfBirth = Date(2005, 6, 6),
                EnrollmentDate = Date(2026, 1, 5),
                AcademicStatus = "Active",
                IsActive = true,
                IsEnrolled = true
            };
            db.Students.Add(student);

            var lecturer = new Lecturer
            {
                EmployeeNumber = $"BEMP{tag}",
                FirstName = "Grace",
                MiddleName = "E",
                LastName = "Amani",
                Email = $"bemp{tag}@tenant-b.test",
                PhoneNumber = "0744444444",
                HireDate = Date(2019, 3, 3),
                IsActive = true
            };
            db.Lecturers.Add(lecturer);
            await db.SaveChangesAsync();

            db.AccommodationAssignments.Add(new AccommodationAssignment
            {
                StudentId = student.Id,
                OccupantType = OccupantType.Student,
                HouseId = house.Id,
                LaneId = lane.Id,
                SemesterId = semester.Id,
                Status = "Active",
                Remarks = "Seeded by AccommodationApiFixture"
            });

            db.AccommodationAssignments.Add(new AccommodationAssignment
            {
                LecturerId = lecturer.Id,
                OccupantType = OccupantType.Lecturer,
                HouseId = house.Id,
                LaneId = lane.Id,
                SemesterId = semester.Id,
                Status = "Active",
                Remarks = "Seeded by AccommodationApiFixture"
            });

            await db.SaveChangesAsync();

            ForeignLaneId = lane.Id;
            ForeignLaneName = lane.LaneName;
            ForeignStudentNumber = student.StudentNumber;
            ForeignEmployeeNumber = lecturer.EmployeeNumber;
            ForeignHouseNumber = foreignFreeHouse.HouseNumber;
        }
    }
}

