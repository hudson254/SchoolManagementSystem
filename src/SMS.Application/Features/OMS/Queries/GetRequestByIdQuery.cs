using FluentValidation;
using MediatR;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Queries
{
    public class GetRequestByIdQuery : IRequest<RequestDetailDto>
    {
        public Guid RequestId { get; set; }
    }

    public class GetRequestByIdQueryValidator : AbstractValidator<GetRequestByIdQuery>
    {
        public GetRequestByIdQueryValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
        }
    }

    public class GetRequestByIdQueryHandler : IRequestHandler<GetRequestByIdQuery, RequestDetailDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;

        public GetRequestByIdQueryHandler(
            IRequestRepository requestRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser)
        {
            _requestRepository = requestRepository;
            _currentUser = currentUser;
        }

        public async Task<RequestDetailDto> Handle(GetRequestByIdQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required to view a request.");

            var req = await _requestRepository.GetByIdWithDetailsAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            var isOwner = string.Equals(req.RequesterUserId, _currentUser.UserId, StringComparison.OrdinalIgnoreCase);
            var isAssignee = !string.IsNullOrWhiteSpace(req.AssignedUserId) &&
                string.Equals(req.AssignedUserId, _currentUser.UserId, StringComparison.OrdinalIgnoreCase);
            var canViewAll = OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewAllRequestsRoles);
            var canViewAssigned = OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewAssignedRequestsRoles);
            var canViewOwn = OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewOwnRequestsRoles);

            if (!(isOwner && canViewOwn) && !(isAssignee && canViewAssigned) && !canViewAll)
                throw new ForbiddenException(OmsPermissions.ViewRequests, _currentUser.UserId);

            return RequestDetailDto.FromEntity(req);
        }
    }
}
