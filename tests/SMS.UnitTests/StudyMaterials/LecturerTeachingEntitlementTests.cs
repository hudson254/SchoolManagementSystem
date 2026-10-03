using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.StudyMaterials.Commands;
using SMS.Application.Features.StudyMaterials.Queries;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using Xunit;
using ICurrentUserService = SMS.Application.Common.Interfaces.ICurrentUserService;
using ITenantContext = SMS.Domain.Interfaces.ITenantContext;

namespace SMS.UnitTests.StudyMaterials
{
    /// <summary>
    /// D4 + D5 regression.
    /// <para>
    /// D4 - a lecturer still in <c>PendingApproval</c> could create study materials,
    /// including materials for another lecturer's unit. Root cause:
    /// <c>LecturerRepository.GetTaughtUnitIdsAsync</c> expanded every
    /// <c>CourseOfferingLecturer</c> row - filtering only <c>IsActive</c>, never
    /// <c>Status</c> - to EVERY unit of that offering. Registration writes the
    /// assignment as <c>PendingConfirmation</c>, so a pending lecturer resolved the
    /// whole offering, including units allocated to a colleague.
    /// </para>
    /// <para>
    /// D5 - Lecturer B deleted Lecturer A's material (HTTP 204, persisted). Root
    /// cause: the delete rule was purely unit-scoped
    /// (<c>currentLecturer.Id == material.LecturerId || LecturerTeachesUnitAsync(...)</c>),
    /// and a unit-level entitlement is shared by every lecturer appointed to it.
    /// </para>
    /// </summary>
    public class LecturerTeachingEntitlementTests
    {
        private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private static ApplicationDbContext CreateContext()
        {
            var currentUser = new Mock<SMS.Domain.Interfaces.ICurrentUserService>();
            currentUser.Setup(x => x.UserId).Returns("unit-test-user");
            currentUser.Setup(x => x.Username).Returns("unit-test");
            currentUser.Setup(x => x.Email).Returns("unit-test@school.test");
            currentUser.Setup(x => x.IsAuthenticated).Returns(true);
            currentUser.Setup(x => x.Roles).Returns(new[] { "Lecturer" });

            var tenant = new Mock<ITenantContext>();
            tenant.Setup(x => x.TenantId).Returns(TenantId.ToString());

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TaughtUnits_{Guid.NewGuid():N}")
                .Options;

            return new ApplicationDbContext(options, currentUser.Object, tenant.Object);
        }

        private static LecturerRepository CreateRepository(ApplicationDbContext context) =>
            new(context, NullLoggerFactory.Instance.CreateLogger<LecturerRepository>());

        private static UnitAllocation Allocation(
            Guid lecturerId, Guid unitId, string status, Guid? offeringId = null) =>
            new()
            {
                LecturerId = lecturerId,
                UnitId = unitId,
                SemesterId = Guid.NewGuid(),
                CourseOfferingId = offeringId,
                Status = status,
                TenantId = TenantId
            };

        private static CourseOfferingLecturer Assignment(
            Guid lecturerId, Guid offeringId, string status) =>
            new()
            {
                LecturerId = lecturerId,
                CourseOfferingId = offeringId,
                Status = status,
                IsActive = true,
                ConfirmationStatus = status == "Active" ? ConfirmationStatus.Confirmed : ConfirmationStatus.Pending,
                TenantId = TenantId
            };

        // ─────────────────────────────────────────────────────────────────────
        // The derivation itself (the root cause of D4)
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task TaughtUnits_ExcludeAPendingAllocation()
        {
            using var context = CreateContext();
            var lecturerId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            context.UnitAllocations.Add(Allocation(lecturerId, unitId, "PendingApproval"));
            await context.SaveChangesAsync();

            var taught = await CreateRepository(context).GetTaughtUnitIdsAsync(lecturerId);

            taught.Should().BeEmpty(
                "registration writes PendingApproval allocations and approval is what activates them");
        }

        [Fact]
        public async Task TaughtUnits_ExcludeAnAllocationWhoseTeachingAssignmentIsStillPendingConfirmation()
        {
            using var context = CreateContext();
            var lecturerId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            var unitId = Guid.NewGuid();

            context.UnitAllocations.Add(Allocation(lecturerId, unitId, "Active", offeringId));
            context.CourseOfferingLecturers.Add(Assignment(lecturerId, offeringId, "PendingConfirmation"));
            await context.SaveChangesAsync();

            var taught = await CreateRepository(context).GetTaughtUnitIdsAsync(lecturerId);

            taught.Should().BeEmpty(
                "the allocation is active but the teaching assignment has not been approved yet");
        }

