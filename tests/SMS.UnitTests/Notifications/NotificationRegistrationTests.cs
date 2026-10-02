using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application;
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.Notifications.Commands;
using SMS.Application.Services;
using SMS.Notifications;
using SMS.Notifications.Hubs;
using SMS.Notifications.Services;
using Xunit;

namespace SMS.UnitTests.Notifications
{
    /// <summary>
    /// Composition-root and transport-security checks for the notification stack.
    /// </summary>
    public class NotificationRegistrationTests
    {
        [Fact]
        public void AddNotifications_RegistersInAppNotificationService()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNotifications();

            services.Should().Contain(d =>
                d.ServiceType == typeof(INotificationService)
                && d.ImplementationType == typeof(NotificationService));
        }

        [Fact]
        public void AddNotifications_RegistersRealtimePublisher()
        {
            // The real-time push is an OPTIONAL collaborator: history, unread counts
            // and read state are served from PostgreSQL and must keep working when no
            // SignalR transport is available.
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNotifications();

            services.Should().Contain(d =>
                d.ServiceType == typeof(INotificationRealtimePublisher)
                && d.ImplementationType == typeof(SignalRNotificationRealtimePublisher));
        }

        [Fact]
        public void AddApplication_RegistersNotificationDispatcher()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();

            services.Should().Contain(d =>
                d.ServiceType == typeof(INotificationDispatcher)
                && d.ImplementationType == typeof(NotificationDispatcher));
        }

        [Fact]
        public void NotificationHub_RequiresAuthorization()
        {
            // /hub was previously mapped with no authorization and the host sets no
            // FallbackPolicy, so it was reachable anonymously.
            typeof(NotificationHub)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Should()
                .NotBeEmpty("the SignalR hub must carry [Authorize]");
        }

        [Fact]
        public void NotificationHub_ExposesNoClientSuppliedSubscriptionMethod()
        {
            // The previous hub exposed SubscribeToNotifications(string userId), which
            // let ANY caller - including an unauthenticated one - join any user's
            // group and read that user's live notification stream.
            var hubMethods = typeof(NotificationHub)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => m.Name)
                .ToList();

            hubMethods.Should().NotContain("SubscribeToNotifications");
            hubMethods.Should().NotContain("MarkAsRead");
        }

        [Fact]
        public void NotificationHub_GroupNamesAreDerivedFromThePrincipal()
        {
            NotificationHub.GroupForUser("abc").Should().Be("user_abc");
            NotificationHub.GroupForRole("Administrator").Should().Be("role_Administrator");
        }
    }
}