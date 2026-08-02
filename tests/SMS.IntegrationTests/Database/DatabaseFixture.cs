using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private ApplicationDbContext? _context;
        private readonly bool _useInMemory;

        public DatabaseFixture()
        {
            // Default to InMemory unless Docker is confirmed available
            // On Windows, Docker Desktop may not be running in CI/dev environments
            // Testcontainers requires Docker to be running; fall back to InMemory otherwise
            var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
            var dockerSocket = Environment.GetEnvironmentVariable("TESTCONTAINERS_DOCKER_SOCKET");
            var isDockerEnv = !string.IsNullOrEmpty(dockerHost)
                || !string.IsNullOrEmpty(dockerSocket)
                || System.IO.File.Exists("/.dockerenv")
                || System.IO.File.Exists("/var/run/docker.sock");

            _useInMemory = !isDockerEnv;
        }

        public ApplicationDbContext CreateContext()
        {
            if (_context != null)
                return _context;

            var mockCurrentUserService = new Mock<ICurrentUserService>();
            mockCurrentUserService.Setup(x => x.UserId).Returns("test-user-id");
            mockCurrentUserService.Setup(x => x.Email).Returns("test@test.com");
            mockCurrentUserService.Setup(x => x.Username).Returns("testuser");
            mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);

            // Use fully qualified type to avoid ambiguity with SMS.Multitenancy.Interfaces.ITenantContext
            var mockTenantContext = new Mock<SMS.Domain.Interfaces.ITenantContext>();
            mockTenantContext.Setup(x => x.TenantId).Returns("11111111-1111-1111-1111-111111111111");

            if (_useInMemory)
            {
                var inMemoryOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}")
                    .Options;

                _context = new ApplicationDbContext(
                    inMemoryOptions,
                    mockCurrentUserService.Object,
                    mockTenantContext.Object);
            }
            else
            {
                var npgsqlOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseNpgsql("Host=localhost;Database=testdb;Username=testuser;Password=testpass123")
                    .Options;

                _context = new ApplicationDbContext(
                    npgsqlOptions,
                    mockCurrentUserService.Object,
                    mockTenantContext.Object);
            }

            return _context;
        }

        public async Task InitializeAsync()
        {
            var context = CreateContext();
            await context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
            {
                try
                {
                    await _context.Database.EnsureDeletedAsync();
                }
                finally
                {
                    await _context.DisposeAsync();
                }
            }
        }

        public async Task ResetDatabaseAsync()
        {
            var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }
    }
}