        [Fact]
        public async Task TaughtUnits_IncludeTheUnit_WhenBothTheAllocationAndTheTeachingAssignmentAreActive()
        {
            using var context = CreateContext();
            var lecturerId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            var unitId = Guid.NewGuid();

            context.UnitAllocations.Add(Allocation(lecturerId, unitId, "Active", offeringId));
            context.CourseOfferingLecturers.Add(Assignment(lecturerId, offeringId, "Active"));
            await context.SaveChangesAsync();

            var taught = await CreateRepository(context).GetTaughtUnitIdsAsync(lecturerId);

            taught.Should().ContainSingle().Which.Should().Be(unitId);
        }

        [Fact]
        public async Task TaughtUnits_NeverIncludeAnotherLecturersUnitFromTheSameOffering()
        {
            // THE D4 SCENARIO: lecturer A selects U1; lecturer B selects U3 against the
            // same offering. B's registration added U3 to the SHARED offering snapshot,
            // which the old "every offering unit" expansion handed to A as well.
            using var context = CreateContext();
            var lecturerA = Guid.NewGuid();
            var lecturerB = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            var unitOne = Guid.NewGuid();
            var unitThree = Guid.NewGuid();

            context.UnitAllocations.Add(Allocation(lecturerA, unitOne, "Active", offeringId));
            context.UnitAllocations.Add(Allocation(lecturerB, unitThree, "Active", offeringId));
            context.CourseOfferingLecturers.Add(Assignment(lecturerA, offeringId, "Active"));
            context.CourseOfferingLecturers.Add(Assignment(lecturerB, offeringId, "Active"));

            // The shared offering snapshot legitimately contains both units.
            context.CourseOfferingUnits.Add(new CourseOfferingUnit
            {
                CourseOfferingId = offeringId,
                UnitId = unitOne,
                Name = "Unit 1",
                Code = "U1",
                Order = 1,
                IsActive = true,
                TenantId = TenantId
            });
            context.CourseOfferingUnits.Add(new CourseOfferingUnit
            {
                CourseOfferingId = offeringId,
                UnitId = unitThree,
                Name = "Unit 3",
                Code = "U3",
                Order = 3,
                IsActive = true,
                TenantId = TenantId
            });
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);

            var taughtByA = (await repository.GetTaughtUnitIdsAsync(lecturerA)).ToList();
            var taughtByB = (await repository.GetTaughtUnitIdsAsync(lecturerB)).ToList();

            taughtByA.Should().ContainSingle().Which.Should().Be(unitOne);
            taughtByA.Should().NotContain(unitThree);
            taughtByB.Should().ContainSingle().Which.Should().Be(unitThree);
            taughtByB.Should().NotContain(unitOne);
        }

        [Fact]
        public async Task TaughtUnits_IncludeADirectAllocationThatHasNoCourseOffering()
        {
            using var context = CreateContext();
            var lecturerId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            context.UnitAllocations.Add(Allocation(lecturerId, unitId, "Active"));
            await context.SaveChangesAsync();

            var taught = await CreateRepository(context).GetTaughtUnitIdsAsync(lecturerId);

            taught.Should().ContainSingle().Which.Should().Be(unitId,
                "an administrative appointment without an offering stands on its own");
        }

        // ─────────────────────────────────────────────────────────────────────
        // The approval gate in the access service
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task PendingLecturer_IsNotEntitledToAnyUnit_EvenWithAnActiveAllocation()
        {
            var lecturerId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            var service = CreateAccessService(
                lecturer: Lecturer(lecturerId, RegistrationStatus.PendingApproval),
                taughtUnitIds: new[] { unitId });

            var teaches = await service.LecturerTeachesUnitAsync(lecturerId, unitId);

            teaches.Should().BeFalse(
                "a lecturer awaiting approval must not perform privileged teaching actions");
        }

        [Theory]
        [InlineData(RegistrationStatus.PendingCourseSelection)]
        [InlineData(RegistrationStatus.PendingApproval)]
        [InlineData(RegistrationStatus.Rejected)]
        public async Task NotApprovedLecturer_IsNeverEntitled(RegistrationStatus status)
        {
            var lecturerId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            var service = CreateAccessService(
                lecturer: Lecturer(lecturerId, status),
                taughtUnitIds: new[] { unitId });

            (await service.LecturerTeachesUnitAsync(lecturerId, unitId)).Should().BeFalse();
        }

        [Fact]
        public async Task ApprovedLecturer_IsEntitledToTheirOwnActiveUnit()
        {
            var lecturerId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            var service = CreateAccessService(
                lecturer: Lecturer(lecturerId, RegistrationStatus.Approved),
                taughtUnitIds: new[] { unitId });

            (await service.LecturerTeachesUnitAsync(lecturerId, unitId)).Should().BeTrue();
        }

