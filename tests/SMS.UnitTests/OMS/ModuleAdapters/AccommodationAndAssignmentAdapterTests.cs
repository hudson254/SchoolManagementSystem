using FluentAssertions;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.Accommodation.Commands;
using SMS.Application.Features.Assignments.Commands;
using SMS.Application.Features.OMS.Commands;
using SMS.Application.Features.OMS.Dtos;
using SMS.Application.Features.OMS.Services;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// ICurrentUserService exists in both the Application and Domain namespaces.
// The handlers are written against the Application-layer one.
using ICurrentUserService = SMS.Application.Common.Interfaces.ICurrentUserService;
using AdapterValidationException = SMS.Application.Exceptions.ValidationException;

namespace SMS.UnitTests.OMS.ModuleAdapters
{
    /// <summary>
    /// Tests for the thin Accommodation and Assignment module request adapters
    /// (Phase 3). Same contract as the Enrollment adapter tests: the adapter
    /// only resolves module context, authorizes the caller, and delegates to
    /// the single Request Core seam.
    /// </summary>
    public class AccommodationAndAssignmentAdapterTests
    {
        private const string StudentUserId = "student-user-1";
        private const string OtherStudentUserId = "student-user-2";
        private const string LecturerUserId = "lecturer-user-1";

        [Fact]
        public async Task AccommodationAdapter_PopulatesTypedRelationships()
        {
            var accommodation = NewAccommodation();

            var captured = await CaptureAccommodationAsync(
                accommodation, NewStudent(StudentUserId), StudentUserId,
                new[] { "Student" }, AccommodationRequestTypes.Allocation);

            captured.AccommodationId.Should().Be(accommodation.Id);
            captured.StudentId.Should().Be(accommodation.StudentId);
            captured.RelatedEntityType.Should().Be("Accommodation");
            captured.RelatedEntityId.Should().Be(accommodation.Id.ToString());
            captured.RequestType.Should().Be(AccommodationRequestTypes.Allocation);
        }

        [Fact]
        public async Task AccommodationAdapter_DefaultsToTransferWhenNoTypeSupplied()
        {
            var captured = await CaptureAccommodationAsync(
                NewAccommodation(), NewStudent(StudentUserId), StudentUserId,
                new[] { "Student" }, null);

            captured.RequestType.Should().Be(AccommodationRequestTypes.Transfer);
        }

        [Fact]
        public async Task AccommodationAdapter_DescriptionStatesModuleRetainsOwnership()
        {
            // No house/lane/occupancy data is changed by raising the request.
            var captured = await CaptureAccommodationAsync(
                NewAccommodation(), NewStudent(StudentUserId), StudentUserId,
                new[] { "Student" }, AccommodationRequestTypes.Transfer);

            captured.Description.Should().Contain("Accommodation module remains the owner");
        }

        [Fact]
        public async Task AssignmentAdapter_PopulatesTypedRelationshipsButNotCourse()
        {
            // CourseOfferingId is deliberately NOT mapped to CourseId: an
            // assignment belongs to a unit, and copying the offering id into the
            // Course field would misrepresent the academic context.
            var assignment = NewAssignment();

            var captured = await CaptureAssignmentAsync(
                assignment, NewLecturer(LecturerUserId), LecturerUserId,
                new[] { "Lecturer" }, AssignmentRequestTypes.Reopen);

            captured.AssignmentId.Should().Be(assignment.Id);
            captured.UnitId.Should().Be(assignment.UnitId);
            captured.LecturerId.Should().Be(assignment.LecturerId);
            captured.CourseId.Should().BeNull();
            captured.RelatedEntityType.Should().Be("Assignment");
        }

        [Fact]
        public async Task AssignmentAdapter_DescriptionStatesModuleRetainsOwnership()
        {
            var captured = await CaptureAssignmentAsync(
                NewAssignment(), NewLecturer(LecturerUserId), LecturerUserId,
                new[] { "Lecturer" }, AssignmentRequestTypes.Extension);

            captured.Description.Should().Contain("Assignment module remains the owner");
        }

        // -------------------------------------------------------------------
        // Object-level authorization
        // -------------------------------------------------------------------

