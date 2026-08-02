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
    public class AssignHouseCommandTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IAccommodationRepository> _repositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<AssignHouseHandler>> _loggerMock;
        private readonly AssignHouseHandler _handler;

        public AssignHouseCommandTests()
        {
            _fixture = new Fixture();
            _repositoryMock = new Mock<IAccommodationRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
            _loggerMock = new Mock<ILogger<AssignHouseHandler>>();
            _handler = new AssignHouseHandler(
                _repositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidAssignment_ShouldAssignHouse()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();

            var command = new AssignHouseCommand
            {
                HouseId = houseId,
                StudentId = studentId,
                SemesterId = semesterId
            };

            var house = new House
            {
                Id = houseId,
                HouseNumber = "001",
                Status = HouseStatus.Vacant,
                IsOccupied = false,
                IsAvailable = true,
                IsEnabled = true,
                LaneId = Guid.NewGuid()
            };

            _repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);

            _repositoryMock.Setup(r => r.GetAssignmentByStudentAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AccommodationAssignment)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            _repositoryMock.Verify(r => r.AddAssignmentAsync(It.IsAny<AccommodationAssignment>(), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.UpdateHouseAsync(It.IsAny<House>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            house.IsOccupied.Should().BeTrue();
            house.OccupantId.Should().Be(studentId);
            house.Status.Should().Be(HouseStatus.Occupied);
        }

        [Fact]
        public async Task Handle_AlreadyOccupiedHouse_ShouldThrowException()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var command = new AssignHouseCommand
            {
                HouseId = houseId,
                StudentId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid()
            };

            var house = new House
            {
                Id = houseId,
                HouseNumber = "001",
                IsOccupied = true,
                IsAvailable = true,
                IsEnabled = true
            };

            _repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
            _repositoryMock.Verify(r => r.AddAssignmentAsync(It.IsAny<AccommodationAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_StudentAlreadyHasAssignment_ShouldThrowException()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var command = new AssignHouseCommand
            {
                HouseId = Guid.NewGuid(),
                StudentId = studentId,
                SemesterId = Guid.NewGuid()
            };

            var house = new House
            {
                Id = Guid.NewGuid(),
                HouseNumber = "001",
                IsOccupied = false,
                IsAvailable = true,
                IsEnabled = true
            };

            var existingAssignment = new AccommodationAssignment
            {
                StudentId = studentId,
                Status = "Active"
            };

            _repositoryMock.Setup(r => r.GetHouseByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);
            _repositoryMock.Setup(r => r.GetAssignmentByStudentAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingAssignment);

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }
    }
}
