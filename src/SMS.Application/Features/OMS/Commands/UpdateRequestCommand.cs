using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Commands
{
    public class UpdateRequestCommand : IRequest<RequestDto>
    {
        public Guid RequestId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public RequestPriority? Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public byte[]? RowVersion { get; set; }
    }

    public class UpdateRequestCommandValidator : AbstractValidator<UpdateRequestCommand>
    {
        public UpdateRequestCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
            RuleFor(x => x.Title).MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
        }
    }

    public class UpdateRequestCommandHandler : IRequestHandler<UpdateRequestCommand, RequestDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<UpdateRequestCommandHandler> _logger;

        public UpdateRequestCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            ILogger<UpdateRequestCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<RequestDto> Handle(UpdateRequestCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated)
                throw new UnauthorizedException("An authenticated user is required.");

            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            if (req.TenantId != ResolveTenantId())
                throw new ForbiddenException(OmsPermissions.UpdateRequest, _currentUser.UserId);

            if (!RequestLifecycle.CanEdit(req.Status))
                throw new BusinessRuleException($"Cannot edit a request in {req.Status} status.");

            // Object-level ownership. Role membership alone is never sufficient:
            // only the requester (or a privileged queue role that may update any
            // request in the tenant) may modify this record. A student therefore
            // cannot rewrite another student's request even though Student is a
            // request-creating role.
            if (!OmsRequestAccess.CanUpdate(req, _currentUser.UserId, _currentUser.Roles))
                throw new ForbiddenException(OmsPermissions.UpdateRequest, _currentUser.UserId);

            if (request.RowVersion != null)
            {
                if (!req.RowVersion!.SequenceEqual(request.RowVersion))
                    throw new BusinessRuleException("Concurrency conflict: the request has been modified by another user.");
            }

            var changes = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                req.UpdateTitle(request.Title.Trim());
                changes.Add($"Title: {request.Title}");
            }
            if (request.Description != null)
            {
                req.UpdateDescription(request.Description.Trim());
                changes.Add("Description updated");
            }
            if (request.Priority.HasValue)
            {
                req.SetPriority(request.Priority.Value);
                changes.Add($"Priority: {request.Priority}");
            }
            if (request.DueDate.HasValue)
            {
                req.SetDueDate(request.DueDate.Value);
                changes.Add($"DueDate: {request.DueDate}");
            }
            else if (request.DueDate == null && req.DueDate.HasValue)
            {
                req.ClearDueDate();
                changes.Add("DueDate cleared");
            }

            await _requestRepository.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogDataChangeAsync("Request", req.Id.ToString(), "Update", string.Join(", ", changes));
            _logger.LogInformation("OMS request {RequestNumber} updated", req.RequestNumber);

            return RequestDto.FromEntity(req);
        }

        private Guid ResolveTenantId() => Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
    }
}