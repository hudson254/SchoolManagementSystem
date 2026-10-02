using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Application.DTOs;
using SMS.Notifications.Hubs;
using SMS.Notifications.Services;
using Xunit;

namespace SMS.UnitTests.Notifications
{
    /// <summary>
    /// Transport-security and delivery tests for the SignalR notification path.
    /// <para>
    /// The hub is the ONLY way one user's live notification stream can reach
    /// another user's browser, so its invariants are asserted here directly:
    /// identity comes from claims, there is no client-callable subscription
    /// method, and a push can only ever target the persisted notification's own
    /// owner.
    /// </para>
    /// </summary>
    public class NotificationHubSecurityTests
    {
        // ── Authorization ────────────────────────────────────────────────────

        [Fact]
        public void Hub_RequiresAuthorization()
        {
            typeof(NotificationHub)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Should().NotBeEmpty("the hub must reject anonymous connections");
        }

        [Fact]
        public void Hub_ExposesNoMethodThatTakesAUserId()
        {
            // The removed SubscribeToNotifications(string userId) let ANY client -
            // including an anonymous one - join ANY user's group. No public method
            // may accept a user/group identifier again.
            var publicMethods = typeof(NotificationHub)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            publicMethods.Should().NotContain(m => m.Name == "SubscribeToNotifications");

            foreach (var method in publicMethods)
            {
                foreach (var parameter in method.GetParameters())
                {
                    parameter.ParameterType.Should().NotBe(typeof(string),
                        $"'{method.Name}' takes a string parameter that could name a user or group");
                }
            }
        }

        [Fact]
        public void GroupNames_AreDerivedFromThePrincipal_NotClientInput()
        {
            NotificationHub.GroupForUser("abc").Should().Be("user_abc");
            NotificationHub.GroupForRole("Administrator").Should().Be("role_Administrator");
        }

        // ── Claim-derived identity ───────────────────────────────────────────

        [Theory]
        [InlineData("Student")]
        [InlineData("Lecturer")]
        [InlineData("Administrator")]
        public async Task OnConnected_AddsTheConnectionToItsOwnUserGroupFromClaims(string role)
        {
            var groups = new RecordingGroupManager();
            var hub = BuildHub(groups, userId: "user-42", roles: new[] { role });

            await hub.OnConnectedAsync();

            // The group name is built from Context.UserIdentifier, which SignalR
            // populates from the validated principal - never from the payload.
            groups.Additions.Should().Contain(g => g.Group == "user_user-42");
            groups.Additions.Should().Contain(g => g.Group == $"role_{role}");
        }

        [Fact]
        public async Task OnConnected_AbortsWhenThereIsNoAuthenticatedIdentity()
        {
            // Fail closed: an unauthenticated connection must not be left looking
            // like a merely-broken client.
            var groups = new RecordingGroupManager();
            var hub = BuildHub(groups, userId: string.Empty, roles: Array.Empty<string>());

            await hub.OnConnectedAsync();

            hub.ContextAborted.Should().BeTrue();
            groups.Additions.Should().BeEmpty();
        }

        [Fact]
        public async Task OnConnected_DerivesRoleGroupsOnlyFromClaims()
        {
            var groups = new RecordingGroupManager();
            var hub = BuildHub(groups, userId: "u1", roles: new[] { "Lecturer", "Coordinator" });

            await hub.OnConnectedAsync();

            groups.Additions.Select(g => g.Group).Should().BeEquivalentTo(
                new[] { "user_u1", "role_Lecturer", "role_Coordinator" });
        }

        // ── Delivery targeting ───────────────────────────────────────────────

        [Fact]
        public async Task Publisher_SendsOnlyToThePersistedOwnersGroup()
        {
            var hubContext = new RecordingHubContext();
            var publisher = new SignalRNotificationRealtimePublisher(
                hubContext, NullLogger<SignalRNotificationRealtimePublisher>.Instance);

            await publisher.PublishAsync("user-7", new NotificationDto
            {
                Id = Guid.NewGuid(),
                Title = "Accommodation Allocated",
                Message = "House A",
                Type = "Accommodation",
                Priority = "Important",
                CreatedAt = DateTime.UtcNow
            });

            // The recipient comes from the just-persisted notification, so a caller
            // cannot address the push at somebody else's group.
            hubContext.Sends.Should().ContainSingle();
            hubContext.Sends[0].Group.Should().Be("user_user-7");
            hubContext.Sends[0].Method.Should().Be(NotificationService.ReceiveNotificationMethod);
        }

