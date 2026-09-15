using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.Assignments.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Assignments
{
    public class AssignmentDocumentAuthorizationTests
    {
        private readonly Guid _assignmentId = Guid.NewGuid();
        private readonly Guid _unitId = Guid.NewGuid();
        private readonly Guid _ownerLecturerId = Guid.NewGuid();
        private readonly Guid _otherLecturerId = Guid.NewGuid();

        private static Stream PdfStream() =>
            new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 });

        private Assignment CreateAssignment(Guid? lecturerId = null)
        {
            return new Assignment
            {
                Id = _assignmentId,
                UnitId = _unitId,
                LecturerId = lecturerId ?? _ownerLecturerId,
                Title = "Test Assignment",
                MaxScore = 100,
                Weight = 20,
                DueDate = DateTime.UtcNow.AddDays(7)
            };
        }

        private static Mock<IAcademicAccessService> CreateAccessService(
            bool isAdmin = false, bool isLecturer = false, bool isStudent = false,
            Guid? currentLecturerId = null, bool teachesUnit = false)
        {
            var access = new Mock<IAcademicAccessService>();
            access.Setup(x => x.IsAdminOrCoordinator()).Returns(isAdmin);
            access.Setup(x => x.IsLecturerRole()).Returns(isLecturer);
            access.Setup(x => x.IsStudentRole()).Returns(isStudent);
            if (currentLecturerId.HasValue)
            {
                access.Setup(x => x.GetCurrentLecturerAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Lecturer { Id = currentLecturerId.Value, FirstName = "L", LastName = "T" });
            }
            access.Setup(x => x.LecturerTeachesUnitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(teachesUnit);
            access.Setup(x => x.StudentEnrolledInUnitAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            return access;
        }

        private static Mock<IAssignmentRepository> CreateAssignmentRepo(Assignment assignment)
        {
            var repo = new Mock<IAssignmentRepository>();
            repo.Setup(x => x.GetAssignmentWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);
            return repo;
        }

        private static Mock<IUploadService> CreateUploadService(bool valid = true)
        {
            var upload = new Mock<IUploadService>();
            upload.Setup(x => x.ValidateAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<UploadCategory>()))
                .ReturnsAsync(new UploadValidationResult
                {
                    IsValid = valid,
                    ErrorMessage = valid ? null : "Invalid file.",
                    DetectedMimeType = "application/pdf",
                    FileSizeBytes = 100
                });
            upload.Setup(x => x.UploadAsync(
                    It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<UploadCategory>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UploadContext>()))
                .ReturnsAsync(new UploadResult
                {
                    FileId = Guid.NewGuid(),
                    GeneratedFileName = "brief_v1.pdf",
                    StoragePath = "briefs/brief_v1.pdf",
                    OriginalFileName = "questions.pdf",
                    Extension = ".pdf",
                    MimeType = "application/pdf",
                    FileSizeBytes = 100,
                    Sha256Hash = new string('a', 64),
                    Version = 1,
                    Status = "Active"
                });
            return upload;
        }

        private static UploadAssignmentDocumentCommandHandler CreateUploadHandler(
            Assignment assignment, Mock<IAcademicAccessService> access, Mock<IUploadService> upload)
        {
            return new UploadAssignmentDocumentCommandHandler(
                CreateAssignmentRepo(assignment).Object,
                upload.Object,
                access.Object,
                Mock.Of<SMS.Application.Common.Interfaces.ICurrentUserService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ILogger<UploadAssignmentDocumentCommandHandler>>());
        }

        [Fact]
        public async Task Upload_OwnerLecturer_ShouldSucceed()
        {
            var access = CreateAccessService(isLecturer: true, currentLecturerId: _ownerLecturerId);
            var handler = CreateUploadHandler(CreateAssignment(), access, CreateUploadService());

            var result = await handler.Handle(new UploadAssignmentDocumentCommand
            {
                AssignmentId = _assignmentId,
                FileStream = PdfStream(),
                OriginalFileName = "questions.pdf"
            }, CancellationToken.None);

            result.Should().NotBeNull();
            result.AssignmentId.Should().Be(_assignmentId);
            result.OriginalFileName.Should().Be("questions.pdf");
        }

        [Fact]
        public async Task Upload_LecturerTeachingUnit_ShouldSucceed()
        {
            var access = CreateAccessService(isLecturer: true, currentLecturerId: _otherLecturerId, teachesUnit: true);
            var handler = CreateUploadHandler(CreateAssignment(), access, CreateUploadService());

            var result = await handler.Handle(new UploadAssignmentDocumentCommand
            {
                AssignmentId = _assignmentId,
                FileStream = PdfStream(),
                OriginalFileName = "questions.pdf"
            }, CancellationToken.None);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Upload_LecturerNotTeachingUnit_ShouldThrowForbidden()
        {
            var access = CreateAccessService(isLecturer: true, currentLecturerId: _otherLecturerId, teachesUnit: false);
            var handler = CreateUploadHandler(CreateAssignment(), access, CreateUploadService());

            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
                new UploadAssignmentDocumentCommand
                {
                    AssignmentId = _assignmentId,
                    FileStream = PdfStream(),
                    OriginalFileName = "questions.pdf"
                },
                CancellationToken.None));
        }

        [Fact]
        public async Task Upload_StudentRole_ShouldThrowForbidden()
        {
            var access = CreateAccessService(isStudent: true);
            var handler = CreateUploadHandler(CreateAssignment(), access, CreateUploadService());

            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
                new UploadAssignmentDocumentCommand
                {
                    AssignmentId = _assignmentId,
                    FileStream = PdfStream(),
                    OriginalFileName = "questions.pdf"
                },
                CancellationToken.None));
        }

        [Fact]
        public async Task Upload_AdminCoordinator_ShouldSucceed()
        {
            var access = CreateAccessService(isAdmin: true);
            var handler = CreateUploadHandler(CreateAssignment(), access, CreateUploadService());

            var result = await handler.Handle(new UploadAssignmentDocumentCommand
            {
                AssignmentId = _assignmentId,
                FileStream = PdfStream(),
                OriginalFileName = "questions.pdf"
            }, CancellationToken.None);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Upload_NonExistentAssignment_ShouldThrowNotFound()
        {
            var repo = new Mock<IAssignmentRepository>();
            repo.Setup(x => x.GetAssignmentWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Assignment?)null);
            var handler = new UploadAssignmentDocumentCommandHandler(
                repo.Object,
                CreateUploadService().Object,
                CreateAccessService(isLecturer: true, currentLecturerId: _ownerLecturerId).Object,
                Mock.Of<SMS.Application.Common.Interfaces.ICurrentUserService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ILogger<UploadAssignmentDocumentCommandHandler>>());

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
                new UploadAssignmentDocumentCommand
                {
                    AssignmentId = Guid.NewGuid(),
                    FileStream = PdfStream(),
                    OriginalFileName = "questions.pdf"
                },
                CancellationToken.None));
        }

        [Fact]
        public async Task Upload_InvalidFile_ShouldThrowValidation()
        {
            var access = CreateAccessService(isLecturer: true, currentLecturerId: _ownerLecturerId);
            var handler = CreateUploadHandler(CreateAssignment(), access, CreateUploadService(valid: false));

            await Assert.ThrowsAsync<SMS.Application.Exceptions.ValidationException>(() => handler.Handle(
                new UploadAssignmentDocumentCommand
                {
                    AssignmentId = _assignmentId,
                    FileStream = PdfStream(),
                    OriginalFileName = "malware.exe"
                },
                CancellationToken.None));
        }
    }
}