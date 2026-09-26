using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Common.Behaviours;  // Change this line
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.OMS.Services;
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

            // OMS request notifications: thin adapter over the existing in-app
            // notification pipeline (no SMTP / Twilio / external gateway).
            services.AddScoped<IOmsRequestNotifier, OmsRequestNotifier>();

            // OMS module request adapters. Each SMS module adapter resolves its
            // own business context and delegates to the single generic Request
            // create path. Registered as one shared pass-through service so no
            // module can grow a second workflow engine.
            services.AddScoped<IOmsRequestModuleAdapter, OmsRequestModuleAdapter>();

            // Thin module request adapters need no registration of their own: they
            // are MediatR IRequestHandler implementations, so the
            // RegisterServicesFromAssembly call above already wires them. Each
            // one derives from OmsModuleRequestAdapter, which builds a generic
            // CreateRequestCommand and dispatches it through the shared ISender.
            // An adapter therefore can never write to oms_requests directly,
            // cannot skip OMS authorization/audit/history, and cannot grow its
            // own status machine.

            return services;
        }
    }
}
