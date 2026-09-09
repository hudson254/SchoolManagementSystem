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
    public class AccommodationCapacityAndCheckInTests
    {
        private readonly Mock<IAccommodationRepository> _repositoryMock;
        private readonly Mock<ISemesterRepository> _semesterRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<AssignHouseHandler>> _assignLoggerMock;
        private readonly Mock<ILogger<CheckInHouseHandler>> _checkInLoggerMock;
        private readonly Mock<ILogger<CheckOutHouseHandler>> _checkOutLoggerMock;
        private readonly AssignHouseHandler _assignHandler;
        private readonly CheckInHouseHandler _checkInHandler;
        private readonly CheckOutHouseHandler _checkOutHandler;

        public AccommodationCapacityAndCheckInTests()
        {
            _repositoryMock = new Mock<IAccommodationRepository>();
            _semesterRepositoryMock = new Mock<ISemesterRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
            _assignLoggerMock = new Mock<ILogger<AssignHouseHandler>>();
            _checkInLoggerMock = new Mock<ILogger<CheckInHouseHandler>>();
            _checkOutLoggerMock = new Mock<ILogger<CheckOutHouseHandler>>();

            _assignHandler = new AssignHouseHandler(
                _repositoryMock.Object,
                _semesterRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                _assignLoggerMock.Object);

            _checkInHandler = new CheckInHouseHandler(
                _repositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                _checkInLoggerMock.Object);

            _checkOutHandler = new CheckOutHouseHandler(
                _repositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                _checkOutLoggerMock.Object);
        }

        [Fact]
        public async Task Assign_WhenCapacityReached_ShouldThrow()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var house = new House
            {
                Id = houseId,
                HouseNumber = "001",
                Status = HouseStatus.Occupied,
                IsAvailable = true,
                IsEnabled = true,
                Capacity = 2,
                OccupiedCount = 2
            };
            _repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);

            var command = new AssignHouseCommand
            {
                HouseId = houseId,
                StudentId = Guid.NewGuid(),
                OccupantType = OccupantType.Student,
                SemesterId = Guid.NewGuid()
            };

            // Act
            Func<Task> act = () => _assignHandler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*capacity*");
            _repositoryMock.Verify(r => r.AddAssignmentAsync(It.IsAny<AccommodationAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
        }
[Fact]
        public async Task Assign_WhenHouseUnavailable_ShouldThrowAndNotAssign()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var house = new House
            {
                Id = houseId,
                HouseNumber = "002",
                Status = HouseStatus.Unavailable,
                IsAvailable = false,
                IsEnabled = true,
                Capacity = 4,
                OccupiedCount = 0
            };
            _repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);

            var command = new AssignHouseCommand
            {
                HouseId = houseId,
                StudentId = Guid.NewGuid(),
                OccupantType = OccupantType.Student,
                SemesterId = Guid.NewGuid()
            };

            // Act
            Func<Task> act = () => _assignHandler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            _repositoryMock.Verify(r => r.AddAssignmentAsync(It.IsAny<AccommodationAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Assign_StudentThenLecturer_SameHouse_ShouldAllowMixedOccupancyUpToCapacity()
        {
            // Arrange
            var houseId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var lecturerId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();

            var house = new House
            {
                Id = houseId,
                HouseNumber = "003",
                Status = HouseStatus.Vacant,
                IsAvailable = true,
                IsEnabled = true,
                Capacity = 4,
                OccupiedCount = 0
            };
            _repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);
            _repositoryMock.Setup(r => r.GetAssignmentByOccupantAsync(It.IsAny<Guid>(), It.IsAny<OccupantType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AccommodationAssignment)null);

            // Act - assign student
            await _assignHandler.Handle(new AssignHouseCommand
            {
                HouseId = houseId,
                StudentId = studentId,
                OccupantType = OccupantType.Student,
                SemesterId = semesterId
            }, CancellationToken.None);

            // Act - assign lecturer to the same house (capacity still available)
            await _assignHandler.Handle(new AssignHouseCommand
            {
                HouseId = houseId,
                LecturerId = lecturerId,
                OccupantType = OccupantType.Lecturer,
                SemesterId = semesterId
            }, CancellationToken.None);

            // Assert - occupancy is shared between students and lecturers
            house.OccupiedCount.Should().Be(2);
            house.IsOccupied.Should().BeTrue();
            house.Status.Should().Be(HouseStatus.Occupied);
            _repositoryMock.Verify(r => r.AddAssignmentAsync(It.IsAny<AccommodationAssignment>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _repositoryMock.Verify(r => r.UpdateHouseAsync(house, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
[Fact]
        public async Task CheckIn_ActiveAssignment_SetsCheckInDateOnce_AndIsIdempotent()
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            var house = new House { Id = Guid.NewGuid(), OccupiedDate = null, HouseNumber = "004" };
            var assignment = new AccommodationAssignment
            {
                Id = assignmentId,
                HouseId = house.Id,
                Status = "Active",
                OccupantType = OccupantType.Student,
                StudentId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid()
            };
            _repositoryMock.Setup(r => r.GetAssignmentWithDetailsAsync(assignmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);
            _repositoryMock.Setup(r => r.GetHouseByIdAsync(house.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);

            // Act 1
            var result = await _checkInHandler.Handle(new CheckInHouseCommand { AssignmentId = assignmentId }, CancellationToken.None);

            // Act 2 (idempotent)
            var secondRun = await _checkInHandler.Handle(new CheckInHouseCommand { AssignmentId = assignmentId }, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            secondRun.Should().BeTrue();
            assignment.CheckInDate.Should().NotBeNull();
            assignment.MoveInDate.Should().NotBeNull();
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CheckIn_NonActiveAssignment_ShouldThrow()
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            var assignment = new AccommodationAssignment
            {
                Id = assignmentId,
                Status = "Vacated",
                HouseId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid()
            };
            _repositoryMock.Setup(r => r.GetAssignmentWithDetailsAsync(assignmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            // Act
            Func<Task> act = () => _checkInHandler.Handle(new CheckInHouseCommand { AssignmentId = assignmentId }, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task CheckOut_ActiveAssignment_ClosesAssignmentReleasesCapacity()
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            var occupantId = Guid.NewGuid();
            var houseId = Guid.NewGuid();
            var house = new House
            {
                Id = houseId,
                HouseNumber = "005",
                Capacity = 2,
                OccupiedCount = 1,
                IsOccupied = true,
                OccupantId = occupantId,
                OccupantType = OccupantType.Student,
                Status = HouseStatus.Occupied,
                SemesterId = Guid.NewGuid()
            };
            var assignment = new AccommodationAssignment
            {
                Id = assignmentId,
                HouseId = houseId,
                Status = "Active",
                OccupantType = OccupantType.Student,
                StudentId = occupantId,
                SemesterId = house.SemesterId!.Value
            };
            _repositoryMock.Setup(r => r.GetAssignmentWithDetailsAsync(assignmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);
            _repositoryMock.Setup(r => r.GetHouseByIdAsync(houseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(house);

            // Act
            var result = await _checkOutHandler.Handle(new CheckOutHouseCommand { AssignmentId = assignmentId }, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            assignment.Status.Should().Be("CheckedOut");
            assignment.CheckOutDate.Should().NotBeNull();
            assignment.MoveOutDate.Should().NotBeNull();
            house.OccupiedCount.Should().Be(0);
            house.IsOccupied.Should().BeFalse();
            house.Status.Should().Be(HouseStatus.Vacant);
            house.OccupantId.Should().BeNull();
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CheckOut_AlreadyCheckedOut_ShouldThrow()
        {
            // Arrange
            var assignmentId = Guid.NewGuid();
            var assignment = new AccommodationAssignment
            {
                Id = assignmentId,
                Status = "CheckedOut",
                HouseId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid()
            };
            _repositoryMock.Setup(r => r.GetAssignmentWithDetailsAsync(assignmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            // Act
            Func<Task> act = () => _checkOutHandler.Handle(new CheckOutHouseCommand { AssignmentId = assignmentId }, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();
        }
    }
}