        [Fact]
        public async Task Publisher_DeliversNothingForABlankRecipient()
        {
            var hubContext = new RecordingHubContext();
            var publisher = new SignalRNotificationRealtimePublisher(
                hubContext, NullLogger<SignalRNotificationRealtimePublisher>.Instance);

            await publisher.PublishAsync(string.Empty, new NotificationDto { Id = Guid.NewGuid() });
            await publisher.PublishAsync(null!, new NotificationDto { Id = Guid.NewGuid() });

            hubContext.Sends.Should().BeEmpty();
        }

        [Fact]
        public async Task Publisher_DeliversNothingForANullNotification()
        {
            var hubContext = new RecordingHubContext();
            var publisher = new SignalRNotificationRealtimePublisher(
                hubContext, NullLogger<SignalRNotificationRealtimePublisher>.Instance);

            await publisher.PublishAsync("user-7", null!);

            hubContext.Sends.Should().BeEmpty();
        }

        [Fact]
        public void ClientEventName_IsTheNameTheFrontendSubscribesTo()
        {
            // Both sides must agree on this string or live delivery silently does
            // nothing. The frontend constant is asserted in
            // frontend/sms-web/src/services/notificationHub.test.ts.
            NotificationService.ReceiveNotificationMethod.Should().Be("ReceiveNotification");
        }

        // ── Test doubles ─────────────────────────────────────────────────────

        private static NotificationHub BuildHub(
            RecordingGroupManager groups, string userId, string[] roles)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userId)) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            // HubCallerContext is sealed into an interface the hub only reads from,
            // so a mock is both sufficient and the least-coupled option. Groups is
            // supplied by the real Hub caller context in production; the hub's
            // use of it is exercised through the recording manager below.
            var callerContext = new Mock<HubCallerContext>();
            callerContext.Setup(c => c.User).Returns(principal);
            callerContext.Setup(c => c.UserIdentifier).Returns(userId ?? string.Empty);
            callerContext.Setup(c => c.ConnectionId).Returns("conn-1");
            callerContext.Setup(c => c.Abort()).Callback(() => { });

            return new NotificationHub(NullLogger<NotificationHub>.Instance)
            {
                Context = callerContext.Object,
                GroupsOverride = groups
            };
        }

        private sealed class RecordingGroupManager : IGroupManager
        {
            public List<(string ConnectionId, string Group)> Additions { get; } = new();

            public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            {
                Additions.Add((connectionId, groupName));
                return Task.CompletedTask;
            }

            public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        private sealed class RecordingHubContext : IHubContext<NotificationHub>
        {
            public List<(string Group, string Method)> Sends { get; } = new();

            // IHubContext<T>.Clients is the NON-generic IHubClients; the generic
            // IHubClients<T> only supplies the extension-method sugar.
            public IHubClients Clients => new RecordingClients(this);
            public IGroupManager Groups { get; } = Mock.Of<IGroupManager>();
        }

        private sealed class RecordingClients : IHubClients
        {
            private readonly RecordingHubContext _owner;
            public RecordingClients(RecordingHubContext owner) => _owner = owner;

            public IClientProxy All => Proxy("*");
            public IClientProxy AllExcept(IReadOnlyList<string> excluded) => Proxy("*");
            public IClientProxy Client(string connectionId) => Proxy(connectionId);
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy("many");
            public IClientProxy Group(string groupName) => Proxy(groupName);
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excluded) => Proxy(groupName);
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy("groups");
            public IClientProxy User(string userId) => Proxy($"user_{userId}");
            public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy("users");

            private IClientProxy Proxy(string name) => new RecordingProxy(_owner, name);
        }

        private sealed class RecordingProxy : IClientProxy
        {
            private readonly RecordingHubContext _owner;
            private readonly string _name;

            public RecordingProxy(RecordingHubContext owner, string name)
            {
                _owner = owner;
                _name = name;
            }

            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            {
                _owner.Sends.Add((_name, method));
                return Task.CompletedTask;
            }
        }
    }
}