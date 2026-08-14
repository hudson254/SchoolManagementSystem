using Microsoft.Extensions.DependencyInjection;
using SMS.Notifications.Hubs;
using SMS.Notifications.Services;

namespace SMS.Notifications
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddNotifications(this IServiceCollection services)
        {
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
            });

            services.AddScoped<INotificationService, NotificationService>();

            return services;
        }
    }
}

