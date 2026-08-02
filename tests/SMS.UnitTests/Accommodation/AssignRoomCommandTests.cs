using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Application.Features.Accommodation.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Accommodation
{
    public class AssignRoomCommandTests
    {
        private readonly AssignRoomCommandValidator _validator;
        private readonly Mock<IAccommodationRepository> _accommodationRepositoryMock;
        private readonly Mock<IStudentRepository> _studentRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public AssignRoomCommandTests()
        {
            _validator = new AssignRoomCommandValidator();
            _accommodationRepositoryMock = new Mock<IAccommodationRepository>();
            _studentRepositoryMock = new Mock<IStudentRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new AssignRoomCommand
            {
                RoomId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid()
            };

            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new AssignRoomCommand
            {
                RoomId = Guid.Empty,
                StudentId = Guid.Empty,
                SemesterId = Guid.Empty
            };

            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.RoomId);
            result.ShouldHaveValidationErrorFor(x => x.StudentId);
            result.ShouldHaveValidationErrorFor(x => x.SemesterId);
        }

        [Fact]
        public async Task Handle_WithNonExistentRoom_ShouldThrowNotFoundException()
        {
            var command = new AssignRoomCommand
            {
                RoomId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid()
            };

            _accommodationRepositoryMock
                .Setup(x => x.GetRoomWithDetailsAsync(command.RoomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Room?)null);

            var handler = new AssignRoomCommandHandler(
                _accommodationRepositoryMock.Object,
                _studentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignRoomCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithUnavailableRoom_ShouldThrowBusinessRuleException()
        {
            var command = new AssignRoomCommand
            {
                RoomId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid()
            };

            var room = new Room
            {
                Id = command.RoomId,
                RoomNumber = "A101",
                IsAvailable = false
            };
            // Set OccupiedCount to simulate IsOccupied = true
            typeof(Room).GetProperty("OccupiedCount")?.SetValue(room, 1);

            _accommodationRepositoryMock
                .Setup(x => x.GetRoomWithDetailsAsync(command.RoomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            var handler = new AssignRoomCommandHandler(
                _accommodationRepositoryMock.Object,
                _studentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignRoomCommandHandler>>());

            await Assert.ThrowsAsync<BusinessRuleException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithNonExistentStudent_ShouldThrowNotFoundException()
        {
            var command = new AssignRoomCommand
            {
                RoomId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid()
            };

            var room = new Room
            {
                Id = command.RoomId,
                RoomNumber = "A101",
                IsAvailable = true
            };
            // Set OccupiedCount to simulate IsOccupied = false
            typeof(Room).GetProperty("OccupiedCount")?.SetValue(room, 0);

            _accommodationRepositoryMock
                .Setup(x => x.GetRoomWithDetailsAsync(command.RoomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            _studentRepositoryMock
                .Setup(x => x.GetStudentWithDetailsAsync(command.StudentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Student?)null);

            var handler = new AssignRoomCommandHandler(
                _accommodationRepositoryMock.Object,
                _studentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignRoomCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithExistingAssignment_ShouldThrowConflictException()
        {
            var roomId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();

            var command = new AssignRoomCommand
            {
                RoomId = roomId,
                StudentId = studentId,
                SemesterId = semesterId
            };

            var room = new Room
            {
                Id = roomId,
                RoomNumber = "A101",
                IsAvailable = true
            };
            // Set OccupiedCount to simulate IsOccupied = false
            typeof(Room).GetProperty("OccupiedCount")?.SetValue(room, 0);

            var student = new Student
            {
                Id = studentId,
                StudentNumber = "STU-001",
                User = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = "John",
                    LastName = "Doe"
                }
            };

            var existingAssignment = new AccommodationAssignment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                SemesterId = semesterId,
                Status = "Active"
            };

            _accommodationRepositoryMock
                .Setup(x => x.GetRoomWithDetailsAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            _studentRepositoryMock
                .Setup(x => x.GetStudentWithDetailsAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(student);

            _accommodationRepositoryMock
                .Setup(x => x.GetAssignmentByStudentAndSemesterAsync(studentId, semesterId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingAssignment);

            var handler = new AssignRoomCommandHandler(
                _accommodationRepositoryMock.Object,
                _studentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignRoomCommandHandler>>());

            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldAssignRoom()
        {
            var roomId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();

            var command = new AssignRoomCommand
            {
                RoomId = roomId,
                StudentId = studentId,
                SemesterId = semesterId,
                Remarks = "First assignment"
            };

            var room = new Room
            {
                Id = roomId,
                RoomNumber = "A101",
                IsAvailable = true
            };
            // Set OccupiedCount to simulate IsOccupied = false
            typeof(Room).GetProperty("OccupiedCount")?.SetValue(room, 0);

            var student = new Student
            {
                Id = studentId,
                StudentNumber = "STU-001",
                User = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = "John",
                    LastName = "Doe"
                }
            };

            _accommodationRepositoryMock
                .Setup(x => x.GetRoomWithDetailsAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            _studentRepositoryMock
                .Setup(x => x.GetStudentWithDetailsAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(student);

            _accommodationRepositoryMock
                .Setup(x => x.GetAssignmentByStudentAndSemesterAsync(studentId, semesterId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AccommodationAssignment?)null);

            _accommodationRepositoryMock
                            .Setup(x => x.AddAssignmentAsync(It.IsAny<AccommodationAssignment>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((AccommodationAssignment a, CancellationToken ct) => a);

            var handler = new AssignRoomCommandHandler(
                _accommodationRepositoryMock.Object,
                _studentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignRoomCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.StudentId.Should().Be(studentId);
            result.RoomId.Should().Be(roomId);
            result.SemesterId.Should().Be(semesterId);
            result.Status.Should().Be("Active");
            result.StudentName.Should().Be("John Doe");
            result.RoomNumber.Should().Be("A101");

            _accommodationRepositoryMock.Verify(x => x.AddAssignmentAsync(It.IsAny<AccommodationAssignment>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
