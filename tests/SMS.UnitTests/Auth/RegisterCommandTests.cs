using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.Auth.Commands;
using SMS.Application.Services;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.UnitTests.Common;
using Xunit;

namespace SMS.UnitTests.Auth
{
    public class RegisterCommandTests
    {
        private readonly RegisterCommandValidator _validator;
        private readonly Mock<IUserManagerService> _userManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<IUsernameGenerator> _usernameGeneratorMock;
        private readonly Mock<INameParser> _nameParserMock;
        private readonly Mock<IStudentRepository> _studentRepositoryMock;
        private readonly Mock<ILecturerRepository> _lecturerRepositoryMock;
        private readonly Mock<ICourseRepository> _courseRepositoryMock;
        private readonly Mock<IUnitRepository> _unitRepositoryMock;
        private readonly Mock<IUnitAllocationRepository> _unitAllocationRepositoryMock;
        private readonly Mock<ISemesterRepository> _semesterRepositoryMock;
        private readonly Mock<SMS.Multitenancy.Interfaces.ITenantContext> _tenantContextMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Microsoft.Extensions.Logging.ILogger<RegisterCommandHandler> _logger;
        private readonly Mock<IEnrollmentRepository> _enrollmentRepositoryMock;
        private readonly Mock<ICourseOfferingRepository> _courseOfferingRepositoryMock;
        private readonly Mock<ICourseOfferingEnrollmentRepository> _courseOfferingEnrollmentRepositoryMock;
        private readonly Mock<ICourseOfferingLecturerRepository> _courseOfferingLecturerRepositoryMock;
        private readonly Mock<ICourseOfferingUnitRepository> _courseOfferingUnitRepositoryMock;
        private readonly Mock<IBusinessEventNotifier> _notifierMock;

        public RegisterCommandTests()
        {
            _validator = new RegisterCommandValidator();
            _userManagerMock = new Mock<IUserManagerService>();
            _jwtServiceMock = new Mock<IJwtService>();
            _auditServiceMock = new Mock<IAuditService>();
            _usernameGeneratorMock = new Mock<IUsernameGenerator>();
            _nameParserMock = new Mock<INameParser>();
            _studentRepositoryMock = new Mock<IStudentRepository>();
            _lecturerRepositoryMock = new Mock<ILecturerRepository>();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _unitRepositoryMock = new Mock<IUnitRepository>();
            _unitAllocationRepositoryMock = new Mock<IUnitAllocationRepository>();
            _semesterRepositoryMock = new Mock<ISemesterRepository>();
            _tenantContextMock = new Mock<SMS.Multitenancy.Interfaces.ITenantContext>();
            _unitOfWorkMock = new Mock<IUnitOfWork>().RunsTransactionInline<SMS.Domain.Entities.User>();
            _enrollmentRepositoryMock = new Mock<IEnrollmentRepository>();
            _courseOfferingRepositoryMock = new Mock<ICourseOfferingRepository>();
            _courseOfferingEnrollmentRepositoryMock = new Mock<ICourseOfferingEnrollmentRepository>();
            _courseOfferingLecturerRepositoryMock = new Mock<ICourseOfferingLecturerRepository>();
            _courseOfferingUnitRepositoryMock = new Mock<ICourseOfferingUnitRepository>();
            _notifierMock = new Mock<IBusinessEventNotifier>();
            _logger = Mock.Of<Microsoft.Extensions.Logging.ILogger<RegisterCommandHandler>>();

            _tenantContextMock.Setup(x => x.TenantId).Returns(Guid.NewGuid().ToString());
        }

        private RegisterCommandHandler CreateHandler()
        {
            return new RegisterCommandHandler(
                _userManagerMock.Object,
                _jwtServiceMock.Object,
                _auditServiceMock.Object,
                _logger,
                _usernameGeneratorMock.Object,
                _nameParserMock.Object,
                _studentRepositoryMock.Object,
                _lecturerRepositoryMock.Object,
                _courseRepositoryMock.Object,
                _unitRepositoryMock.Object,
                _unitAllocationRepositoryMock.Object,
                _semesterRepositoryMock.Object,
                _tenantContextMock.Object,
                _unitOfWorkMock.Object,
                new PasswordPolicyService(),
                _enrollmentRepositoryMock.Object,
                _courseOfferingRepositoryMock.Object,
                _courseOfferingEnrollmentRepositoryMock.Object,
                _courseOfferingLecturerRepositoryMock.Object,
                _courseOfferingUnitRepositoryMock.Object,
                _notifierMock.Object);
        }

