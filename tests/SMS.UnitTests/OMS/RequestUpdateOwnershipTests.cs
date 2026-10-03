using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Application.Common;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using Xunit;
using ICurrentUserService = SMS.Application.Common.Interfaces.ICurrentUserService;
using ITenantContext = SMS.Domain.Interfaces.ITenantContext;

namespace SMS.UnitTests.OMS
{
    /// <summary>
    /// D2 regression: "any student can update any student's request"
    /// (<c>PUT /api/v1/oms/requests/{requestId}</c> returned 200 and persisted).
    /// <para>
    /// Root cause: the handler guarded with
    /// <c>req.RequesterUserId != currentUserId &amp;&amp; !HasAnyRole(roles, UpdateRequestRoles)</c>,
    /// but <c>OmsAuthorization.UpdateRequestRoles</c> was aliased to
    /// <c>CreateRequestRoles</c> - "all roles", including Student. For any student the
    /// second operand was true, so the ownership check never fired.
    /// </para>
    /// <para>
    /// The read path was already correct (<c>OmsRequestAccess.CanView</c> plus the
    /// <c>Oms.CanViewOwnRequest</c> policy), so the repair reuses that same object-level
    /// infrastructure for the mutation verb instead of inventing a second rule.
    /// </para>
    /// </summary>
    public class RequestUpdateOwnershipTests
    {
        private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private const string OwnerUserId = "student-a-user-id";
        private const string OtherStudentUserId = "student-b-user-id";

        private static Mock<ICurrentUserService> CurrentUser(
            string userId, string tenantId, params string[] roles)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(x => x.UserId).Returns(userId);
            mock.Setup(x => x.Email).Returns($"{userId}@school.test");
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            mock.Setup(x => x.Roles).Returns(roles);
            return mock;
        }

        private static Mock<ITenantContext> TenantContext(string tenantId)
        {
            var mock = new Mock<ITenantContext>();
            mock.Setup(x => x.TenantId).Returns(tenantId);
            return mock;
        }

        private static Request OwnedRequest(
            Guid? tenantId = null, string requesterUserId = OwnerUserId)
        {
            var request = Request.Create(
                "REQ-2026-000001", "GENERAL", "General", "Original title",
                "Original description", requesterUserId);
            request.TenantId = tenantId ?? TenantA;
            return request;
        }

        private static UpdateRequestCommandHandler CreateHandler(
            Mock<IRequestRepository> repository,
            Mock<ICurrentUserService> currentUser,
            string tenantId)
        {
            return new UpdateRequestCommandHandler(
                repository.Object,
                Mock.Of<IUnitOfWork>(),
                Mock.Of<IAuditService>(),
                currentUser.Object,
                TenantContext(tenantId).Object,
                NullLoggerFactory.Instance.CreateLogger<UpdateRequestCommandHandler>());
        }

