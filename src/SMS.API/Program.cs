using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
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
using SMS.API.Logging;
using SMS.API.Middleware;
using SMS.API.Options;
using SMS.Notifications;
using SMS.Notifications.Hubs;
using SMS.Reporting;

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
        NameClaimType = "name"
    };
});

// Register Application Layer
builder.Services.AddApplication();

// Register Infrastructure Options
// NOTE: SMTP/EmailOptions removed — email functionality fully disabled per
// owner requirement. Password resets are now admin-mediated (Phase 2).
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));
builder.Services.Configure<SmsOptions>(builder.Configuration.GetSection("Sms"));

// Register HttpClient for SMS service (RISK-13)
builder.Services.AddHttpClient("SmsClient");

// Register services
builder.Services.AddScoped<IUserManagerService, SMS.Infrastructure.Services.UserManagerService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
// Register the username generator used by RegisterCommandHandler and
// CheckUsernameAvailabilityQueryHandler. Without this the API fails to start
// with "Unable to resolve service IUsernameGenerator" (service validation).
builder.Services.AddScoped<SMS.Application.Common.Interfaces.IUsernameGenerator, UsernameGenerator>();
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

// Register UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Report Authentication Services
builder.Services.Configure<ReportAuthenticationOptions>(builder.Configuration.GetSection("ReportVerification"));
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IWatermarkService, WatermarkService>();
builder.Services.AddScoped<IReportTokenService, ReportTokenService>();
builder.Services.AddScoped<IReportHashService, ReportHashService>();
builder.Services.AddScoped<IReportAuthenticationService, ReportAuthenticationService>();

// Register token revocation service (in-memory deny-list for access tokens).
// Suitable for single-instance LAN deployment. Replace with a distributed
// cache implementation if horizontal scaling is introduced.
builder.Services.AddSingleton<ITokenRevocationService, InMemoryTokenRevocationService>();

// Register SMS.Notifications and SMS.Reporting (RISK-14)
builder.Services.AddNotifications();
builder.Services.AddReporting();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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

// Make Program class accessible to test projects
public partial class Program { }
