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
    public class HouseManagementCommandTests
    {
        private readonly IFixture _fixture;

        public HouseManagementCommandTests()
        {
            _fixture = new Fixture();
        }

        #region CreateHouseCommand Tests

        [Fact]
        public async Task CreateHouseHandler_ValidCommand_ShouldCreateHouses()
        {
            // Arrange
            var laneId = Guid.NewGuid();
            var repositoryMock = new Mock<IAccommodationRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var auditServiceMock = new Mock<IAuditService>();
            var loggerMock = new Mock<ILogger<CreateHouseHandler>>();

            var handler = new CreateHouseHandler(
                repositoryMock.Object,
                unitOfWorkMock.Object,
                auditServiceMock.Object,
                loggerMock.Object);

            var command = new CreateHouseCommand
            {
                LaneId = laneId,
                NumberOfHouses = 5,
                NumberingFormat = "D3",
                StartingHouseNumber = 1
            };

            var lane = new Lane { Id = laneId, LaneName = "Test Lane", IsActive = true };

            repositoryMock.Setup(r => r.GetLaneByIdAsync(laneId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lane);
            repositoryMock.Setup(r => r.GetNextHouseNumberSequenceAsync(laneId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().HaveCount(5);
            repositoryMock.Verify(r => r.AddHousesRangeAsync(
                It.Is<IEnumerable<House>>(h => h.Count() == 5),
                It.IsAny<CancellationToken>()), Times.Once);
            unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateHouseHandler_InactiveLane_ShouldThrowException()
        {
            // Arrange
            var laneId = Guid.NewGuid();
            var repositoryMock = new Mock<IAccommodationRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var auditServiceMock = new Mock<IAuditService>();
            var loggerMock = new Mock<ILogger<CreateHouseHandler>>();

            var handler = new CreateHouseHandler(
                repositoryMock.Object,
                unitOfWorkMock.Object,
                auditServiceMock.Object,
                loggerMock.Object);

            var command = new CreateHouseCommand { LaneId = laneId, NumberOfHouses = 1 };
            var lane = new Lane { Id = laneId, LaneName = "Test Lane", IsActive = false };

            repositoryMock.Setup(r => r.GetLaneByIdAsync(laneId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lane);

            // Act
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public void CreateHouseValidator_InvalidLaneId_ShouldFail()
        {
            var validator = new CreateHouseCommandValidator();
            var command = new CreateHouseCommand { LaneId = Guid.Empty, NumberOfHouses = 1 };
            var result = validator.Validate(command);
            result.IsValid.Should().BeFalse();
        }

        #endregion

        #region SetHouseMaintenanceCommand Tests

        [Fact]
        public async Task SetMaintenanceHandler_ShouldMarkHouseAsMaintenance()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var repositoryMock = new Mock<IAccommodationRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var auditServiceMock = new Mock<IAuditService>();
            var loggerMock = new Mock<ILogger<SetHouseMaintenanceHandler>>();

            var handler = new SetHouseMaintenanceHandler(
                repositoryMock.Object,
                unitOfWorkMock.Object,
                auditServiceMock.Object,
                loggerMock.Object);

            var command = new SetHouseMaintenanceCommand
            {
                HouseId = houseId,
                IsUnderMaintenance = true,
                Notes = "Plumbing repair"
            };

            var house = new House
            {
                Id = houseId,
                HouseNumber = "001",
                Status = HouseStatus.Vacant,
                IsOccupied = false,
                IsAvailable = true
            };

            repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            house.Status.Should().Be(HouseStatus.Maintenance);
            house.IsAvailable.Should().BeFalse();
            house.Notes.Should().Be("Plumbing repair");
            repositoryMock.Verify(r => r.UpdateHouseAsync(house, It.IsAny<CancellationToken>()), Times.Once);
            unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetMaintenanceHandler_OccupiedHouse_ShouldThrowException()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var repositoryMock = new Mock<IAccommodationRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var auditServiceMock = new Mock<IAuditService>();
            var loggerMock = new Mock<ILogger<SetHouseMaintenanceHandler>>();

            var handler = new SetHouseMaintenanceHandler(
                repositoryMock.Object,
                unitOfWorkMock.Object,
                auditServiceMock.Object,
                loggerMock.Object);

            var command = new SetHouseMaintenanceCommand
            {
                HouseId = houseId,
                IsUnderMaintenance = true
            };

            var house = new House
            {
                Id = houseId,
                HouseNumber = "001",
                IsOccupied = true,
                Status = HouseStatus.Occupied
            };

            repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);

            // Act
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        #endregion

        #region DeleteHouseCommand Tests

        [Fact]
        public async Task DeleteHouseHandler_HouseWithoutOccupant_ShouldDelete()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var repositoryMock = new Mock<IAccommodationRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var auditServiceMock = new Mock<IAuditService>();
            var loggerMock = new Mock<ILogger<DeleteHouseHandler>>();

            var handler = new DeleteHouseHandler(
                repositoryMock.Object,
                unitOfWorkMock.Object,
                auditServiceMock.Object,
                loggerMock.Object);

            var command = new DeleteHouseCommand { Id = houseId };
            var house = new House { Id = houseId, HouseNumber = "001", IsOccupied = false };

            repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            repositoryMock.Verify(r => r.DeleteHouseAsync(houseId, It.IsAny<CancellationToken>()), Times.Once);
            unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteHouseHandler_HouseWithOccupant_ShouldThrowException()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var repositoryMock = new Mock<IAccommodationRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var auditServiceMock = new Mock<IAuditService>();
            var loggerMock = new Mock<ILogger<DeleteHouseHandler>>();

            var handler = new DeleteHouseHandler(
                repositoryMock.Object,
                unitOfWorkMock.Object,
                auditServiceMock.Object,
                loggerMock.Object);

            var command = new DeleteHouseCommand { Id = houseId };
            var house = new House
            {
                Id = houseId,
                HouseNumber = "001",
                IsOccupied = true,
                OccupantId = Guid.NewGuid()
            };

            repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);

            // Act
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        #endregion
    }
}
