using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Commands;

public class ActivateRequestTypeCommand : IRequest<RequestTypeDto>
{
    public Guid Id { get; set; }
}

public class ActivateRequestTypeCommandHandler : IRequestHandler<ActivateRequestTypeCommand, RequestTypeDto>
{
    private readonly IRequestRepository _requestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
    private readonly ILogger<ActivateRequestTypeCommandHandler> _logger;

    public ActivateRequestTypeCommandHandler(
        IRequestRepository requestRepository,
        IUnitOfWork unitOfWork,
        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
        ILogger<ActivateRequestTypeCommandHandler> logger)
    {
        _requestRepository = requestRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<RequestTypeDto> Handle(ActivateRequestTypeCommand request, CancellationToken cancellationToken)
    {
        EnsureCanManage();

        var type = await _requestRepository.GetRequestTypeByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(RequestType), request.Id);

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            type.IsActive = true;
            await _requestRepository.UpdateRequestTypeAsync(type, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Request type {Code} activated by {UserId}", type.Code, _currentUser.UserId);
            return RequestTypeDto.FromEntity(type);
        }, cancellationToken);
    }

    private void EnsureCanManage()
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedException("An authenticated user is required.");

        if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ManageRequestTypesRoles))
            throw new ForbiddenException(OmsPermissions.ManageRequestTypes, _currentUser.UserId);
    }
}