        [Fact]
        public async Task AccommodationAdapter_RejectsStudentRaisingRequestForAnotherOccupant()
        {
            var accommodation = NewAccommodation();

            var accommodations = new Mock<IAccommodationRepository>();
            accommodations
                .Setup(r => r.GetByIdAsync(accommodation.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(accommodation);

            var students = new Mock<IStudentRepository>();
            students
                .Setup(r => r.GetByIdAsync(accommodation.StudentId!.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(NewStudent(OtherStudentUserId));

            var adapter = new Mock<IOmsRequestModuleAdapter>();

            var handler = new CreateAccommodationRequestCommandHandler(
                accommodations.Object, students.Object,
                CurrentUser(StudentUserId, new[] { "Student" }),
                adapter.Object);

            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
                new CreateAccommodationRequestCommand { AccommodationId = accommodation.Id },
                CancellationToken.None));

            adapter.Verify(
                a => a.CreateAsync(It.IsAny<CreateRequestCommand>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task AccommodationAdapter_RejectsUnknownReference()
        {
            var handler = new CreateAccommodationRequestCommandHandler(
                new Mock<IAccommodationRepository>().Object,   // GetByIdAsync -> null
                new Mock<IStudentRepository>().Object,
                CurrentUser(StudentUserId, new[] { "Student" }),
                new Mock<IOmsRequestModuleAdapter>().Object);

            var ex = await Assert.ThrowsAsync<AdapterValidationException>(() => handler.Handle(
                new CreateAccommodationRequestCommand { AccommodationId = Guid.NewGuid() },
                CancellationToken.None));

            ex.Errors.Should().ContainKey("AccommodationId");
        }

        [Fact]
        public async Task AssignmentAdapter_RejectsLecturerRaisingRequestForAnotherLecturersAssignment()
        {
            var assignment = NewAssignment();

            var assignments = new Mock<IAssignmentRepository>();
            assignments
                .Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            // The assignment belongs to a different lecturer.
            var lecturers = new Mock<ILecturerRepository>();
            lecturers
                .Setup(r => r.GetByIdAsync(assignment.LecturerId!.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(NewLecturer("some-other-lecturer"));

            var handler = new CreateAssignmentRequestCommandHandler(
                assignments.Object, lecturers.Object,
                CurrentUser(LecturerUserId, new[] { "Lecturer" }),
                new Mock<IOmsRequestModuleAdapter>().Object);

            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
                new CreateAssignmentRequestCommand { AssignmentId = assignment.Id },
                CancellationToken.None));
        }

        [Fact]
        public async Task AssignmentAdapter_RejectsNonPrivilegedCallerOnUnownedAssignment()
        {
            // An assignment with no lecturer is unowned teaching material and
            // must not be requestable by an ordinary student.
            var assignment = NewAssignment();
            assignment.LecturerId = null;

            var assignments = new Mock<IAssignmentRepository>();
            assignments
                .Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            var handler = new CreateAssignmentRequestCommandHandler(
                assignments.Object, new Mock<ILecturerRepository>().Object,
                CurrentUser(StudentUserId, new[] { "Student" }),
                new Mock<IOmsRequestModuleAdapter>().Object);

            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
                new CreateAssignmentRequestCommand { AssignmentId = assignment.Id },
                CancellationToken.None));
        }

        [Fact]
        public async Task AssignmentAdapter_AllowsCoordinatorOnUnownedAssignment()
        {
            var assignment = NewAssignment();
            assignment.LecturerId = null;

            var assignments = new Mock<IAssignmentRepository>();
            assignments
                .Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            var adapter = new Mock<IOmsRequestModuleAdapter>();
            adapter
                .Setup(a => a.CreateAsync(It.IsAny<CreateRequestCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RequestDto());

            var handler = new CreateAssignmentRequestCommandHandler(
                assignments.Object, new Mock<ILecturerRepository>().Object,
                CurrentUser("coordinator-1", new[] { "Coordinator" }),
                adapter.Object);

            await handler.Handle(
                new CreateAssignmentRequestCommand { AssignmentId = assignment.Id },
                CancellationToken.None);

            adapter.Verify(
                a => a.CreateAsync(It.IsAny<CreateRequestCommand>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AssignmentAdapter_RejectsRequestTypeOutsideItsAllowList()
        {
            var assignment = NewAssignment();

            var assignments = new Mock<IAssignmentRepository>();
            assignments
                .Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            var lecturers = new Mock<ILecturerRepository>();
            lecturers
                .Setup(r => r.GetByIdAsync(assignment.LecturerId!.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(NewLecturer(LecturerUserId));

            var handler = new CreateAssignmentRequestCommandHandler(
                assignments.Object, lecturers.Object,
                CurrentUser(LecturerUserId, new[] { "Lecturer" }),
                new Mock<IOmsRequestModuleAdapter>().Object);

            var ex = await Assert.ThrowsAsync<AdapterValidationException>(() => handler.Handle(
                new CreateAssignmentRequestCommand
                {
                    AssignmentId = assignment.Id,
                    RequestType = "ENROLLMENT_COURSE_CHANGE"
                },
                CancellationToken.None));

            ex.Errors.Should().ContainKey("RequestType");
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private static async Task<CreateRequestCommand> CaptureAccommodationAsync(
            SMS.Domain.Entities.Accommodation accommodation, Student student, string callerUserId,
            string[] callerRoles, string? requestType)
        {
            var accommodations = new Mock<IAccommodationRepository>();
            accommodations
                .Setup(r => r.GetByIdAsync(accommodation.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(accommodation);

            var students = new Mock<IStudentRepository>();
            students
                .Setup(r => r.GetByIdAsync(accommodation.StudentId!.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(student);

            CreateRequestCommand? captured = null;
            var adapter = new Mock<IOmsRequestModuleAdapter>();
            adapter
                .Setup(a => a.CreateAsync(It.IsAny<CreateRequestCommand>(), It.IsAny<CancellationToken>()))
                .Callback<CreateRequestCommand, CancellationToken>((c, _) => captured = c)
                .ReturnsAsync(new RequestDto());

            var handler = new CreateAccommodationRequestCommandHandler(
                accommodations.Object, students.Object,
                CurrentUser(callerUserId, callerRoles),
                adapter.Object);

            await handler.Handle(
                new CreateAccommodationRequestCommand
                {
                    AccommodationId = accommodation.Id,
                    RequestType = requestType
                },
                CancellationToken.None);

            captured.Should().NotBeNull();
            return captured!;
        }

        private static async Task<CreateRequestCommand> CaptureAssignmentAsync(
            Assignment assignment, Lecturer lecturer, string callerUserId,
            string[] callerRoles, string requestType)
        {
            var assignments = new Mock<IAssignmentRepository>();
            assignments
                .Setup(r => r.GetByIdAsync(assignment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignment);

            var lecturers = new Mock<ILecturerRepository>();
            lecturers
                .Setup(r => r.GetByIdAsync(assignment.LecturerId!.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lecturer);

            CreateRequestCommand? captured = null;
            var adapter = new Mock<IOmsRequestModuleAdapter>();
            adapter
                .Setup(a => a.CreateAsync(It.IsAny<CreateRequestCommand>(), It.IsAny<CancellationToken>()))
                .Callback<CreateRequestCommand, CancellationToken>((c, _) => captured = c)
                .ReturnsAsync(new RequestDto());

            var handler = new CreateAssignmentRequestCommandHandler(
                assignments.Object, lecturers.Object,
                CurrentUser(callerUserId, callerRoles),
                adapter.Object);

            await handler.Handle(
                new CreateAssignmentRequestCommand
                {
                    AssignmentId = assignment.Id,
                    RequestType = requestType
                },
                CancellationToken.None);

            captured.Should().NotBeNull();
            return captured!;
        }

        private static ICurrentUserService CurrentUser(string userId, string[] roles)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(u => u.UserId).Returns(userId);
            mock.Setup(u => u.Username).Returns(userId);
            mock.Setup(u => u.Email).Returns($"{userId}@sms.test");
            mock.Setup(u => u.IsAuthenticated).Returns(true);
            mock.Setup(u => u.Roles).Returns(new List<string>(roles));
            return mock.Object;
        }

        private static SMS.Domain.Entities.Accommodation NewAccommodation() =>
            new SMS.Domain.Entities.Accommodation
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                HouseId = Guid.NewGuid(),
                LaneId = Guid.NewGuid(),
                Status = "Active"
            };

        private static Assignment NewAssignment() => new Assignment
        {
            Id = Guid.NewGuid(),
            Title = "Essay 1",
            UnitId = Guid.NewGuid(),
            LecturerId = Guid.NewGuid(),
            CourseOfferingId = Guid.NewGuid(),
            DueDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = "Published"
        };

        private static Student NewStudent(string userId) => new Student
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StudentNumber = "S-001",
            FirstName = "Test",
            LastName = "Student",
            Email = $"{userId}@sms.test"
        };

        private static Lecturer NewLecturer(string userId) => new Lecturer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = "Test",
            LastName = "Lecturer",
            Email = $"{userId}@sms.test",
            EmployeeNumber = "E-001"
        };
    }
}
