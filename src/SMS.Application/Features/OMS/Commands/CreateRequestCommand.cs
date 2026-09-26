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
    public class CreateRequestCommand : IRequest<RequestDto>
    {
        public string RequestType { get; set; } = string.Empty;
        public string? TypeDisplayName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Optional typed SMS references. Populated by thin module adapters that
        /// have already resolved and authorized the module business context.
        /// The generic endpoint leaves them null. These fields only *link* a
        /// request to existing SMS data; they never grant the Request engine
        /// ownership of, or write access to, that data.
        /// </summary>
        public Guid? StudentId { get; set; }
        public Guid? LecturerId { get; set; }
        public Guid? CourseId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? EnrollmentId { get; set; }
        public Guid? AccommodationId { get; set; }
        public Guid? AssignmentId { get; set; }
        public Guid? CertificateId { get; set; }
    }

    public class CreateRequestCommandValidator : AbstractValidator<CreateRequestCommand>
    {
        public CreateRequestCommandValidator()
        {
            RuleFor(x => x.RequestType)
                .NotEmpty().WithMessage("Request type is required")
                .MaximumLength(100);

            RuleFor(x => x.TypeDisplayName)
                .MaximumLength(200);

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Request title is required")
                .MaximumLength(200);

            RuleFor(x => x.Description)
                .MaximumLength(2000);

            RuleFor(x => x.RelatedEntityId)
                .MaximumLength(100);

            RuleFor(x => x.RelatedEntityType)
                .MaximumLength(100);
        }
    }

    public class CreateRequestCommandHandler : IRequestHandler<CreateRequestCommand, RequestDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IRequestTypeRepository _requestTypeRepository;
        private readonly IRequestNumberGenerator _numberGenerator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<CreateRequestCommandHandler> _logger;

        public CreateRequestCommandHandler(
            IRequestRepository requestRepository,
            IRequestTypeRepository requestTypeRepository,
            IRequestNumberGenerator numberGenerator,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
                        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            ILogger<CreateRequestCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _requestTypeRepository = requestTypeRepository;
            _numberGenerator = numberGenerator;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
                        _currentUser = currentUser;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<RequestDto> Handle(CreateRequestCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required to create a request.");

            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.CreateRequestRoles))
                throw new ForbiddenException(OmsPermissions.CreateRequest, _currentUser.UserId);

            var tenantId = ResolveTenantId();
            if (tenantId == Guid.Empty)
                throw new UnauthorizedException("A resolved tenant context is required to create a request.");

            // RequestType configuration is authoritative, not advisory: an
            // unknown or deactivated type must not be usable to open a request.
            // (Phase 2 section 8 - configuration parity.)
            var requestType = await _requestTypeRepository.GetByCodeAsync(
                request.RequestType?.Trim() ?? string.Empty, cancellationToken);
            if (requestType == null)
                throw new SMS.Application.Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["RequestType"] = new[] { $"Unknown request type '{request.RequestType}'." }
                });
            if (!requestType.IsActive)
                throw new SMS.Application.Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["RequestType"] = new[] { $"Request type '{requestType.Code}' is inactive and cannot be used for new requests." }
                });

            var requestNumber = await _numberGenerator.GenerateNextRequestNumberAsync(tenantId, cancellationToken);

            var req = Request.Create(
                requestNumber,
                requestType.Code,
                request.TypeDisplayName?.Trim() ?? requestType.DisplayName,
                request.Title.Trim(),
                request.Description?.Trim(),
                _currentUser.UserId,
                request.Priority,
                request.StudentId,
                request.LecturerId,
                request.CourseId,
                request.UnitId,
                request.RelatedEntityType,
                request.RelatedEntityId,
                request.EnrollmentId,
                request.AccommodationId,
                request.AssignmentId,
                request.CertificateId);
            req.TenantId = tenantId;
            if (request.DueDate.HasValue)
                req.SetDueDate(request.DueDate.Value);

            await _requestRepository.AddAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestCreated", "Request", req.Id.ToString(),
                $"Request {req.RequestNumber} created by {_currentUser.UserId}");

            _logger.LogInformation("OMS request {RequestNumber} created", req.RequestNumber);

            return RequestDto.FromEntity(req);
        }

        private Guid ResolveTenantId()
        {
            var raw = _tenantContext?.TenantId;
            return Guid.TryParse(raw, out var tenantId) ? tenantId : Guid.Empty;
        }
    }
}