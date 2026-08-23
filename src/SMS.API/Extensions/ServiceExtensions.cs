using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using SMS.Application;
using SMS.Persistence;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using SMS.Identity;
using SMS.Identity.Services;
using SMS.Infrastructure;
using SMS.Infrastructure.Services;
using SMS.Infrastructure.MultiTenancy;
using SMS.Infrastructure.Options;
using SMS.Domain.Interfaces;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;

namespace SMS.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(SMS.Application.DependencyInjection).Assembly);
            });

            // Add FluentValidation
            services.AddValidatorsFromAssembly(typeof(SMS.Application.DependencyInjection).Assembly);

            return services;
        }

        public static IServiceCollection AddPersistenceServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register DbContext
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    x => x.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

                // Add the tenant context interceptor to set PostgreSQL session variable for RLS
                var tenantContext = serviceProvider.GetRequiredService<ITenantContext>();
                var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TenantContextDbInterceptor>>();
                options.AddInterceptors(new TenantContextDbInterceptor(tenantContext, logger));
            });

            // Register repositories
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<ILecturerRepository, LecturerRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<IUnitRepository, UnitRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITitleRepository, TitleRepository>();

            // Register assessment repositories
            services.AddScoped<IAssessmentRepository, AssessmentRepository>();
            services.AddScoped<IStudentAssessmentMarkRepository, StudentAssessmentMarkRepository>();
            services.AddScoped<IAssessmentTypeRepository, AssessmentTypeRepository>();
            services.AddScoped<IAssessmentTemplateRepository, AssessmentTemplateRepository>();
            services.AddScoped<IGradingScaleRepository, GradingScaleRepository>();
            services.AddScoped<IGradeBandRepository, GradeBandRepository>();
            services.AddScoped<ICertificateRuleRepository, CertificateRuleRepository>();
            services.AddScoped<IStudentCertificateEligibilityRepository, StudentCertificateEligibilityRepository>();
            services.AddScoped<IGradeChangeHistoryRepository, GradeChangeHistoryRepository>();
            services.AddScoped<IUnitResultRepository, UnitResultRepository>();
            services.AddScoped<IModerationRecordRepository, ModerationRecordRepository>();
            services.AddScoped<IAssessmentExemptionRepository, AssessmentExemptionRepository>();

            return services;
        }

        public static IServiceCollection AddInfrastructureServices(
                    this IServiceCollection services,
                    IConfiguration configuration)
        {
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<SMS.Application.Common.Interfaces.IUsernameGenerator, UsernameGenerator>();
            services.AddScoped<INameParser, NameParser>();
            services.AddScoped<ITitleConfiguration, TitleConfiguration>();
            services.Configure<TitleOptions>(configuration.GetSection("TitleConfiguration"));

            // Register Assessment Engine
            services.AddScoped<IAssessmentEngine, AssessmentEngine>();

            // Register File Storage Service
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));

            // Register Upload Service (Centralized Enterprise Upload)
            services.AddScoped<IUploadRepository, UploadRepository>();
            services.AddScoped<IUploadService, UploadService>();
            services.Configure<UploadSettings>(configuration.GetSection(UploadSettings.SectionName));

            return services;
        }

        public static IServiceCollection AddIdentityServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<Domain.Interfaces.IUserManagerService, SMS.Identity.Services.UserManagerService>();

            return services;
        }
    }
}

