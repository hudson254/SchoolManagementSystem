using FluentValidation;
using MediatR;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Common;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Queries
{
    public class GetRequestsQuery : IRequest<PagedResult<RequestListItemDto>>
    {
        public RequestStatus? Status { get; set; }
        public string? RequestType { get; set; }
        public string? RequesterUserId { get; set; }
        public string? AssignedUserId { get; set; }
        public string? Search { get; set; }
        /// <summary>
        /// Server-side queue scoping: "mine" (requests I raised), "assigned"
        /// (requests assigned to me), "all" (full queue — privileged roles only).
        /// When omitted, non-privileged callers are scoped to "mine".
        /// </summary>
        public string? Scope { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetRequestsQueryValidator : AbstractValidator<GetRequestsQuery>
    {
        public GetRequestsQueryValidator()
        {
            RuleFor(x => x.PageNumber).InclusiveBetween(1, 10000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Search).MaximumLength(200);
            RuleFor(x => x.RequesterUserId).MaximumLength(100);
            RuleFor(x => x.AssignedUserId).MaximumLength(100);
            RuleFor(x => x.RequestType).MaximumLength(100);
        }
    }

    public class GetRequestsQueryHandler : IRequestHandler<GetRequestsQuery, PagedResult<RequestListItemDto>>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;

        public GetRequestsQueryHandler(
            IRequestRepository requestRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser)
        {
            _requestRepository = requestRepository;
            _currentUser = currentUser;
        }

        public async Task<PagedResult<RequestListItemDto>> Handle(GetRequestsQuery request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();

            var effectiveRequesterUserId = request.RequesterUserId;
            var effectiveAssignedUserId = request.AssignedUserId;

            switch (ResolveScope(request.Scope))
            {
                case "mine":
                    // Object-level scoping is enforced server-side: a "mine"
                    // caller can only ever see requests they raised.
                    effectiveRequesterUserId = _currentUser.UserId;
                    effectiveAssignedUserId = null;
                    break;

                case "assigned":
                    // A caller can only see requests assigned to them.
                    effectiveAssignedUserId = _currentUser.UserId;
                    effectiveRequesterUserId = null;
                    break;

                case "all":
                    // Privileged queue: client-supplied filters are honoured.
                    break;
            }

            var (items, totalCount) = await _requestRepository.GetPagedAsync(
                request.Status,
                request.RequestType,
                effectiveRequesterUserId,
                effectiveAssignedUserId,
                request.Search,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            return new PagedResult<RequestListItemDto>
            {
                Items = items.Select(RequestListItemDto.FromEntity).ToList(),
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        /// <summary>
        /// Resolves the effective queue scope. Non-privileged callers default to
        /// their own requests; the full queue ("all") requires an explicit
        /// privileged role (Admin/Coordinator). Unknown values are rejected
        /// rather than silently defaulting, so a typo can never widen access.
        ///
        /// "mine" and "assigned" are object-level scopes: the handler overwrites
        /// the caller-supplied user filters with the authenticated user id, so
        /// the scope can only ever narrow the result set for any authenticated
        /// user. "all" is the only scope that exposes other users' requests and
        /// therefore requires the privileged role.
        /// </summary>
        private string ResolveScope(string? requestedScope)
        {
            var canViewAll = OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewAllRequestsRoles);

            if (string.IsNullOrWhiteSpace(requestedScope))
                return canViewAll ? "all" : "mine";

            switch (requestedScope.Trim().ToLowerInvariant())
            {
                case "mine":
                    return "mine";
                case "assigned":
                    return "assigned";
                case "all":
                    if (!canViewAll)
                        throw new ForbiddenException(OmsPermissions.ViewAllRequests, _currentUser.UserId);
                    return "all";
                default:
                    throw new SMS.Application.Exceptions.ValidationException(new Dictionary<string, string[]>
                    {
                        ["scope"] = new[] { "Scope must be one of: mine, assigned, all." }
                    });
            }
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required to list requests.");
        }
    }
}
