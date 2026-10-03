using MediatR;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Queries;

public class GetRequestTypesQuery : IRequest<IReadOnlyList<RequestTypeDto>>
{
}

public class GetRequestTypesQueryHandler : IRequestHandler<GetRequestTypesQuery, IReadOnlyList<RequestTypeDto>>
{
    private readonly IRequestRepository _requestRepository;
    private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;

    public GetRequestTypesQueryHandler(
        IRequestRepository requestRepository,
        SMS.Application.Common.Interfaces.ICurrentUserService currentUser)
    {
        _requestRepository = requestRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<RequestTypeDto>> Handle(
        GetRequestTypesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedException("An authenticated user is required.");

        // The request-type catalogue is reference/configuration data (code, display
        // name, default priority) - it exposes no request content and no tenant
        // records. Every role that is permitted to CREATE a request needs it to
        // render the New Request form, so it is gated on "may view own requests"
        // (which includes Student and Receptionist) as well as the privileged queue
        // roles. Actual request DATA stays protected by OmsRequestAccess.
        if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewOwnRequestsRoles)
            && !OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewRequestsRoles))
            throw new ForbiddenException(OmsPermissions.ViewRequests, _currentUser.UserId);

        var types = await _requestRepository.GetAllTypesAsync(cancellationToken);
        return types.Select(RequestTypeDto.FromEntity).ToList().AsReadOnly();
    }
}
