using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
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

namespace SMS.ApiTests
{
    public class ApiTestFixture : WebApplicationFactory<Program>
    {
        // xUnit runs test classes in parallel by default, each with its own
        // fixture instance. EF Core InMemory creates a SEPARATE store per DI
        // container (unless an explicit shared InMemoryDatabaseRoot is provided).
        // Parallel fixtures therefore cannot share a static seeded state.
        // Test assembly parallelization is disabled (TestAssemblyConfig.cs) so
        // each fixture instance builds & seeds its OWN store sequentially. Seed
        // state is therefore per-instance (not static).
        private readonly SemaphoreSlim InitLock = new(1, 1);
        private bool _initialized;
        private string? _cachedAdminToken;
        private bool _disposed;

        private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private const string AdminEmail = "admin@school.com";
        private const string AdminPassword = "Admin123!";

        public ApiTestFixture()
        {
            // Constructor must NOT access Services (server not built yet).
            // Seed data is initialized lazily on first use.
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Program.cs skips AddDbContext for "Testing" environment (line ~84).
                // We register InMemory here so Identity stores (already registered by Program.cs's AddIdentity)
                // resolve to the InMemory context.
                RemoveServiceDescriptors<ApplicationDbContext>(services);
                RemoveServiceDescriptors(typeof(DbContextOptions<ApplicationDbContext>), services);
                RemoveServiceDescriptors(typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<ApplicationDbContext>), services);

