using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Application.Features.CourseOfferings.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.CourseOfferings
{
    public class CreateCourseOfferingCommandTests
    {
        private readonly CreateCourseOfferingCommandValidator _validator;
        private readonly Mock<ICourseOfferingRepository> _offeringRepoMock;
        private readonly Mock<ICourseRepository> _courseRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public CreateCourseOfferingCommandTests()
        {
            _validator = new CreateCourseOfferingCommandValidator();
            _offeringRepoMock = new Mock<ICourseOfferingRepository>();
            _courseRepoMock = new Mock<ICourseRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new CreateCourseOfferingCommand
            {
                CourseId = Guid.NewGuid(),
                AcademicYearId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddMonths(6),
                Status = CourseOfferingStatus.Draft
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new CreateCourseOfferingCommand
            {
                CourseId = Guid.Empty,
                AcademicYearId = Guid.Empty,
                SemesterId = Guid.Empty,
                StartDate = DateTime.MinValue,
                EndDate = DateTime.MinValue
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.CourseId);
            result.ShouldHaveValidationErrorFor(x => x.AcademicYearId);
            result.ShouldHaveValidationErrorFor(x => x.SemesterId);
            result.ShouldHaveValidationErrorFor(x => x.StartDate);
            result.ShouldHaveValidationErrorFor(x => x.EndDate);
        }

        [Fact]
        public async Task Handle_WithNonExistentCourse_ShouldThrowNotFoundException()
        {
            var command = new CreateCourseOfferingCommand
            {
                CourseId = Guid.NewGuid(),
                AcademicYearId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddMonths(6)
            };

            _courseRepoMock
                .Setup(x => x.GetByIdAsync(command.CourseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Course?)null);

            var handler = new CreateCourseOfferingCommandHandler(
                _offeringRepoMock.Object,
                _courseRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateCourseOfferingCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldCreateCourseOffering()
        {
            var courseId = Guid.NewGuid();
            var command = new CreateCourseOfferingCommand
            {
                CourseId = courseId,
                AcademicYearId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                Intake = "2026 Intake A",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddMonths(6),
                Status = CourseOfferingStatus.Draft
            };

            var course = new Course
            {
                Id = courseId,
                Name = "Wildlife Management",
                Code = "WM101",
                IsActive = true
            };

            var offering = new CourseOffering
            {
                Id = Guid.NewGuid(),
                OfferingCode = "WM101-2026-1-1",
                CourseId = courseId,
                AcademicYearId = command.AcademicYearId,
                SemesterId = command.SemesterId,
                Intake = command.Intake,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                Status = CourseOfferingStatus.Draft,
                IsActive = true
            };

            _courseRepoMock
                .Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);

            _offeringRepoMock
                .Setup(x => x.GetNextSequenceForCourseAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _offeringRepoMock
                .Setup(x => x.GenerateOfferingCodeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("WM101-2026-1-1");

            _offeringRepoMock
                .Setup(x => x.AddAsync(It.IsAny<CourseOffering>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOffering o, CancellationToken ct) => o);

            var handler = new CreateCourseOfferingCommandHandler(
                _offeringRepoMock.Object,
                _courseRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateCourseOfferingCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.CourseId.Should().Be(courseId);
            result.OfferingCode.Should().Be("WM101-2026-1-1");
            result.Status.Should().Be(CourseOfferingStatus.Draft);

            _offeringRepoMock.Verify(x => x.AddAsync(It.IsAny<CourseOffering>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class UpdateCourseOfferingCommandTests
    {
        private readonly UpdateCourseOfferingCommandValidator _validator;
        private readonly Mock<ICourseOfferingRepository> _offeringRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public UpdateCourseOfferingCommandTests()
        {
            _validator = new UpdateCourseOfferingCommandValidator();
            _offeringRepoMock = new Mock<ICourseOfferingRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new UpdateCourseOfferingCommand
            {
                Id = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddMonths(6),
                Status = CourseOfferingStatus.Active
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new UpdateCourseOfferingCommand
            {
                Id = Guid.Empty,
                StartDate = DateTime.MinValue,
                EndDate = DateTime.MinValue
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Id);
            result.ShouldHaveValidationErrorFor(x => x.StartDate);
            result.ShouldHaveValidationErrorFor(x => x.EndDate);
        }

        [Fact]
        public async Task Handle_WithNonExistentOffering_ShouldThrowNotFoundException()
        {
            var command = new UpdateCourseOfferingCommand
            {
                Id = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddMonths(6)
            };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOffering?)null);

            var handler = new UpdateCourseOfferingCommandHandler(
                _offeringRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<UpdateCourseOfferingCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldUpdateCourseOffering()
        {
            var offeringId = Guid.NewGuid();
            var command = new UpdateCourseOfferingCommand
            {
                Id = offeringId,
                Intake = "2026 Intake B",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddMonths(6),
                Status = CourseOfferingStatus.Active,
                IsActive = true,
                Notes = "Updated notes"
            };

            var offering = new CourseOffering
            {
                Id = offeringId,
                OfferingCode = "WM101-2026-1-1",
                CourseId = Guid.NewGuid(),
                AcademicYearId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                Intake = "2026 Intake A",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddMonths(6),
                Status = CourseOfferingStatus.Draft,
                IsActive = true
            };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(offering);

            _offeringRepoMock
                .Setup(x => x.UpdateAsync(It.IsAny<CourseOffering>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var handler = new UpdateCourseOfferingCommandHandler(
                _offeringRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<UpdateCourseOfferingCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.Id.Should().Be(offeringId);
            result.Intake.Should().Be("2026 Intake B");
            result.Status.Should().Be(CourseOfferingStatus.Active);

            _offeringRepoMock.Verify(x => x.UpdateAsync(It.IsAny<CourseOffering>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class AssignStudentToOfferingCommandTests
    {
        private readonly AssignStudentToOfferingCommandValidator _validator;
        private readonly Mock<ICourseOfferingEnrollmentRepository> _enrollmentRepoMock;
        private readonly Mock<ICourseOfferingRepository> _offeringRepoMock;
        private readonly Mock<IStudentRepository> _studentRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public AssignStudentToOfferingCommandTests()
        {
            _validator = new AssignStudentToOfferingCommandValidator();
            _enrollmentRepoMock = new Mock<ICourseOfferingEnrollmentRepository>();
            _offeringRepoMock = new Mock<ICourseOfferingRepository>();
            _studentRepoMock = new Mock<IStudentRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new AssignStudentToOfferingCommand
            {
                CourseOfferingId = Guid.NewGuid(),
                StudentId = Guid.NewGuid()
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public async Task Handle_WithNonExistentOffering_ShouldThrowNotFoundException()
        {
            var command = new AssignStudentToOfferingCommand
            {
                CourseOfferingId = Guid.NewGuid(),
                StudentId = Guid.NewGuid()
            };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(command.CourseOfferingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOffering?)null);

            var handler = new AssignStudentToOfferingCommandHandler(
                _enrollmentRepoMock.Object,
                _offeringRepoMock.Object,
                _studentRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignStudentToOfferingCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithNonExistentStudent_ShouldThrowNotFoundException()
        {
            var command = new AssignStudentToOfferingCommand
            {
                CourseOfferingId = Guid.NewGuid(),
                StudentId = Guid.NewGuid()
            };

            var offering = new CourseOffering
            {
                Id = command.CourseOfferingId,
                OfferingCode = "WM101-2026-1-1",
                CourseId = Guid.NewGuid(),
                AcademicYearId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                Status = CourseOfferingStatus.Active,
                IsActive = true
            };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(command.CourseOfferingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(offering);

            _studentRepoMock
                .Setup(x => x.GetByIdAsync(command.StudentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Student?)null);

            var handler = new AssignStudentToOfferingCommandHandler(
                _enrollmentRepoMock.Object,
                _offeringRepoMock.Object,
                _studentRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignStudentToOfferingCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithAlreadyEnrolledStudent_ShouldThrowConflictException()
        {
            var command = new AssignStudentToOfferingCommand
            {
                CourseOfferingId = Guid.NewGuid(),
                StudentId = Guid.NewGuid()
            };

            var offering = new CourseOffering
            {
                Id = command.CourseOfferingId,
                OfferingCode = "WM101-2026-1-1",
                CourseId = Guid.NewGuid(),
                AcademicYearId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                Status = CourseOfferingStatus.Active,
                IsActive = true
            };

            var student = new Student
            {
                Id = command.StudentId,
                FirstName = "Test",
                LastName = "Student",
                StudentNumber = "SN-001",
                IsActive = true
            };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(command.CourseOfferingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(offering);

            _studentRepoMock
                .Setup(x => x.GetByIdAsync(command.StudentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(student);

            _enrollmentRepoMock
                .Setup(x => x.ExistsByOfferingAndStudentAsync(command.CourseOfferingId, command.StudentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new AssignStudentToOfferingCommandHandler(
                _enrollmentRepoMock.Object,
                _offeringRepoMock.Object,
                _studentRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignStudentToOfferingCommandHandler>>());

            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldAssignStudentToOffering()
        {
            var offeringId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var command = new AssignStudentToOfferingCommand
            {
                CourseOfferingId = offeringId,
                StudentId = studentId
            };

            var offering = new CourseOffering
            {
                Id = offeringId,
                OfferingCode = "WM101-2026-1-1",
                CourseId = Guid.NewGuid(),
                AcademicYearId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                Status = CourseOfferingStatus.Active,
                IsActive = true
            };

            var student = new Student
            {
                Id = studentId,
                FirstName = "Test",
                LastName = "Student",
                StudentNumber = "SN-001",
                IsActive = true
            };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(offering);

            _studentRepoMock
                .Setup(x => x.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(student);

            _enrollmentRepoMock
                .Setup(x => x.ExistsByOfferingAndStudentAsync(offeringId, studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _enrollmentRepoMock
                .Setup(x => x.GetAttemptCountAsync(offeringId, studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _enrollmentRepoMock
                .Setup(x => x.AddAsync(It.IsAny<CourseOfferingEnrollment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOfferingEnrollment e, CancellationToken ct) => e);

            var handler = new AssignStudentToOfferingCommandHandler(
                _enrollmentRepoMock.Object,
                _offeringRepoMock.Object,
                _studentRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignStudentToOfferingCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.StudentId.Should().Be(studentId);
            result.CourseOfferingId.Should().Be(offeringId);
            result.AttemptNumber.Should().Be(1);
            result.ConfirmationStatus.Should().Be(ConfirmationStatus.Pending);

            _enrollmentRepoMock.Verify(x => x.AddAsync(It.IsAny<CourseOfferingEnrollment>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
