using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SMS.Application;
using SMS.Domain.Interfaces;
using SMS.Domain.Entities;
using SMS.Multitenancy.Interfaces;
using SMS.Infrastructure.MultiTenancy;
using SMS.Identity.Models;
using SMS.Identity.Services;
using SMS.Infrastructure.Options;
using SMS.Infrastructure.Services;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using SMS.API.Logging;
using SMS.API.Middleware;
using SMS.API.Options;
using SMS.Notifications;
using SMS.Notifications.Hubs;
using SMS.Reporting;
using SMS.Certificates;
using SMS.Certificates.Domain.Interfaces;

// CLI Command implementations (must be defined before they are called)
/// <summary>
/// Migrates the database to the latest migration.
/// Usage: dotnet run --project src/SMS.API -- migrate-database
/// </summary>
static async Task RunMigrateDatabaseAsync(string[] args)
{
    Console.WriteLine("Starting database migration...");

    try
    {
        // Build a minimal service provider for database operations
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog();

        // Load configuration
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();

        // Get connection string
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("ERROR: DefaultConnection connection string not configured.");
            Environment.Exit(1);
            return;
        }

        // Register stub services required by ApplicationDbContext constructor
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService>(_ => new StubCurrentUserService());
        builder.Services.AddScoped<SMS.Domain.Interfaces.ITenantContext>(_ => new StubTenantContext());

        // Ensure DbContext is registered
        builder.Services.AddDbContext<SMS.Persistence.Data.ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3);
                npgsqlOptions.CommandTimeout(60);
            }));

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SMS.Persistence.Data.ApplicationDbContext>();

            Console.WriteLine($"Connecting to database: {connectionString.Split(';')[0]}...");

            // Apply migrations
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                Console.WriteLine($"Applying {pendingMigrations.Count()} pending migration(s)...");
                await dbContext.Database.MigrateAsync();
                Console.WriteLine("Database migrations applied successfully.");
            }
            else
            {
                Console.WriteLine("Database is already up to date. No migrations to apply.");
            }

            Console.WriteLine("Migration completed successfully!");
            Environment.Exit(0);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: Migration failed: {ex.Message}");
        Console.WriteLine($"Details: {ex.InnerException?.Message ?? ex.StackTrace}");
        Environment.Exit(1);
    }
}

/// <summary>
/// Seeds the database with initial data (tenant, roles, administrator).
/// Usage: dotnet run --project src/SMS.API -- seed-data
/// </summary>
static async Task RunSeedDataAsync(string[] args)
{
    Console.WriteLine("Starting database seeding...");

    try
    {
        // Build a minimal service provider for database operations
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog();

        // Load configuration
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();

        // Get connection string
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("ERROR: DefaultConnection connection string not configured.");
            Environment.Exit(1);
            return;
        }

        // Ensure DbContext is registered
        builder.Services.AddDbContext<SMS.Persistence.Data.ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3);
                npgsqlOptions.CommandTimeout(60);
            }));

        // Add Identity services
        builder.Services.AddIdentity<SMS.Domain.Entities.User, SMS.Domain.Entities.Role>()
            .AddEntityFrameworkStores<SMS.Persistence.Data.ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Register stub services required by ApplicationDbContext constructor
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService>(_ => new StubCurrentUserService());
        builder.Services.AddScoped<SMS.Domain.Interfaces.ITenantContext>(_ => new StubTenantContext());

        // Add tenant context
        builder.Services.AddScoped<SMS.Multitenancy.Interfaces.ITenantContext, SMS.Infrastructure.MultiTenancy.TenantContext>();
        builder.Services.AddScoped<SMS.Infrastructure.MultiTenancy.TenantContext>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SMS.Persistence.Data.ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<SMS.Domain.Entities.User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<SMS.Domain.Entities.Role>>();
            var tenantContext = scope.ServiceProvider.GetRequiredService<SMS.Multitenancy.Interfaces.ITenantContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var seederLogger = scope.ServiceProvider.GetRequiredService<ILogger<SMS.Persistence.Services.DatabaseSeeder>>();

            Console.WriteLine($"Connecting to database: {connectionString.Split(';')[0]}...");

            // Ensure database exists and is migrated
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("Database migrations verified.");

            // Run seeding
            var seeder = new SMS.Persistence.Services.DatabaseSeeder(
                dbContext,
                userManager,
                roleManager,
                tenantContext,
                configuration,
                seederLogger);

            await seeder.SeedAsync();

            Console.WriteLine("Database seeding completed successfully!");
            Environment.Exit(0);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: Seeding failed: {ex.Message}");
        Console.WriteLine($"Details: {ex.InnerException?.Message ?? ex.StackTrace}");
        Environment.Exit(1);
    }
}

