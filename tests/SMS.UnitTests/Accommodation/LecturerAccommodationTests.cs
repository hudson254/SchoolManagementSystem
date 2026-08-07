using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Features.Accommodation.Commands;
using SMS.Application.Features.Accommodation.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Accommodation
{
    public class LecturerAccommodationTests
    {
        private readonly Mock<IAccommodationRepository> _repositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<AssignHouseHandler>> _loggerMock;
        private readonly AssignHouseHandler _handler;

        public LecturerAccommodationTests()
        {
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
        public async Task AssignLecturerToHouse_ShouldSetOccupantTypeToLecturer()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var lecturerId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();

            var command = new AssignHouseCommand
            {
                HouseId = houseId,
                LecturerId = lecturerId,
                OccupantType = OccupantType.Lecturer,
                SemesterId = semesterId
            };

            var house = new House
            {
                Id = houseId,
                HouseNumber = "L-001",
                Status = HouseStatus.Vacant,
                IsOccupied = false,
                IsAvailable = true,
                IsEnabled = true,
                LaneId = Guid.NewGuid()
            };

            _repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);
            _repositoryMock.Setup(r => r.GetAssignmentByOccupantAsync(lecturerId, OccupantType.Lecturer, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AccommodationAssignment)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            house.OccupantId.Should().Be(lecturerId);
            house.OccupantType.Should().Be(OccupantType.Lecturer);
            house.Status.Should().Be(HouseStatus.Occupied);
            _repositoryMock.Verify(r => r.AddAssignmentAsync(
                It.Is<AccommodationAssignment>(a => a.LecturerId == lecturerId && a.OccupantType == OccupantType.Lecturer),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AssignLecturer_WhenLecturerAlreadyHasActiveAssignment_ShouldThrow()
        {
            // Arrange
            var lecturerId = Guid.NewGuid();
            var command = new AssignHouseCommand
            {
                HouseId = Guid.NewGuid(),
                LecturerId = lecturerId,
                OccupantType = OccupantType.Lecturer,
                SemesterId = Guid.NewGuid()
            };

            var house = new House
            {
                Id = Guid.NewGuid(),
                HouseNumber = "L-002",
                Status = HouseStatus.Vacant,
                IsOccupied = false,
                IsAvailable = true,
                IsEnabled = true
            };

            var existing = new AccommodationAssignment
            {
                LecturerId = lecturerId,
                OccupantType = OccupantType.Lecturer,
                Status = "Active"
            };

            _repositoryMock.Setup(r => r.GetHouseByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);
            _repositoryMock.Setup(r => r.GetAssignmentByOccupantAsync(lecturerId, OccupantType.Lecturer, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
            _repositoryMock.Verify(r => r.AddAssignmentAsync(
                It.IsAny<AccommodationAssignment>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetLecturerAssignment_ShouldReturnLecturerDetails()
        {
            // Arrange
            var lecturerId = Guid.NewGuid();
            var lecturer = new Lecturer
            {
                Id = lecturerId,
                FirstName = "Jane",
                LastName = "Smith",
                EmployeeNumber = "EMP-002"
            };

            var assignment = new AccommodationAssignment
            {
                Id = Guid.NewGuid(),
                LecturerId = lecturerId,
                OccupantType = OccupantType.Lecturer,
                Status = "Active",
                AssignmentDate = DateTime.UtcNow,
                SemesterId = Guid.NewGuid(),
                Lecturer = lecturer
            };

            var lecturerRepositoryMock = new Mock<ILecturerRepository>();
            lecturerRepositoryMock.Setup(r => r.GetByIdAsync(lecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lecturer);

            var accommodationRepositoryMock = new Mock<IAccommodationRepository>();
            accommodationRepositoryMock.Setup(r => r.GetAssignmentByLecturerAsync(lecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            var loggerMock = new Mock<ILogger<GetLecturerAssignmentQueryHandler>>();
            var handler = new GetLecturerAssignmentQueryHandler(
                accommodationRepositoryMock.Object,
                lecturerRepositoryMock.Object,
                loggerMock.Object);

            var query = new GetLecturerAssignmentQuery { LecturerId = lecturerId };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.LecturerId.Should().Be(lecturerId);
            result.OccupantType.Should().Be(OccupantType.Lecturer);
            result.LecturerName.Should().Be("Jane Smith");
            result.EmployeeNumber.Should().Be("EMP-002");
        }

        [Fact]
        public async Task GetLecturerAssignment_LecturerNotFound_ShouldThrow()
        {
            // Arrange
            var lecturerId = Guid.NewGuid();
            var lecturerRepositoryMock = new Mock<ILecturerRepository>();
            lecturerRepositoryMock.Setup(r => r.GetByIdAsync(lecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Lecturer)null);

            var accommodationRepositoryMock = new Mock<IAccommodationRepository>();
            var loggerMock = new Mock<ILogger<GetLecturerAssignmentQueryHandler>>();

            var handler = new GetLecturerAssignmentQueryHandler(
                accommodationRepositoryMock.Object,
                lecturerRepositoryMock.Object,
                loggerMock.Object);

            var query = new GetLecturerAssignmentQuery { LecturerId = lecturerId };

            // Act
            Func<Task> act = () => handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }
    }
}