        [Fact]
        public async Task ApprovedLecturer_IsNotEntitledToAnUnassignedUnit()
        {
            var lecturerId = Guid.NewGuid();
            var assigned = Guid.NewGuid();
            var unassigned = Guid.NewGuid();
            var service = CreateAccessService(
                lecturer: Lecturer(lecturerId, RegistrationStatus.Approved),
                taughtUnitIds: new[] { assigned });

            (await service.LecturerTeachesUnitAsync(lecturerId, unassigned)).Should().BeFalse();
        }

        [Fact]
        public async Task UnknownLecturer_IsNotEntitled()
        {
            var unitId = Guid.NewGuid();
            var service = CreateAccessService(lecturer: null, taughtUnitIds: new[] { unitId });

            (await service.LecturerTeachesUnitAsync(Guid.NewGuid(), unitId)).Should().BeFalse();
        }

        [Fact]
        public async Task PendingLecturer_IsOfferedNoStudyMaterialUnits()
        {
            // D4, read path: the unit selector must not advertise units whose uploads
            // the API would refuse.
            var lecturerId = Guid.NewGuid();
            var unitId = Guid.NewGuid();

            var access = new Mock<IAcademicAccessService>();
            access.Setup(x => x.IsLecturerRole()).Returns(true);
            access.Setup(x => x.GetCurrentLecturerAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Lecturer(lecturerId, RegistrationStatus.PendingApproval));

            var handler = CreateSelectorHandler(access, lecturerId, unitId, new[] { unitId });

            var result = await handler.Handle(new GetMyStudyMaterialUnitsQuery(), CancellationToken.None);

            result.Should().BeEmpty();
        }

