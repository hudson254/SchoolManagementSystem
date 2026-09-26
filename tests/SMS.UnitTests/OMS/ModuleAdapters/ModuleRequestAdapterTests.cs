using FluentAssertions;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.Enrollments.Commands;
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
    /// Tests for the thin Enrollment module request adapter (Phase 3).
    ///
    /// These assert the two properties that matter most for the
    /// "no second workflow engine" rule:
    ///   1. The adapter ONLY produces a fully-formed CreateRequestCommand and
    ///      hands it to the shared Request Core delegation seam.
    ///   2. The adapter never mutates the module's own data.
    ///
    /// They also cover the adapter-specific preconditions: tenant isolation,
    /// object-level authorization (ownership), request-type allow-listing, and
    /// reference resolution.
    /// </summary>
    public class ModuleRequestAdapterTests
    {
        private const string StudentUserId = "student-user-1";
        private const string OtherStudentUserId = "student-user-2";

        [Fact]
        public async Task Adapter_PopulatesTypedRelationshipsFromResolvedEnrollment()
        {
            var enrollment = NewEnrollment();

            var captured = await CaptureAsync(
                enrollment, NewStudent(StudentUserId), StudentUserId,
                new[] { "Student" }, EnrollmentRequestTypes.CourseChange);

            captured.Should().NotBeNull();
            captured.EnrollmentId.Should().Be(enrollment.Id);
            captured.StudentId.Should().Be(enrollment.StudentId);
            captured.CourseId.Should().Be(enrollment.CourseId);
            captured.UnitId.Should().Be(enrollment.UnitId);
            captured.RelatedEntityType.Should().Be("Enrollment");
            captured.RelatedEntityId.Should().Be(enrollment.Id.ToString());
            captured.RequestType.Should().Be(EnrollmentRequestTypes.CourseChange);
        }

        [Fact]
        public async Task Adapter_DefaultsToCourseChangeWhenNoTypeSupplied()
        {
            var captured = await CaptureAsync(
                NewEnrollment(), NewStudent(StudentUserId), StudentUserId,
                new[] { "Student" }, null);

            captured.RequestType.Should().Be(EnrollmentRequestTypes.CourseChange);
        }

        [Fact]
        public async Task Adapter_DescriptionStatesModuleRetainsOwnership()
        {
            // The generated description must make it unambiguous that raising a
            // request does not itself change module state.
            var captured = await CaptureAsync(
                NewEnrollment(), NewStudent(StudentUserId), StudentUserId,
                new[] { "Student" }, EnrollmentRequestTypes.Exception);

            captured.Description.Should().Contain("Enrollment module remains the owner");
        }

        [Fact]
        public async Task Adapter_DelegatesThroughTheSharedRequestCoreSeam()
        {
            // Proves the adapter uses IOmsRequestModuleAdapter (the single
            // delegation point) rather than writing to oms_requests itself.
            var enrollment = NewEnrollment();

            var enrollments = new Mock<IEnrollmentRepository>();
            enrollments
                .Setup(r => r.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            var students = new Mock<IStudentRepository>();
            students
                .Setup(r => r.GetByIdAsync(enrollment.StudentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(NewStudent(StudentUserId));

            var adapter = new Mock<IOmsRequestModuleAdapter>();
            adapter
                .Setup(a => a.CreateAsync(It.IsAny<CreateRequestCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RequestDto { RequestNumber = "REQ-2026-000001" });

            var handler = new CreateEnrollmentRequestCommandHandler(
                enrollments.Object, students.Object,
                CurrentUser(StudentUserId, new[] { "Student" }),
                adapter.Object);

            var result = await handler.Handle(
                new CreateEnrollmentRequestCommand { EnrollmentId = enrollment.Id },
                CancellationToken.None);

            result.RequestNumber.Should().Be("REQ-2026-000001");
            adapter.Verify(
                a => a.CreateAsync(It.IsAny<CreateRequestCommand>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // -------------------------------------------------------------------
        // Creation failures
        // -------------------------------------------------------------------

        [Fact]
        public async Task Adapter_RejectsUnknownEnrollmentReference()
        {
            // A tenant-filtered repository returns null for an id that is not
            // visible to this tenant, so a bad or cross-tenant reference lands
            // here identically.
            var handler = new CreateEnrollmentRequestCommandHandler(
                new Mock<IEnrollmentRepository>().Object,   // GetByIdAsync -> null
                new Mock<IStudentRepository>().Object,
                CurrentUser(StudentUserId, new[] { "Student" }),
                new Mock<IOmsRequestModuleAdapter>().Object);

            var ex = await Assert.ThrowsAsync<AdapterValidationException>(() => handler.Handle(
                new CreateEnrollmentRequestCommand { EnrollmentId = Guid.NewGuid() },
                CancellationToken.None));

            // Names the offending field so the caller gets an actionable 400.
            ex.Errors.Should().ContainKey("EnrollmentId");
        }

        [Fact]
        public async Task Adapter_RejectsRequestTypeOutsideItsAllowList()
        {
            var enrollment = NewEnrollment();

            var enrollments = new Mock<IEnrollmentRepository>();
            enrollments
                .Setup(r => r.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            var students = new Mock<IStudentRepository>();
            students
                .Setup(r => r.GetByIdAsync(enrollment.StudentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(NewStudent(StudentUserId));

            var handler = new CreateEnrollmentRequestCommandHandler(
                enrollments.Object, students.Object,
                CurrentUser(StudentUserId, new[] { "Student" }),
                new Mock<IOmsRequestModuleAdapter>().Object);

            // A caller must not be able to raise an "enrollment" request that is
            // actually typed as an unrelated workflow.
            var ex = await Assert.ThrowsAsync<AdapterValidationException>(() => handler.Handle(
                new CreateEnrollmentRequestCommand
                {
                    EnrollmentId = enrollment.Id,
                    RequestType = "CERTIFICATE_CORRECTION"
                },
                CancellationToken.None));

            ex.Errors.Should().ContainKey("RequestType");
        }

        [Fact]
        public async Task Adapter_RejectsUnauthenticatedCaller()
        {
            var user = new Mock<ICurrentUserService>();
            user.Setup(u => u.IsAuthenticated).Returns(false);
            user.Setup(u => u.UserId).Returns(string.Empty);

            var handler = new CreateEnrollmentRequestCommandHandler(
                new Mock<IEnrollmentRepository>().Object,
                new Mock<IStudentRepository>().Object,
                user.Object,
                new Mock<IOmsRequestModuleAdapter>().Object);

            await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(
                new CreateEnrollmentRequestCommand { EnrollmentId = Guid.NewGuid() },
                CancellationToken.None));
        }

        [Fact]
        public async Task Adapter_RejectsRoleNotPermittedToRaiseRequests()
        {
            // "Receptionist" is a real SMS role but is deliberately absent from
            // OmsAuthorization.CreateRequestRoles.
            var handler = new CreateEnrollmentRequestCommandHandler(
                new Mock<IEnrollmentRepository>().Object,
                new Mock<IStudentRepository>().Object,
                CurrentUser("receptionist-1", new[] { "Receptionist" }),
                new Mock<IOmsRequestModuleAdapter>().Object);

            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
                new CreateEnrollmentRequestCommand { EnrollmentId = Guid.NewGuid() },
                CancellationToken.None));
        }

        // -------------------------------------------------------------------
        // Object-level authorization
        // -------------------------------------------------------------------

        [Fact]
        public async Task Adapter_RejectsStudentRaisingRequestForAnotherStudent()
        {
            var enrollment = NewEnrollment();

            var enrollments = new Mock<IEnrollmentRepository>();
            enrollments
                .Setup(r => r.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            // The enrollment belongs to a DIFFERENT student than the caller.
            var students = new Mock<IStudentRepository>();
            students
                .Setup(r => r.GetByIdAsync(enrollment.StudentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(NewStudent(OtherStudentUserId));

            var adapter = new Mock<IOmsRequestModuleAdapter>();

            var handler = new CreateEnrollmentRequestCommandHandler(
                enrollments.Object, students.Object,
                CurrentUser(StudentUserId, new[] { "Student" }),
                adapter.Object);

            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
                new CreateEnrollmentRequestCommand { EnrollmentId = enrollment.Id },
                CancellationToken.None));

            // Rejected before the Request Core is ever reached.
            adapter.Verify(
                a => a.CreateAsync(It.IsAny<CreateRequestCommand>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Adapter_AllowsCoordinatorToRaiseOnBehalfOfStudent()
        {
            var captured = await CaptureAsync(
                NewEnrollment(), NewStudent(OtherStudentUserId), "coordinator-1",
                new[] { "Coordinator" }, EnrollmentRequestTypes.CourseChange);

            captured.Should().NotBeNull();
        }

        [Fact]
        public async Task Adapter_MatchesStudentOwnershipByEmailWhenNoUserIdLink()
        {
            // Some student rows predate the identity link, so ownership also
            // falls back to the student's email address.
            var enrollment = NewEnrollment();

            var enrollments = new Mock<IEnrollmentRepository>();
            enrollments
                .Setup(r => r.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            var student = NewStudent(OtherStudentUserId);
            student.UserId = null;
            student.Email = "owner@sms.test";

            var students = new Mock<IStudentRepository>();
            students
                .Setup(r => r.GetByIdAsync(enrollment.StudentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(student);

            var adapter = new Mock<IOmsRequestModuleAdapter>();
            adapter
                .Setup(a => a.CreateAsync(It.IsAny<CreateRequestCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RequestDto());

            var user = new Mock<ICurrentUserService>();
            user.Setup(u => u.UserId).Returns("student-user-1");
            user.Setup(u => u.Email).Returns("owner@sms.test");
            user.Setup(u => u.IsAuthenticated).Returns(true);
            user.Setup(u => u.Roles).Returns(new List<string> { "Student" });

            var handler = new CreateEnrollmentRequestCommandHandler(
                enrollments.Object, students.Object, user.Object, adapter.Object);

            await handler.Handle(
                new CreateEnrollmentRequestCommand { EnrollmentId = enrollment.Id },
                CancellationToken.None);

            adapter.Verify(
                a => a.CreateAsync(It.IsAny<CreateRequestCommand>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // -------------------------------------------------------------------
        // Tenant isolation
        // -------------------------------------------------------------------

        [Fact]
        public async Task Adapter_CrossTenantReferenceDoesNotLeakExistence()
        {
            // A repository under the global tenant filter returns null for
            // another tenant's row, so the adapter reports a field-level
            // validation error. It must not surface a 404 (which would confirm
            // the id exists elsewhere) nor mention tenancy at all.
            var handler = new CreateEnrollmentRequestCommandHandler(
                new Mock<IEnrollmentRepository>().Object,
                new Mock<IStudentRepository>().Object,
                CurrentUser(StudentUserId, new[] { "Student" }),
                new Mock<IOmsRequestModuleAdapter>().Object);

            var ex = await Assert.ThrowsAsync<AdapterValidationException>(() => handler.Handle(
                new CreateEnrollmentRequestCommand { EnrollmentId = Guid.NewGuid() },
                CancellationToken.None));

            ex.Message.Should().NotContain("tenant");
            ex.Message.Should().NotContain("Tenant");
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private static async Task<CreateRequestCommand> CaptureAsync(
            Enrollment enrollment, Student student, string callerUserId,
            string[] callerRoles, string? requestType)
        {
            var enrollments = new Mock<IEnrollmentRepository>();
            enrollments
                .Setup(r => r.GetByIdAsync(enrollment.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            var students = new Mock<IStudentRepository>();
            students
                .Setup(r => r.GetByIdAsync(enrollment.StudentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(student);

            CreateRequestCommand? captured = null;
            var adapter = new Mock<IOmsRequestModuleAdapter>();
            adapter
                .Setup(a => a.CreateAsync(It.IsAny<CreateRequestCommand>(), It.IsAny<CancellationToken>()))
                .Callback<CreateRequestCommand, CancellationToken>((c, _) => captured = c)
                .ReturnsAsync(new RequestDto());

            var handler = new CreateEnrollmentRequestCommandHandler(
                enrollments.Object, students.Object,
                CurrentUser(callerUserId, callerRoles),
                adapter.Object);

            await handler.Handle(
                new CreateEnrollmentRequestCommand
                {
                    EnrollmentId = enrollment.Id,
                    RequestType = requestType,
                    Reason = "Testing"
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

        private static Enrollment NewEnrollment() => new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            UnitId = Guid.NewGuid(),
            Status = "Active",
            IsActive = true
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
    }
}
