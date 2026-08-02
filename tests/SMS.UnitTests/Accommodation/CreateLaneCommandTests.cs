using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Features.Accommodation.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Accommodation
{
    public class CreateLaneCommandTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IAccommodationRepository> _repositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<CreateLaneHandler>> _loggerMock;
        private readonly CreateLaneHandler _handler;

        public CreateLaneCommandTests()
        {
            _fixture = new Fixture();
            _repositoryMock = new Mock<IAccommodationRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
            _loggerMock = new Mock<ILogger<CreateLaneHandler>>();
            _handler = new CreateLaneHandler(
                _repositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateLaneWithHouses()
        {
            // Arrange
            var command = new CreateLaneCommand
            {
                LaneName = "East Lane",
                Description = "Eastern wing lane",
                NumberOfHouses = 10,
                NumberingFormat = "D3",
                StartingHouseNumber = 1
            };

            _repositoryMock.Setup(r => r.LaneExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _repositoryMock.Setup(r => r.AddLaneAsync(It.IsAny<Lane>(), It.IsAny<CancellationToken>()))
                .Callback<Lane, CancellationToken>((lane, _) => { lane.GetType().GetProperty("Id")?.SetValue(lane, Guid.NewGuid()); });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            _repositoryMock.Verify(r => r.AddLaneAsync(It.IsAny<Lane>(), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddHousesRangeAsync(It.IsAny<IEnumerable<House>>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            _auditServiceMock.Verify(a => a.LogAsync("Create", "Lane", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DuplicateLaneName_ShouldThrowConflictException()
        {
            // Arrange
            var command = new CreateLaneCommand
            {
                LaneName = "East Lane",
                NumberOfHouses = 5
            };

            _repositoryMock.Setup(r => r.LaneExistsAsync(command.LaneName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
            _repositoryMock.Verify(r => r.AddLaneAsync(It.IsAny<Lane>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ZeroHouses_ShouldStillCreateLane()
        {
            // Arrange
            var command = new CreateLaneCommand
            {
                LaneName = "Empty Lane",
                NumberOfHouses = 0
            };

            _repositoryMock.Setup(r => r.LaneExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            _repositoryMock.Verify(r => r.AddHousesRangeAsync(It.IsAny<IEnumerable<House>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Validator_EmptyLaneName_ShouldFail()
        {
            // Arrange
            var validator = new CreateLaneCommandValidator();
            var command = new CreateLaneCommand { LaneName = "", NumberOfHouses = 5 };

            // Act
            var result = validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "LaneName");
        }

        [Fact]
        public void Validator_TooManyHouses_ShouldFail()
        {
            // Arrange
            var validator = new CreateLaneCommandValidator();
            var command = new CreateLaneCommand { LaneName = "Lane A", NumberOfHouses = 501 };

            // Act
            var result = validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "NumberOfHouses");
        }
    }
}
