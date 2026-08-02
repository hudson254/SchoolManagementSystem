using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SMS.Persistence.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgreSqlContainer;
        private ApplicationDbContext? _context;

        public DatabaseFixture()
        {
            _postgreSqlContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass123")
                .WithCleanUp(true)
                .Build();
        }

        public ApplicationDbContext CreateContext()
        {
            if (_context != null)
                return _context;

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_postgreSqlContainer.GetConnectionString())
                .Options;

            _context = new ApplicationDbContext(
                options,
                Mock.Of<ITenantResolver>(),
                Mock.Of<IAuditService>());

            return _context;
        }

        public async Task InitializeAsync()
        {
            await _postgreSqlContainer.StartAsync();
            
            var context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
            {
                await _context.DisposeAsync();
            }
            await _postgreSqlContainer.DisposeAsync();
        }

        public async Task ResetDatabaseAsync()
        {
            var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
        }
    }
}