using MediatR;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Queries;

public class GetRequestTypeByIdQuery : IRequest<RequestTypeDto>
{
    public Guid Id { get; set; }
}

public class GetRequestTypeByIdQueryHandler : IRequestHandler<GetRequestTypeByIdQuery, RequestTypeDto>
{
    private readonly IRequestRepository _requestRepository;
    private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;

    public GetRequestTypeByIdQueryHandler(
        IRequestRepository requestRepository,
        SMS.Application.Common.Interfaces.ICurrentUserService currentUser)
    {
        _requestRepository = requestRepository;
        _currentUser = currentUser;
    }

    public async Task<RequestTypeDto> Handle(GetRequestTypeByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedException("An authenticated user is required.");

        var type = await _requestRepository.GetRequestTypeByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(RequestType), request.Id);

        return RequestTypeDto.FromEntity(type);
    }
}
