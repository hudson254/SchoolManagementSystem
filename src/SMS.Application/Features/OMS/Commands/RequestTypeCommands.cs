using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Commands;

public class CreateRequestTypeCommand : IRequest<RequestTypeDto>
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultPriority { get; set; } = 3;
}

public class CreateRequestTypeCommandValidator : AbstractValidator<CreateRequestTypeCommand>
{
    public CreateRequestTypeCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Request type code is required")
            .MaximumLength(50);
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.DefaultPriority).InclusiveBetween(1, 4).WithMessage("Default priority must be between 1 and 4");
    }
}

public class CreateRequestTypeCommandHandler : IRequestHandler<CreateRequestTypeCommand, RequestTypeDto>
{
    private readonly IRequestRepository _requestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
    private readonly ILogger<CreateRequestTypeCommandHandler> _logger;
    private readonly ITenantContext _tenantContext;

    public CreateRequestTypeCommandHandler(
        IRequestRepository requestRepository,
        IUnitOfWork unitOfWork,
        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
        ITenantContext tenantContext,
        ILogger<CreateRequestTypeCommandHandler> logger)
    {
        _requestRepository = requestRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<RequestTypeDto> Handle(CreateRequestTypeCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedException("An authenticated user is required.");

        if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ManageRequestTypesRoles))
            throw new ForbiddenException(OmsPermissions.ManageRequestTypes, _currentUser.UserId);

        if (string.IsNullOrWhiteSpace(request.Code))
            throw new SMS.Application.Exceptions.ValidationException("Request type code is required");

        var tenantId = ResolveTenantId();
        if (tenantId == Guid.Empty)
            throw new UnauthorizedException("A resolved tenant context is required.");

        if (await _requestRepository.ExistsByTypeCodeAsync(request.Code, tenantId, cancellationToken))
            throw new ConflictException("RequestType", request.Code, "A request type with this code already exists");

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var type = RequestType.Create(
                request.Code.Trim(), request.DisplayName.Trim(), request.Description?.Trim());
            type.DefaultPriority = (RequestPriority)request.DefaultPriority;

            await _requestRepository.AddRequestTypeAsync(type, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Request type {Code} created by {UserId}", request.Code, _currentUser.UserId);
            return RequestTypeDto.FromEntity(type);
        }, cancellationToken);
    }

    private Guid ResolveTenantId()
    {
        var raw = _tenantContext?.TenantId;
        return Guid.TryParse(raw, out var tenantId) ? tenantId : Guid.Empty;
    }
}

public class UpdateRequestTypeCommand : IRequest<RequestTypeDto>
{
    public Guid RequestTypeId { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateRequestTypeCommandValidator : AbstractValidator<UpdateRequestTypeCommand>
{
    public UpdateRequestTypeCommandValidator()
    {
        RuleFor(x => x.RequestTypeId).NotEmpty();
        RuleFor(x => x.DisplayName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpdateRequestTypeCommandHandler : IRequestHandler<UpdateRequestTypeCommand, RequestTypeDto>
{
    private readonly IRequestRepository _requestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
    private readonly ILogger<UpdateRequestTypeCommandHandler> _logger;

    public UpdateRequestTypeCommandHandler(
        IRequestRepository requestRepository,
        IUnitOfWork unitOfWork,
        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
        ILogger<UpdateRequestTypeCommandHandler> logger)
    {
        _requestRepository = requestRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<RequestTypeDto> Handle(UpdateRequestTypeCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedException("An authenticated user is required.");

        if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ManageRequestTypesRoles))
            throw new ForbiddenException(OmsPermissions.ManageRequestTypes, _currentUser.UserId);

        var type = await _requestRepository.GetRequestTypeByIdAsync(request.RequestTypeId, cancellationToken);
        if (type == null)
            throw new NotFoundException("RequestType", request.RequestTypeId);

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(request.DisplayName))
                type.DisplayName = request.DisplayName.Trim();
            if (request.Description != null)
                type.Description = request.Description.Trim();
            if (request.IsActive.HasValue)
                type.IsActive = request.IsActive.Value;

            await _requestRepository.UpdateRequestTypeAsync(type, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Request type {Code} updated by {UserId}", type.Code, _currentUser.UserId);
            return RequestTypeDto.FromEntity(type);
        }, cancellationToken);
    }
}

public class DeactivateRequestTypeCommand : IRequest<MediatR.Unit>
{
    public Guid RequestTypeId { get; set; }
}

public class DeactivateRequestTypeCommandHandler : IRequestHandler<DeactivateRequestTypeCommand, MediatR.Unit>
{
    private readonly IRequestRepository _requestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
    private readonly ILogger<DeactivateRequestTypeCommandHandler> _logger;

    public DeactivateRequestTypeCommandHandler(
        IRequestRepository requestRepository,
        IUnitOfWork unitOfWork,
        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
        ILogger<DeactivateRequestTypeCommandHandler> logger)
    {
        _requestRepository = requestRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<MediatR.Unit> Handle(DeactivateRequestTypeCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedException("An authenticated user is required.");

        if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ManageRequestTypesRoles))
            throw new ForbiddenException(OmsPermissions.ManageRequestTypes, _currentUser.UserId);

        var type = await _requestRepository.GetRequestTypeByIdAsync(request.RequestTypeId, cancellationToken);
        if (type == null)
            throw new NotFoundException("RequestType", request.RequestTypeId);

        type.IsActive = false;
        await _requestRepository.UpdateRequestTypeAsync(type, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Request type {Code} deactivated by {UserId}", type.Code, _currentUser.UserId);
        return MediatR.Unit.Value;
    }
}