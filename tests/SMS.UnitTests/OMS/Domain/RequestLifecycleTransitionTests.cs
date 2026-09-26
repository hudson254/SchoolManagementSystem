using FluentAssertions;
using SMS.Domain.Common;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.UnitTests.OMS.Domain
{
    /// <summary>
    /// Guards the request lifecycle transition table.
    ///
    /// <para>
    /// Production deployment found the table and the handlers disagreeing.
    /// <c>ApproveRequestCommand</c> accepts a request in <c>Assigned</c> status, but
    /// <c>RequestLifecycle.AllowedTransitions</c> did not list <c>Approved</c> as a
    /// target from <c>Assigned</c>. The domain therefore rejected a transition the
    /// handler advertised, which made the approve/complete path unreachable through the
    /// API and surfaced as an unmapped <c>InvalidOperationException</c> (HTTP 500)
    /// instead of a clean business-rule error.
    /// </para>
    ///
    /// <para>
    /// These tests pin the transitions the API actually exposes, and the terminal
    /// immutability guarantee.
    /// </para>
    /// </summary>
    public class RequestLifecycleTransitionTests
    {
        [Fact]
        public void Assigned_CanTransitionToApproved_SoApproveAndCompleteAreReachable()
        {
            RequestLifecycle.AllowedTransitions[RequestStatus.Assigned]
                .Should().Contain(RequestStatus.Approved);

            RequestLifecycle.CanTransition(RequestStatus.Assigned, RequestStatus.Approved)
                .Should().BeTrue("ApproveRequestCommand accepts an Assigned request");
        }

        [Fact]
        public void Approved_CanTransitionToCompleted_SoCompleteIsReachable()
        {
            RequestLifecycle.CanTransition(RequestStatus.Approved, RequestStatus.Completed)
                .Should().BeTrue("CompleteRequestCommand accepts an Approved request");
        }

        [Fact]
        public void Draft_CanTransitionToSubmitted_SoCreationAndSubmitWork()
        {
            RequestLifecycle.CanTransition(RequestStatus.Draft, RequestStatus.Submitted)
                .Should().BeTrue();
        }

        [Fact]
        public void PendingReview_CanTransitionToAssigned_AndBackViaReturned()
        {
            RequestLifecycle.CanTransition(RequestStatus.PendingReview, RequestStatus.Assigned)
                .Should().BeTrue();
            RequestLifecycle.CanTransition(RequestStatus.PendingReview, RequestStatus.Returned)
                .Should().BeTrue();
        }

        [Theory]
        [InlineData(RequestStatus.Completed)]
        [InlineData(RequestStatus.Rejected)]
        [InlineData(RequestStatus.Cancelled)]
        public void TerminalStatuses_HaveNoOutgoingTransitions(RequestStatus terminal)
        {
            RequestLifecycle.AllowedTransitions[terminal].Should().BeEmpty(
                "{0} is terminal and must be immutable", terminal);
            RequestLifecycle.IsTerminal(terminal).Should().BeTrue();
        }

        [Fact]
        public void CanTransition_RejectsTransitionThatIsNotInTheTable()
        {
            // PendingReview -> Approved was never a legal transition and must stay illegal.
            RequestLifecycle.CanTransition(RequestStatus.PendingReview, RequestStatus.Approved)
                .Should().BeFalse();
        }
    }
}
