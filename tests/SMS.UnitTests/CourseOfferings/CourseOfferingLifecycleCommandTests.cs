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
    public class DeleteCourseOfferingCommandTests
    {
        private readonly Mock<ICourseOfferingRepository> _offeringRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public DeleteCourseOfferingCommandTests()
        {
            _offeringRepoMock = new Mock<ICourseOfferingRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public async Task Handle_WithNonExistentOffering_ShouldThrowNotFoundException()
        {
            var command = new DeleteCourseOfferingCommand { Id = Guid.NewGuid() };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOffering?)null);

            var handler = new DeleteCourseOfferingCommandHandler(
                _offeringRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<DeleteCourseOfferingCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidOffering_ShouldSoftDeleteAndReturnTrue()
        {
            var offeringId = Guid.NewGuid();
            var command = new DeleteCourseOfferingCommand { Id = offeringId };

            var offering = new CourseOffering
            {
                Id = offeringId,
                OfferingCode = "WM101-2026-1-1",
                CourseId = Guid.NewGuid(),
                AcademicYearId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow,
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

            var handler = new DeleteCourseOfferingCommandHandler(
                _offeringRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<DeleteCourseOfferingCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();
            offering.IsActive.Should().BeFalse();
            offering.Status.Should().Be(CourseOfferingStatus.Cancelled);

            _offeringRepoMock.Verify(x => x.UpdateAsync(offering, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _auditServiceMock.Verify(x => x.LogAsync("CourseOffering", "Delete", offeringId.ToString()), Times.Once);
        }
    }

    public class CreateCourseOfferingUnitCommandTests
    {
        private readonly CreateCourseOfferingUnitCommandValidator _validator;
        private readonly Mock<ICourseOfferingUnitRepository> _unitRepoMock;
        private readonly Mock<ICourseOfferingRepository> _offeringRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public CreateCourseOfferingUnitCommandTests()
        {
            _validator = new CreateCourseOfferingUnitCommandValidator();
            _unitRepoMock = new Mock<ICourseOfferingUnitRepository>();
            _offeringRepoMock = new Mock<ICourseOfferingRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new CreateCourseOfferingUnitCommand
            {
                CourseOfferingId = Guid.NewGuid(),
                Name = "Introduction to Wildlife",
                Code = "WM101U1",
                Credits = 3,
                ContactHours = 30
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new CreateCourseOfferingUnitCommand
            {
                CourseOfferingId = Guid.Empty,
                Name = "",
                Code = "",
                Credits = -1,
                ContactHours = -5
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.CourseOfferingId);
            result.ShouldHaveValidationErrorFor(x => x.Name);
            result.ShouldHaveValidationErrorFor(x => x.Code);
            result.ShouldHaveValidationErrorFor(x => x.Credits);
            result.ShouldHaveValidationErrorFor(x => x.ContactHours);
        }

        [Fact]
        public async Task Handle_WithNonExistentOffering_ShouldThrowNotFoundException()
        {
            var command = new CreateCourseOfferingUnitCommand
            {
                CourseOfferingId = Guid.NewGuid(),
                Name = "Introduction to Wildlife",
                Code = "WM101U1",
                Credits = 3,
                ContactHours = 30
            };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(command.CourseOfferingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOffering?)null);

            var handler = new CreateCourseOfferingUnitCommandHandler(
                _unitRepoMock.Object,
                _offeringRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateCourseOfferingUnitCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldCreateUnit()
        {
            var offeringId = Guid.NewGuid();
            var command = new CreateCourseOfferingUnitCommand
            {
                CourseOfferingId = offeringId,
                UnitId = Guid.NewGuid(),
                Name = "Introduction to Wildlife",
                Code = "WM101U1",
                Description = "First unit",
                Credits = 3,
                ContactHours = 30,
                Order = 1,
                LearningOutcomes = "Understand wildlife",
                AssessmentMethods = "Exam",
                AssessmentWeighting = "{\"exam\": 70, \"coursework\": 30}",
                IsActive = true
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
                Status = CourseOfferingStatus.Draft,
                IsActive = true
            };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(offering);

            _unitRepoMock
                .Setup(x => x.AddAsync(It.IsAny<CourseOfferingUnit>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOfferingUnit u, CancellationToken ct) => u);

            var handler = new CreateCourseOfferingUnitCommandHandler(
                _unitRepoMock.Object,
                _offeringRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<CreateCourseOfferingUnitCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.CourseOfferingId.Should().Be(offeringId);
            result.Name.Should().Be("Introduction to Wildlife");
            result.Code.Should().Be("WM101U1");
            result.Credits.Should().Be(3);
            result.ContactHours.Should().Be(30);
            result.Order.Should().Be(1);

            _unitRepoMock.Verify(x => x.AddAsync(It.IsAny<CourseOfferingUnit>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class AssignLecturerToOfferingCommandTests
    {
        private readonly AssignLecturerToOfferingCommandValidator _validator;
        private readonly Mock<ICourseOfferingLecturerRepository> _lecturerRepoMock;
        private readonly Mock<ICourseOfferingRepository> _offeringRepoMock;
        private readonly Mock<ILecturerRepository> _lecturerRepo;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public AssignLecturerToOfferingCommandTests()
        {
            _validator = new AssignLecturerToOfferingCommandValidator();
            _lecturerRepoMock = new Mock<ICourseOfferingLecturerRepository>();
            _offeringRepoMock = new Mock<ICourseOfferingRepository>();
            _lecturerRepo = new Mock<ILecturerRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new AssignLecturerToOfferingCommand
            {
                CourseOfferingId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid(),
                IsPrimary = true
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new AssignLecturerToOfferingCommand
            {
                CourseOfferingId = Guid.Empty,
                LecturerId = Guid.Empty
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.CourseOfferingId);
            result.ShouldHaveValidationErrorFor(x => x.LecturerId);
        }

        [Fact]
        public async Task Handle_WithNonExistentOffering_ShouldThrowNotFoundException()
        {
            var command = new AssignLecturerToOfferingCommand
            {
                CourseOfferingId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid()
            };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(command.CourseOfferingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOffering?)null);

            var handler = new AssignLecturerToOfferingCommandHandler(
                _lecturerRepoMock.Object,
                _offeringRepoMock.Object,
                _lecturerRepo.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignLecturerToOfferingCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithNonExistentLecturer_ShouldThrowNotFoundException()
        {
            var command = new AssignLecturerToOfferingCommand
            {
                CourseOfferingId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid()
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

            _lecturerRepo
                .Setup(x => x.GetByIdAsync(command.LecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Lecturer?)null);

            var handler = new AssignLecturerToOfferingCommandHandler(
                _lecturerRepoMock.Object,
                _offeringRepoMock.Object,
                _lecturerRepo.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignLecturerToOfferingCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithAlreadyAssignedLecturer_ShouldThrowConflictException()
        {
            var command = new AssignLecturerToOfferingCommand
            {
                CourseOfferingId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid()
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

            var lecturer = new Lecturer
            {
                Id = command.LecturerId,
                FirstName = "Test",
                LastName = "Lecturer",
                Email = "test@test.com",
                EmployeeNumber = "EMP-001",
                IsActive = true
            };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(command.CourseOfferingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(offering);

            _lecturerRepo
                .Setup(x => x.GetByIdAsync(command.LecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lecturer);

            _lecturerRepoMock
                .Setup(x => x.ExistsByOfferingAndLecturerAsync(command.CourseOfferingId, command.LecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new AssignLecturerToOfferingCommandHandler(
                _lecturerRepoMock.Object,
                _offeringRepoMock.Object,
                _lecturerRepo.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignLecturerToOfferingCommandHandler>>());

            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldAssignLecturerToOffering()
        {
            var offeringId = Guid.NewGuid();
            var lecturerId = Guid.NewGuid();
            var command = new AssignLecturerToOfferingCommand
            {
                CourseOfferingId = offeringId,
                LecturerId = lecturerId,
                IsPrimary = true,
                Notes = "Primary lecturer"
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

            var lecturer = new Lecturer
            {
                Id = lecturerId,
                FirstName = "Test",
                LastName = "Lecturer",
                Email = "test@test.com",
                EmployeeNumber = "EMP-001",
                IsActive = true
            };

            _offeringRepoMock
                .Setup(x => x.GetByIdAsync(offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(offering);

            _lecturerRepo
                .Setup(x => x.GetByIdAsync(lecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lecturer);

            _lecturerRepoMock
                .Setup(x => x.ExistsByOfferingAndLecturerAsync(offeringId, lecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _lecturerRepoMock
                .Setup(x => x.AddAsync(It.IsAny<CourseOfferingLecturer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOfferingLecturer a, CancellationToken ct) => a);

            var handler = new AssignLecturerToOfferingCommandHandler(
                _lecturerRepoMock.Object,
                _offeringRepoMock.Object,
                _lecturerRepo.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<AssignLecturerToOfferingCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.CourseOfferingId.Should().Be(offeringId);
            result.LecturerId.Should().Be(lecturerId);
            result.IsPrimary.Should().BeTrue();
            result.IsActive.Should().BeTrue();

            _lecturerRepoMock.Verify(x => x.AddAsync(It.IsAny<CourseOfferingLecturer>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class ConfirmEnrollmentCommandTests
    {
        private readonly ConfirmEnrollmentCommandValidator _validator;
        private readonly Mock<ICourseOfferingEnrollmentRepository> _enrollmentRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public ConfirmEnrollmentCommandTests()
        {
            _validator = new ConfirmEnrollmentCommandValidator();
            _enrollmentRepoMock = new Mock<ICourseOfferingEnrollmentRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new ConfirmEnrollmentCommand { EnrollmentId = Guid.NewGuid() };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new ConfirmEnrollmentCommand { EnrollmentId = Guid.Empty };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.EnrollmentId);
        }

        [Fact]
        public async Task Handle_WithNonExistentEnrollment_ShouldThrowNotFoundException()
        {
            var command = new ConfirmEnrollmentCommand { EnrollmentId = Guid.NewGuid() };

            _enrollmentRepoMock
                .Setup(x => x.GetByIdAsync(command.EnrollmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOfferingEnrollment?)null);

            var handler = new ConfirmEnrollmentCommandHandler(
                _enrollmentRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<ConfirmEnrollmentCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ConfirmTrue_ShouldSetEnrollmentToConfirmed()
        {
            var enrollmentId = Guid.NewGuid();
            var command = new ConfirmEnrollmentCommand
            {
                EnrollmentId = enrollmentId,
                Confirm = true,
                Notes = "Confirmed by student"
            };

            var enrollment = new CourseOfferingEnrollment
            {
                Id = enrollmentId,
                CourseOfferingId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Status = "PendingConfirmation",
                IsActive = true,
                AttemptNumber = 1,
                ConfirmationStatus = ConfirmationStatus.Pending
            };

            _enrollmentRepoMock
                .Setup(x => x.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            _enrollmentRepoMock
                .Setup(x => x.UpdateAsync(It.IsAny<CourseOfferingEnrollment>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var handler = new ConfirmEnrollmentCommandHandler(
                _enrollmentRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<ConfirmEnrollmentCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.Id.Should().Be(enrollmentId);
            result.ConfirmationStatus.Should().Be(ConfirmationStatus.Confirmed);
            result.Status.Should().Be("Active");
            result.ConfirmedDate.Should().NotBeNull();

            _enrollmentRepoMock.Verify(x => x.UpdateAsync(enrollment, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ConfirmFalse_ShouldRevertEnrollmentToPending()
        {
            var enrollmentId = Guid.NewGuid();
            var command = new ConfirmEnrollmentCommand
            {
                EnrollmentId = enrollmentId,
                Confirm = false,
                Notes = "Need to review"
            };

            var enrollment = new CourseOfferingEnrollment
            {
                Id = enrollmentId,
                CourseOfferingId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Status = "Active",
                IsActive = true,
                AttemptNumber = 1,
                ConfirmationStatus = ConfirmationStatus.Confirmed,
                ConfirmedDate = DateTime.UtcNow
            };

            _enrollmentRepoMock
                .Setup(x => x.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            _enrollmentRepoMock
                .Setup(x => x.UpdateAsync(It.IsAny<CourseOfferingEnrollment>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var handler = new ConfirmEnrollmentCommandHandler(
                _enrollmentRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<ConfirmEnrollmentCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.ConfirmationStatus.Should().Be(ConfirmationStatus.Pending);
            result.Status.Should().Be("PendingConfirmation");
            result.ConfirmedDate.Should().BeNull();

            _enrollmentRepoMock.Verify(x => x.UpdateAsync(enrollment, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class ConfirmTeachingAssignmentCommandTests
    {
        private readonly ConfirmTeachingAssignmentCommandValidator _validator;
        private readonly Mock<ICourseOfferingLecturerRepository> _lecturerRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public ConfirmTeachingAssignmentCommandTests()
        {
            _validator = new ConfirmTeachingAssignmentCommandValidator();
            _lecturerRepoMock = new Mock<ICourseOfferingLecturerRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new ConfirmTeachingAssignmentCommand { AssignmentId = Guid.NewGuid() };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new ConfirmTeachingAssignmentCommand { AssignmentId = Guid.Empty };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.AssignmentId);
        }

        [Fact]
        public async Task Handle_WithNonExistentAssignment_ShouldThrowNotFoundException()
        {
            var command = new ConfirmTeachingAssignmentCommand { AssignmentId = Guid.NewGuid() };

            _lecturerRepoMock
                .Setup(x => x.GetByIdAsync(command.AssignmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOfferingLecturer?)null);

            var handler = new ConfirmTeachingAssignmentCommandHandler(
                _lecturerRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<ConfirmTeachingAssignmentCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ConfirmTrue_ShouldSetAssignmentToConfirmed()
        {
            var assignmentId = Guid.NewGuid();
            var command = new ConfirmTeachingAssignmentCommand
            {
                AssignmentId = assignmentId,
                Confirm = true,
                Notes = "Accepted by lecturer"
            };

            var assignment = new CourseOfferingLecturer
            {
                Id = assignmentId,
                CourseOfferingId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid(),
                Status = "PendingConfirmation",
                IsActive = true,
                IsPrimary = true,
                ConfirmationStatus = ConfirmationStatus.Pending,
                AssignmentDate = DateTime.UtcNow
            };

            _lecturerRepoMock
                .Setup(x => x.GetByIdAsync(assignmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            _lecturerRepoMock
                .Setup(x => x.UpdateAsync(It.IsAny<CourseOfferingLecturer>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var handler = new ConfirmTeachingAssignmentCommandHandler(
                _lecturerRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<ConfirmTeachingAssignmentCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.Id.Should().Be(assignmentId);
            result.IsPrimary.Should().BeTrue();
            result.IsActive.Should().BeTrue();

            assignment.ConfirmationStatus.Should().Be(ConfirmationStatus.Confirmed);
            assignment.Status.Should().Be("Active");
            assignment.ConfirmedDate.Should().NotBeNull();

            _lecturerRepoMock.Verify(x => x.UpdateAsync(assignment, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ConfirmFalse_ShouldRevertAssignmentToPending()
        {
            var assignmentId = Guid.NewGuid();
            var command = new ConfirmTeachingAssignmentCommand
            {
                AssignmentId = assignmentId,
                Confirm = false,
                Notes = "Declined"
            };

            var assignment = new CourseOfferingLecturer
            {
                Id = assignmentId,
                CourseOfferingId = Guid.NewGuid(),
                LecturerId = Guid.NewGuid(),
                Status = "Active",
                IsActive = true,
                IsPrimary = true,
                ConfirmationStatus = ConfirmationStatus.Confirmed,
                ConfirmedDate = DateTime.UtcNow,
                AssignmentDate = DateTime.UtcNow
            };

            _lecturerRepoMock
                .Setup(x => x.GetByIdAsync(assignmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            _lecturerRepoMock
                .Setup(x => x.UpdateAsync(It.IsAny<CourseOfferingLecturer>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var handler = new ConfirmTeachingAssignmentCommandHandler(
                _lecturerRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<ConfirmTeachingAssignmentCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.Id.Should().Be(assignmentId);

            assignment.ConfirmationStatus.Should().Be(ConfirmationStatus.Pending);
            assignment.Status.Should().Be("PendingConfirmation");
            assignment.ConfirmedDate.Should().BeNull();

            _lecturerRepoMock.Verify(x => x.UpdateAsync(assignment, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class ReportAssignmentIssueCommandTests
    {
        private readonly ReportAssignmentIssueCommandValidator _validator;
        private readonly Mock<IAssignmentIssueReportRepository> _issueRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public ReportAssignmentIssueCommandTests()
        {
            _validator = new ReportAssignmentIssueCommandValidator();
            _issueRepoMock = new Mock<IAssignmentIssueReportRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new ReportAssignmentIssueCommand
            {
                ReporterUserId = Guid.NewGuid(),
                AssignmentType = "Enrollment",
                CourseOfferingId = Guid.NewGuid(),
                Reason = "Wrong enrollment"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new ReportAssignmentIssueCommand
            {
                ReporterUserId = Guid.Empty,
                AssignmentType = "Invalid",
                CourseOfferingId = Guid.Empty,
                Reason = ""
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.ReporterUserId);
            result.ShouldHaveValidationErrorFor(x => x.AssignmentType);
            result.ShouldHaveValidationErrorFor(x => x.CourseOfferingId);
            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldCreateIssueReport()
        {
            var command = new ReportAssignmentIssueCommand
            {
                ReporterUserId = Guid.NewGuid(),
                AssignmentType = "Enrollment",
                CourseOfferingId = Guid.NewGuid(),
                CourseOfferingEnrollmentId = Guid.NewGuid(),
                Reason = "Student was assigned to the wrong offering"
            };

            _issueRepoMock
                .Setup(x => x.AddAsync(It.IsAny<AssignmentIssueReport>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AssignmentIssueReport r, CancellationToken ct) => r);

            var handler = new ReportAssignmentIssueCommandHandler(
                _issueRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<ReportAssignmentIssueCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.CourseOfferingId.Should().Be(command.CourseOfferingId);
            result.IssueType.Should().Be("Enrollment");
            result.Description.Should().Be("Student was assigned to the wrong offering");
            result.Status.Should().Be(AssignmentIssueStatus.Pending);
            result.ReportedDate.Should().NotBe(default(DateTime));

            _issueRepoMock.Verify(x => x.AddAsync(It.IsAny<AssignmentIssueReport>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithTeachingType_ShouldCreateTeachingIssueReport()
        {
            var command = new ReportAssignmentIssueCommand
            {
                ReporterUserId = Guid.NewGuid(),
                AssignmentType = "Teaching",
                CourseOfferingId = Guid.NewGuid(),
                CourseOfferingLecturerId = Guid.NewGuid(),
                Reason = "Lecturer assignment is incorrect"
            };

            _issueRepoMock
                .Setup(x => x.AddAsync(It.IsAny<AssignmentIssueReport>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AssignmentIssueReport r, CancellationToken ct) => r);

            var handler = new ReportAssignmentIssueCommandHandler(
                _issueRepoMock.Object,
                _unitOfWorkMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<ReportAssignmentIssueCommandHandler>>());

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.IssueType.Should().Be("Teaching");
            result.Status.Should().Be(AssignmentIssueStatus.Pending);

            _issueRepoMock.Verify(x => x.AddAsync(It.IsAny<AssignmentIssueReport>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
