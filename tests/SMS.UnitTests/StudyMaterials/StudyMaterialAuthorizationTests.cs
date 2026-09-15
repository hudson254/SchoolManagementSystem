using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.StudyMaterials.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.StudyMaterials
{
    public class StudyMaterialAuthorizationTests
    {
        private static readonly Guid _unitId = Guid.NewGuid();
        private readonly Guid _lecturerId = Guid.NewGuid();
        private readonly Guid _otherLecturerId = Guid.NewGuid();

        private static Stream PdfStream() =>
            new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 });

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
            return access;
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
                    GeneratedFileName = "notes_v1.pdf",
                    StoragePath = "notes/notes_v1.pdf",
                    OriginalFileName = "lecture.pdf",
                    Extension = ".pdf",
                    MimeType = "application/pdf",
                    FileSizeBytes = 100,
                    Sha256Hash = new string('b', 64),
                    Version = 1,
                    Status = "Active"
                });
            return upload;
        }

        private CreateStudyMaterialCommandHandler CreateHandler(
            Mock<IAcademicAccessService> access, Mock<IUploadService> upload,
            bool unitExists = true, Guid? specifiedLecturerResolved = null)
        {
            var unitRepo = new Mock<IUnitRepository>();
            if (unitExists)
            {
                unitRepo.Setup(x => x.GetByIdAsync(_unitId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Unit { Id = _unitId, Name = "Data Structures", Code = "CSC201" });
            }
            else
            {
                unitRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Unit?)null);
            }

            var lecturerRepo = new Mock<ILecturerRepository>();
            if (specifiedLecturerResolved.HasValue)
            {
                lecturerRepo.Setup(x => x.GetByIdAsync(specifiedLecturerResolved.Value, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Lecturer { Id = specifiedLecturerResolved.Value });
            }

            var noteRepo = new Mock<ILectureNoteRepository>();
            noteRepo.Setup(x => x.AddAsync(It.IsAny<LectureNote>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LectureNote());

            return new CreateStudyMaterialCommandHandler(
                unitRepo.Object,
                lecturerRepo.Object,
                noteRepo.Object,
                upload.Object,
                access.Object,
                Mock.Of<SMS.Application.Common.Interfaces.ICurrentUserService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ILogger<CreateStudyMaterialCommandHandler>>());
        }

        private static CreateStudyMaterialCommand CreateCommand() =>
            new()
            {
                UnitId = _unitId,
                Title = "Lecture Notes 1",
                FileStream = PdfStream(),
                OriginalFileName = "lecture.pdf"
            };

        [Fact]
        public async Task Create_LecturerTeachesUnit_ShouldSucceed()
        {
            var access = CreateAccessService(isLecturer: true, currentLecturerId: _lecturerId, teachesUnit: true);
            var handler = CreateHandler(access, CreateUploadService());

            var result = await handler.Handle(CreateCommand(), CancellationToken.None);

            result.Should().NotBeNull();
            result.Title.Should().Be("Lecture Notes 1");
            result.UnitId.Should().Be(_unitId);
        }

        [Fact]
        public async Task Create_LecturerNotTeachingUnit_ShouldThrowForbidden()
        {
            var access = CreateAccessService(isLecturer: true, currentLecturerId: _lecturerId, teachesUnit: false);
            var handler = CreateHandler(access, CreateUploadService());

            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(CreateCommand(), CancellationToken.None));
        }

        [Fact]
        public async Task Create_StudentRole_ShouldThrowForbidden()
        {
            var access = CreateAccessService(isStudent: true);
            var handler = CreateHandler(access, CreateUploadService());

            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(CreateCommand(), CancellationToken.None));
        }

        [Fact]
        public async Task Create_AdminSpecifiedLecturer_ShouldThrowsForbidden_WhenLecturerNotTeachingUnit()
        {
            var access = CreateAccessService(isAdmin: true);
            var handler = CreateHandler(access, CreateUploadService(), specifiedLecturerResolved: _otherLecturerId);

            var command = CreateCommand();
            command.SpecifiedLecturerId = _otherLecturerId;

            // The specified lecturer does not teach the unit → authorized admin
            // still cannot attach material to an unrelated unit.
            var mockAccess = Moq.Mock.Get(access.Object);
            mockAccess.Setup(x => x.LecturerTeachesUnitAsync(
                    _otherLecturerId, _unitId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Create_AdminSpecifiedLecturer_ShouldSucceed_WhenLecturerTeachesUnit()
        {
            var access = CreateAccessService(isAdmin: true);
            var handler = CreateHandler(access, CreateUploadService(), specifiedLecturerResolved: _otherLecturerId);

            var command = CreateCommand();
            command.SpecifiedLecturerId = _otherLecturerId;

            var mockAccess = Moq.Mock.Get(access.Object);
            mockAccess.Setup(x => x.LecturerTeachesUnitAsync(
                    _otherLecturerId, _unitId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_NonExistentUnit_ShouldThrowNotFound()
        {
            var access = CreateAccessService(isLecturer: true, currentLecturerId: _lecturerId, teachesUnit: true);
            var handler = CreateHandler(access, CreateUploadService(), unitExists: false);

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(CreateCommand(), CancellationToken.None));
        }

        [Fact]
        public async Task Create_InvalidFile_ShouldThrowValidation()
        {
            var access = CreateAccessService(isLecturer: true, currentLecturerId: _lecturerId, teachesUnit: true);
            var handler = CreateHandler(access, CreateUploadService(valid: false));

            await Assert.ThrowsAsync<SMS.Application.Exceptions.ValidationException>(() => handler.Handle(
                new CreateStudyMaterialCommand
                {
                    UnitId = _unitId,
                    Title = "Bad File",
                    FileStream = PdfStream(),
                    OriginalFileName = "bad.exe"
                },
                CancellationToken.None));
        }
    }
}