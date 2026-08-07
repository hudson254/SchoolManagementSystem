using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SMS.Domain.Interfaces;
using System.Collections.Generic;

namespace SMS.Persistence.Data
{
    /// <summary>
    /// Design-time factory for EF Core migrations. Used when running `dotnet ef migrations add`.
    /// Provides mock services for design-time tools.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();

            builder.UseNpgsql(
                "Host=localhost;Port=5433;Database=SchoolManagementSystem;Username=sms_user;Password=SecurePassword123!",
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(3);
                    npgsqlOptions.CommandTimeout(60);
                });

            return new ApplicationDbContext(
                builder.Options,
                new DesignTimeCurrentUserService(),
                new DesignTimeTenantContext());
        }
    }

    /// <summary>
    /// Mock ICurrentUserService for design-time EF Core tools
    /// </summary>
    public class DesignTimeCurrentUserService : ICurrentUserService
    {
        public string UserId => string.Empty;
        public string Username => string.Empty;
        public string Email => string.Empty;
        public bool IsAuthenticated => false;
        public IEnumerable<string> Roles => new List<string>();
    }

    /// <summary>
    /// Mock ITenantContext for design-time EF Core tools
    /// </summary>
    public class DesignTimeTenantContext : ITenantContext
    {
        public string TenantId => "00000000-0000-0000-0000-000000000000";
        public string TenantName => "DesignTime";
        public string ConnectionString => string.Empty;
    }
}
