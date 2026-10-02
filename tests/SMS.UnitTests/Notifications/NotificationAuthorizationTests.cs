using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.Notifications.Commands;
using SMS.Application.Features.Notifications.Queries;
using SMS.Domain.Interfaces;
using SMS.Domain.Notifications;
using Xunit;

// Both namespaces declare ICurrentUserService; the Application one merely re-exports
// the Domain interface. Alias it, as Program.cs already does, so the mock targets
// the abstraction the handlers actually consume.
using ICurrentUserService = SMS.Application.Common.Interfaces.ICurrentUserService;

namespace SMS.UnitTests.Notifications
{
    /// <summary>
    /// Ownership and authorization tests for the notification read/write surface.
    /// <para>
    /// These lock in a real defect that was fixed: <c>GetNotification</c>,
    /// <c>MarkAsRead</c> and <c>Delete</c> each resolved the row by primary key alone,
    /// so ANY authenticated user could read, mark-read, or delete ANY other user's
    /// notification in the tenant. Every handler now resolves the recipient from the
    /// authenticated principal and refuses a row the caller does not own.
    /// </para>
    /// </summary>
    public class NotificationAuthorizationTests
    {
        private const string OwnerUserId = "user-owner";
        private const string OtherUserId = "user-other";

        private static Mock<ICurrentUserService> CurrentUser(string? userId)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(x => x.UserId).Returns(userId ?? string.Empty);
            mock.Setup(x => x.IsAuthenticated).Returns(!string.IsNullOrWhiteSpace(userId));
            return mock;
        }

        /// <summary>
        /// Repository double that behaves like the real NotificationRepository: the
        /// ownership-enforcing methods only ever succeed for the row's OWN user.
        /// </summary>
        private static Mock<INotificationRepository> Repository(
            Domain.Entities.Notification? stored,
            bool markResult = true,
            bool deleteResult = true)
        {
            var mock = new Mock<INotificationRepository>();

            mock.Setup(r => r.GetForUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, string userId, CancellationToken _) =>
                    stored != null && stored.UserId == userId && stored.Id == id ? stored : null);

