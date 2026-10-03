using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Commands;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Queries
{
    public class GetRequestStatusHistoryQuery : IRequest<IReadOnlyList<RequestStatusHistoryDto>>
    {
        public Guid RequestId { get; set; }
    }

    public class GetRequestStatusHistoryQueryHandler
        : IRequestHandler<GetRequestStatusHistoryQuery, IReadOnlyList<RequestStatusHistoryDto>>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        public GetRequestStatusHistoryQueryHandler(
            IRequestRepository requestRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser)
        {
            _requestRepository = requestRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<RequestStatusHistoryDto>> Handle(
            GetRequestStatusHistoryQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required.");

            // Coarse role gate only. Whether the caller may see THIS request is decided
            // by OmsRequestAccess.EnsureCanView below, which enforces ownership,
            // assignment and tenant scoping. A role that may raise a request
            // (Student, Receptionist) must be able to read its own history,
            // otherwise the request detail page 403s for the very request the
            // user just created.
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewOwnRequestsRoles)
                && !OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewRequestsRoles))
                throw new ForbiddenException(OmsPermissions.ViewRequestHistory, _currentUser.UserId);

            // Object-level authorization: history rows belong to a specific request,
            // so the caller must be allowed to view that request itself. The
            // tenant-scoped lookup returns null for another tenant's request,
            // which surfaces as NotFound rather than leaking its existence.
            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            OmsRequestAccess.EnsureCanView(req, _currentUser.UserId, _currentUser.Roles);

            var history = await _requestRepository.GetStatusHistoryAsync(request.RequestId, cancellationToken);
            return history.Select(RequestStatusHistoryDto.FromEntity).ToList().AsReadOnly();
        }
    }

    public class GetRequestCommentsQuery : IRequest<IReadOnlyList<RequestCommentDto>>
    {
        public Guid RequestId { get; set; }
    }

    public class GetRequestCommentsQueryHandler
        : IRequestHandler<GetRequestCommentsQuery, IReadOnlyList<RequestCommentDto>>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;

        public GetRequestCommentsQueryHandler(
            IRequestRepository requestRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser)
        {
            _requestRepository = requestRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<RequestCommentDto>> Handle(
            GetRequestCommentsQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required.");

            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewRequestsRoles))
                throw new ForbiddenException(OmsPermissions.ViewRequests, _currentUser.UserId);

            // Object-level authorization: comments belong to a specific request,
            // so the caller must be allowed to view that request itself.
            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            OmsRequestAccess.EnsureCanView(req, _currentUser.UserId, _currentUser.Roles);

            var comments = await _requestRepository.GetCommentsAsync(request.RequestId, cancellationToken);
            return comments.Select(RequestCommentDto.FromEntity).ToList().AsReadOnly();
        }
    }

    public class GetRequestAttachmentsQuery : IRequest<IReadOnlyList<RequestAttachmentDto>>
    {
        public Guid RequestId { get; set; }
    }

    public class GetRequestAttachmentsQueryValidator : AbstractValidator<GetRequestAttachmentsQuery>
    {
        public GetRequestAttachmentsQueryValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
        }
    }

    public class GetRequestAttachmentsQueryHandler
        : IRequestHandler<GetRequestAttachmentsQuery, IReadOnlyList<RequestAttachmentDto>>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;

        public GetRequestAttachmentsQueryHandler(
            IRequestRepository requestRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser)
        {
            _requestRepository = requestRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<RequestAttachmentDto>> Handle(
            GetRequestAttachmentsQuery query, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required.");

            // Coarse gate only; OmsRequestAccess.EnsureCanView below owns the object-level
            // decision. A requester (Student, Receptionist) must be able to see the
            // attachments on the request they raised.
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewOwnRequestsRoles)
                && !OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewRequestsRoles))
                throw new ForbiddenException(OmsPermissions.ViewRequests, _currentUser.UserId);

            var req = await _requestRepository.GetByIdAsync(query.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", query.RequestId);

            OmsRequestAccess.EnsureCanView(req, _currentUser.UserId, _currentUser.Roles);

            var attachments = await _requestRepository.GetAttachmentsByRequestIdAsync(query.RequestId);
            return attachments.Select(RequestAttachmentDto.FromEntity).ToList().AsReadOnly();
        }
    }

    public class DownloadRequestAttachmentQuery : IRequest<RequestAttachmentDownloadDto>
    {
        public Guid AttachmentId { get; set; }
    }

    public class DownloadRequestAttachmentQueryHandler
        : IRequestHandler<DownloadRequestAttachmentQuery, RequestAttachmentDownloadDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IFileStorageService _fileStorage;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;

        public DownloadRequestAttachmentQueryHandler(
            IRequestRepository requestRepository,
            IFileStorageService fileStorage,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser)
        {
            _requestRepository = requestRepository;
            _fileStorage = fileStorage;
            _currentUser = currentUser;
        }

        public async Task<RequestAttachmentDownloadDto> Handle(
            DownloadRequestAttachmentQuery query, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required.");

            // Coarse gate only; OmsRequestAccess.EnsureCanView below owns the object-level
            // decision (ownership of the owning request).
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewOwnRequestsRoles)
                && !OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewRequestsRoles))
                throw new ForbiddenException(OmsPermissions.ViewRequests, _currentUser.UserId);

            var attachment = await _requestRepository.GetAttachmentByIdAsync(query.AttachmentId);
            if (attachment == null)
                throw new NotFoundException("RequestAttachment", query.AttachmentId);

            var req = await _requestRepository.GetByIdAsync(attachment.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", attachment.RequestId);

            OmsRequestAccess.EnsureCanView(req, _currentUser.UserId, _currentUser.Roles);

            var bytes = await _fileStorage.GetFileAsync(attachment.StorageKey);
            return new RequestAttachmentDownloadDto
            {
                FileName = attachment.FileName,
                ContentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                    ? "application/octet-stream"
                    : attachment.ContentType,
                Content = bytes
            };
        }
    }

    public class DeleteRequestAttachmentCommand : IRequest
    {
        public Guid AttachmentId { get; set; }
    }

    public class DeleteRequestAttachmentCommandHandler : IRequestHandler<DeleteRequestAttachmentCommand>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<DeleteRequestAttachmentCommandHandler> _logger;

        public DeleteRequestAttachmentCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            IFileStorageService fileStorage,
            ILogger<DeleteRequestAttachmentCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task Handle(
            DeleteRequestAttachmentCommand command, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required.");

            var attachment = await _requestRepository.GetAttachmentByIdAsync(command.AttachmentId);
            if (attachment == null)
                throw new NotFoundException("RequestAttachment", command.AttachmentId);

            var req = await _requestRepository.GetByIdAsync(attachment.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", attachment.RequestId);

            OmsRequestAccess.EnsureCanDeleteAttachment(
                req, attachment.UploadedByUserId, _currentUser.UserId, _currentUser.Roles);

            _requestRepository.RemoveAttachment(attachment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _fileStorage.DeleteFileAsync(attachment.StorageKey);

            await _auditService.LogActivityAsync(
                "RequestAttachmentDeleted", "Request", req.Id.ToString(),
                $"Attachment {attachment.FileName} removed from request {req.RequestNumber} by {_currentUser.UserId}.");

            _logger.LogInformation(
                "OMS request {RequestNumber} attachment {FileName} deleted",
                req.RequestNumber, attachment.FileName);
        }
    }
}
