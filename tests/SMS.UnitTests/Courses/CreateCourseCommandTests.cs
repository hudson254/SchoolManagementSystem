using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Application.Features.Courses.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Courses
{
    public class CreateCourseCommandTests
    {
        private readonly CreateCourseCommandValidator _validator;
        private readonly Mock<ICourseRepository> _courseRepositoryMock;
        private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public CreateCourseCommandTests()
        {
            _validator = new CreateCourseCommandValidator();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _departmentRepositoryMock = new Mock<IDepartmentRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var command = new CreateCourseCommand
            {
                Name = "Bachelor of Science in Computer Science",
                Code = "BSCS",
                Duration = 48,
                TotalCredits = 160,
                DepartmentId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            // Arrange
            var command = new CreateCourseCommand
            {
                Name = "",
                Code = "invalid code",
                Duration = 0,
                TotalCredits = 0,
                DepartmentId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name);
            result.ShouldHaveValidationErrorFor(x => x.Code);
            result.ShouldHaveValidationErrorFor(x => x.Duration);
            result.ShouldHaveValidationErrorFor(x => x.TotalCredits);
            result.ShouldHaveValidationErrorFor(x => x.DepartmentId);
        }

        [Fact]
        public async Task Handle_WithExistingCode_ShouldThrowConflictException()
        {
            // Arrange
            var command = new CreateCourseCommand
            {
                Name = "Bachelor of Science in Computer Science",
                Code = "BSCS",
                Duration = 48,
                TotalCredits = 160,
                DepartmentId = Guid.NewGuid()
            };

            var existingCourse = new Course { Code = "BSCS" };

            _courseRepositoryMock
                .Setup(x => x.GetByCodeAsync(command.Code, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCourse);

            var handler = new CreateCourseCommandHandler(
                _courseRepositoryMock.Object,
                _departmentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateCourseCommandHandler>>());

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithNonExistentDepartment_ShouldThrowNotFoundException()
        {
            // Arrange
            var command = new CreateCourseCommand
            {
                Name = "Bachelor of Science in Computer Science",
                Code = "BSCS",
                Duration = 48,
                TotalCredits = 160,
                DepartmentId = Guid.NewGuid()
            };

            _courseRepositoryMock
                .Setup(x => x.GetByCodeAsync(command.Code, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Course?)null);

            _departmentRepositoryMock
                .Setup(x => x.GetByIdAsync(command.DepartmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Department?)null);

            var handler = new CreateCourseCommandHandler(
                _courseRepositoryMock.Object,
                _departmentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateCourseCommandHandler>>());

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldCreateCourse()
        {
            // Arrange
            var departmentId = Guid.NewGuid();
            var command = new CreateCourseCommand
            {
                Name = "Bachelor of Science in Computer Science",
                Code = "BSCS",
                Duration = 48,
                TotalCredits = 160,
                DepartmentId = departmentId
            };

            var department = new Department
            {
                Id = departmentId,
                Name = "Computer Science",
                Code = "CS"
            };

            _courseRepositoryMock
                .Setup(x => x.GetByCodeAsync(command.Code, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Course?)null);

            _departmentRepositoryMock
                .Setup(x => x.GetByIdAsync(command.DepartmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(department);

            _courseRepositoryMock
                            .Setup(x => x.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((Course c, CancellationToken ct) => c);

            var handler = new CreateCourseCommandHandler(
                _courseRepositoryMock.Object,
                _departmentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateCourseCommandHandler>>());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(command.Name);
            result.Code.Should().Be(command.Code);
            result.DepartmentId.Should().Be(departmentId);
            result.DepartmentName.Should().Be(department.Name);

            _courseRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

