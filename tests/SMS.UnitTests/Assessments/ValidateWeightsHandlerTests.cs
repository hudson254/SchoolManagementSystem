using FluentAssertions;
using Moq;
using SMS.Application.Features.Assessments.Handlers;
using SMS.Application.Features.Assessments.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SMS.UnitTests.Assessments
{
    public class ValidateWeightsHandlerTests
    {
        private static IAssessmentRepository Repo(params Assessment[] assessments)
        {
            var repo = new Mock<IAssessmentRepository>();
            repo.Setup(x => x.GetByUnitAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(assessments.ToList());
            return repo.Object;
        }

        [Fact]
        public async Task WeightsTotallingExactlyOneHundred_ShouldBeValid()
        {
            var handler = new ValidateWeightsHandler(Repo(
                new Assessment { Title = "Assignment 1", Weight = 10m, IsActive = true },
                new Assessment { Title = "Assignment 2", Weight = 15m, IsActive = true },
                new Assessment { Title = "CAT", Weight = 15m, IsActive = true },
                new Assessment { Title = "Project", Weight = 20m, IsActive = true },
                new Assessment { Title = "Final Exam", Weight = 40m, IsActive = true }));

            var result = await handler.Handle(new ValidateWeightsQuery { UnitId = Guid.NewGuid() }, CancellationToken.None);

            result.IsValid.Should().BeTrue();
            result.TotalWeight.Should().Be(100m);
        }

        [Fact]
        public async Task WeightsTotallingNinetyNinePointNineNine_ShouldBeInvalid()
        {
            var handler = new ValidateWeightsHandler(Repo(
                new Assessment { Title = "A1", Weight = 99.99m, IsActive = true }));

            var result = await handler.Handle(new ValidateWeightsQuery { UnitId = Guid.NewGuid() }, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task WeightsTotallingOneHundredPointZeroOne_ShouldBeInvalid()
        {
            var handler = new ValidateWeightsHandler(Repo(
                new Assessment { Title = "A1", Weight = 100.01m, IsActive = true }));

            var result = await handler.Handle(new ValidateWeightsQuery { UnitId = Guid.NewGuid() }, CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task NegativeWeight_ShouldBeInvalid()
        {
            var handler = new ValidateWeightsHandler(Repo(
                new Assessment { Title = "A1", Weight = -5m, IsActive = true }));

            var result = await handler.Handle(new ValidateWeightsQuery { UnitId = Guid.NewGuid() }, CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task NoAssessments_TotalWeightZero_ShouldBeInvalid()
        {
            var handler = new ValidateWeightsHandler(Repo());

            var result = await handler.Handle(new ValidateWeightsQuery { UnitId = Guid.NewGuid() }, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.TotalWeight.Should().Be(0m);
        }
    }
}