            mock.Setup(r => r.MarkAsReadForUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, string userId, CancellationToken _) =>
                    markResult && stored != null && stored.UserId == userId && stored.Id == id);

            mock.Setup(r => r.DeleteForUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, string userId, CancellationToken _) =>
                    deleteResult && stored != null && stored.UserId == userId && stored.Id == id);

            return mock;
        }

        private static Domain.Entities.Notification NewNotification(string userId) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Accommodation allocated",
            Message = "You have been allocated House 12.",
            Type = NotificationTypes.Accommodation,
            Priority = NotificationPriorities.Normal,
            IsRead = false,
            CreatedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        // ── GetNotification ───────────────────────────────────────────────

        [Fact]
        public async Task GetNotification_ReturnsNotificationOwnedByCaller()
        {
            var notification = NewNotification(OwnerUserId);
            var handler = new GetNotificationHandler(
                Repository(notification).Object,
                CurrentUser(OwnerUserId).Object,
                NullLogger<GetNotificationHandler>.Instance);

            var result = await handler.Handle(
                new GetNotificationQuery { NotificationId = notification.Id }, CancellationToken.None);

            result.Id.Should().Be(notification.Id);
            result.Title.Should().Be(notification.Title);
        }

        [Fact]
        public async Task GetNotification_ThrowsNotFoundForAnotherUsersNotification()
        {
            // The stored row belongs to somebody else, so the ownership-enforcing
            // lookup returns null and the handler raises NotFound (404), never
            // Forbidden - 403 would confirm the id exists.
            var someoneElses = NewNotification(OtherUserId);
            var handler = new GetNotificationHandler(
                Repository(someoneElses).Object,
                CurrentUser(OwnerUserId).Object,
                NullLogger<GetNotificationHandler>.Instance);

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
                new GetNotificationQuery { NotificationId = someoneElses.Id }, CancellationToken.None));
        }

        [Fact]
        public async Task GetNotification_ThrowsNotFoundWithNoAuthenticatedPrincipal()
        {
            var handler = new GetNotificationHandler(
                Repository(null).Object,
                CurrentUser(null).Object,
                NullLogger<GetNotificationHandler>.Instance);

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
                new GetNotificationQuery { NotificationId = Guid.NewGuid() }, CancellationToken.None));
        }

        // ── MarkAsRead ───────────────────────────────────────────────────────

        [Fact]
        public async Task MarkAsRead_SucceedsForOwnedNotification()
        {
            var notification = NewNotification(OwnerUserId);
            var unitOfWork = new Mock<IUnitOfWork>();

            var handler = new MarkNotificationAsReadHandler(
                Repository(notification).Object,
                CurrentUser(OwnerUserId).Object,
                unitOfWork.Object,
                NullLogger<MarkNotificationAsReadHandler>.Instance);

            await handler.Handle(
                new MarkNotificationAsReadCommand { NotificationId = notification.Id }, CancellationToken.None);

            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MarkAsRead_ThrowsNotFoundForAnotherUsersNotification()
        {
            var someoneElses = NewNotification(OtherUserId);
            var unitOfWork = new Mock<IUnitOfWork>();

            var handler = new MarkNotificationAsReadHandler(
                Repository(someoneElses, markResult: false).Object,
                CurrentUser(OwnerUserId).Object,
                unitOfWork.Object,
                NullLogger<MarkNotificationAsReadHandler>.Instance);

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
                new MarkNotificationAsReadCommand { NotificationId = someoneElses.Id }, CancellationToken.None));

            // The operation must be abandoned, not merely reported.
            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task MarkAsRead_ThrowsNotFoundWithNoAuthenticatedPrincipal()
        {
            var unitOfWork = new Mock<IUnitOfWork>();
            var handler = new MarkNotificationAsReadHandler(
                Repository(null).Object,
                CurrentUser(null).Object,
                unitOfWork.Object,
                NullLogger<MarkNotificationAsReadHandler>.Instance);

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
                new MarkNotificationAsReadCommand { NotificationId = Guid.NewGuid() }, CancellationToken.None));

            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Delete ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_SucceedsForOwnedNotification()
        {
            var notification = NewNotification(OwnerUserId);
            var unitOfWork = new Mock<IUnitOfWork>();

            var handler = new DeleteNotificationHandler(
                Repository(notification).Object,
                CurrentUser(OwnerUserId).Object,
                unitOfWork.Object,
                NullLogger<DeleteNotificationHandler>.Instance);

            await handler.Handle(
                new DeleteNotificationCommand { NotificationId = notification.Id }, CancellationToken.None);

            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Delete_ThrowsNotFoundForAnotherUsersNotification()
        {
            var someoneElses = NewNotification(OtherUserId);
            var unitOfWork = new Mock<IUnitOfWork>();

            var handler = new DeleteNotificationHandler(
                Repository(someoneElses, deleteResult: false).Object,
                CurrentUser(OwnerUserId).Object,
                unitOfWork.Object,
                NullLogger<DeleteNotificationHandler>.Instance);

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
                new DeleteNotificationCommand { NotificationId = someoneElses.Id }, CancellationToken.None));

            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── MarkAll / list scoping ────────────────────────────────────────────

        [Fact]
        public async Task MarkAllAsRead_TargetsOnlyTheAuthenticatedUser()
        {
            var repository = new Mock<INotificationRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var handler = new MarkAllNotificationsAsReadHandler(
                repository.Object,
                CurrentUser(OwnerUserId).Object,
                unitOfWork.Object,
                NullLogger<MarkAllNotificationsAsReadHandler>.Instance);

            await handler.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);

            repository.Verify(r => r.MarkAllAsReadAsync(OwnerUserId, It.IsAny<CancellationToken>()), Times.Once);
            repository.Verify(
                r => r.MarkAllAsReadAsync(It.Is<string>(id => id != OwnerUserId), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task MarkAllAsRead_DoesNothingWithNoAuthenticatedPrincipal()
        {
            var repository = new Mock<INotificationRepository>();
            var handler = new MarkAllNotificationsAsReadHandler(
                repository.Object,
                CurrentUser(null).Object,
                new Mock<IUnitOfWork>().Object,
                NullLogger<MarkAllNotificationsAsReadHandler>.Instance);

            await handler.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);

            repository.Verify(r => r.MarkAllAsReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

// ── List scoping and bounding ─────────────────────────────────────

        [Fact]
        public async Task GetMyNotifications_QueriesWithTheAuthenticatedUserId()
        {
            // The query contract carries no client-supplied UserId, so a caller cannot
            // enumerate another user's history by passing their id.
            var repository = PagedRepository();

            var handler = new GetMyNotificationsHandler(
                repository.Object,
                CurrentUser(OwnerUserId).Object,
                NullLogger<GetMyNotificationsHandler>.Instance);

            await handler.Handle(new GetMyNotificationsQuery(), CancellationToken.None);

            repository.Verify(r => r.GetPagedForUserAsync(
                OwnerUserId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetMyNotifications_ClampsPageSizeSoAClientCannotRequestUnboundedPage()
        {
            var repository = PagedRepository();

            var handler = new GetMyNotificationsHandler(
                repository.Object,
                CurrentUser(OwnerUserId).Object,
                NullLogger<GetMyNotificationsHandler>.Instance);

            var result = await handler.Handle(
                new GetMyNotificationsQuery { Page = 1, PageSize = 100_000 }, CancellationToken.None);

            repository.Verify(r => r.GetPagedForUserAsync(
                OwnerUserId, 1, GetMyNotificationsHandler.MaxPageSize, It.IsAny<bool?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

            // PageSize must be echoed so PagedResult computes a correct TotalPages;
            // leaving it at the default 10 made the page count disagree with the
            // items actually returned.
            result.PageSize.Should().Be(GetMyNotificationsHandler.MaxPageSize);
        }

        [Fact]
        public async Task GetMyNotifications_ReturnsEmptyResultWithNoAuthenticatedPrincipal()
        {
            var repository = PagedRepository();

            var handler = new GetMyNotificationsHandler(
                repository.Object,
                CurrentUser(null).Object,
                NullLogger<GetMyNotificationsHandler>.Instance);

            var result = await handler.Handle(new GetMyNotificationsQuery(), CancellationToken.None);

            result.TotalCount.Should().Be(0);
            result.Items.Should().BeEmpty();
            repository.Verify(
                r => r.GetPagedForUserAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private static Mock<INotificationRepository> PagedRepository()
        {
            var repository = new Mock<INotificationRepository>();
            repository.Setup(r => r.GetPagedForUserAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool?>(),
                    It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<Domain.Entities.Notification>(), 0));
            return repository;
        }
    }
}