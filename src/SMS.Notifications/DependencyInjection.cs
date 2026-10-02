using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Features.Notifications.Commands;
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

            // Real-time push. Scoped alongside the DbContext so the SignalR IHubContext
            // and the notification write share one request lifetime. The notification
            // history, unread counts and read state do NOT depend on this registration -
            // they are served from PostgreSQL - so an unavailable hub degrades the live
            // experience without losing a single notification.
            services.AddScoped<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();

            return services;
        }
    }
}

