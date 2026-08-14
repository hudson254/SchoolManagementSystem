using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common.Behaviours;  // Change this line
using SMS.Application.Services;

namespace SMS.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            });

            // Register the shared password policy service (server-side authority).
            services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();

            // Register Assessment Engine - Centralized assessment and grading service
            services.AddScoped<AssessmentEngine>();

            return services;
        }
    }
}
