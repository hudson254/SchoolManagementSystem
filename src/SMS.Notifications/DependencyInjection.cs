using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SMS.Domain.Interfaces;
using SMS.Notifications.Hubs;
using SMS.Notifications.Services;

namespace SMS.Notifications
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddNotifications(this IServiceCollection services)
        {
            // Register SignalR hub
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
            });

            // Register services
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ISmsService, SmsService>();

            return services;
        }
    }
}

