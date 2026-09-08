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
    public class EnterMarkHandlerTests
    {
        private static Mock<IStudentAssessmentMarkRepository> MarkRepo()
        {
            var markRepo = new Mock<IStudentAssessmentMarkRepository>();
            markRepo.Setup(x => x.GetByAssessmentAndStudentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StudentAssessmentMark?)null);
            return markRepo;
        }

        [Fact]
        public async Task ScoreOutsideAssessmentMaximum_ShouldReject()
        {
            var engine = new Mock<IAssessmentEngine>();
            var repo = new Mock<IAssessmentRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Assessment { Title = "A1", MaxScore = 100m });

            var handler = new EnterMarkHandler(engine.Object, repo.Object, MarkRepo().Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<EnterMarkHandler>>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
                new EnterMarkCommand { AssessmentId = Guid.NewGuid(), StudentId = Guid.NewGuid(), Score = 101m, MaxScore = 100m, IsDraft = false },
                CancellationToken.None));
        }

        [Fact]
        public async Task DuplicateMark_ShouldThrowConflict()
        {
            var engine = new Mock<IAssessmentEngine>();
            var repo = new Mock<IAssessmentRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Assessment { Title = "A1", MaxScore = 100m });
            var markRepo = new Mock<IStudentAssessmentMarkRepository>();
            markRepo.Setup(x => x.GetByAssessmentAndStudentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StudentAssessmentMark { Id = Guid.NewGuid() });

            var handler = new EnterMarkHandler(engine.Object, repo.Object, markRepo.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<EnterMarkHandler>>());

            await Assert.ThrowsAsync<SMS.Application.Exceptions.ConflictException>(() => handler.Handle(
                new EnterMarkCommand { AssessmentId = Guid.NewGuid(), StudentId = Guid.NewGuid(), Score = 85m, MaxScore = 100m, IsDraft = false },
                CancellationToken.None));
        }

        [Fact]
        public async Task FinalMark_ShouldDelegateToEngine()
        {
            var engine = new Mock<IAssessmentEngine>();
            var mark = new StudentAssessmentMark { Id = Guid.NewGuid(), AssessmentId = Guid.NewGuid(), StudentId = Guid.NewGuid(), Mark = 85m, Percentage = 85m, WeightedScore = 8.5m, IsDraft = false };
            engine.Setup(x => x.CalculateAndSaveMarkAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mark);

            var repo = new Mock<IAssessmentRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Assessment { Title = "A1", MaxScore = 100m, Weight = 10m });

            var handler = new EnterMarkHandler(engine.Object, repo.Object, MarkRepo().Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<EnterMarkHandler>>());

            var result = await handler.Handle(
                new EnterMarkCommand { AssessmentId = mark.AssessmentId, StudentId = mark.StudentId, Score = 85m, MaxScore = 100m, IsDraft = false },
                CancellationToken.None);

            engine.Verify(x => x.CalculateAndSaveMarkAsync(mark.AssessmentId, mark.StudentId, 85m, It.IsAny<CancellationToken>()), Times.Once);
            result.Score.Should().Be(85m);
        }

        [Fact]
        public async Task DraftMark_ShouldUseDraftPathOnEngine()
        {
            var engine = new Mock<IAssessmentEngine>();
            var mark = new StudentAssessmentMark { Id = Guid.NewGuid(), AssessmentId = Guid.NewGuid(), StudentId = Guid.NewGuid(), Mark = 60m, Percentage = 60m, WeightedScore = 6m, IsDraft = true };
            engine.Setup(x => x.SaveDraftMarkAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mark);

            var repo = new Mock<IAssessmentRepository>();
            repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Assessment { Title = "A1", MaxScore = 100m, Weight = 10m });

            var handler = new EnterMarkHandler(engine.Object, repo.Object, MarkRepo().Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<EnterMarkHandler>>());

            var result = await handler.Handle(
                new EnterMarkCommand { AssessmentId = mark.AssessmentId, StudentId = mark.StudentId, Score = 60m, MaxScore = 100m, IsDraft = true },
                CancellationToken.None);

            engine.Verify(x => x.SaveDraftMarkAsync(mark.AssessmentId, mark.StudentId, 60m, null, It.IsAny<CancellationToken>()), Times.Once);
            result.IsDraft.Should().BeTrue();
        }
    }
}