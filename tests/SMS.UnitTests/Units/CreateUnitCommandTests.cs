using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Application.Features.Units.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Units
{
    public class CreateUnitCommandTests
    {
        private readonly CreateUnitCommandValidator _validator;
        private readonly Mock<IUnitRepository> _unitRepositoryMock;
        private readonly Mock<ICourseRepository> _courseRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public CreateUnitCommandTests()
        {
            _validator = new CreateUnitCommandValidator();
            _unitRepositoryMock = new Mock<IUnitRepository>();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new CreateUnitCommand
            {
                Name = "Introduction to Programming",
                Code = "CSC101",
                Credits = 3,
                ContactHours = 3,
                CourseId = Guid.NewGuid()
            };

            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new CreateUnitCommand
            {
                Name = "",
                Code = "invalid code",
                Credits = 0,
                ContactHours = 0,
                CourseId = Guid.Empty
            };

            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Name);
            result.ShouldHaveValidationErrorFor(x => x.Code);
            result.ShouldHaveValidationErrorFor(x => x.Credits);
            result.ShouldHaveValidationErrorFor(x => x.ContactHours);
            result.ShouldHaveValidationErrorFor(x => x.CourseId);
        }

        [Fact]
        public void CreditsExceedsMax_ShouldHaveValidationError()
        {
            var command = new CreateUnitCommand
            {
                Name = "Test Unit",
                Code = "TEST",
                Credits = 10,
                ContactHours = 3,
                CourseId = Guid.NewGuid()
            };

            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Credits);
        }

        [Fact]
        public async Task Handle_WithExistingCode_ShouldThrowConflictException()
        {
            var command = new CreateUnitCommand
            {
                Name = "Introduction to Programming",
                Code = "CSC101",
                Credits = 3,
                ContactHours = 3,
                CourseId = Guid.NewGuid()
            };

            var existingUnit = new SMS.Domain.Entities.Unit { Code = "CSC101" };

            _unitRepositoryMock
                .Setup(x => x.GetByCodeAsync(command.Code, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUnit);

            var handler = new CreateUnitCommandHandler(
                _unitRepositoryMock.Object,
                _courseRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateUnitCommandHandler>>());

            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithNonExistentCourse_ShouldThrowNotFoundException()
        {
            var command = new CreateUnitCommand
            {
                Name = "Introduction to Programming",
                Code = "CSC101",
                Credits = 3,
                ContactHours = 3,
                CourseId = Guid.NewGuid()
            };

            _unitRepositoryMock
                .Setup(x => x.GetByCodeAsync(command.Code, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SMS.Domain.Entities.Unit?)null);

            _courseRepositoryMock
                .Setup(x => x.GetByIdAsync(command.CourseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Course?)null);

            var handler = new CreateUnitCommandHandler(
                _unitRepositoryMock.Object,
                _courseRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateUnitCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldCreateUnit()
        {
            var courseId = Guid.NewGuid();
            var command = new CreateUnitCommand
            {
                Name = "Introduction to Programming",
                Code = "CSC101",
                Credits = 3,
                ContactHours = 3,
                CourseId = courseId
            };

            var course = new Course
            {
                Id = courseId,
                Name = "Computer Science",
                Code = "BSCS"
            };

            _unitRepositoryMock
                .Setup(x => x.GetByCodeAsync(command.Code, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SMS.Domain.Entities.Unit?)null);

            _courseRepositoryMock
                .Setup(x => x.GetByIdAsync(command.CourseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);

            _unitRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<SMS.Domain.Entities.Unit>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SMS.Domain.Entities.Unit u, CancellationToken ct) => u);

            var handler = new CreateUnitCommandHandler(
                _unitRepositoryMock.Object,
                _courseRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateUnitCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.Name.Should().Be(command.Name);
            result.Code.Should().Be(command.Code);
            result.Credits.Should().Be(command.Credits);
            result.CourseId.Should().Be(courseId);

            _unitRepositoryMock.Verify(x => x.AddAsync(It.IsAny<SMS.Domain.Entities.Unit>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidPrerequisite_ShouldCreateUnitWithPrerequisite()
        {
            var courseId = Guid.NewGuid();
            var prerequisiteId = Guid.NewGuid();
            var command = new CreateUnitCommand
            {
                Name = "Advanced Programming",
                Code = "CSC201",
                Credits = 3,
                ContactHours = 3,
                CourseId = courseId,
                PrerequisiteUnitId = prerequisiteId
            };

            var course = new Course
            {
                Id = courseId,
                Name = "Computer Science",
                Code = "BSCS"
            };

            var prerequisite = new SMS.Domain.Entities.Unit
            {
                Id = prerequisiteId,
                Name = "Introduction to Programming",
                Code = "CSC101"
            };

            _unitRepositoryMock
                .Setup(x => x.GetByCodeAsync(command.Code, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SMS.Domain.Entities.Unit?)null);

            _courseRepositoryMock
                .Setup(x => x.GetByIdAsync(command.CourseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);

            _unitRepositoryMock
                .Setup(x => x.GetByIdAsync(prerequisiteId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(prerequisite);

            _unitRepositoryMock
                            .Setup(x => x.AddAsync(It.IsAny<SMS.Domain.Entities.Unit>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((SMS.Domain.Entities.Unit u, CancellationToken ct) => u);

            var handler = new CreateUnitCommandHandler(
                _unitRepositoryMock.Object,
                _courseRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateUnitCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.PrerequisiteUnitId.Should().Be(prerequisiteId);

            _unitRepositoryMock.Verify(x => x.GetByIdAsync(prerequisiteId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