        private static Mock<IRequestRepository> RepositoryReturning(Request request)
        {
            var repository = new Mock<IRequestRepository>();
            repository.Setup(x => x.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(request);
            return repository;
        }

        // ─────────────────────────────────────────────────────────────────────
        // The defect itself
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task StudentB_CannotUpdateStudentAsRequest()
        {
            var request = OwnedRequest();
            var repository = RepositoryReturning(request);
            var attacker = CurrentUser(OtherStudentUserId, TenantA.ToString(), "Student");

            var act = () => CreateHandler(repository, attacker, TenantA.ToString())
                .Handle(new UpdateRequestCommand { RequestId = request.Id, Title = "HIJACKED" },
                    CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenException>();

            request.Title.Should().Be("Original title",
                "the rejected update must not have mutated the aggregate");

            repository.Verify(
                x => x.UpdateAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "a forbidden caller must never reach persistence");
        }

        [Fact]
        public async Task Lecturer_CannotUpdateAnotherUsersRequest()
        {
            var request = OwnedRequest();
            var repository = RepositoryReturning(request);
            var caller = CurrentUser("lecturer-1-user-id", TenantA.ToString(), "Lecturer");

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                CreateHandler(repository, caller, TenantA.ToString())
                    .Handle(new UpdateRequestCommand { RequestId = request.Id, Title = "Hijacked" },
                        CancellationToken.None));
        }

        [Fact]
        public async Task StudentFromAnotherTenant_CannotUpdateTheRequest()
        {
            var request = OwnedRequest();
            var repository = RepositoryReturning(request);
            var intruder = CurrentUser(OwnerUserId, TenantB.ToString(), "Student");

            var act = () => CreateHandler(repository, intruder, TenantB.ToString())
                .Handle(new UpdateRequestCommand { RequestId = request.Id, Title = "Hijacked" },
                    CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenException>(
                "tenant isolation must be checked before ownership");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Legitimate behaviour must be preserved
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task StudentA_CanUpdateTheirOwnDraftRequest()
        {
            var request = OwnedRequest();
            var repository = RepositoryReturning(request);
            var owner = CurrentUser(OwnerUserId, TenantA.ToString(), "Student");

            var result = await CreateHandler(repository, owner, TenantA.ToString())
                .Handle(new UpdateRequestCommand { RequestId = request.Id, Title = "My updated title" },
                    CancellationToken.None);

            result.Title.Should().Be("My updated title");
            request.Title.Should().Be("My updated title");
            repository.Verify(
                x => x.UpdateAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("SystemAdministrator")]
        [InlineData("Administrator")]
        [InlineData("Coordinator")]
        public async Task PrivilegedQueueRoles_CanStillUpdateAnotherUsersRequest(string role)
        {
            var request = OwnedRequest();
            var repository = RepositoryReturning(request);
            var coordinator = CurrentUser("coordinator-1-user-id", TenantA.ToString(), role);

            var result = await CreateHandler(repository, coordinator, TenantA.ToString())
                .Handle(new UpdateRequestCommand { RequestId = request.Id, Title = "Triaged title" },
                    CancellationToken.None);

            result.Title.Should().Be("Triaged title");
        }

        [Fact]
        public async Task Update_RejectsAnInvalidStatusTransition()
        {
            var request = OwnedRequest();
            // Draft -> Submitted is not an editable status, so the owner is refused.
            request.Submit("coordinator-1-user-id", "coordinator");
            request.Status.Should().Be(RequestStatus.Submitted);
            var repository = RepositoryReturning(request);
            var owner = CurrentUser(OwnerUserId, TenantA.ToString(), "Student");

            var act = () => CreateHandler(repository, owner, TenantA.ToString())
                .Handle(new UpdateRequestCommand { RequestId = request.Id, Title = "Too late" },
                    CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>(
                "a terminal request is immutable even for its own requester");
        }

        [Fact]
        public async Task Update_RejectsAnUnauthenticatedCaller()
        {
            var repository = new Mock<IRequestRepository>();
            var caller = new Mock<ICurrentUserService>();
            caller.Setup(x => x.IsAuthenticated).Returns(false);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                CreateHandler(repository, caller, TenantA.ToString())
                    .Handle(new UpdateRequestCommand { RequestId = Guid.NewGuid(), Title = "x" },
                        CancellationToken.None));
        }

        // ─────────────────────────────────────────────────────────────────────
        // The role sets themselves must not drift back
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void UpdateRequestRoles_StayOwnershipScoped_AndAreNotTheCreateRoles()
        {
            // SECURITY GUARD: the defect was an alias between the own-record update
            // role set and the (much wider) create role set. Keep them distinct.
            OmsAuthorization.UpdateRequestRoles.Should().NotBeSameAs(OmsAuthorization.CreateRequestRoles);
        }

        [Theory]
        [InlineData("Student")]
        [InlineData("Lecturer")]
        [InlineData("Receptionist")]
        public void NonPrivilegedRoles_AreNotInTheUpdateAnyRequestSet(string role)
        {
            OmsAuthorization.UpdateAnyRequestRoles.Should().NotContain(role);
        }

        [Theory]
        [InlineData("SystemAdministrator")]
        [InlineData("Administrator")]
        [InlineData("Coordinator")]
        public void PrivilegedRoles_AreInTheUpdateAnyRequestSet(string role)
        {
            OmsAuthorization.UpdateAnyRequestRoles.Should().Contain(role);
        }

        [Theory]
        [InlineData("student-a-user-id", true)]
        [InlineData("student-b-user-id", false)]
        public void CanUpdate_RequiresOwnership_UnlessPrivileged(string callerUserId, bool expected)
        {
            var request = OwnedRequest();

            OmsRequestAccess.CanUpdate(request, callerUserId, new[] { "Student" })
                .Should().Be(expected);
        }
    }
}
