using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using SMS.Application.Exceptions;
using SMS.Application.Features.Students.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Identity.Services;
using Xunit;

namespace SMS.UnitTests.Students
{
    public class CreateStudentCommandTests
    {
        private readonly CreateStudentCommandValidator _validator;
        private readonly Mock<IUserManagerService> _userManagerMock;
        private readonly Mock<IStudentRepository> _studentRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public CreateStudentCommandTests()
        {
            _validator = new CreateStudentCommandValidator();
            _userManagerMock = new Mock<IUserManagerService>();
            _studentRepositoryMock = new Mock<IStudentRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var command = new CreateStudentCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "+254712345678",
                DateOfBirth = new DateTime(2000, 1, 1)
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
            var command = new CreateStudentCommand
            {
                FirstName = "",
                LastName = "",
                Email = "invalid-email",
                PhoneNumber = "",
                DateOfBirth = DateTime.UtcNow
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FirstName);
            result.ShouldHaveValidationErrorFor(x => x.LastName);
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
            result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
        }

        [Fact]
        public async Task Handle_WithExistingEmail_ShouldThrowConflictException()
        {
            // Arrange
            var command = new CreateStudentCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "existing@example.com",
                PhoneNumber = "+254712345678",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            var existingUser = new User { Email = "existing@example.com" };
            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(existingUser);

            var handler = new CreateStudentCommandHandler(
                _userManagerMock.Object,
                _studentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateStudentCommandHandler>>());

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldCreateStudent()
        {
            // Arrange
            var command = new CreateStudentCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "+254712345678",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User?)null);

            _userManagerMock
                .Setup(x => x.CreateUserAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync((User user, string password) => new IdentityResult { Succeeded = true });

            var handler = new CreateStudentCommandHandler(
                _userManagerMock.Object,
                _studentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateStudentCommandHandler>>());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.FirstName.Should().Be(command.FirstName);
            result.LastName.Should().Be(command.LastName);
            result.Email.Should().Be(command.Email);
            
            _studentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _auditServiceMock.Verify(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }

    // Mock IdentityResult for testing
    public class IdentityResult
    {
        public bool Succeeded { get; set; }
        public IEnumerable<IdentityError> Errors { get; set; } = new List<IdentityError>();
    }

    public class IdentityError
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}