// CLI Commands for database operations (must be at top level before WebApplication builder)
if (args.Length > 0)
{
    var command = args[0].ToLowerInvariant();

    if (command == "migrate-database")
    {
        await RunMigrateDatabaseAsync(args);
        return;
    }
    else if (command == "seed-data")
    {
        await RunSeedDataAsync(args);
        return;
    }
}

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with structured JSON logging for the enterprise
// centralized logging pipeline. All components log through this pipeline.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SchoolManagementSystem")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/sms-.json",
        rollingInterval: RollingInterval.Day,
        formatter: new Serilog.Formatting.Json.JsonFormatter())
    .WriteTo.File(
        path: "logs/sms-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
// Suppress the implicit [Required] that ASP.NET Core infers for non-nullable
// reference type properties (e.g. CreateStudentCommand.Password). With NRT
// enabled, [ApiController] would otherwise reject any request that omits such
// a property with a 400 before FluentValidation (where these fields are
// optional) can run. Validation is deliberately delegated to FluentValidation.
builder.Services.AddControllers(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
.AddJsonOptions(options =>
{
    // Allow enum values to be sent as strings (e.g. "Draft" instead of 0)
    // in JSON request/response bodies. Without this, clients must send
    // numeric enum values which is error-prone and less readable.
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

// Configure RateLimiting options (bound from the "RateLimiting" config section)
builder.Services.Configure<RateLimitingOptions>(
    builder.Configuration.GetSection("RateLimiting"));

// Configure session support (required for audit session tracking)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure CORS with explicit allowed origins from configuration.
// In development, falls back to http://localhost:5173 (Vite default).
// In production, must be explicitly set via Cors:AllowedOrigins or Frontend:Url.
// Wildcard origins are NOT permitted when AllowCredentials() is used.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins == null || corsOrigins.Length == 0)
{
    var frontendUrl = builder.Configuration.GetValue<string>("Frontend:Url");
    if (!string.IsNullOrWhiteSpace(frontendUrl))
    {
        corsOrigins = new[] { frontendUrl };
    }
    else
    {
        corsOrigins = builder.Environment.IsDevelopment()
            ? new[] { "http://localhost:5173", "http://localhost:3000" }
            : Array.Empty<string>();
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // Production fallback: deny all cross-origin requests
            // This ensures CORS is restrictive by default
            policy.WithOrigins()
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure API Versioning.
// RISK-15: Microsoft.AspNetCore.Mvc.Versioning (the "Microsoft.*" package) is
// deprecated. Versioning is now provided by the Asp.Versioning family
// (Asp.Versioning.Mvc.ApiExplorer), which uses the Asp.Versioning namespace
// and adds the ApiExplorer plumbing required for Swagger to discover versioned
// endpoints (AddApiVersioning().AddApiExplorer()).
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    // Group endpoints by API version so Swagger can render one document per
    // version (e.g. v1). Without this, versioned endpoints are not exposed
    // through the IApiDescriptionProvider pipeline.
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Configure Health Checks with database connectivity check
// Uses a custom health check that verifies PostgreSQL connectivity
// without requiring the AspNetCore.Diagnostics.HealthChecks.EntityFrameworkCore package.
builder.Services.AddHealthChecks()
    .AddCheck<SMS.API.HealthChecks.DatabaseHealthCheck>(
        "postgresql",
        tags: new[] { "database", "postgresql" });

// Configure PostgreSQL with scoped DbContext (required because ApplicationDbContext depends on scoped services)
// The Testing environment also uses PostgreSQL (via the Docker test database) so that
// API tests exercise the same database provider as production. This eliminates the
// InMemory-vs-PostgreSQL behavioral differences that caused test failures.
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
            npgsqlOptions.CommandTimeout(60);
        }),
    contextLifetime: ServiceLifetime.Scoped,
    optionsLifetime: ServiceLifetime.Scoped);

// Configure Identity
builder.Services.AddIdentity<User, Role>(options =>
{
    // Password policy tightened to match the server-side PasswordPolicyService.
    // Registration is additionally validated by RegisterCommandValidator and
    // IPasswordPolicyService (weak/blacklist/entropy checks). These Identity
    // options are the final line of defense for programmatic user creation.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;
    options.SignIn.RequireConfirmedEmail = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure ASP.NET Core Data Protection for production key persistence.
// Keys are stored in a Docker volume mounted at /app/dataprotection-keys
// to survive container recreation. The directory is owned by the non-root
// appuser (UID 1001) and is not accessible to other containers.
var dataProtectionKeysDir = Environment.GetEnvironmentVariable("DATAPROTECTION_KEYS_DIR") ?? "/app/dataprotection-keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDir))
    .SetApplicationName("SchoolManagementSystem");

// Configure JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Read JWT secret from environment variable or configuration
var jwtConfig = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtConfig["Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("JWT Secret not configured. Set JWT_SECRET environment variable or configure JwtSettings:Secret in appsettings.");

if (jwtSecret.Length < 64)
    throw new InvalidOperationException($"JWT Secret must be at least 64 characters long (current length: {jwtSecret.Length}). Generate a strong secret using a cryptographically secure random generator.");

// Validate UTF-8 byte length for HMAC-SHA256 key strength
var jwtSecretBytes = Encoding.UTF8.GetBytes(jwtSecret);
if (jwtSecretBytes.Length < 64)
    throw new InvalidOperationException($"JWT Secret must provide at least 512 bits (64 bytes) of entropy when encoded as UTF-8. Current byte length: {jwtSecretBytes.Length}.");

// Single source of truth: push the resolved secret back into configuration so that
// JwtService (via IOptions<JwtSettings>) signs with the SAME key that this pipeline validates with.
// Without this, if JWT_SECRET env var is set, tokens are signed with config secret but
// validated with the env secret -> all authenticated requests fail with 401.
jwtConfig["Secret"] = jwtSecret;

var key = Encoding.UTF8.GetBytes(jwtSecret);

// Validate production configuration requirements
if (builder.Environment.IsProduction())
{
    var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? builder.Configuration["Frontend:Url"];
    if (string.IsNullOrWhiteSpace(frontendUrl))
        throw new InvalidOperationException("FRONTEND_URL is required in production. Set FRONTEND_URL environment variable.");

    var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (configuredCorsOrigins == null || configuredCorsOrigins.Length == 0 || configuredCorsOrigins.All(string.IsNullOrWhiteSpace))
        throw new InvalidOperationException("Cors:AllowedOrigins must be configured in production. At least one origin is required for frontend access.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Disable inbound claim mapping so the JWT's "sub" claim remains "sub"
    // (instead of being remapped to ClaimTypes.NameIdentifier). Controllers
    // read User.FindFirst("sub") directly; without this, authenticated
    // endpoints like /api/v1/auth/me return 401 because the claim is missing.
    options.MapInboundClaims = false;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;

    // RISK-08: tokens are stored in httpOnly cookies (set by AuthController).
    // The JWT is read from the access_token cookie by default; the
    // Authorization header is still honored first for non-browser clients
    // (e.g. API tests, Swagger, scripts).
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(accessToken) &&
                context.Request.Cookies.TryGetValue("access_token", out var cookieToken) &&
                !string.IsNullOrEmpty(cookieToken))
            {
                context.Token = cookieToken;
            }
            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtConfig["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtConfig["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        // JwtService emits roles as the plain "role" claim (see
        // JwtService.GenerateToken). Configure the role claim type so
        // [Authorize(Roles="...")] and policy RequireRole(...) resolve.
        RoleClaimType = "role",

        // The JWT "sub" claim holds the user id. MapInboundClaims=false
        // keeps it as "sub" so controllers read User.FindFirst("sub").
        NameClaimType = "name",

        // Explicitly enforce the expected signing algorithm (HS256).
        // This prevents algorithm confusion attacks (e.g., alg:none,
        // HS256 vs RS256 confusion, or any unexpected algorithm).
        // Without this, the framework accepts the algorithm indicated
        // by the token header, which could be manipulated by an attacker.
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
    };
});

// Register Application Layer
builder.Services.AddApplication();

// Register Infrastructure Options
// NOTE: SMTP/EmailOptions removed — email functionality fully disabled per
// owner requirement. Password resets are now admin-mediated (Phase 2).
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));

// Register services
builder.Services.AddScoped<IUserManagerService, SMS.Infrastructure.Services.UserManagerService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
// Register the username generator used by RegisterCommandHandler and
// CheckUsernameAvailabilityQueryHandler. Without this the API fails to start
// with "Unable to resolve service IUsernameGenerator" (service validation).
builder.Services.AddScoped<SMS.Application.Common.Interfaces.IUsernameGenerator, UsernameGenerator>();
// Register the name parser and title configuration used by
// RegisterCommandHandler/CreateStudentCommandHandler. NameParser depends on
// ITitleConfiguration (which in turn needs TitleOptions); without these the
// Register/Student flows throw "Unable to resolve service INameParser" and
// return HTTP 500.
builder.Services.AddScoped<SMS.Application.Common.Interfaces.INameParser, NameParser>();
builder.Services.AddScoped<SMS.Application.Common.Interfaces.ITitleConfiguration, TitleConfiguration>();
builder.Services.Configure<TitleOptions>(builder.Configuration.GetSection("TitleConfiguration"));
// The Application layer consumes SMS.Application.Common.Interfaces.ICurrentUserService
// (a distinct re-export of the domain interface). Register it to the same concrete
// implementation; previously only the domain variant was registered, which made the
// API fail at startup (service validation) outside the Testing environment.
builder.Services.AddScoped<SMS.Application.Common.Interfaces.ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AuditHelper>();
// Register the centralized error logging pipeline (Phase 5)
builder.Services.AddScoped<IErrorLoggingService, ErrorLoggingService>();
// Register the searchable error repository (Phase 6) - in-memory for single-instance
builder.Services.AddSingleton<SMS.Infrastructure.Services.IErrorRepository, SMS.Infrastructure.Services.InMemoryErrorRepository>();
// IEmailService registration removed — email functionality fully disabled.
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
// NOTE: IPdfGenerator/IExcelGenerator are registered by SMS.Reporting (AddReporting)
// which provides the real QuestPDF/EPPlus implementations. The Infrastructure
// placeholders (PdfGenerator/ExcelGenerator) are removed to avoid duplicate registrations.
builder.Services.AddScoped<SMS.Multitenancy.Interfaces.ITenantResolver, TenantResolver>();
builder.Services.AddScoped<SMS.Infrastructure.MultiTenancy.TenantContext>();
builder.Services.AddScoped<SMS.Domain.Interfaces.ITenantContext>(sp =>
    sp.GetRequiredService<SMS.Infrastructure.MultiTenancy.TenantContext>());
builder.Services.AddScoped<SMS.Multitenancy.Interfaces.ITenantContext>(sp =>
    sp.GetRequiredService<SMS.Infrastructure.MultiTenancy.TenantContext>());
builder.Services.AddScoped<ITenantStore, TenantStore>();
builder.Services.AddHttpContextAccessor();

// Register repositories
builder.Services.AddScoped<IPasswordResetRequestRepository, PasswordResetRequestRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IUnitRepository, UnitRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IGradeRepository, GradeRepository>();
builder.Services.AddScoped<ILecturerRepository, LecturerRepository>();
builder.Services.AddScoped<ITimetableRepository, TimetableRepository>();
builder.Services.AddScoped<IAccommodationRepository, AccommodationRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IReportVerificationRepository, ReportVerificationRepository>();
builder.Services.AddScoped<IUnitAllocationRepository, UnitAllocationRepository>();
builder.Services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();
builder.Services.AddScoped<ICourseOfferingRepository, CourseOfferingRepository>();
builder.Services.AddScoped<ICourseOfferingUnitRepository, CourseOfferingUnitRepository>();
builder.Services.AddScoped<ICourseOfferingEnrollmentRepository, CourseOfferingEnrollmentRepository>();
builder.Services.AddScoped<ICourseOfferingLecturerRepository, CourseOfferingLecturerRepository>();
builder.Services.AddScoped<IAssignmentIssueReportRepository, AssignmentIssueReportRepository>();
builder.Services.AddScoped<IAssessmentRepository, AssessmentRepository>();
builder.Services.AddScoped<IStudentAssessmentMarkRepository, StudentAssessmentMarkRepository>();
builder.Services.AddScoped<IAssessmentTypeRepository, AssessmentTypeRepository>();
builder.Services.AddScoped<IAssessmentTemplateRepository, AssessmentTemplateRepository>();
builder.Services.AddScoped<IGradingScaleRepository, GradingScaleRepository>();
builder.Services.AddScoped<IGradeBandRepository, GradeBandRepository>();
builder.Services.AddScoped<IStudentCertificateEligibilityRepository, StudentCertificateEligibilityRepository>();
builder.Services.AddScoped<IGradeChangeHistoryRepository, GradeChangeHistoryRepository>();
builder.Services.AddScoped<IUnitResultRepository, UnitResultRepository>();
builder.Services.AddScoped<IModerationRecordRepository, ModerationRecordRepository>();
builder.Services.AddScoped<IAssessmentExemptionRepository, AssessmentExemptionRepository>();
builder.Services.AddScoped<ICertificateRuleRepository, CertificateRuleRepository>();

// Register Certificate repositories
builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped<ICertificateTemplateRepository, CertificateTemplateRepository>();
builder.Services.AddScoped<ICertificateAuditLogRepository, CertificateAuditLogRepository>();

// Register UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Report Authentication Services
builder.Services.Configure<ReportAuthenticationOptions>(builder.Configuration.GetSection("ReportVerification"));
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IWatermarkService, WatermarkService>();
builder.Services.AddScoped<IReportTokenService, ReportTokenService>();
builder.Services.AddScoped<IReportHashService, ReportHashService>();
builder.Services.AddScoped<IReportAuthenticationService, ReportAuthenticationService>();

// Register token revocation service.
// In development/testing, uses in-memory deny-list for access tokens.
// In production, configure RedisTokenRevocation:ConnectionString to use
// Redis-backed revocation that survives restarts and works across instances.
builder.Services.AddSingleton<ITokenRevocationService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var redisConnectionString = config.GetValue<string>("RedisTokenRevocation:ConnectionString");

    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        // Use Redis-backed revocation for production
        var redisOptions = sp.GetRequiredService<IOptions<RedisTokenRevocationOptions>>();
        var logger = sp.GetRequiredService<ILogger<RedisTokenRevocationService>>();
        return new RedisTokenRevocationService(redisOptions, logger);
    }

    // Fall back to in-memory for development/testing
    var cache = sp.GetRequiredService<IMemoryCache>();
    var inMemoryLogger = sp.GetRequiredService<ILogger<InMemoryTokenRevocationService>>();
    return new InMemoryTokenRevocationService(cache, inMemoryLogger);
});
// Register Redis token revocation options (bound from configuration)
builder.Services.Configure<RedisTokenRevocationOptions>(
    builder.Configuration.GetSection("RedisTokenRevocation"));

