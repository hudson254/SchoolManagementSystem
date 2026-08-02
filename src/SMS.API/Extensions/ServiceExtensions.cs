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
using SMS.Domain.Interfaces;
using SMS.Application.Common;

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
            });

            // Register repositories
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<ILecturerRepository, LecturerRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<IUnitRepository, UnitRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        public static IServiceCollection AddInfrastructureServices(
                    this IServiceCollection services,
                    IConfiguration configuration)
        {
            services.AddScoped<IAuditService, AuditService>();

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
