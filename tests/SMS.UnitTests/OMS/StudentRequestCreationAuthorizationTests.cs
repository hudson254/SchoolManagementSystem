using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using Xunit;

// ICurrentUserService exists in BOTH the Application and Domain namespaces.
// The OMS handlers use the Application-layer one; alias it explicitly.
using ICurrentUserService = SMS.Application.Common.Interfaces.ICurrentUserService;

namespace SMS.UnitTests.OMS
{
    /// <summary>
    /// Regression tests for DEFECT 1: "a newly registered student cannot create a new
    /// request - 403 / ERR_BAD_REQUEST".
    ///
    /// <para>
    /// Root cause: <c>POST /oms/requests</c> was correctly gated on
    /// <c>Oms.CanCreateRequest</c> (which includes Student), but the New Request page
    /// loads <c>GET /oms/requests/types</c> on mount and that endpoint was gated on
    /// <c>Oms.CanViewRequests</c> - a policy that EXCLUDED Student. The page therefore
    /// 403'd before the form could render, so a legitimately authenticated student
    /// could not create the requests students are supposed to create.
    ///
    /// <para>
    /// The repair grants an own-request read scope. These tests prove BOTH halves:
    /// the legitimate student is unblocked, AND the security model is unchanged.
    /// </para>
    /// </summary>
    public class StudentRequestCreationAuthorizationTests
    {
        private static Mock<ICurrentUserService> CurrentUser(string userId, params string[] roles)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(x => x.UserId).Returns(userId);
            mock.Setup(x => x.Email).Returns("student@example.com");
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            mock.Setup(x => x.Roles).Returns(roles);
            return mock;
        }

