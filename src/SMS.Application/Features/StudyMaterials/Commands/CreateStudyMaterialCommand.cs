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

namespace SMS.Application.Features.StudyMaterials.Commands
{
    public class CreateStudyMaterialCommand : IRequest<StudyMaterialDto>
    {
        public Guid UnitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Stream? FileStream { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// Optional lecturer override. Only honored for Administrator/Coordinator
        /// callers (they may upload material on behalf of a lecturer who teaches
        /// the unit). For regular lecturer callers this value is ignored and the
        /// lecturer is resolved from the authenticated user.
        /// </summary>
        public Guid? SpecifiedLecturerId { get; set; }
    }

    public class CreateStudyMaterialCommandValidator : AbstractValidator<CreateStudyMaterialCommand>
    {
        public CreateStudyMaterialCommandValidator()
        {
            RuleFor(x => x.UnitId)
                .NotEmpty().WithMessage("Unit ID is required");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("A title is required for the study material")
                .MaximumLength(200);

            RuleFor(x => x.Description)
                .MaximumLength(1000);

            RuleFor(x => x.OriginalFileName)
                .NotEmpty().WithMessage("A file is required");
        }
    }

    public class CreateStudyMaterialCommandHandler : IRequestHandler<CreateStudyMaterialCommand, StudyMaterialDto>
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ILectureNoteRepository _lectureNoteRepository;
        private readonly IUploadService _uploadService;
        private readonly IAcademicAccessService _academicAccessService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateStudyMaterialCommandHandler> _logger;

        public CreateStudyMaterialCommandHandler(
            IUnitRepository unitRepository,
            ILecturerRepository lecturerRepository,
            ILectureNoteRepository lectureNoteRepository,
            IUploadService uploadService,
            IAcademicAccessService academicAccessService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<CreateStudyMaterialCommandHandler> logger)
        {
            _unitRepository = unitRepository;
            _lecturerRepository = lecturerRepository;
            _lectureNoteRepository = lectureNoteRepository;
            _uploadService = uploadService;
            _academicAccessService = academicAccessService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<StudyMaterialDto> Handle(CreateStudyMaterialCommand request, CancellationToken cancellationToken)
        {
            if (request.FileStream == null)
            {
                throw new SMS.Application.Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["file"] = new[] { "A file must be provided." }
                });
            }

            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null)
            {
                throw new NotFoundException("Unit", request.UnitId);
            }

            // Resolve the lecturer server-side (never trust a client-supplied role).
            SMS.Domain.Entities.Lecturer? lecturer;
            if (_academicAccessService.IsAdminOrCoordinator())
            {
                lecturer = await ResolveAdminOrCoordinatorLecturerAsync(request, cancellationToken);
            }
            else if (_academicAccessService.IsLecturerRole())
            {
                lecturer = await _academicAccessService.GetCurrentLecturerAsync(cancellationToken);
                if (lecturer == null)
                {
                    throw new ForbiddenException("StudyMaterial", _currentUserService.UserId ?? "unknown");
                }
            }
            else
            {
                throw new ForbiddenException("StudyMaterial", _currentUserService.UserId ?? "unknown");
            }

            // Server-side relationship check: the resolved lecturer must teach this unit.
            var teachesUnit = await _academicAccessService.LecturerTeachesUnitAsync(
                lecturer.Id, request.UnitId, cancellationToken);
            if (!teachesUnit)
            {
                _logger.LogWarning(
                    "Study material upload blocked: lecturer {LecturerId} does not teach unit {UnitId}",
                    lecturer.Id, request.UnitId);
                throw new ForbiddenException($"Lecturer is not allocated to unit '{unit.Code}'");
            }

            // Validate (extension, MIME/magic bytes, size) before storing anything.
            request.FileStream.Position = 0;
            var validation = await _uploadService.ValidateAsync(
                request.FileStream, request.OriginalFileName, UploadCategory.LecturerNotes);
            if (!validation.IsValid)
            {
                throw new SMS.Application.Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["file"] = new[] { validation.ErrorMessage ?? "Invalid file." }
                });
            }

            request.FileStream.Position = 0;
            UploadResult upload;
            try
            {
                upload = await _uploadService.UploadAsync(
                    request.FileStream,
                    request.OriginalFileName,
                    UploadCategory.LecturerNotes,
                    _currentUserService.UserId ?? "system",
                    _currentUserService.Username ?? "system",
                    new UploadContext
                    {
                        UnitId = unit.Id,
                        UnitCode = unit.Code,
                        UnitName = unit.Name,
                        LecturerId = lecturer.Id,
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

            var note = new SMS.Domain.Entities.LectureNote
            {
                Title = request.Title,
                Description = request.Description,
                UnitId = unit.Id,
                LecturerId = lecturer.Id,
                FileName = upload.OriginalFileName,
                FilePath = upload.StoragePath,
                ContentType = upload.MimeType,
                FileSize = upload.FileSizeBytes,
                Version = upload.Version,
                UploadDate = DateTime.UtcNow,
                IsPublished = true,
                UploadFileId = upload.FileId
            };

            await _lectureNoteRepository.AddAsync(note, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Study material '{Title}' uploaded for unit {UnitCode} by lecturer {LecturerId} (file {FileId})",
                note.Title, unit.Code, lecturer.Id, upload.FileId);

            var lecturerName = lecturer.User != null
                ? lecturer.User.FullName
                : $"{lecturer.FirstName} {lecturer.LastName}".Trim();

            return new StudyMaterialDto
            {
                Id = note.Id,
                UnitId = note.UnitId,
                Title = note.Title,
                Description = note.Description,
                LecturerId = note.LecturerId,
                LecturerName = lecturerName,
                UploadFileId = upload.FileId,
                OriginalFileName = upload.OriginalFileName,
                Extension = upload.Extension,
                ContentType = upload.MimeType,
                FileSizeBytes = upload.FileSizeBytes,
                Version = upload.Version,
                UploadedAt = note.UploadDate,
                IsPublished = note.IsPublished
            };
        }

        private async Task<SMS.Domain.Entities.Lecturer?> ResolveAdminOrCoordinatorLecturerAsync(
            CreateStudyMaterialCommand request, CancellationToken cancellationToken)
        {
            if (request.SpecifiedLecturerId.HasValue)
            {
                var lecturer = await _lecturerRepository.GetByIdAsync(request.SpecifiedLecturerId.Value, cancellationToken);
                if (lecturer == null)
                {
                    throw new NotFoundException("Lecturer", request.SpecifiedLecturerId.Value);
                }
                return lecturer;
            }

            return await _academicAccessService.GetCurrentLecturerAsync(cancellationToken);
        }
    }
}