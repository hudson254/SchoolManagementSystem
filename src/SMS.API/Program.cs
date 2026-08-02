using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
using SMS.Infrastructure.Services;
using SMS.Infrastructure.Options;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using SMS.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
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
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

// Configure session support (required for audit session tracking)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetValue<string>("Frontend:Url") ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
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

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Configure Health Checks
builder.Services.AddHealthChecks();

// Configure PostgreSQL with scoped DbContext (required because ApplicationDbContext depends on scoped services)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(
        options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3);
                npgsqlOptions.CommandTimeout(60);
            }),
        contextLifetime: ServiceLifetime.Scoped,
        optionsLifetime: ServiceLifetime.Scoped);
}

// Configure Identity
builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.SignIn.RequireConfirmedEmail = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Read JWT secret from environment variable or configuration
var jwtConfig = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtConfig["Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("JWT Secret not configured. Set JWT_SECRET environment variable or configure JwtSettings:Secret in appsettings.");

// Single source of truth: push the resolved secret back into configuration so that
// JwtService (via IOptions<JwtSettings>) signs with the SAME key that this pipeline validates with.
// Without this, if JWT_SECRET env var is set, tokens are signed with config secret but
// validated with the env secret -> all authenticated requests fail with 401.
jwtConfig["Secret"] = jwtSecret;

var key = Encoding.UTF8.GetBytes(jwtSecret);

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
        NameClaimType = "name"
    };
});

// Register Application Layer
builder.Services.AddApplication();

// Register Infrastructure Options
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("SMTP"));
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));

// Register services
builder.Services.AddScoped<IUserManagerService, SMS.Infrastructure.Services.UserManagerService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AuditHelper>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IExcelGenerator, ExcelGenerator>();
builder.Services.AddScoped<IPdfGenerator, PdfGenerator>();
builder.Services.AddScoped<SMS.Multitenancy.Interfaces.ITenantResolver, TenantResolver>();
builder.Services.AddScoped<SMS.Infrastructure.MultiTenancy.TenantContext>();
builder.Services.AddScoped<SMS.Domain.Interfaces.ITenantContext>(sp =>
    sp.GetRequiredService<SMS.Infrastructure.MultiTenancy.TenantContext>());
builder.Services.AddScoped<SMS.Multitenancy.Interfaces.ITenantContext>(sp =>
    sp.GetRequiredService<SMS.Infrastructure.MultiTenancy.TenantContext>());
builder.Services.AddScoped<ITenantStore, TenantStore>();
builder.Services.AddHttpContextAccessor();

// Register repositories
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

// Register UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Report Authentication Services
builder.Services.Configure<ReportAuthenticationOptions>(builder.Configuration.GetSection("ReportVerification"));
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IWatermarkService, WatermarkService>();
builder.Services.AddScoped<IReportTokenService, ReportTokenService>();
builder.Services.AddScoped<IReportHashService, ReportHashService>();
builder.Services.AddScoped<IReportAuthenticationService, ReportAuthenticationService>();

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdministratorAccess", policy =>
        policy.RequireRole("Administrator"));
    options.AddPolicy("ModeratorAccess", policy =>
        policy.RequireRole("Administrator", "Moderator"));
    options.AddPolicy("LecturerAccess", policy =>
        policy.RequireRole("Administrator", "Moderator", "Lecturer"));
    options.AddPolicy("StudentAccess", policy =>
        policy.RequireRole("Administrator", "Moderator", "Lecturer", "Student"));
    options.AddPolicy("ReceptionistAccess", policy =>
        policy.RequireRole("Administrator", "Moderator", "Receptionist"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<LoggingEnrichmentMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Skip HTTPS redirect in Testing environment to avoid redirect loops with TestServer (HTTP only).
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
app.UseSession();

// Apply migrations and seed data (skip migrations in automated test environment)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        if (!app.Environment.IsEnvironment("Testing"))
        {
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully.");
        }
        else
        {
            Log.Information("Skipping database migrations in Testing environment.");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error applying migrations. The application will continue without migrations.");
    }
}

app.UseAuthentication();
app.UseAuthorization();

// Map health checks
app.MapHealthChecks("/health");

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

// Make Program class accessible to test projects
public partial class Program { }