                // Use a FIXED database name AND a SHARED InMemoryDatabaseRoot so that
                // all scopes (seed scope, request pipeline scopes) AND all fixture
                // instances (each with its own WebApplicationFactory host / DI container)
                // point to the SAME physical InMemory store. Without the shared root,
                // each host gets a fresh empty database -> authenticated/DB-dependent
                // tests fail with empty responses.
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("ApiTestDb");
                }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);

                // NOTE: Do NOT call services.AddIdentity<User,Role>(...) here.
                // Program.cs already registers Identity with AddIdentity + AddEntityFrameworkStores<ApplicationDbContext>().
                // Calling AddIdentity again would add duplicate auth schemes (Identity.Application, etc.)
                // and throw "Scheme already exists: Identity.Application" at host startup.
                // The EF stores registered by Program.cs will resolve to the InMemory ApplicationDbContext above.

                // Mock ICurrentUserService — the controller resolves the
                // APPLICATION interface (SMS.Application.Common.Interfaces),
                // NOT the Domain one. Register the mock for the Application
                // interface so the StudentController ownership check uses it.
                var appCurrentUserServiceType = typeof(SMS.Application.Common.Interfaces.ICurrentUserService);
                RemoveServiceDescriptors(appCurrentUserServiceType, services);
                var mockCurrentUser = new Mock<SMS.Application.Common.Interfaces.ICurrentUserService>();
                mockCurrentUser.Setup(x => x.UserId).Returns("test-user-id");
                mockCurrentUser.Setup(x => x.Email).Returns("test@test.com");
                mockCurrentUser.Setup(x => x.Username).Returns("testuser");
                mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
                mockCurrentUser.Setup(x => x.Roles).Returns(new[] { "Administrator" });
                services.AddScoped(_ => mockCurrentUser.Object);

                // Mock Domain ITenantContext (fully qualify to avoid ambiguity)
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

        private static void RemoveServiceDescriptors<TService>(IServiceCollection services)
        {
            var descriptors = services.Where(d => d.ServiceType == typeof(TService)).ToList();
            foreach (var d in descriptors)
                services.Remove(d);
        }

        private static void RemoveServiceDescriptors(Type serviceType, IServiceCollection services)
        {
            var descriptors = services.Where(d => d.ServiceType == serviceType).ToList();
            foreach (var d in descriptors)
                services.Remove(d);
        }

        /// <summary>
        /// Ensures the database is seeded with roles and admin user.
        /// Called lazily after the server is built (Services is available).
        /// </summary>
        private async Task EnsureSeedDataAsync()
        {
            if (_initialized || _disposed) return;

            await InitLock.WaitAsync();
            try
            {
                if (_initialized || _disposed) return;

                using (var scope = Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await dbContext.Database.EnsureCreatedAsync();

                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

                    // Create roles
                    foreach (var roleName in new[] { "Administrator", "Lecturer", "Student", "Moderator" })
                    {
                        if (!await roleManager.RoleExistsAsync(roleName))
                        {
                            var createRoleResult = await roleManager.CreateAsync(new Role
                            {
                                Name = roleName,
                                NormalizedName = roleName.ToUpperInvariant(),
                                IsActive = true
                            });

                            if (!createRoleResult.Succeeded)
                            {
                                throw new InvalidOperationException(
                                    $"Failed creating role '{roleName}': {string.Join(", ", createRoleResult.Errors.Select(e => e.Description))}");
                            }
                        }
                    }

                    // Create/find admin user (seed via Identity pipeline)
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

                        var createResult = await userManager.CreateAsync(adminUser, AdminPassword);
                        if (!createResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                $"Failed creating admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        adminUser.TenantId = DefaultTenantId;
                        await userManager.UpdateAsync(adminUser);
                    }

                    // Ensure admin role membership
                    if (!await userManager.IsInRoleAsync(adminUser, "Administrator"))
                    {
                        var addRoleResult = await userManager.AddToRoleAsync(adminUser, "Administrator");
                        if (!addRoleResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                $"Failed adding admin role: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
                        }
                    }
                }

                _initialized = true;
            }
            finally
            {
                InitLock.Release();
            }
        }

        /// <summary>
        /// Hides WebApplicationFactory.CreateClient() so that the InMemory database is
        /// always seeded (roles + admin user) before any test request is issued.
        /// WebApplicationFactory.CreateClient() is not virtual, so we use method hiding (`new`).
        /// All test fields are typed as ApiTestFixture, so this method is resolved at call sites.
        /// CreateClientWithTenant() calls base.CreateClient() (no recursion).
        /// </summary>
        public new HttpClient CreateClient()
        {
            return CreateClientWithTenant(null);
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }

        private HttpClient CreateClientWithTenant(string? bearerToken)
        {
            // Create the client first to ensure the server is built (Services becomes available)
            var client = base.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            }

            // Seed data lazily after server is built
            EnsureSeedDataAsync().GetAwaiter().GetResult();

            return client;
        }

        /// <summary>
        /// RISK-08: login/register/refresh no longer return tokens in the JSON
        /// body — they are set as httpOnly Set-Cookie headers. Extract the
        /// access_token cookie value from the Set-Cookie response header so
        /// tests keep authenticating via the existing Bearer-token interface.
        /// </summary>
        private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                foreach (var header in values)
                {
                    // Header shape: access_token=eyJ...; path=/; HttpOnly; SameSite=Lax
                    var firstPart = header.Split(';')[0].Trim();
                    var eq = firstPart.IndexOf('=');
                    if (eq > 0 && firstPart.Substring(0, eq).Trim() == cookieName)
                        return firstPart.Substring(eq + 1).Trim();
                }
            }
            return string.Empty;
        }

        private async Task<string> EnsureAdminTokenAsync()
        {
            if (!string.IsNullOrWhiteSpace(_cachedAdminToken))
                return _cachedAdminToken;

            // CreateClientWithTenant builds the server first (via base.CreateClient()),
            // then seeds the database. Do NOT call EnsureSeedDataAsync() before server is built.
            // The client is disposed after we extract the token (the token string is what we cache).
            var client = CreateClientWithTenant(null);
            try
            {
                var loginRequest = new { email = AdminEmail, password = AdminPassword, rememberMe = true };
                var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
                response.EnsureSuccessStatusCode();

                // Tokens now arrive via Set-Cookie (httpOnly cookies) — no body.
                _cachedAdminToken = ExtractCookieValue(response, "access_token");
            }
            finally
            {
                client.Dispose();
            }
            return _cachedAdminToken;
        }

        public async Task<string> GetAuthTokenAsync()
        {
            return await EnsureAdminTokenAsync();
        }

        public async Task<string> GetAuthTokenAsync(string email, string password)
        {
            // CreateClientWithTenant builds the server first, then seeds the database.
            // Do NOT call EnsureSeedDataAsync() before CreateClient().
            var client = CreateClientWithTenant(null);
            try
            {
                var loginRequest = new { email, password, rememberMe = true };
                var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
                response.EnsureSuccessStatusCode();

                return ExtractCookieValue(response, "access_token");
            }
            finally
            {
                client.Dispose();
            }
        }

        public async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            var token = await EnsureAdminTokenAsync();
            return CreateClientWithTenant(token);
        }

        public HttpClient CreateAuthenticatedClient()
        {
            var token = Task.Run(EnsureAdminTokenAsync).GetAwaiter().GetResult();
            return CreateClientWithTenant(token);
        }
    }
}