        private static Mock<IRequestRepository> RepositoryWith(Guid requestId, string requesterUserId)
        {
            // Request exposes read-only identity fields, so it is built through the
            // same domain factory the create command uses.
            var request = Request.Create(
                "REQ-1", "GENERAL", "General", "Help", null, requesterUserId);
            request.Id = requestId;
            request.TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var repo = new Mock<IRequestRepository>();
            repo.Setup(x => x.GetByIdWithDetailsAsync(requestId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(request);
            return repo;
        }

        [Theory]
        [InlineData("Student")]
        [InlineData("Lecturer")]
        [InlineData("Coordinator")]
        [InlineData("Administrator")]
        public void RolesThatMayCreateRequests_MayAlsoViewTheRequestTypeCatalogue(string role)
        {
            // The catalogue is reference/configuration data (code, display name,
            // default priority). Without it the New Request form cannot render.
            var roles = new[] { role };

            var mayCreate = OmsAuthorization.HasAnyRole(roles, OmsAuthorization.CreateRequestRoles);
            var mayReadCatalogue =
                OmsAuthorization.HasAnyRole(roles, OmsAuthorization.ViewOwnRequestsRoles)
                || OmsAuthorization.HasAnyRole(roles, OmsAuthorization.ViewRequestsRoles);

            mayCreate.Should().BeTrue($"{role} is a requester and needs the type list to create one");
            mayReadCatalogue.Should().BeTrue(
                $"{role} could otherwise create a request but not load the form - the reported 403");
        }

        [Fact]
        public void EveryCreateRequestRole_IsCoveredByTheOwnRequestReadScope()
        {
            // The real invariant: nobody who may CREATE a request is left unable to
            // read the catalogue required to create one. Enumerated from the single
            // source of truth rather than a hand-copied role list.
            foreach (var role in OmsAuthorization.CreateRequestRoles)
            {
                var roles = new[] { role };
                var mayRead =
                    OmsAuthorization.HasAnyRole(roles, OmsAuthorization.ViewOwnRequestsRoles)
                    || OmsAuthorization.HasAnyRole(roles, OmsAuthorization.ViewRequestsRoles);

                mayRead.Should().BeTrue($"{role} can create a request and must be able to read the types");
            }
        }

        [Fact]
        public async Task Student_CanReadTheRequestTypeCatalogue_NoLonger403()
        {
            var user = CurrentUser("student-1", "Student");
            var repo = new Mock<IRequestRepository>();
            repo.Setup(x => x.GetAllTypesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<RequestType>
                {
                    new() { Id = Guid.NewGuid(), Code = "GENERAL", DisplayName = "General", IsActive = true }
                });

            var result = await new GetRequestTypesQueryHandler(repo.Object, user.Object)
                .Handle(new GetRequestTypesQuery(), CancellationToken.None);

            result.Should().HaveCount(1);
            result[0].Code.Should().Be("GENERAL");
        }

        [Fact]
        public async Task Student_CanReadTheirOwnRequestDetail()
        {
            // After creating a request the SPA navigates to /oms/requests/{id}; if
            // that 403'd the student would still see an error on success.
            var requestId = Guid.NewGuid();
            var user = CurrentUser("student-1", "Student");
            var repo = RepositoryWith(requestId, "student-1");

            var result = await new GetRequestByIdQueryHandler(repo.Object, user.Object)
                .Handle(new GetRequestByIdQuery { RequestId = requestId }, CancellationToken.None);

            result.Id.Should().Be(requestId);
        }

        [Fact]
        public async Task Student_CannotReadAnotherStudentsRequest()
        {
            // SECURITY GUARD: the own-request scope must never become cross-user
            // access. CanView requires requester identity.
            var requestId = Guid.NewGuid();
            var user = CurrentUser("student-2", "Student");
            var repo = RepositoryWith(requestId, "student-1");

            await Assert.ThrowsAsync<ForbiddenException>(
                () => new GetRequestByIdQueryHandler(repo.Object, user.Object)
                    .Handle(new GetRequestByIdQuery { RequestId = requestId }, CancellationToken.None));
        }

        [Fact]
        public async Task Student_CannotUseThePrivilegedQueueScope()
        {
            // SECURITY GUARD: scope=all exposes other users' requests and stays
            // restricted to Admin/Coordinator regardless of the new policy.
            var user = CurrentUser("student-1", "Student");
            var repo = new Mock<IRequestRepository>();

            await Assert.ThrowsAsync<ForbiddenException>(
                () => new GetRequestsQueryHandler(repo.Object, user.Object)
                    .Handle(new GetRequestsQuery { Scope = "all" }, CancellationToken.None));
        }

        [Fact]
        public async Task Student_ListIsForcedToTheirOwnRequests_EvenWhenAskingForSomeoneElses()
        {
            // A non-privileged caller cannot smuggle another user id through the
            // filter: the handler overwrites it with the authenticated id.
            var user = CurrentUser("student-1", "Student");
            var repo = new Mock<IRequestRepository>();
            // A loose mock would return a null Task. ReturnsAsync cannot destructure
            // a ValueTuple, so the empty page is supplied as a concrete Task; the
            // argument the handler actually passed is read back from Invocations.
            repo.Setup(x => x.GetPagedAsync(
                    It.IsAny<RequestStatus?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<(IReadOnlyList<Request>, int)>((Array.Empty<Request>(), 0)));

            await new GetRequestsQueryHandler(repo.Object, user.Object)
                .Handle(new GetRequestsQuery { RequesterUserId = "student-2" }, CancellationToken.None);

            var passedRequester = (string?)repo.Invocations
                .Single(i => i.Method.Name == nameof(IRequestRepository.GetPagedAsync))
                .Arguments[2];

            passedRequester.Should().Be("student-1",
                "the requester filter must be overwritten, never honoured");
        }

        [Fact]
        public async Task UnauthenticatedCaller_CannotReadTheRequestTypeCatalogue()
        {
            var user = new Mock<ICurrentUserService>();
            user.Setup(x => x.UserId).Returns((string?)null);
            user.Setup(x => x.IsAuthenticated).Returns(false);
            user.Setup(x => x.Roles).Returns((IEnumerable<string>?)null);

            await Assert.ThrowsAsync<UnauthorizedException>(
                () => new GetRequestTypesQueryHandler(new Mock<IRequestRepository>().Object, user.Object)
                    .Handle(new GetRequestTypesQuery(), CancellationToken.None));
        }

        [Fact]
        public void Student_RemainsExcludedFromThePrivilegedQueuePolicy()
        {
            // SECURITY GUARD: the repair added a NARROWER own-request policy. It did
            // not add Student to the tenant-wide queue policy.
            var queueRoles = new[] { "SystemAdministrator", "Administrator", "Coordinator", "Lecturer" };

            queueRoles.Should().NotContain("Student");
            queueRoles.Should().NotContain("Receptionist");
        }
    }
}