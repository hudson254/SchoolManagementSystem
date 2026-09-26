using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Commands
{
    /// <summary>
    /// Upload policy for OMS request attachments. Kept in the Application layer
    /// so the same rules are enforced for every entry point (generic OMS
    /// endpoint and thin module adapters alike). The physical write goes through
    /// the existing <see cref="IFileStorageService"/> - no second storage stack.
    /// </summary>
    public static class RequestAttachmentPolicy
    {
        /// <summary>Maximum accepted attachment size (matches FileStorage:MaxFileSizeMB).</summary>
        public const long MaxFileSizeBytes = 10L * 1024 * 1024;

        /// <summary>Container (sub-directory) under the configured storage root.</summary>
        public const string ContainerName = "oms-request-attachments";

        /// <summary>
        /// Document-oriented allow-list. Executables, scripts and archive types
        /// with executable content are intentionally excluded.
        /// </summary>
        public static readonly IReadOnlySet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".csv", ".txt", ".rtf", ".odt", ".ods",
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tif", ".tiff"
        };

        public static bool IsAllowed(string? fileName) =>
            !string.IsNullOrWhiteSpace(fileName) &&
            AllowedExtensions.Contains(Path.GetExtension(fileName));
    }

    /// <summary>
    /// Attaches a file to an existing OMS request. The bytes are stored via the
    /// existing file storage service; only the association row is persisted here.
    /// </summary>
    public class UploadRequestAttachmentCommand : IRequest<RequestAttachmentDto>
    {
        public Guid RequestId { get; set; }
        public Stream? FileStream { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long FileSizeBytes { get; set; }
    }

    public class UploadRequestAttachmentCommandValidator : AbstractValidator<UploadRequestAttachmentCommand>
    {
        public UploadRequestAttachmentCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty().WithMessage("Request ID is required");
            RuleFor(x => x.OriginalFileName)
                .NotEmpty().WithMessage("A file name is required")
                .Must(RequestAttachmentPolicy.IsAllowed)
                .WithMessage("File type is not permitted for request attachments.");
            RuleFor(x => x.FileSizeBytes)
                .GreaterThan(0).WithMessage("An empty file cannot be attached.")
                .LessThanOrEqualTo(RequestAttachmentPolicy.MaxFileSizeBytes)
                .WithMessage($"Attachment exceeds the maximum size of {RequestAttachmentPolicy.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }
    }

    public class UploadRequestAttachmentCommandHandler : IRequestHandler<UploadRequestAttachmentCommand, RequestAttachmentDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<UploadRequestAttachmentCommandHandler> _logger;

        public UploadRequestAttachmentCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            IFileStorageService fileStorage,
            ILogger<UploadRequestAttachmentCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task<RequestAttachmentDto> Handle(
            UploadRequestAttachmentCommand command,
            CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new Exceptions.UnauthorizedException("An authenticated user is required to attach a file.");

            var req = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);
            if (req == null)
                throw new Exceptions.NotFoundException("Request", command.RequestId);

            var tenantId = Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
            if (req.TenantId != tenantId)
                throw new Exceptions.ForbiddenException(OmsPermissions.UpdateRequest, _currentUser.UserId);

            OmsRequestAccess.EnsureCanAttach(req, _currentUser.UserId, _currentUser.Roles);

            if (command.FileStream == null || command.FileStream == Stream.Null)
                throw new Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["file"] = new[] { "A file stream is required." }
                });

            if (string.IsNullOrWhiteSpace(command.OriginalFileName)
                || !RequestAttachmentPolicy.IsAllowed(command.OriginalFileName))
                throw new Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["fileName"] = new[] { "File type is not permitted for request attachments." }
                });

            if (command.FileSizeBytes <= 0
                || command.FileSizeBytes > RequestAttachmentPolicy.MaxFileSizeBytes)
                throw new Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["file"] = new[] { $"Attachment exceeds the maximum size of {RequestAttachmentPolicy.MaxFileSizeBytes / (1024 * 1024)} MB." }
                });

            var safeName = Path.GetFileName(command.OriginalFileName.Trim());
            if (string.IsNullOrWhiteSpace(safeName))
                throw new Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["fileName"] = new[] { "A file name is required." }
                });

            var storageKey = await _fileStorage.UploadFileAsync(
                command.FileStream, safeName, RequestAttachmentPolicy.ContainerName);

            var attachment = new RequestAttachment
            {
                RequestId = req.Id,
                FileName = safeName,
                ContentType = command.ContentType ?? "application/octet-stream",
                Size = command.FileSizeBytes,
                StorageKey = storageKey,
                UploadedByUserId = _currentUser.UserId,
                UploadedByUserName = _currentUser.Username,
                TenantId = tenantId
            };

            await _requestRepository.AddAttachmentAsync(attachment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestAttachmentUploaded", "Request", req.Id.ToString(),
                $"Attachment {safeName} uploaded to request {req.RequestNumber} by {_currentUser.UserId}.");

            _logger.LogInformation(
                "OMS request {RequestNumber} attachment {FileName} uploaded",
                req.RequestNumber, safeName);

            return RequestAttachmentDto.FromEntity(attachment);
        }
    }
}