        // ─────────────────────────────────────────────────────────────────────
        // D5 - object-level authorization on delete
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task LecturerB_CannotDeleteLecturerAsMaterial_EvenWhenTeachingTheSameUnit()
        {
            var material = BuildMaterial(lecturerId: Guid.NewGuid());
            var lecturerB = Guid.NewGuid();
            var handler = CreateDeleteHandler(
                currentLecturerId: lecturerB,
                teachesUnit: true,   // <-- the decisive detail: B does teach the unit
                material: material);

            var act = () => handler.Handle(BuildDeleteCommand(material), CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenException>(
                "a shared unit entitlement is not ownership of somebody else's material");

            material.IsDeleted.Should().BeFalse("the material must survive the rejected delete");
            material.IsPublished.Should().BeTrue();
        }

        [Fact]
        public async Task LecturerA_CanDeleteTheirOwnMaterialForAnAssignedUnit()
        {
            var lecturerA = Guid.NewGuid();
            var material = BuildMaterial(lecturerId: lecturerA);
            var handler = CreateDeleteHandler(currentLecturerId: lecturerA, teachesUnit: true, material: material);

            var deleted = await handler.Handle(BuildDeleteCommand(material), CancellationToken.None);

            deleted.Should().BeTrue();
            material.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task LecturerA_CannotDeleteTheirOwnMaterialOnceTheyLostTheUnit()
        {
            // Ownership alone is not enough: the appointment must still be approved
            // and active, otherwise a stale material row stays manageable forever.
            var lecturerA = Guid.NewGuid();
            var material = BuildMaterial(lecturerId: lecturerA);
            var handler = CreateDeleteHandler(currentLecturerId: lecturerA, teachesUnit: false, material: material);

            var act = () => handler.Handle(BuildDeleteCommand(material), CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenException>();
            material.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task AdminOrCoordinator_DeleteOverrideIsPreserved()
        {
            var material = BuildMaterial(lecturerId: Guid.NewGuid());
            var handler = CreateDeleteHandler(
                currentLecturerId: null, isAdmin: true, teachesUnit: false, material: material);

            var deleted = await handler.Handle(BuildDeleteCommand(material), CancellationToken.None);

            deleted.Should().BeTrue("the administrative override is unchanged");
        }

        [Fact]
        public async Task Student_CannotDeleteAnyMaterial()
        {
            var material = BuildMaterial(lecturerId: Guid.NewGuid());
            var handler = CreateDeleteHandler(
                currentLecturerId: null, isStudent: true, teachesUnit: false, material: material);

            var act = () => handler.Handle(BuildDeleteCommand(material), CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenException>();
            material.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task Delete_RejectsAUnitIdThatDoesNotMatchTheStoredMaterial()
        {
            var material = BuildMaterial(lecturerId: Guid.NewGuid());
            var handler = CreateDeleteHandler(
                currentLecturerId: material.LecturerId, teachesUnit: true, material: material);

            var act = () => handler.Handle(
                new DeleteStudyMaterialCommand { MaterialId = material.Id, UnitId = Guid.NewGuid() },
                CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>(
                "the stored UnitId, never the query parameter, decides the relationship");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static Lecturer Lecturer(Guid id, RegistrationStatus status) =>
            new()
            {
                Id = id,
                FirstName = "Verification",
                LastName = "Lecturer",
                Email = $"{id}@school.test",
                RegistrationStatus = status,
                IsActive = true
            };

        private static AcademicAccessService CreateAccessService(
            Lecturer? lecturer, IEnumerable<Guid> taughtUnitIds)
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(x => x.IsAuthenticated).Returns(true);
            currentUser.Setup(x => x.Email).Returns(lecturer?.Email ?? "unknown@school.test");
            currentUser.Setup(x => x.UserId).Returns(lecturer?.Id.ToString());
            currentUser.Setup(x => x.Roles).Returns(new[] { "Lecturer" });

            var lecturerRepo = new Mock<ILecturerRepository>();
            lecturerRepo.Setup(x => x.GetLecturerByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(lecturer);
            var lecturerIdToUse = lecturer?.Id ?? Guid.Empty;
            lecturerRepo.Setup(x => x.GetByIdAsync(lecturerIdToUse, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lecturer);
            lecturerRepo.Setup(x => x.GetTaughtUnitIdsAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(taughtUnitIds);

            return new AcademicAccessService(currentUser.Object, lecturerRepo.Object, Mock.Of<IStudentRepository>());
        }

        private static LectureNote BuildMaterial(Guid lecturerId) =>
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Lecturer A lecture notes",
                UnitId = Guid.NewGuid(),
                LecturerId = lecturerId,
                FileName = "notes.pdf",
                FilePath = "notes/notes.pdf",
                IsPublished = true,
                UploadDate = DateTime.UtcNow,
                TenantId = TenantId
            };

        private static DeleteStudyMaterialCommand BuildDeleteCommand(LectureNote material) =>
            new() { MaterialId = material.Id, UnitId = material.UnitId };

        private static DeleteStudyMaterialCommandHandler CreateDeleteHandler(
            Guid? currentLecturerId,
            bool teachesUnit,
            LectureNote material,
            bool isAdmin = false,
            bool isStudent = false)
        {
            var access = new Mock<IAcademicAccessService>();
            access.Setup(x => x.IsAdminOrCoordinator()).Returns(isAdmin);
            access.Setup(x => x.IsLecturerRole()).Returns(!isAdmin && !isStudent);
            access.Setup(x => x.IsStudentRole()).Returns(isStudent);
            access.Setup(x => x.GetCurrentLecturerAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentLecturerId.HasValue
                    ? Lecturer(currentLecturerId.Value, RegistrationStatus.Approved)
                    : null);
            access.Setup(x => x.LecturerTeachesUnitAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(teachesUnit);

            var noteRepository = new Mock<ILectureNoteRepository>();
            noteRepository.Setup(x => x.GetByIdAsync(material.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(material);

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(x => x.UserId).Returns(currentLecturerId?.ToString() ?? "admin-user-id");

            return new DeleteStudyMaterialCommandHandler(
                noteRepository.Object,
                Mock.Of<IUploadService>(),
                access.Object,
                currentUser.Object,
                Mock.Of<IUnitOfWork>(),
                NullLoggerFactory.Instance.CreateLogger<DeleteStudyMaterialCommandHandler>());
        }

        private static GetMyStudyMaterialUnitsQueryHandler CreateSelectorHandler(
            Mock<IAcademicAccessService> access, Guid lecturerId, Guid unitId, Guid[] taughtUnitIds)
        {
            var lecturerRepository = new Mock<ILecturerRepository>();
            lecturerRepository.Setup(x => x.GetTaughtUnitIdsAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(taughtUnitIds);

            var unitRepository = new Mock<IUnitRepository>();
            unitRepository.Setup(x => x.GetUnitsByIdsAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                    ids.Where(taughtUnitIds.Contains)
                        .Select(id => new Unit { Id = id, Code = "U1", Name = "Unit 1" })
                        .ToList());

            return new GetMyStudyMaterialUnitsQueryHandler(
                unitRepository.Object,
                lecturerRepository.Object,
                Mock.Of<IStudentRepository>(),
                access.Object,
                Mock.Of<ICurrentUserService>(),
                NullLoggerFactory.Instance.CreateLogger<GetMyStudyMaterialUnitsQueryHandler>());
        }
    }
}
