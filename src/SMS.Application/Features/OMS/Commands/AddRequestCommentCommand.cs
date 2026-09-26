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

namespace SMS.Application.Features.OMS.Commands
{
    public class AddRequestCommentCommand : IRequest<RequestCommentDto>
    {
        public Guid RequestId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AddRequestCommentCommandValidator : AbstractValidator<AddRequestCommentCommand>
    {
        public AddRequestCommentCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Comment message is required")
                .MaximumLength(2000);
        }
    }

    public class AddRequestCommentCommandHandler : IRequestHandler<AddRequestCommentCommand, RequestCommentDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
                private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly IOmsRequestNotifier _notifier;
        private readonly ILogger<AddRequestCommentCommandHandler> _logger;

        public AddRequestCommentCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
                        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            IOmsRequestNotifier notifier,
            ILogger<AddRequestCommentCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
                        _currentUser = currentUser;
            _tenantContext = tenantContext;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<RequestCommentDto> Handle(AddRequestCommentCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();

            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            if (req.TenantId != ResolveTenantId())
                throw new ForbiddenException(OmsPermissions.CommentOnRequest, _currentUser.UserId);

            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.CommentOnRequestRoles))
                throw new ForbiddenException(OmsPermissions.CommentOnRequest, _currentUser.UserId);

            var comment = RequestComment.Create(
                request.RequestId,
                _currentUser.UserId,
                request.Message.Trim());
            comment.TenantId = req.TenantId;

            await _unitOfWork.RequestComments.AddAsync(comment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestCommentAdded", "RequestComment", comment.Id.ToString(),
                $"Comment added to request {req.RequestNumber} by {_currentUser.UserId}");

            // Notify the request owner
            if (req.RequesterUserId != _currentUser.UserId)
            {
                // Notify relevant participants where appropriate: the requester
                // is told about new activity on their request.
                await _notifier.NotifyAsync(
                    req.RequesterUserId,
                    $"New comment on {req.RequestNumber}",
                    $"{_currentUser.Username ?? _currentUser.UserId} commented on your request '{req.Title}' ({req.RequestNumber}).",
                    req.Id,
                    cancellationToken);
            }

            _logger.LogInformation("OMS request {RequestNumber} comment added by {UserId}", req.RequestNumber, _currentUser.UserId);

            return RequestCommentDto.FromEntity(comment);
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required.");
        }

        private Guid ResolveTenantId() => Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
    }
}