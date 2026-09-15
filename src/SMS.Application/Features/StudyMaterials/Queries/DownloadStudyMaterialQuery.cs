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

namespace SMS.Application.Features.StudyMaterials.Queries
{
    public class DownloadStudyMaterialQuery : IRequest<FileDownloadResult>
    {
        public Guid MaterialId { get; set; }
    }

    public class DownloadStudyMaterialQueryValidator : AbstractValidator<DownloadStudyMaterialQuery>
    {
        public DownloadStudyMaterialQueryValidator()
        {
            RuleFor(x => x.MaterialId).NotEmpty().WithMessage("Material ID is required");
        }
    }

    public class DownloadStudyMaterialQueryHandler
        : IRequestHandler<DownloadStudyMaterialQuery, FileDownloadResult>
    {
        private readonly ILectureNoteRepository _lectureNoteRepository;
        private readonly IUploadService _uploadService;
        private readonly IAcademicAccessService _academicAccessService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILogger<DownloadStudyMaterialQueryHandler> _logger;

        public DownloadStudyMaterialQueryHandler(
            ILectureNoteRepository lectureNoteRepository,
            IUploadService uploadService,
            IAcademicAccessService academicAccessService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILogger<DownloadStudyMaterialQueryHandler> logger)
        {
            _lectureNoteRepository = lectureNoteRepository;
            _uploadService = uploadService;
            _academicAccessService = academicAccessService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<FileDownloadResult> Handle(
            DownloadStudyMaterialQuery request, CancellationToken cancellationToken)
        {
            var material = await _lectureNoteRepository.GetByIdAsync(request.MaterialId, cancellationToken);
            if (material == null || !material.IsPublished)
            {
                throw new NotFoundException("StudyMaterial", request.MaterialId);
            }

            if (!await CanAccessAsync(material.UnitId, cancellationToken))
            {
                throw new ForbiddenException("StudyMaterial", _currentUserService.UserId ?? "unknown");
            }

            var stream = await _uploadService.DownloadByPathAsync(material.FilePath);

            var contentType = !string.IsNullOrEmpty(material.UploadFile?.MimeType)
                ? material.UploadFile.MimeType
                : material.ContentType ?? "application/octet-stream";

            var fileName = !string.IsNullOrEmpty(material.UploadFile?.OriginalFileName)
                ? material.UploadFile.OriginalFileName
                : material.FileName;

            return new FileDownloadResult
            {
                FileName = fileName,
                ContentType = contentType,
                Length = stream?.Length ?? 0,
                Stream = stream
            };
        }

        private async Task<bool> CanAccessAsync(Guid unitId, CancellationToken cancellationToken)
        {
            if (_academicAccessService.IsAdminOrCoordinator())
            {
                return true;
            }

            if (_academicAccessService.IsLecturerRole() &&
                await _academicAccessService.LecturerTeachesUnitAsync(unitId, cancellationToken))
            {
                return true;
            }

            if (_academicAccessService.IsStudentRole() &&
                await _academicAccessService.StudentEnrolledInUnitAsync(unitId, cancellationToken))
            {
                return true;
            }

            return false;
        }
    }
}