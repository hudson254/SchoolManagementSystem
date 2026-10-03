using System.Threading;
using System.Threading.Tasks;
using Moq;
using SMS.Application.Common.Interfaces;

namespace SMS.UnitTests
{
    /// <summary>
    /// Builds a fully-stubbed <see cref="IBusinessEventNotifier"/>.
    /// <para>
    /// Business-handler tests assert BUSINESS behaviour (house assigned, enrollment
    /// created, assignment published) and only need the notifier to be harmless.
    /// Notification routing itself - who is resolved, what type/priority/action URL
    /// is produced - is asserted separately in
    /// <c>tests/SMS.UnitTests/Notifications/BusinessEventNotifierTests.cs</c>.
    /// </para>
    /// <para>
    /// Every method is stubbed explicitly rather than relying on a loose Moq mock.
    /// A loose mock returns <c>null</c> for an unstubbed Task-returning method, and
    /// <c>await null</c> throws a NullReferenceException inside the handler under
    /// test - which fails the test for a reason that has nothing to do with what it
    /// is asserting.
    /// </para>
    /// </summary>
    internal static class BusinessEventNotifierStub
    {
        public static Mock<IBusinessEventNotifier> Create()
        {
            var mock = new Mock<IBusinessEventNotifier>();

            mock.Setup(m => m.NotifyAccommodationAllocatedAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyAccommodationChangedAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyAccommodationEndedAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyAssignmentPublishedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyAssignmentSubmittedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyAssignmentGradedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyAssignmentIssueAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyUnitChangedAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyCourseChangedAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyStudentEnrolledAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyEnrollmentStatusChangedAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyRegistrationDecisionAsync(
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyAccommodationRequiredAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            mock.Setup(m => m.NotifyRegistrationAwaitingApprovalAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            return mock;
        }
    }
}