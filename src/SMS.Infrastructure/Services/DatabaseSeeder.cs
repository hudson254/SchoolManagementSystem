using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Infrastructure.MultiTenancy;
using SMS.Multitenancy.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Persistence.Services
{
    /// <summary>
    /// Handles database seeding including tenant initialization, role creation, and administrator user creation.
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ITenantContext _tenantContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ITenantContext tenantContext,
            IConfiguration configuration,
            ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantContext = tenantContext;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Seeds the database with initial data including default tenant, roles, and administrator user.
        /// </summary>
        public async Task SeedAsync()
        {
            _logger.LogInformation("Starting database seeding...");

            try
            {
                // Ensure database is created and migrated
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Database migrations verified.");

                // Seed roles
                await SeedRolesAsync();

                // Seed default tenant
                var tenant = await SeedDefaultTenantAsync();

                // Seed administrator user
                await SeedAdministratorAsync(tenant);

                _logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database seeding");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            _logger.LogInformation("Seeding roles...");

            var roles = new[]
            {
                "SYSTEM ADMINISTRATOR",
                "Administrator",
                "COORDINATOR",
                "Lecturer",
                "Student",
                "Receptionist"
            };

            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var role = new Role
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = roleName,
                        NormalizedName = roleName.ToUpper(),
                        Description = GetRoleDescription(roleName)
                    };

                    var result = await _roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Created role: {RoleName}", roleName);
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to create role '{roleName}': {errors}");
                    }
                }
                else
                {
                    _logger.LogInformation("Role already exists: {RoleName}", roleName);
                }
            }
        }

        private async Task<Tenant> SeedDefaultTenantAsync()
        {
            _logger.LogInformation("Seeding default tenant...");

            var defaultTenantIdString = _configuration["Tenant:DefaultTenantId"] ?? "default-tenant";

            // Parse tenant ID to Guid
            if (!Guid.TryParse(defaultTenantIdString, out var defaultTenantId))
            {
                // If it's not a valid Guid, create one from the string
                defaultTenantId = Guid.NewGuid();
            }

            // Check if tenant exists (bypass tenant filter for this check)
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == defaultTenantId);

            if (tenant == null)
            {
                tenant = new Tenant
                {
                    Id = defaultTenantId,
                    Name = "Default Tenant",
                    Organization = "Default Organization",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created default tenant: {TenantId}", defaultTenantId);
            }
            else
            {
                _logger.LogInformation("Default tenant already exists: {TenantId}", defaultTenantId);
            }

            return tenant;
        }

        private async Task SeedAdministratorAsync(Tenant tenant)
        {
            _logger.LogInformation("Seeding administrator user...");

            // Read administrator credentials from environment variables
            var adminEmail = _configuration["ADMIN_EMAIL"] ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL");
            var adminPassword = _configuration["ADMIN_PASSWORD"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            var adminFirstName = _configuration["ADMIN_FIRST_NAME"] ?? Environment.GetEnvironmentVariable("ADMIN_FIRST_NAME");
            var adminLastName = _configuration["ADMIN_LAST_NAME"] ?? Environment.GetEnvironmentVariable("ADMIN_LAST_NAME");

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                throw new InvalidOperationException(
                    "Administrator email not configured. Set ADMIN_EMAIL environment variable or configure in appsettings.");
            }

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "Administrator password not configured. Set ADMIN_PASSWORD environment variable or configure in appsettings.");
            }

            if (string.IsNullOrWhiteSpace(adminFirstName))
            {
                adminFirstName = "System";
            }

            if (string.IsNullOrWhiteSpace(adminLastName))
            {
                adminLastName = "Administrator";
            }

            // Check if administrator already exists (bypass tenant filter)
            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin != null)
            {
                _logger.LogInformation("Administrator user already exists: {Email}", adminEmail);

                // Ensure administrator has the correct role
                if (!await _userManager.IsInRoleAsync(existingAdmin, "Administrator"))
                {
                    await _userManager.AddToRoleAsync(existingAdmin, "Administrator");
                    _logger.LogInformation("Added Administrator role to existing user: {Email}", adminEmail);
                }

                return;
            }

            // Get tenant ID for administrator
            var tenantId = _tenantContext.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                // If tenant context is not set (e.g., CLI mode), use the default tenant
                tenantId = _configuration["Tenant:DefaultTenantId"] ?? "default-tenant";
            }

            if (!Guid.TryParse(tenantId, out var tenantGuid))
            {
                throw new InvalidOperationException($"Invalid tenant ID: {tenantId}");
            }

            // Create administrator user with a proper username
            var adminUsername = GenerateUsername(adminFirstName, adminLastName);
            var adminUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = adminUsername,
                Email = adminEmail,
                FirstName = adminFirstName,
                LastName = adminLastName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantGuid
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create administrator user: {errors}");
            }

            // Assign SYSTEM ADMINISTRATOR role (highest privilege)
            result = await _userManager.AddToRoleAsync(adminUser, "SYSTEM ADMINISTRATOR");
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign Administrator role: {errors}");
            }

            _logger.LogInformation("Created administrator user: {Email}", adminEmail);
        }

        /// <summary>
        /// Generates a system username from first and last name.
        /// Uses first initial + last name pattern, sanitized and lowercase.
        /// </summary>
        private static string GenerateUsername(string firstName, string lastName)
        {
            var first = (firstName ?? "").Trim().ToLowerInvariant();
            var last = (lastName ?? "").Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
                return $"user{Guid.NewGuid():N}"[..8];

            if (string.IsNullOrEmpty(first))
                return last.Length > 10 ? last[..10] : last;

            if (string.IsNullOrEmpty(last))
                return first.Length > 10 ? first[..10] : first;

            // First initial + last name: JSmith
            var username = $"{first[0]}{last}";

            // Remove any special characters, keep only letters and numbers
            var sanitized = new string(username.Where(c => char.IsLetterOrDigit(c)).ToArray());

            // Ensure reasonable length
            if (sanitized.Length > 50)
                sanitized = sanitized[..50];

            return sanitized;
        }

        private string GetRoleDescription(string roleName)
        {
            return roleName switch
            {
                "SYSTEM ADMINISTRATOR" => "Super administrator with unrestricted system access",
                "Administrator" => "Full system access with all permissions",
                "COORDINATOR" => "Elevated access for content and user management",
                "Lecturer" => "Teaching staff with course and grade management",
                "Student" => "Student access for learning and enrollment",
                "Receptionist" => "Front desk access for registration and inquiries",
                _ => "System role"
            };
        }
    }
}
