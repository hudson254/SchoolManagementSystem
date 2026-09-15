using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Assignments.Commands
{
    public class UploadAssignmentDocumentCommand : IRequest<AssignmentDocumentDto>
    {
        public Guid AssignmentId { get; set; }
        public string? Description { get; set; }
        public Stream? FileStream { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
    }

    public class UploadAssignmentDocumentCommandValidator : AbstractValidator<UploadAssignmentDocumentCommand>
    {
        public UploadAssignmentDocumentCommandValidator()
        {
            RuleFor(x => x.AssignmentId).NotEmpty().WithMessage("Assignment ID is required");
            RuleFor(x => x.OriginalFileName).NotEmpty().WithMessage("A file is required");
            RuleFor(x => x.Description).MaximumLength(1000);
        }
    }

    public class UploadAssignmentDocumentCommandHandler
        : IRequestHandler<UploadAssignmentDocumentCommand, AssignmentDocumentDto>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUploadService _uploadService;
        private readonly IAcademicAccessService _academicAccessService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UploadAssignmentDocumentCommandHandler> _logger;

        public UploadAssignmentDocumentCommandHandler(
            IAssignmentRepository assignmentRepository,
            IUploadService uploadService,
            IAcademicAccessService academicAccessService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<UploadAssignmentDocumentCommandHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _uploadService = uploadService;
            _academicAccessService = academicAccessService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AssignmentDocumentDto> Handle(
            UploadAssignmentDocumentCommand request, CancellationToken cancellationToken)
        {
            if (request.FileStream == null)
            {
                throw new SMS.Application.Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["file"] = new[] { "A file must be provided." }
                });
            }

            var assignment = await _assignmentRepository.GetAssignmentWithDetailsAsync(
                request.AssignmentId, cancellationToken);
            if (assignment == null)
            {
                throw new NotFoundException("Assignment", request.AssignmentId);
            }

            // Server-side authorization: only the owning lecturer (or an
            // admin/coordinator) may attach question documents to an assignment.
            if (!await CanManageAssignmentAsync(assignment, cancellationToken))
            {
                _logger.LogWarning(
                    "Assignment document upload blocked: user {User} is not authorized for assignment {AssignmentId}",
                    _currentUserService.UserId, request.AssignmentId);
                throw new ForbiddenException("Assignment", _currentUserService.UserId ?? "unknown");
            }

            // Validate (extension, MIME/magic bytes, size) before storing anything.
            request.FileStream.Position = 0;
            var validation = await _uploadService.ValidateAsync(
                request.FileStream, request.OriginalFileName, UploadCategory.AssignmentBrief);
            if (!validation.IsValid)
            {
                throw new SMS.Application.Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["file"] = new[] { validation.ErrorMessage ?? "Invalid file." }
                });
            }

            // Store through the centralized upload pipeline (secure naming, hashing,
            // tenant-aware storage path, persistent volume in production).
            UploadResult upload;
            try
            {
                request.FileStream.Position = 0;
                upload = await _uploadService.UploadAsync(
                    request.FileStream,
                    request.OriginalFileName,
                    UploadCategory.AssignmentBrief,
                    _currentUserService.UserId ?? "system",
                    _currentUserService.Username ?? "system",
                    new UploadContext
                    {
                        AssignmentId = assignment.Id,
                        UnitId = assignment.UnitId,
                        CourseOfferingId = assignment.CourseOfferingId,
                        LecturerId = assignment.LecturerId,
                        Description = request.Description
                    });
            }
            catch (InvalidOperationException ex)
            {
                throw new SMS.Application.Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["file"] = new[] { ex.Message }
                });
            }

            _logger.LogInformation(
                "Assignment question document uploaded for assignment {AssignmentId} (file {FileId}) by {User}",
                request.AssignmentId, upload.FileId, _currentUserService.UserId);

            return AssignmentDocumentMappings.ToDto(new SMS.Domain.Entities.UploadFile
            {
                Id = upload.FileId,
                AssignmentId = request.AssignmentId,
                OriginalFileName = upload.OriginalFileName,
                GeneratedFileName = upload.GeneratedFileName,
                StoragePath = upload.StoragePath,
                Extension = upload.Extension,
                MimeType = upload.MimeType,
                FileSizeBytes = upload.FileSizeBytes,
                Sha256Hash = upload.Sha256Hash,
                Category = UploadCategory.AssignmentBrief,
                Version = upload.Version,
                UploadedByUserId = _currentUserService.UserId,
                UploadedByUsername = _currentUserService.Username,
                UploadedAt = DateTime.UtcNow,
                Status = upload.Status,
                Description = request.Description
            });
        }

        private async Task<bool> CanManageAssignmentAsync(
            SMS.Domain.Entities.Assignment assignment, CancellationToken cancellationToken)
        {
            // Admin/Coordinator retain management access (existing authorization model).
            if (_academicAccessService.IsAdminOrCoordinator())
            {
                return true;
            }

            if (!_academicAccessService.IsLecturerRole())
            {
                return false;
            }

            var lecturer = await _academicAccessService.GetCurrentLecturerAsync(cancellationToken);
            if (lecturer == null)
            {
                return false;
            }

            // Owner lecturer may always manage their own assignment.
            if (assignment.LecturerId.HasValue && assignment.LecturerId.Value == lecturer.Id)
            {
                return true;
            }

            // Otherwise the lecturer must teach the assignment's unit. The chain is
            // resolved server-side (UnitAllocation / CourseOfferingLecturer), never
            // from a client-supplied flag.
            return await _academicAccessService.LecturerTeachesUnitAsync(
                lecturer.Id, assignment.UnitId, cancellationToken);
        }
    }
}