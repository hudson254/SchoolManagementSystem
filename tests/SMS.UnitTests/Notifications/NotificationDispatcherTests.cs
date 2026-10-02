using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.Notifications.Commands;
using SMS.Application.DTOs;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Domain.Notifications;
using Xunit;

// Both namespaces declare ICurrentUserService; the Application one merely re-exports
// the Domain interface. Alias it, as Program.cs already does, so the mock targets
// the abstraction the dispatcher actually consumes.
using ICurrentUserService = SMS.Application.Common.Interfaces.ICurrentUserService;

// SMS.Domain.Entities.Unit (an academic unit) and MediatR.Unit both exist; the
// notification path only ever uses the MediatR marker.
using MediatorUnit = MediatR.Unit;

namespace SMS.UnitTests.Notifications
{
    /// <summary>
    /// Tests for <see cref="NotificationDispatcher"/>, the single write path business
    /// handlers use to raise in-app notifications.
    /// <para>
    /// The load-bearing property here is fault isolation: a notification is a
    /// side-effect of a state transition that has ALREADY committed, so a
    /// notification failure must never propagate and abort the business operation.
    /// </para>
    /// </summary>
    public class NotificationDispatcherTests
    {
        /// <summary>
        /// Captures every command the dispatcher sends.
        /// <para>
        /// MediatR 12's <c>ISender.Send&lt;TResponse&gt;</c> is generic, and
        /// <c>CreateNotificationCommand</c> is <c>IRequest&lt;NotificationDto&gt;</c>, so
        /// the callback must yield a <c>Task&lt;NotificationDto&gt;</c>. Returning
        /// <c>Task&lt;Unit&gt;</c> makes Moq throw at setup time.
        /// </para>
        /// </summary>
        private static Mock<ISender> RecordingSender(out List<CreateNotificationCommand> captured)
        {
            var sent = new List<CreateNotificationCommand>();
            captured = sent;

            var sender = new Mock<ISender>();
            sender.Setup(s => s.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
                .Returns((CreateNotificationCommand cmd, CancellationToken _) =>
                {
                    sent.Add(cmd);
                    return Task.FromResult(new NotificationDto
                    {
                        Id = Guid.NewGuid(),
                        Title = cmd.Title,
                        Message = cmd.Message,
                        Type = cmd.Type ?? NotificationTypes.System,
                        Priority = cmd.Priority ?? NotificationPriorities.Normal,
                        CreatedAt = DateTime.UtcNow
                    });
                });

            return sender;
        }

        /// <summary>
        /// Builds a dispatcher around a sender mock. Takes the <see cref="Mock{T}"/> so
        /// callers pass the mock itself and assertions can be made on it.
        /// </summary>
        private static NotificationDispatcher Build(
            Mock<ISender> sender,
            IUserManagerService? userManager = null,
            ICurrentUserService? currentUser = null)
        {
            return new NotificationDispatcher(
                sender.Object,
                userManager ?? new Mock<IUserManagerService>().Object,
                currentUser ?? new Mock<ICurrentUserService>().Object,
                NullLogger<NotificationDispatcher>.Instance);
        }

        [Fact]
        public async Task NotifyUserAsync_CreatesOnePersistedNotification()
        {
            var sender = RecordingSender(out var sent);
            var dispatcher = Build(sender);

            await dispatcher.NotifyUserAsync(
                "user-1", "Accommodation allocated", "House 12", NotificationTypes.Accommodation);

            sent.Should().HaveCount(1);
            sent[0].UserId.Should().Be("user-1");
            sent[0].Type.Should().Be(NotificationTypes.Accommodation);
            // Priority defaults from the type, so a caller cannot forget to set it.
            sent[0].Priority.Should().Be(NotificationPriorities.Normal);
        }

        [Fact]
        public async Task NotifyUserAsync_IsANoOpWithoutARecipient()
        {
            var sender = RecordingSender(out var sent);
            var dispatcher = Build(sender);

            await dispatcher.NotifyUserAsync(null, "Title", "Message");
            await dispatcher.NotifyUserAsync("   ", "Title", "Message");

            sent.Should().BeEmpty();
        }

        [Fact]
        public async Task NotifyUsersAsync_DeduplicatesRecipients()
        {
            // A user holding two roles, or listed twice by a caller, must receive
            // exactly ONE notification rather than duplicate spam.
            var sender = RecordingSender(out var sent);
            var dispatcher = Build(sender);

            await dispatcher.NotifyUsersAsync(
                new[] { "user-1", "USER-1", "user-1", " user-2 " }, "Title", "Message");

            sent.Should().HaveCount(2);
            sent.Select(c => c.UserId).Should().BeEquivalentTo(new[] { "user-1", "user-2" });
        }

        [Fact]
        public async Task NotifyUserAsync_SanitisesActionUrlBeforeDispatch()
        {
            var sender = RecordingSender(out var sent);
            var dispatcher = Build(sender);

            await dispatcher.NotifyUserAsync(
                "user-1", "Title", "Message", actionUrl: "https://evil.example/phish");

            sent.Should().HaveCount(1);
            sent[0].ActionUrl.Should().BeNull();
        }

        [Fact]
        public async Task NotifyUserAsync_ClampsRecipientsToABoundedBatch()
        {
            var sender = RecordingSender(out var sent);
            var dispatcher = Build(sender);

            var tooMany = Enumerable
                .Range(0, NotificationDispatcher.MaxRecipients + 500)
                .Select(i => $"user-{i}")
                .ToList();

            await dispatcher.NotifyUsersAsync(tooMany, "Title", "Message");

            sent.Count.Should().Be(NotificationDispatcher.MaxRecipients);
        }

// ── Fault isolation ────────────────────────────────────────────────

        [Fact]
        public async Task NotifyUserAsync_DoesNotFailTheCallerWhenPersistenceThrows()
        {
            var sender = new Mock<ISender>();
            sender.Setup(s => s.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("db down"));

            var dispatcher = Build(sender);

            await dispatcher.NotifyUserAsync("user-1", "Title", "Message");

            sender.Verify(
                s => s.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyUsersAsync_ContinuesAfterOneRecipientFails()
        {
            var attempts = 0;
            var sender = new Mock<ISender>();
            sender.Setup(s => s.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
                .Returns((CreateNotificationCommand _, CancellationToken __) =>
                {
                    attempts++;
                    if (attempts == 1) throw new InvalidOperationException("transient");
                    return Task.FromResult(new NotificationDto
                    {
                        Id = Guid.NewGuid(),
                        Title = "Title",
                        Message = "Message",
                        Type = NotificationTypes.System,
                        Priority = NotificationPriorities.Normal
                    });
                });

            var dispatcher = Build(sender);
            await dispatcher.NotifyUsersAsync(new[] { "user-1", "user-2" }, "Title", "Message");

            // Both recipients attempted despite the first failing.
            attempts.Should().Be(2);
        }

        [Fact]
        public async Task NotifyUserAsync_SkipsEmptyTitles()
        {
            var sender = RecordingSender(out var sent);
            var dispatcher = Build(sender);

            await dispatcher.NotifyUserAsync("user-1", "   ", "Message");

            sent.Should().BeEmpty();
        }

        // ── Role targeting ────────────────────────────────────────────────────

        [Fact]
        public async Task NotifyRolesAsync_ResolvesMembersAndDeduplicates()
        {
            var userManager = new Mock<IUserManagerService>();
            // Role resolution is case-insensitive, as it is in ASP.NET Identity. The
            // caller passes "coordinator" here while the seeded role is
            // "Coordinator", so a case-sensitive double would wrongly resolve
            // nothing and hide a recipient.
            userManager.Setup(m => m.GetUsersByRoleAsync(It.Is<string>(r =>
                    string.Equals(r, "Administrator", StringComparison.OrdinalIgnoreCase))))
                .ReturnsAsync(new List<User> { new User { Id = "user-1" }, new User { Id = "user-2" } });
            userManager.Setup(m => m.GetUsersByRoleAsync(It.Is<string>(r =>
                    string.Equals(r, "Coordinator", StringComparison.OrdinalIgnoreCase))))
                .ReturnsAsync(new List<User> { new User { Id = "user-2" }, new User { Id = "user-3" } });

            var sender = RecordingSender(out var sent);
            var dispatcher = Build(sender, userManager.Object);

            await dispatcher.NotifyRolesAsync(
                new[] { "Administrator", "coordinator" }, "Pending approvals", "3 awaiting review");

            sent.Should().HaveCount(3); // user-2 holds both roles but gets one copy
            sent.Select(c => c.UserId).Should().BeEquivalentTo(new[] { "user-1", "user-2", "user-3" });
        }

        [Fact]
        public async Task NotifyRolesAsync_ContinuesWhenOneRoleFailsToResolve()
        {
            var userManager = new Mock<IUserManagerService>();
            userManager.Setup(m => m.GetUsersByRoleAsync("Broken"))
                .ThrowsAsync(new InvalidOperationException("identity unavailable"));
            userManager.Setup(m => m.GetUsersByRoleAsync("Administrator"))
                .ReturnsAsync(new List<User> { new User { Id = "user-1" } });

            var sender = RecordingSender(out var sent);
            var dispatcher = Build(sender, userManager.Object);

            await dispatcher.NotifyRolesAsync(new[] { "Broken", "Administrator" }, "Title", "Message");

            sent.Should().HaveCount(1);
            sent[0].UserId.Should().Be("user-1");
        }

        [Fact]
        public async Task NotifyCurrentUserAsync_TargetsTheAuthenticatedUser()
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(u => u.UserId).Returns("current-user");

            var sender = RecordingSender(out var sent);
            var dispatcher = Build(sender, null, currentUser.Object);

            await dispatcher.NotifyCurrentUserAsync("Password reset", "Submitted", NotificationTypes.Security);

            sent.Should().HaveCount(1);
            sent[0].UserId.Should().Be("current-user");
            sent[0].Priority.Should().Be(NotificationPriorities.Critical);
        }

        [Fact]
        public async Task NotifyCurrentUserAsync_DoesNothingWhenNotAuthenticated()
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(u => u.UserId).Returns(string.Empty);

            var sender = RecordingSender(out var sent);
            var dispatcher = Build(sender, null, currentUser.Object);

            await dispatcher.NotifyCurrentUserAsync("Title", "Message");

            sent.Should().BeEmpty();
        }
    }
}