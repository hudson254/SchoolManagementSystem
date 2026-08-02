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
    public class VacateHouseCommandTests
    {
        private readonly Mock<IAccommodationRepository> _repositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<VacateHouseHandler>> _loggerMock;
        private readonly VacateHouseHandler _handler;

        public VacateHouseCommandTests()
        {
            _repositoryMock = new Mock<IAccommodationRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
            _loggerMock = new Mock<ILogger<VacateHouseHandler>>();
            _handler = new VacateHouseHandler(
                _repositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_OccupiedHouse_ShouldVacateSuccessfully()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var occupantId = Guid.NewGuid();
            var command = new VacateHouseCommand { HouseId = houseId };

            var house = new House
            {
                Id = houseId,
                HouseNumber = "001",
                IsOccupied = true,
                OccupantId = occupantId,
                Status = HouseStatus.Occupied
            };

            var assignment = new AccommodationAssignment
            {
                StudentId = occupantId,
                HouseId = houseId,
                Status = "Active"
            };

            _repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);
            _repositoryMock.Setup(r => r.GetAssignmentByStudentAsync(occupantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            house.IsOccupied.Should().BeFalse();
            house.OccupantId.Should().BeNull();
            house.Status.Should().Be(HouseStatus.Vacant);
            assignment.Status.Should().Be("Vacated");
            _repositoryMock.Verify(r => r.UpdateHouseAsync(house, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAssignmentAsync(assignment, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_VacantHouse_ShouldThrowException()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var command = new VacateHouseCommand { HouseId = houseId };

            var house = new House
            {
                Id = houseId,
                HouseNumber = "001",
                IsOccupied = false,
                Status = HouseStatus.Vacant
            };

            _repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task Handle_HouseNotFound_ShouldThrowException()
        {
            // Arrange
            var command = new VacateHouseCommand { HouseId = Guid.NewGuid() };

            _repositoryMock.Setup(r => r.GetHouseByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((House)null);

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }
    }
}
