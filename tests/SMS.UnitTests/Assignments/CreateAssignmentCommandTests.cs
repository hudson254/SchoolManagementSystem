using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Application.Features.Assignments.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Assignments
{
    public class CreateAssignmentCommandTests
    {
        private readonly CreateAssignmentCommandValidator _validator;
        private readonly Mock<IAssignmentRepository> _assignmentRepositoryMock;
        private readonly Mock<IUnitRepository> _unitRepositoryMock;
        private readonly Mock<ILecturerRepository> _lecturerRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public CreateAssignmentCommandTests()
        {
            _validator = new CreateAssignmentCommandValidator();
            _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
            _unitRepositoryMock = new Mock<IUnitRepository>();
            _lecturerRepositoryMock = new Mock<ILecturerRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new CreateAssignmentCommand
            {
                Title = "Data Structures Assignment 1",
                UnitId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                MaxScore = 100,
                Weight = 20,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new CreateAssignmentCommand
            {
                Title = "",
                UnitId = Guid.Empty,
                LecturerId = Guid.Empty,
                SemesterId = Guid.Empty,
                MaxScore = 0,
                Weight = 0,
                DueDate = DateTime.UtcNow.AddDays(-1),
                LatePenaltyPercent = 150
            };

            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Title);
            result.ShouldHaveValidationErrorFor(x => x.UnitId);
            result.ShouldHaveValidationErrorFor(x => x.LecturerId);
            result.ShouldHaveValidationErrorFor(x => x.SemesterId);
            result.ShouldHaveValidationErrorFor(x => x.MaxScore);
            result.ShouldHaveValidationErrorFor(x => x.Weight);
            result.ShouldHaveValidationErrorFor(x => x.DueDate);
            result.ShouldHaveValidationErrorFor(x => x.LatePenaltyPercent);
        }

        [Fact]
        public async Task Handle_WithNonExistentUnit_ShouldThrowNotFoundException()
        {
            var command = new CreateAssignmentCommand
            {
                Title = "Data Structures Assignment 1",
                UnitId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                MaxScore = 100,
                Weight = 20,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            _unitRepositoryMock
                .Setup(x => x.GetByIdAsync(command.UnitId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SMS.Domain.Entities.Unit?)null);

            var handler = new CreateAssignmentCommandHandler(
                _assignmentRepositoryMock.Object,
                _unitRepositoryMock.Object,
                _lecturerRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateAssignmentCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithNonExistentLecturer_ShouldThrowNotFoundException()
        {
            var unitId = Guid.NewGuid();
            var command = new CreateAssignmentCommand
            {
                Title = "Data Structures Assignment 1",
                UnitId = unitId,
                LecturerId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                MaxScore = 100,
                Weight = 20,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            var unit = new SMS.Domain.Entities.Unit
            {
                Id = unitId,
                Name = "Data Structures",
                Code = "CSC201"
            };

            _unitRepositoryMock
                .Setup(x => x.GetByIdAsync(command.UnitId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(unit);

            _lecturerRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Lecturer?)null);

            var handler = new CreateAssignmentCommandHandler(
                _assignmentRepositoryMock.Object,
                _unitRepositoryMock.Object,
                _lecturerRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateAssignmentCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldCreateAssignment()
        {
            var unitId = Guid.NewGuid();
            var lecturerId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();
            var dueDate = DateTime.UtcNow.AddDays(7);

            var command = new CreateAssignmentCommand
            {
                Title = "Data Structures Assignment 1",
                UnitId = unitId,
                LecturerId = lecturerId,
                SemesterId = semesterId,
                MaxScore = 100,
                Weight = 20,
                DueDate = dueDate,
                Instructions = "Complete all questions",
                AllowLateSubmission = true,
                LatePenaltyPercent = 10
            };

            var unit = new SMS.Domain.Entities.Unit
            {
                Id = unitId,
                Name = "Data Structures",
                Code = "CSC201"
            };

            var lecturer = new Lecturer
            {
                Id = lecturerId,
                EmployeeNumber = "LEC-001",
                User = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = "John",
                    LastName = "Smith"
                }
            };

            _unitRepositoryMock
                .Setup(x => x.GetByIdAsync(unitId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(unit);

            _lecturerRepositoryMock
                .Setup(x => x.GetByIdAsync(lecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lecturer);

            _assignmentRepositoryMock
                            .Setup(x => x.AddAsync(It.IsAny<Assignment>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((Assignment a, CancellationToken ct) => a);

            var handler = new CreateAssignmentCommandHandler(
                _assignmentRepositoryMock.Object,
                _unitRepositoryMock.Object,
                _lecturerRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateAssignmentCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.Title.Should().Be(command.Title);
            result.UnitId.Should().Be(unitId);
            result.LecturerId.Should().Be(lecturerId);
            result.SemesterId.Should().Be(semesterId);
            result.MaxScore.Should().Be(command.MaxScore);
            result.Weight.Should().Be(command.Weight);
            result.DueDate.Should().Be(dueDate);
            result.AllowLateSubmission.Should().BeTrue();
            result.LatePenaltyPercent.Should().Be(command.LatePenaltyPercent);
            result.Status.Should().Be("Published");

            _assignmentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Assignment>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