// Register SMS.Notifications and SMS.Reporting (RISK-14)
builder.Services.AddNotifications();
builder.Services.AddReporting();

// Register Certificate Generation and Verification Module
builder.Services.AddCertificateModule();

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdministratorAccess", policy =>
        policy.RequireRole("Administrator"));
    options.AddPolicy("ModeratorAccess", policy =>
        policy.RequireRole("Administrator", "Coordinator"));
    options.AddPolicy("LecturerAccess", policy =>
        policy.RequireRole("Administrator", "Coordinator", "Lecturer"));
    options.AddPolicy("StudentAccess", policy =>
        policy.RequireRole("Administrator", "Coordinator", "Lecturer", "Student"));
    options.AddPolicy("ReceptionistAccess", policy =>
        policy.RequireRole("Administrator", "Coordinator", "Receptionist"));
    options.AddPolicy("SystemAdministratorAccess", policy =>
        policy.RequireRole("SystemAdministrator"));
});

var app = builder.Build();

// Process X-Forwarded-* headers from the nginx reverse proxy.
// The API runs HTTP-only behind nginx which terminates TLS and forwards
// X-Forwarded-Proto/X-Forwarded-For. Without this, the API sees all requests
// as HTTP, which breaks HSTS detection (SecurityHeadersMiddleware) and
// scheme-dependent logic. In Docker, nginx's IP is dynamic (Docker-assigned),
// so we clear the known networks/proxies to trust forwarded headers from any
// source on the internal Docker network. The API port is not directly exposed
// to untrusted networks in production (nginx is the only entry point).
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Configure the HTTP request pipeline.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<LoggingEnrichmentMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
// RISK-10: double-submit cookie CSRF protection. Must run before
// UseAuthentication so state-changing requests are validated first.
app.UseMiddleware<CsrfProtectionMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<MetricsMiddleware>();