        [Fact]
        public void ValidStudentCommand_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Student",
                Organization = "Test University",
                PhoneNumber = "+254700000000",
                CourseId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ValidLecturerCommand_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var command = new RegisterCommand
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Lecturer",
                Organization = "Test University",
                PhoneNumber = "+254711111111",
                Specialization = "Computer Science",
                // A lecturer must choose the units they will teach. Registering
                // with an empty unit set is what produced lecturers with no
                // teaching assignment, so it is now rejected by the validator.
                CourseId = Guid.NewGuid(),
                UnitIds = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void LecturerCommand_WithoutUnits_ShouldFailValidation()
        {
            // Regression guard for the reported bug: a lecturer registration
            // with no unit selection used to be accepted, which silently created
            // a lecturer account with no course and no teaching assignment.
            var command = new RegisterCommand
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Lecturer",
                Organization = "Test University",
                PhoneNumber = "+254711111111",
                Specialization = "Computer Science"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UnitIds);
        }

        [Fact]
        public void LecturerCommand_WithUnitsButNoCourse_ShouldFailValidation()
        {
            // Units cannot be validated against a course that was not chosen.
            var command = new RegisterCommand
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Lecturer",
                Organization = "Test University",
                PhoneNumber = "+254711111111",
                Specialization = "Computer Science",
                UnitIds = new List<Guid> { Guid.NewGuid() }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.CourseId);
        }

        [Fact]
        public void LecturerCommand_WithDuplicateUnits_ShouldFailValidation()
        {
            // Duplicate ids would otherwise create duplicate UnitAllocation rows.
            var unitId = Guid.NewGuid();
            var command = new RegisterCommand
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Lecturer",
                Organization = "Test University",
                PhoneNumber = "+254711111111",
                Specialization = "Computer Science",
                CourseId = Guid.NewGuid(),
                UnitIds = new List<Guid> { unitId, unitId }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.UnitIds);
        }

        [Fact]
        public void StudentCommand_WithoutUnitIds_ShouldNotHaveValidationErrors()
        {
            // Students are enrolled in ALL units of the course by the existing
            // business rule, so the client does not need to send unit ids. This
            // must stay valid - the course is the only required selection.
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Student",
                Organization = "Test University",
                PhoneNumber = "+254700000000",
                CourseId = Guid.NewGuid()
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            // Arrange
            var command = new RegisterCommand
            {
                FirstName = "",
                LastName = "",
                Email = "invalid-email",
                Password = "weak",
                ConfirmPassword = "different",
                Role = "Administrator",
                Organization = ""
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FirstName);
            result.ShouldHaveValidationErrorFor(x => x.LastName);
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Password);
            result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
            result.ShouldHaveValidationErrorFor(x => x.Role);
            result.ShouldHaveValidationErrorFor(x => x.Organization);
        }

        [Fact]
        public async Task Handle_WithExistingEmail_ShouldThrowConflictException()
        {
            // Arrange
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "existing@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Student",
                Organization = "Test University",
                CourseId = Guid.NewGuid()
            };

            var existingUser = new User { Email = "existing@example.com" };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(existingUser);

            var handler = CreateHandler();

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidStudentData_ShouldRegisterUserWithStudentRole()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var courseId = Guid.NewGuid();
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Student",
                Organization = "Test University",
                PhoneNumber = "+254700000000",
                CourseId = courseId
            };

            var course = new Course
            {
                Id = courseId,
                Name = "Computer Science",
                Code = "CS101",
                IsActive = true,
                ProgrammeId = Guid.NewGuid()
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User?)null);

            _nameParserMock
                .Setup(x => x.ParseName(It.IsAny<string>()))
                .Returns(new NameParseResult
                {
                    FirstName = "John",
                    LastName = "Doe",
                    IsValid = true
                });

            _usernameGeneratorMock
                .Setup(x => x.GenerateUsernameAsync("John", "Doe"))
                .ReturnsAsync("john.doe");

            _userManagerMock
                .Setup(x => x.CreateUserAsync("john.doe", command.Email, command.Password, command.Role))
                .ReturnsAsync((string username, string email, string password, string role) => new User
                {
                    Id = userId,
                    Email = email,
                    UserName = username,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    IsActive = true
                });

            _userManagerMock
                .Setup(x => x.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string> { "Student" });

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns("test-access-token");

            _userManagerMock
                .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync("test-refresh-token");

            _courseRepositoryMock
                .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);

            _unitRepositoryMock
                .Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Unit>());

            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("test-access-token");
            result.RefreshToken.Should().Be("test-refresh-token");
            result.Email.Should().Be(command.Email);
            result.Roles.Should().Contain("Student");
            result.FullName.Should().Be("John Doe");

            _userManagerMock.Verify(x => x.CreateUserAsync("john.doe", command.Email, command.Password, "Student"), Times.Once);
            _auditServiceMock.Verify(x => x.LogAsync("Register", It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_WithPasswordMismatch_ShouldThrowValidationException()
        {
            // Arrange
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "DifferentPassword!",
                Role = "Student",
                Organization = "Test University"
            };

            var handler = CreateHandler();

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithInvalidRole_ShouldThrowValidationException()
        {
            // Arrange
            var command = new RegisterCommand
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.doe@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Administrator",
                Organization = "Test University"
            };

            var handler = CreateHandler();

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithWeakPassword_ShouldThrowValidationException()
        {
            // Arrange
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "password123",
                ConfirmPassword = "password123",
                Role = "Student",
                Organization = "Test University",
                CourseId = Guid.NewGuid()
            };

            var handler = CreateHandler();

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithCommonBlacklistedPassword_ShouldThrowValidationException()
        {
            // Arrange
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Admin12345!",
                ConfirmPassword = "Admin12345!",
                Role = "Student",
                Organization = "Test University",
                CourseId = Guid.NewGuid()
            };

            var handler = CreateHandler();

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithTitle_ShouldParseTitleAndNotLeakIntoUsername()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var courseId = Guid.NewGuid();
            var command = new RegisterCommand
            {
                Title = "Dr.",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Student",
                Organization = "Test University",
                PhoneNumber = "+254700000000",
                CourseId = courseId
            };

            var course = new Course
            {
                Id = courseId,
                Name = "Computer Science",
                Code = "CS101",
                IsActive = true,
                ProgrammeId = Guid.NewGuid()
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User?)null);

            _nameParserMock
                .Setup(x => x.ParseName(It.IsAny<string>()))
                .Returns(new NameParseResult
                {
                    Title = "Dr.",
                    FirstName = "John",
                    LastName = "Doe",
                    IsValid = true
                });

            _usernameGeneratorMock
                .Setup(x => x.GenerateUsernameAsync("John", "Doe"))
                .ReturnsAsync("john.doe");

            _userManagerMock
                .Setup(x => x.CreateUserAsync("john.doe", command.Email, command.Password, "Student"))
                .ReturnsAsync((string username, string email, string password, string role) => new User
                {
                    Id = userId,
                    Email = email,
                    UserName = username,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    Title = "Dr.",
                    IsActive = true
                });

            _userManagerMock
                .Setup(x => x.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string> { "Student" });

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns("test-access-token");

            _userManagerMock
                .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync("test-refresh-token");

            _courseRepositoryMock
                .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);

            _unitRepositoryMock
                .Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Unit>());

            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("test-access-token");
            result.Email.Should().Be(command.Email);
            result.Roles.Should().Contain("Student");
            result.FullName.Should().Be("Dr. John Doe");
            result.Title.Should().Be("Dr.");

            // Verify username was generated WITHOUT the title
            _usernameGeneratorMock.Verify(x => x.GenerateUsernameAsync("John", "Doe"), Times.Once);
            _usernameGeneratorMock.Verify(x => x.GenerateUsernameAsync(It.Is<string>(s => s.Contains("Dr")), It.IsAny<string>()), Times.Never);
            _userManagerMock.Verify(x => x.CreateUserAsync("john.doe", command.Email, command.Password, "Student"), Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidLecturerData_ShouldRegisterUserWithLecturerRole()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var courseId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            var command = new RegisterCommand
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Lecturer",
                Organization = "Test University",
                PhoneNumber = "+254711111111",
                Specialization = "Computer Science",
                CourseId = courseId,
                UnitIds = new List<Guid> { unitId }
            };

            // The course and its units must resolve, otherwise the handler
            // rejects the registration before creating the lecturer.
            _courseRepositoryMock
                .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course
                {
                    Id = courseId,
                    Code = "CS101",
                    Name = "Computer Science",
                    IsActive = true
                });

            _unitRepositoryMock
                .Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Unit>
                {
                    new Unit { Id = unitId, Code = "CS101.1", Name = "Intro to CS", CourseId = courseId, IsActive = true }
                });

            // UnitAllocation.SemesterId is a non-nullable FK, so the tenant must
            // have a resolvable academic period.
            _semesterRepositoryMock
                .Setup(x => x.GetCurrentOrDefaultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Semester { Id = Guid.NewGuid(), Name = "Semester 1", IsActive = true, IsCurrent = true });

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User?)null);

            _nameParserMock
                .Setup(x => x.ParseName(It.IsAny<string>()))
                .Returns(new NameParseResult
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    IsValid = true
                });

            _usernameGeneratorMock
                .Setup(x => x.GenerateUsernameAsync("Jane", "Smith"))
                .ReturnsAsync("jane.smith");

            _userManagerMock
                .Setup(x => x.CreateUserAsync("jane.smith", command.Email, command.Password, "Lecturer"))
                .ReturnsAsync((string username, string email, string password, string role) => new User
                {
                    Id = userId,
                    Email = email,
                    UserName = username,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    IsActive = true
                });

            _userManagerMock
                .Setup(x => x.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string> { "Lecturer" });

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns("test-access-token");

            _userManagerMock
                .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync("test-refresh-token");

            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("test-access-token");
            result.RefreshToken.Should().Be("test-refresh-token");
            result.Email.Should().Be(command.Email);
            result.Roles.Should().Contain("Lecturer");

            _userManagerMock.Verify(x => x.CreateUserAsync("jane.smith", command.Email, command.Password, "Lecturer"), Times.Once);
            _auditServiceMock.Verify(x => x.LogAsync("Register", It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));

            // The teaching assignment must actually be persisted. Before the
            // repair the lecturer's course/unit selection was discarded here and
            // no UnitAllocation row was ever created.
            _unitAllocationRepositoryMock.Verify(
                x => x.AddAsync(It.Is<UnitAllocation>(a =>
                    a.UnitId == unitId && a.Status == "PendingApproval"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_LecturerWithUnitFromAnotherCourse_ShouldThrowValidation()
        {
            // A tampered payload must not be able to attach a unit that does not
            // belong to the selected course.
            var userId = Guid.NewGuid().ToString();
            var courseId = Guid.NewGuid();
            var ownUnitId = Guid.NewGuid();
            var foreignUnitId = Guid.NewGuid();

            var command = new RegisterCommand
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Lecturer",
                Organization = "Test University",
                PhoneNumber = "+254711111111",
                Specialization = "Computer Science",
                CourseId = courseId,
                UnitIds = new List<Guid> { ownUnitId, foreignUnitId }
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User?)null);
            _nameParserMock.Setup(x => x.ParseName(It.IsAny<string>()))
                .Returns(new NameParseResult { FirstName = "Jane", LastName = "Smith", IsValid = true });
            _usernameGeneratorMock.Setup(x => x.GenerateUsernameAsync("Jane", "Smith")).ReturnsAsync("jane.smith");
            _userManagerMock
                .Setup(x => x.CreateUserAsync("jane.smith", command.Email, command.Password, "Lecturer"))
                .ReturnsAsync((string username, string email, string password, string role) => new User
                {
                    Id = userId,
                    Email = email,
                    UserName = username,
                    IsActive = true
                });

            _courseRepositoryMock
                .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Code = "CS101", Name = "Computer Science", IsActive = true });

            // The course owns only ownUnitId; foreignUnitId belongs elsewhere.
            _unitRepositoryMock
                .Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Unit>
                {
                    new Unit { Id = ownUnitId, Code = "CS101.1", Name = "Intro", CourseId = courseId, IsActive = true }
                });

            var handler = CreateHandler();

            await Assert.ThrowsAsync<SMS.Application.Exceptions.ValidationException>(
                () => handler.Handle(command, CancellationToken.None));

            // Nothing may be persisted for a rejected registration.
            _unitAllocationRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<UnitAllocation>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
