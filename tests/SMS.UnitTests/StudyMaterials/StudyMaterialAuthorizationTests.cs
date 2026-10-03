using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.StudyMaterials.Commands;
using SMS.Application.Features.StudyMaterials.Queries;
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

        [Fact]
        public async Task Create_AdminWithNoLecturerProfile_ShouldThrowForbiddenNotNullReference()
        {
            // Regression: production returned HTTP 500 here. An Administrator or
            // Coordinator who does not also hold a Lecturer profile resolved a null
            // lecturer, and the handler dereferenced lecturer.Id, raising
            // NullReferenceException instead of refusing the upload.
            var access = CreateAccessService(isAdmin: true);
            // GetCurrentLecturerAsync returns null (no profile) and no
            // SpecifiedLecturerId was supplied.

            var handler = CreateHandler(access, CreateUploadService());

            var act = () => handler.Handle(CreateCommand(), CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenException>(
                "an admin with no lecturer profile must be refused, not crash the request");
        }

        [Fact]
        public async Task Create_AdminWithNoLecturerProfile_AndSpecifiedLecturerNotFound_ShouldThrowNotFound()
        {
            // SpecifiedLecturerId that resolves to nothing -> NotFound, still no crash.
            var access = CreateAccessService(isAdmin: true);
            var handler = CreateHandler(access, CreateUploadService());

            var command = CreateCommand();
            command.SpecifiedLecturerId = Guid.NewGuid();

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Save-failure compensation: the upload bytes and UploadFile metadata row
        // are already committed when the LectureNote insert runs, so a failed
        // insert must retire the orphaned upload rather than leak it forever.
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_WhenSaveFails_ShouldRetireOrphanedUploadAndRethrow()
        {
            var access = CreateAccessService(isLecturer: true, currentLecturerId: _lecturerId, teachesUnit: true);
            var upload = CreateUploadService();
            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("db down"));

            var noteRepo = new Mock<ILectureNoteRepository>();
            noteRepo.Setup(x => x.AddAsync(It.IsAny<LectureNote>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LectureNote());

            var unitRepo = new Mock<IUnitRepository>();
            unitRepo.Setup(x => x.GetByIdAsync(_unitId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Unit { Id = _unitId, Name = "Data Structures", Code = "CSC201" });

            var handler = new CreateStudyMaterialCommandHandler(
                unitRepo.Object,
                Mock.Of<ILecturerRepository>(),
                noteRepo.Object,
                upload.Object,
                access.Object,
                Mock.Of<SMS.Application.Common.Interfaces.ICurrentUserService>(),
                unitOfWork.Object,
                Mock.Of<ILogger<CreateStudyMaterialCommandHandler>>());

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(CreateCommand(), CancellationToken.None));

            upload.Verify(
                x => x.DeleteAsync(It.IsAny<Guid>(), "system"),
                Times.Once,
                "the orphaned upload metadata must be retired when the insert fails");
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetMyStudyMaterialUnits — the Academics → Study Materials unit selector.
        // It must expose exactly the units the API would authorize: units the
        // lecturer is appointed to teach, units the student is enrolled in, and
        // nothing else.
        // ─────────────────────────────────────────────────────────────────────

        private static Mock<IAcademicAccessService> CreateSelectorAccess(
            bool isAdmin = false, bool isLecturer = false, bool isStudent = false,
            Guid? lecturerId = null, Guid? studentId = null,
            RegistrationStatus lecturerStatus = RegistrationStatus.Approved)
        {
            var access = new Mock<IAcademicAccessService>();
            access.Setup(x => x.IsAdminOrCoordinator()).Returns(isAdmin);
            access.Setup(x => x.IsLecturerRole()).Returns(isLecturer);
            access.Setup(x => x.IsStudentRole()).Returns(isStudent);
            // The registration status matters: privileged teaching entitlement is
            // gated on the lecturer being Approved, exactly as the real
            // AcademicAccessService derives it.
            access.Setup(x => x.GetCurrentLecturerAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(lecturerId.HasValue
                    ? new Lecturer
                    {
                        Id = lecturerId.Value,
                        FirstName = "L",
                        LastName = "T",
                        RegistrationStatus = lecturerStatus
                    }
                    : null);
            access.Setup(x => x.GetCurrentStudentAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(studentId.HasValue
                    ? new Student { Id = studentId.Value, FirstName = "S", LastName = "T" }
                    : null);
            return access;
        }

        private static Mock<IUnitRepository> CreateUnitRepoReturning(params Guid[] presentIds)
        {
            var present = new HashSet<Guid>(presentIds);
            var unitRepo = new Mock<IUnitRepository>();
            unitRepo.Setup(x => x.GetUnitsByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                    ids.Where(present.Contains)
                        .Select(id => new Unit
                        {
                            Id = id,
                            Code = "CSC201",
                            Name = "Data Structures",
                            Credits = 3,
                            Course = new Course { Name = "Computer Science" }
                        })
                        .ToList());
            return unitRepo;
        }

        private static GetMyStudyMaterialUnitsQueryHandler CreateSelectorHandler(
            Mock<IAcademicAccessService> access,
            IEnumerable<Guid> taughtUnitIds,
            IEnumerable<Guid> enrolledUnitIds,
            Mock<IUnitRepository>? unitRepo = null)
        {
            var lecturerRepo = new Mock<ILecturerRepository>();
            lecturerRepo.Setup(x => x.GetTaughtUnitIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(taughtUnitIds);

            var studentRepo = new Mock<IStudentRepository>();
            studentRepo.Setup(x => x.GetEnrolledUnitIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrolledUnitIds);

            return new GetMyStudyMaterialUnitsQueryHandler(
                (unitRepo ?? CreateUnitRepoReturning()).Object,
                lecturerRepo.Object,
                studentRepo.Object,
                access.Object,
                Mock.Of<SMS.Application.Common.Interfaces.ICurrentUserService>(),
                Mock.Of<ILogger<GetMyStudyMaterialUnitsQueryHandler>>());
        }

        [Fact]
        public async Task MyUnits_Lecturer_ShouldReturnOnlyTaughtUnits()
        {
            var taught = Guid.NewGuid();
            var access = CreateSelectorAccess(isLecturer: true, lecturerId: _lecturerId);

            var handler = CreateSelectorHandler(
                access,
                taughtUnitIds: new[] { taught },
                enrolledUnitIds: Array.Empty<Guid>(),
                unitRepo: CreateUnitRepoReturning(taught));

            var result = await handler.Handle(new GetMyStudyMaterialUnitsQuery(), CancellationToken.None);

            result.Should().HaveCount(1);
            result[0].UnitId.Should().Be(taught);
            result[0].AccessRole.Should().Be("Lecturer");
        }

        [Fact]
        public async Task MyUnits_Student_ShouldReturnOnlyEnrolledUnits()
        {
            var enrolled = Guid.NewGuid();
            var access = CreateSelectorAccess(isStudent: true, studentId: Guid.NewGuid());

            var handler = CreateSelectorHandler(
                access,
                taughtUnitIds: Array.Empty<Guid>(),
                enrolledUnitIds: new[] { enrolled },
                unitRepo: CreateUnitRepoReturning(enrolled));

            var result = await handler.Handle(new GetMyStudyMaterialUnitsQuery(), CancellationToken.None);

            result.Should().HaveCount(1);
            result[0].UnitId.Should().Be(enrolled);
            result[0].AccessRole.Should().Be("Student");
        }

        [Fact]
        public async Task MyUnits_ShouldNeverReturnAUnitOutsideTheCallersRelationships()
        {
            // A unit belonging to another lecturer or another tenant is never part
            // of the caller's entitlement set, so its id is never even submitted to
            // the (tenant-scoped) unit lookup.
            var entitled = Guid.NewGuid();
            var notEntitled = Guid.NewGuid();
            var access = CreateSelectorAccess(isLecturer: true, lecturerId: _lecturerId);

            var handler = CreateSelectorHandler(
                access,
                taughtUnitIds: new[] { entitled },
                enrolledUnitIds: Array.Empty<Guid>(),
                unitRepo: CreateUnitRepoReturning(entitled, notEntitled));

            var result = await handler.Handle(new GetMyStudyMaterialUnitsQuery(), CancellationToken.None);

            result.Should().HaveCount(1);
            result.Should().NotContain(u => u.UnitId == notEntitled);
        }

        [Fact]
        public async Task MyUnits_ReceptionistLikeRole_ShouldThrowForbidden()
        {
            var access = CreateSelectorAccess(); // no lecturer/student/admin role

            var handler = CreateSelectorHandler(
                access,
                taughtUnitIds: new[] { Guid.NewGuid() },
                enrolledUnitIds: new[] { Guid.NewGuid() });

            await Assert.ThrowsAsync<ForbiddenException>(
                () => handler.Handle(new GetMyStudyMaterialUnitsQuery(), CancellationToken.None));
        }

        [Fact]
        public async Task MyUnits_AuthorizedRoleWithNoRelationships_ShouldReturnEmpty()
        {
            // Holds the Lecturer role but has no persisted teaching relationship.
            var access = CreateSelectorAccess(isLecturer: true);

            var handler = CreateSelectorHandler(
                access,
                taughtUnitIds: Array.Empty<Guid>(),
                enrolledUnitIds: Array.Empty<Guid>());

            var result = await handler.Handle(new GetMyStudyMaterialUnitsQuery(), CancellationToken.None);

            result.Should().BeEmpty();
        }
    }
}