// Configure Swagger based on explicit configuration (not just environment)
// Swagger should be disabled by default in production for security
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger__Enabled", false);
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
    Log.Information("Swagger enabled via configuration");
}
else
{
    Log.Information("Swagger disabled (set Swagger__Enabled=true to enable)");
}

// HTTPS redirect is only needed in local development (direct browser access
// to the API with both HTTP+HTTPS endpoints). In Docker/Production the API
// runs HTTP-only behind nginx which terminates TLS and performs the
// HTTP→HTTPS redirect itself. Running UseHttpsRedirection here would redirect
// to an HTTPS endpoint that doesn't exist inside the container, causing a
// redirect loop. Skip in Testing to avoid redirect loops with TestServer.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
app.UseSession();

// RISK-26: Serve uploaded files at /uploads/ so the nginx reverse proxy can
// forward requests to the API. The FileStorage:Path config (default "uploads")
// is resolved relative to the app working directory. In Docker the api_data
// volume is mounted at /app/data and uploads live under /app/uploads.
var fileStoragePath = builder.Configuration.GetValue<string>("FileStorage:Path") ?? "uploads";
if (!Path.IsPathRooted(fileStoragePath))
{
    fileStoragePath = Path.Combine(Directory.GetCurrentDirectory(), fileStoragePath);
}
// Ensure the uploads directory exists before constructing the
// PhysicalFileProvider — otherwise the API fails to start when the
// directory is absent (e.g. in the test bin directory).
Directory.CreateDirectory(fileStoragePath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(fileStoragePath),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true,
    OnPrepareResponse = ctx =>
    {
        // Prevent browsers from sniffing uploaded content as HTML/JS (XSS).
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    }
});

