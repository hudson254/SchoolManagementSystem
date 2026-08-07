using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Exceptions;
using SMS.Application.Features.Accommodation.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Accommodation
{
    public class ReassignHouseCommandTests
    {
        private readonly Mock<IAccommodationRepository> _repositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<ReassignHouseHandler>> _loggerMock;
        private readonly ReassignHouseHandler _handler;

        public ReassignHouseCommandTests()
        {
            _repositoryMock = new Mock<IAccommodationRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
            _loggerMock = new Mock<ILogger<ReassignHouseHandler>>();
            _handler = new ReassignHouseHandler(
                _repositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidReassignment_ShouldTransferStudent()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var oldHouseId = Guid.NewGuid();
            var newHouseId = Guid.NewGuid();
            var laneId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();

            var command = new ReassignHouseCommand
            {
                StudentId = studentId,
                OccupantType = OccupantType.Student,
                NewHouseId = newHouseId
            };

            var currentAssignment = new AccommodationAssignment
            {
                StudentId = studentId,
                OccupantType = OccupantType.Student,
                HouseId = oldHouseId,
                LaneId = laneId,
                SemesterId = semesterId,
                Status = "Active"
            };

            var oldHouse = new House
            {
                Id = oldHouseId,
                HouseNumber = "001",
                IsOccupied = true,
                OccupantId = studentId,
                Status = HouseStatus.Occupied
            };

            var newHouse = new House
            {
                Id = newHouseId,
                HouseNumber = "002",
                LaneId = laneId,
                IsOccupied = false,
                IsAvailable = true,
                IsEnabled = true,
                Status = HouseStatus.Vacant
            };

            _repositoryMock.Setup(r => r.GetAssignmentByOccupantAsync(studentId, OccupantType.Student, It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentAssignment);
            _repositoryMock.Setup(r => r.GetHouseByIdAsync(oldHouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(oldHouse);
            _repositoryMock.Setup(r => r.GetHouseByIdAsync(newHouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(newHouse);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            oldHouse.IsOccupied.Should().BeFalse();
            oldHouse.OccupantId.Should().BeNull();
            oldHouse.Status.Should().Be(HouseStatus.Vacant);
            newHouse.IsOccupied.Should().BeTrue();
            newHouse.OccupantId.Should().Be(studentId);
            newHouse.Status.Should().Be(HouseStatus.Occupied);
            currentAssignment.Status.Should().Be("Completed");
            _repositoryMock.Verify(r => r.AddAssignmentAsync(It.IsAny<AccommodationAssignment>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NoActiveAssignment_ShouldThrowException()
        {
            // Arrange
            var command = new ReassignHouseCommand
            {
                StudentId = Guid.NewGuid(),
                OccupantType = OccupantType.Student,
                NewHouseId = Guid.NewGuid()
            };

            _repositoryMock.Setup(r => r.GetAssignmentByOccupantAsync(It.IsAny<Guid>(), OccupantType.Student, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AccommodationAssignment)null);

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_NewHouseOccupied_ShouldThrowException()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var oldHouseId = Guid.NewGuid();
            var newHouseId = Guid.NewGuid();

            var command = new ReassignHouseCommand
            {
                StudentId = studentId,
                OccupantType = OccupantType.Student,
                NewHouseId = newHouseId
            };

            var currentAssignment = new AccommodationAssignment
            {
                StudentId = studentId,
                OccupantType = OccupantType.Student,
                HouseId = oldHouseId,
                Status = "Active"
            };

            var oldHouse = new House { Id = oldHouseId, HouseNumber = "001", IsOccupied = true };
            var newHouse = new House
            {
                Id = newHouseId,
                HouseNumber = "002",
                IsOccupied = true,
                IsAvailable = false,
                IsEnabled = true,
                Status = HouseStatus.Occupied
            };

            _repositoryMock.Setup(r => r.GetAssignmentByOccupantAsync(studentId, OccupantType.Student, It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentAssignment);
            _repositoryMock.Setup(r => r.GetHouseByIdAsync(oldHouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(oldHouse);
            _repositoryMock.Setup(r => r.GetHouseByIdAsync(newHouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(newHouse);

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();
        }
    }
}
