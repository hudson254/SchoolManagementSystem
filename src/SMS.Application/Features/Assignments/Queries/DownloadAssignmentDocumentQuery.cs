using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Assignments.Queries
{
    /// <summary>
    /// Streams an assignment question document for download through the
    /// authenticated endpoint. The same server-side access rules as listing
    /// apply (owner lecturer / unit lecturer / admin-coordinator / enrolled student).
    /// </summary>
    public class DownloadAssignmentDocumentQuery : IRequest<FileDownloadResult>
    {
        public Guid AssignmentId { get; set; }
        public Guid FileId { get; set; }
    }

    public class DownloadAssignmentDocumentQueryValidator : AbstractValidator<DownloadAssignmentDocumentQuery>
    {
        public DownloadAssignmentDocumentQueryValidator()
        {
            RuleFor(x => x.AssignmentId).NotEmpty().WithMessage("Assignment ID is required");
            RuleFor(x => x.FileId).NotEmpty().WithMessage("File ID is required");
        }
    }

    public class DownloadAssignmentDocumentQueryHandler
        : IRequestHandler<DownloadAssignmentDocumentQuery, FileDownloadResult>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUploadRepository _uploadRepository;
        private readonly IUploadService _uploadService;
        private readonly IAcademicAccessService _academicAccessService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILogger<DownloadAssignmentDocumentQueryHandler> _logger;

        public DownloadAssignmentDocumentQueryHandler(
            IAssignmentRepository assignmentRepository,
            IUploadRepository uploadRepository,
            IUploadService uploadService,
            IAcademicAccessService academicAccessService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILogger<DownloadAssignmentDocumentQueryHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _uploadRepository = uploadRepository;
            _uploadService = uploadService;
            _academicAccessService = academicAccessService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<FileDownloadResult> Handle(
            DownloadAssignmentDocumentQuery request, CancellationToken cancellationToken)
        {
            var assignment = await _assignmentRepository.GetAssignmentWithDetailsAsync(
                request.AssignmentId, cancellationToken);
            if (assignment == null)
            {
                throw new NotFoundException("Assignment", request.AssignmentId);
            }

            if (!await AssignmentDocumentAccess.CanAccessAsync(
                    assignment, _academicAccessService, cancellationToken))
            {
                _logger.LogWarning(
                    "User {User} denied download of file {FileId} for assignment {AssignmentId}",
                    _currentUserService.UserId, request.FileId, request.AssignmentId);
                throw new ForbiddenException("AssignmentDocument", _currentUserService.UserId ?? "unknown");
            }

            var file = await _uploadRepository.GetByIdAsync(request.FileId);
            if (file == null || file.IsDeleted || file.Status != "Active" ||
                file.AssignmentId != request.AssignmentId)
            {
                throw new NotFoundException("AssignmentDocument", request.FileId);
            }

            var stream = await _uploadService.DownloadAsync(file.Id);

            _logger.LogInformation(
                "Assignment document {FileId} downloaded for assignment {AssignmentId} by {User}",
                file.Id, request.AssignmentId, _currentUserService.UserId);

            return new FileDownloadResult
            {
                FileName = file.OriginalFileName,
                ContentType = string.IsNullOrEmpty(file.MimeType)
                    ? "application/octet-stream"
                    : file.MimeType,
                Length = file.FileSizeBytes,
                Stream = stream
            };
        }
    }
}
