using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SMS.Notifications;
using SMS.Notifications.Services;
using Xunit;

namespace SMS.UnitTests.Notifications
{
    public class NotificationServiceTests
    {
        [Fact]
        public void AddNotifications_RegistersInAppNotificationService()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNotifications();

            var registration = services.FirstOrDefault(descriptor =>
                descriptor.ServiceType == typeof(INotificationService) &&
                descriptor.ImplementationType == typeof(NotificationService));

            Assert.NotNull(registration);
        }
    }
}
