using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Features.Classes.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Classes
{
    /// <summary>
    /// Regression tests for the new Class management workflow (coordinator can
    /// create and modify classes using the existing Class domain entity).
    /// </summary>
    public class CreateClassCommandTests
    {
        private readonly CreateClassCommandValidator _validator;
        private readonly Mock<IClassRepository> _classRepositoryMock;
        private readonly Mock<IUnitRepository> _unitRepositoryMock;
        private readonly Mock<ILecturerRepository> _lecturerRepositoryMock;
        private readonly Mock<ISemesterRepository> _semesterRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public CreateClassCommandTests()
        {
            _validator = new CreateClassCommandValidator();
            _classRepositoryMock = new Mock<IClassRepository>();
            _unitRepositoryMock = new Mock<IUnitRepository>();
            _lecturerRepositoryMock = new Mock<ILecturerRepository>();
            _semesterRepositoryMock = new Mock<ISemesterRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = ValidCommand();

            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new CreateClassCommand
            {
                Name = "",
                Code = "",
                UnitId = Guid.Empty,
                LecturerId = Guid.Empty,
                SemesterId = Guid.Empty
            };

            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Name);
            result.ShouldHaveValidationErrorFor(x => x.Code);
            result.ShouldHaveValidationErrorFor(x => x.UnitId);
            result.ShouldHaveValidationErrorFor(x => x.LecturerId);
            result.ShouldHaveValidationErrorFor(x => x.SemesterId);
        }

        [Fact]
        public void InvalidScheduleDay_ShouldHaveValidationError()
        {
            var command = ValidCommand();
            command.ScheduleDay = "Funday";

            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.ScheduleDay);
        }

        [Fact]
        public async Task Handle_ShouldCreateClassWithAllFields()
        {
            var command = ValidCommand();
            var unit = new Unit { Id = command.UnitId, Name = "Unit One", Code = "U01" };
            var lecturer = new Lecturer { Id = command.LecturerId, FirstName = "Jane", LastName = "Doe" };
            var semester = new Semester { Id = command.SemesterId, Name = "Semester 1" };

            _unitRepositoryMock
                .Setup(x => x.GetByIdAsync(command.UnitId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(unit);
            _lecturerRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lecturer);
            _semesterRepositoryMock
                .Setup(x => x.GetByIdAsync(command.SemesterId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(semester);
            _classRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Class>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Class c, CancellationToken ct) => c);

            var handler = new CreateClassCommandHandler(
                _classRepositoryMock.Object,
                _unitRepositoryMock.Object,
                _lecturerRepositoryMock.Object,
                _semesterRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateClassCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.Name.Should().Be(command.Name);
            result.Code.Should().Be(command.Code);
            result.UnitId.Should().Be(command.UnitId);
            result.LecturerId.Should().Be(command.LecturerId);
            result.SemesterId.Should().Be(command.SemesterId);
            result.StartTime.Should().Be(command.StartTime);
            result.EndTime.Should().Be(command.EndTime);
            result.ScheduleDay.Should().Be(command.ScheduleDay);
            result.UnitName.Should().Be("Unit One");
            result.LecturerName.Should().Be("Jane Doe");
            result.SemesterName.Should().Be("Semester 1");

            _classRepositoryMock.Verify(
                x => x.AddAsync(
                    It.Is<Class>(c => c.Name == command.Name && c.Code == command.Code),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithMissingUnit_ShouldThrowNotFoundException()
        {
            var command = ValidCommand();
            _unitRepositoryMock
                .Setup(x => x.GetByIdAsync(command.UnitId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Unit?)null);

            var handler = new CreateClassCommandHandler(
                _classRepositoryMock.Object,
                _unitRepositoryMock.Object,
                _lecturerRepositoryMock.Object,
                _semesterRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateClassCommandHandler>>());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<SMS.Application.Exceptions.NotFoundException>();
        }

        private static CreateClassCommand ValidCommand()
        {
            return new CreateClassCommand
            {
                Name = "Computer Science Year 1",
                Code = "CSC-Y1",
                UnitId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                MaxCapacity = 50,
                StartDate = new DateTime(2026, 1, 5),
                EndDate = new DateTime(2026, 5, 30),
                ScheduleDay = "Monday",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                IsActive = true
            };
        }
    }
}