// Apply migrations and seed data.
// The Testing environment also applies migrations because it uses a real
// PostgreSQL database (via Docker) to exercise the same database provider
// as production. This ensures API tests validate the actual schema.
// When using InMemory provider (e.g., some test fixtures), migrations are
// skipped gracefully since InMemory does not support relational operations.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Check if the database provider supports relational operations
        // (e.g., PostgreSQL, SQL Server) before attempting migrations.
        // InMemory provider does not support GetPendingMigrationsAsync.
        if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                Log.Information("Applying {Count} pending migration(s)...", pendingMigrations.Count());
                await dbContext.Database.MigrateAsync();
                Log.Information("Database migrations applied successfully.");
            }
            else
            {
                Log.Information("Database is already up to date. No migrations to apply.");
            }
        }
        else
        {
            Log.Information("InMemory provider detected. Skipping migrations (schema created via EnsureCreated).");
            await dbContext.Database.EnsureCreatedAsync();
        }
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "CRITICAL: Database migration failed. The application cannot start with an incompatible database schema.");
        throw;  // Re-throw to prevent application from running with wrong schema
    }
}

app.UseAuthentication();
app.UseAuthorization();

// Map health checks with detailed response for internal monitoring
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(json);
    }
});

// Map Prometheus metrics endpoint (internal network only)
app.MapGet("/metrics", async context =>
{
    context.Response.ContentType = "text/plain; charset=utf-8";
    var metrics = SMS.API.Middleware.MetricsMiddleware.GenerateMetrics();
    await context.Response.WriteAsync(metrics);
});

// Map SignalR NotificationHub (RISK-14)
app.MapHub<NotificationHub>("/hub");

app.MapControllers();

try
{
    Log.Information("Starting SMS API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Stub implementations for CLI commands (migration/seed) that run outside
// an HTTP request context. These provide empty/default values for services
// that would otherwise require an active HttpContext.
internal class StubCurrentUserService : ICurrentUserService
{
    public string UserId => string.Empty;
    public string Username => "migration";
    public string Email => string.Empty;
    public bool IsAuthenticated => false;
    public System.Collections.Generic.IEnumerable<string> Roles => System.Linq.Enumerable.Empty<string>();
}

internal class StubTenantContext : SMS.Domain.Interfaces.ITenantContext
{
    public string TenantId => string.Empty;
    public string TenantName => string.Empty;
    public string ConnectionString => string.Empty;
}

// Make Program class accessible to test projects
public partial class Program { }
