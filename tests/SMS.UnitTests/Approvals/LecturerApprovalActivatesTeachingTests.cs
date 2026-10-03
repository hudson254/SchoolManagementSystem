using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.Approvals.Commands;
using SMS.Application.Features.Dashboard.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Approvals
{
    /// <summary>
    /// D3 regression: after administrator approval the lecturer dashboard courses
    /// list remained empty.
    /// <para>
    /// Root cause: registration writes the <c>course_offering_lecturers</c> row as
    /// <c>PendingConfirmation</c>, and <c>GetActiveByLecturerAsync</c> - which the
    /// lecturer dashboard reads - filters <c>Status == "Active"</c>. The approval
    /// handler flipped <c>Lecturer.RegistrationStatus</c> and
    /// <c>UnitAllocation.Status</c> but never touched
    /// <c>course_offering_lecturers</c>, so an approved lecturer stayed invisible.
    /// </para>
    /// <para>
    /// The second half: the dashboard listed every unit of the shared offering
    /// snapshot, so each lecturer also saw their colleagues' units.
    /// </para>
    /// </summary>
    public class LecturerApprovalActivatesTeachingTests
    {
        private static readonly Guid OfferingId = Guid.NewGuid();

        private static Lecturer PendingLecturer() =>
            new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Approved",
                LastName = "Lecturer",
                Email = "lecturer@school.test",
                EmployeeNumber = "LEC001",
                UserId = Guid.NewGuid().ToString(),
                RegistrationStatus = RegistrationStatus.PendingApproval,
                IsActive = true
            };

        private static UnitAllocation AllocationRow(Guid lecturerId, Guid unitId, string status) =>
            new()
            {
                Id = Guid.NewGuid(),
                LecturerId = lecturerId,
                UnitId = unitId,
                SemesterId = Guid.NewGuid(),
                CourseOfferingId = OfferingId,
                Status = status
            };

        private static CourseOfferingLecturer AssignmentRow(Guid lecturerId, string status) =>
            new()
            {
                Id = Guid.NewGuid(),
                LecturerId = lecturerId,
                CourseOfferingId = OfferingId,
                Status = status,
                IsActive = true,
                ConfirmationStatus = status == "Active" ? ConfirmationStatus.Confirmed : ConfirmationStatus.Pending
            };

        private static (ApproveRegistrationCommandHandler Handler,
            Mock<ICourseOfferingLecturerRepository> Assignments,
            Mock<ICourseOfferingUnitRepository> OfferingUnits) CreateApproveHandler(
            Lecturer lecturer,
            List<UnitAllocation> allocations,
            List<CourseOfferingLecturer> assignments)
        {
            var lecturerRepository = new Mock<ILecturerRepository>();
            lecturerRepository.Setup(x => x.GetByIdAsync(lecturer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lecturer);

            var allocationRepository = new Mock<IUnitAllocationRepository>();
            allocationRepository.Setup(x => x.GetByLecturerAsync(lecturer.Id))
                .ReturnsAsync(allocations);

            var assignmentRepository = new Mock<ICourseOfferingLecturerRepository>();
            assignmentRepository.Setup(x => x.GetByLecturerIdAsync(lecturer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignments);

            var offeringUnitRepository = new Mock<ICourseOfferingUnitRepository>();
            offeringUnitRepository.Setup(x => x.GetByOfferingAndUnitAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CourseOfferingUnit)null!);
            offeringUnitRepository.Setup(x => x.GetMaxOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            var handler = new ApproveRegistrationCommandHandler(
                Mock.Of<IStudentRepository>(),
                lecturerRepository.Object,
                Mock.Of<IEnrollmentRepository>(),
                allocationRepository.Object,
                assignmentRepository.Object,
                offeringUnitRepository.Object,
                Mock.Of<IAuditService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<IBusinessEventNotifier>(),
                NullLoggerFactory.Instance.CreateLogger<ApproveRegistrationCommandHandler>());

            return (handler, assignmentRepository, offeringUnitRepository);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Approval activates the teaching assignment (the D3 defect)
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Approval_ActivatesThePendingTeachingAssignment()
        {
            var lecturer = PendingLecturer();
            var assignment = AssignmentRow(lecturer.Id, "PendingConfirmation");
            var (handler, _, _) = CreateApproveHandler(
                lecturer,
                new List<UnitAllocation> { AllocationRow(lecturer.Id, Guid.NewGuid(), "PendingApproval") },
                new List<CourseOfferingLecturer> { assignment });

            var result = await handler.Handle(
                new ApproveRegistrationCommand { UserId = lecturer.Id, UserType = "Lecturer" },
                CancellationToken.None);

            result.Status.Should().Be("Approved");
            lecturer.RegistrationStatus.Should().Be(RegistrationStatus.Approved);
            assignment.Status.Should().Be("Active",
                "GetActiveByLecturerAsync filters Status == Active; approval is what activates it");
            assignment.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Approval_ActivatesThePendingUnitAllocations()
        {
            var lecturer = PendingLecturer();
            var allocation = AllocationRow(lecturer.Id, Guid.NewGuid(), "PendingApproval");
            var (handler, _, _) = CreateApproveHandler(
                lecturer,
                new List<UnitAllocation> { allocation },
                new List<CourseOfferingLecturer> { AssignmentRow(lecturer.Id, "PendingConfirmation") });

            await handler.Handle(
                new ApproveRegistrationCommand { UserId = lecturer.Id, UserType = "Lecturer" },
                CancellationToken.None);

            allocation.Status.Should().Be("Active");
        }

        [Fact]
        public async Task Approval_DoesNotTouchAnotherLecturersAssignment()
        {
            var lecturer = PendingLecturer();
            var colleagueId = Guid.NewGuid();
            var colleagueAssignment = AssignmentRow(colleagueId, "PendingConfirmation");

            var (handler, assignments, _) = CreateApproveHandler(
                lecturer,
                new List<UnitAllocation> { AllocationRow(lecturer.Id, Guid.NewGuid(), "PendingApproval") },
                new List<CourseOfferingLecturer> { AssignmentRow(lecturer.Id, "PendingConfirmation") });

            await handler.Handle(
                new ApproveRegistrationCommand { UserId = lecturer.Id, UserType = "Lecturer" },
                CancellationToken.None);

            colleagueAssignment.Status.Should().Be("PendingConfirmation",
                "approval must never activate a colleague's teaching appointment");

            assignments.Verify(
                x => x.GetByLecturerIdAsync(colleagueId, It.IsAny<CancellationToken>()),
                Times.Never,
                "the handler only ever loads the approved lecturer's own assignments");
        }

        [Fact]
        public async Task Approval_LeavesAnAlreadyCancelledAssignmentAlone()
        {
            var lecturer = PendingLecturer();
            var cancelled = AssignmentRow(lecturer.Id, "Cancelled");

            var (handler, _, _) = CreateApproveHandler(
                lecturer,
                new List<UnitAllocation> { AllocationRow(lecturer.Id, Guid.NewGuid(), "PendingApproval") },
                new List<CourseOfferingLecturer> { cancelled });

            await handler.Handle(
                new ApproveRegistrationCommand { UserId = lecturer.Id, UserType = "Lecturer" },
                CancellationToken.None);

            cancelled.Status.Should().Be("Cancelled",
                "only PendingConfirmation rows are activated - terminal states are preserved");
        }

        [Fact]
        public async Task Approval_CreatesTheOfferingUnitRelationshipForEachApprovedUnit()
        {
            var lecturer = PendingLecturer();
            var unitOne = Guid.NewGuid();
            var unitTwo = Guid.NewGuid();

            var (handler, _, offeringUnits) = CreateApproveHandler(
                lecturer,
                new List<UnitAllocation>
                {
                    AllocationRow(lecturer.Id, unitOne, "PendingApproval"),
                    AllocationRow(lecturer.Id, unitTwo, "PendingApproval")
                },
                new List<CourseOfferingLecturer> { AssignmentRow(lecturer.Id, "PendingConfirmation") });

            await handler.Handle(
                new ApproveRegistrationCommand { UserId = lecturer.Id, UserType = "Lecturer" },
                CancellationToken.None);

            offeringUnits.Verify(
                x => x.AddAsync(It.Is<CourseOfferingUnit>(u =>
                        u.CourseOfferingId == OfferingId && u.UnitId == unitOne && u.IsActive),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            offeringUnits.Verify(
                x => x.AddAsync(It.Is<CourseOfferingUnit>(u =>
                        u.CourseOfferingId == OfferingId && u.UnitId == unitTwo && u.IsActive),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Approval_IsRejectedWhenTheLecturerIsNotPending()
        {
            var lecturer = PendingLecturer();
            lecturer.RegistrationStatus = RegistrationStatus.Approved;

            var (handler, _, _) = CreateApproveHandler(
                lecturer, new List<UnitAllocation>(), new List<CourseOfferingLecturer>());

            await Assert.ThrowsAsync<SMS.Application.Exceptions.ValidationException>(() =>
                handler.Handle(
                    new ApproveRegistrationCommand { UserId = lecturer.Id, UserType = "Lecturer" },
                    CancellationToken.None));
        }

        // ─────────────────────────────────────────────────────────────────────
        // The dashboard reads the activated assignment and only the lecturer's units
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Dashboard_ListsNoCourses_WhileTheTeachingAssignmentIsStillPending()
        {
            var lecturer = PendingLecturer();
            var handler = CreateDashboardHandler(
                lecturer,
                new List<CourseOfferingLecturer>(),
                new List<UnitAllocation>());

            var result = await handler.Handle(new GetMyLecturerDashboardQuery(), CancellationToken.None);

            result.Courses.Should().BeEmpty(
                "before approval there is no active teaching assignment to show");
        }

        [Fact]
        public async Task Dashboard_ListsTheApprovedCourseAndOnlyTheSelectedUnits()
        {
            var lecturer = PendingLecturer();
            lecturer.RegistrationStatus = RegistrationStatus.Approved;

            var unitOne = Guid.NewGuid();
            var unitTwo = Guid.NewGuid();
            var unitThree = Guid.NewGuid();   // allocated to a colleague, same offering

            var handler = CreateDashboardHandler(
                lecturer,
                new List<CourseOfferingLecturer> { AssignmentRow(lecturer.Id, "Active") },
                new List<UnitAllocation>
                {
                    AllocationRow(lecturer.Id, unitOne, "Active"),
                    AllocationRow(lecturer.Id, unitTwo, "Active")
                },
                new List<Guid> { unitOne, unitTwo, unitThree });

            var result = await handler.Handle(new GetMyLecturerDashboardQuery(), CancellationToken.None);

            result.Courses.Should().ContainSingle();
            var course = result.Courses.Single();
            course.CourseCode.Should().Be("CS01");
            course.Units.Select(u => u.UnitId).Should().BeEquivalentTo(new[] { unitOne, unitTwo });
            course.Units.Should().NotContain(u => u.UnitId == unitThree,
                "a unit the lecturer never selected must never appear on their dashboard");
        }

        [Fact]
        public async Task Dashboard_ShowsTheFullOfferingSnapshot_WhenTheLecturerHasNoAllocationsForIt()
        {
            // An administrative appointment with no allocation of its own keeps the
            // pre-existing behaviour rather than silently showing nothing.
            var lecturer = PendingLecturer();
            lecturer.RegistrationStatus = RegistrationStatus.Approved;

            var unitOne = Guid.NewGuid();
            var unitTwo = Guid.NewGuid();

            var handler = CreateDashboardHandler(
                lecturer,
                new List<CourseOfferingLecturer> { AssignmentRow(lecturer.Id, "Active") },
                new List<UnitAllocation>(),
                new List<Guid> { unitOne, unitTwo });

            var result = await handler.Handle(new GetMyLecturerDashboardQuery(), CancellationToken.None);

            result.Courses.Should().ContainSingle();
            result.Courses.Single().Units.Should().HaveCount(2);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static GetMyLecturerDashboardQueryHandler CreateDashboardHandler(
            Lecturer lecturer,
            List<CourseOfferingLecturer> assignments,
            List<UnitAllocation> allocations,
            List<Guid>? offeringUnitIds = null)
        {
            var access = new Mock<IAcademicAccessService>();
            access.Setup(x => x.GetCurrentLecturerAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(lecturer);

            var assignmentRepository = new Mock<ICourseOfferingLecturerRepository>();
            assignmentRepository.Setup(x => x.GetActiveByLecturerAsync(lecturer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignments);

            var offeringRepository = new Mock<ICourseOfferingRepository>();
            offeringRepository.Setup(x => x.GetWithDetailsAsync(OfferingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CourseOffering
                {
                    Id = OfferingId,
                    CourseId = Guid.NewGuid(),
                    OfferingCode = "CS-2026-S1-001",
                    AcademicYearName = "2026/2027",
                    SemesterName = "Semester 1",
                    Course = new Course { Name = "Computer Science", Code = "CS01" }
                });

            var unitIds = offeringUnitIds ?? new List<Guid>();
            var offeringUnitRepository = new Mock<ICourseOfferingUnitRepository>();
            offeringUnitRepository.Setup(x => x.GetOrderedUnitsAsync(OfferingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(unitIds.Select((id, index) => new CourseOfferingUnit
                {
                    Id = Guid.NewGuid(),
                    CourseOfferingId = OfferingId,
                    UnitId = id,
                    Code = $"U{index + 1}",
                    Name = $"Unit {index + 1}",
                    Credits = 3,
                    Order = index + 1,
                    IsActive = true
                }).ToList());

            var allocationRepository = new Mock<IUnitAllocationRepository>();
            allocationRepository.Setup(x => x.GetByLecturerAsync(lecturer.Id))
                .ReturnsAsync(allocations);

            return new GetMyLecturerDashboardQueryHandler(
                access.Object,
                assignmentRepository.Object,
                offeringRepository.Object,
                offeringUnitRepository.Object,
                allocationRepository.Object,
                Mock.Of<IUnitRepository>(),
                Mock.Of<IAccommodationRepository>(),
                NullLoggerFactory.Instance.CreateLogger<GetMyLecturerDashboardQueryHandler>());
        }
    }
}
