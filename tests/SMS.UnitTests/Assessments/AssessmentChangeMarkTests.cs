using FluentAssertions;
using Moq;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.Handlers;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SMS.UnitTests.Assessments
{
    public class ChangeMarkHandlerTests
    {
        [Fact]
        public async Task ChangeWithoutReason_ShouldBeRejected()
        {
            var engine = new Mock<IAssessmentEngine>();
            var handler = new ChangeMarkHandler(engine.Object,
                Mock.Of<SMS.Domain.Interfaces.ICurrentUserService>(),
                Mock.Of<Microsoft.Extensions.Logging.ILogger<ChangeMarkHandler>>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
                new ChangeMarkCommand { MarkId = Guid.NewGuid(), NewScore = 90m, Reason = "" },
                CancellationToken.None));
        }

        [Fact]
        public async Task ChangeWithReason_ShouldDelegateToEngineWithUser()
        {
            var engine = new Mock<IAssessmentEngine>();
            engine.Setup(x => x.UpdateMarkAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StudentAssessmentMark { Id = Guid.NewGuid(), Mark = 90m });

            var currentUser = new Mock<SMS.Domain.Interfaces.ICurrentUserService>();
            currentUser.Setup(x => x.Username).Returns("coordinator.test");

            var handler = new ChangeMarkHandler(engine.Object, currentUser.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ChangeMarkHandler>>());

            var markId = Guid.NewGuid();
            var result = await handler.Handle(new ChangeMarkCommand { MarkId = markId, NewScore = 90m, Reason = "Moderation adjustment" }, CancellationToken.None);

            engine.Verify(x => x.UpdateMarkAsync(markId, 90m, "Moderation adjustment", null, false, "coordinator.test", It.IsAny<CancellationToken>()), Times.Once);
            result.Should().NotBeNull();
        }
    }
}