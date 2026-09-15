using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.StudyMaterials.Commands
{
    public class DeleteStudyMaterialCommand : IRequest<bool>
    {
        public Guid MaterialId { get; set; }
        public Guid UnitId { get; set; }
    }

    public class DeleteStudyMaterialCommandValidator : AbstractValidator<DeleteStudyMaterialCommand>
    {
        public DeleteStudyMaterialCommandValidator()
        {
            RuleFor(x => x.MaterialId).NotEmpty().WithMessage("Material ID is required");
            RuleFor(x => x.UnitId).NotEmpty().WithMessage("Unit ID is required");
        }
    }

    public class DeleteStudyMaterialCommandHandler : IRequestHandler<DeleteStudyMaterialCommand, bool>
    {
        private readonly ILectureNoteRepository _lectureNoteRepository;
        private readonly IUploadService _uploadService;
        private readonly IAcademicAccessService _academicAccessService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteStudyMaterialCommandHandler> _logger;

        public DeleteStudyMaterialCommandHandler(
            ILectureNoteRepository lectureNoteRepository,
            IUploadService uploadService,
            IAcademicAccessService academicAccessService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<DeleteStudyMaterialCommandHandler> logger)
        {
            _lectureNoteRepository = lectureNoteRepository;
            _uploadService = uploadService;
            _academicAccessService = academicAccessService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteStudyMaterialCommand request, CancellationToken cancellationToken)
        {
            var material = await _lectureNoteRepository.GetByIdAsync(request.MaterialId, cancellationToken);
            if (material == null || material.UnitId != request.UnitId)
            {
                throw new NotFoundException("StudyMaterial", request.MaterialId);
            }

            bool allowed;
            if (_academicAccessService.IsAdminOrCoordinator())
            {
                allowed = true;
            }
            else if (_academicAccessService.IsLecturerRole())
            {
                var currentLecturer = await _academicAccessService.GetCurrentLecturerAsync(cancellationToken);
                allowed = currentLecturer != null &&
                          (currentLecturer.Id == material.LecturerId ||
                           await _academicAccessService.LecturerTeachesUnitAsync(
                               currentLecturer.Id, material.UnitId, cancellationToken));
            }
            else
            {
                allowed = false;
            }

            if (!allowed)
            {
                throw new ForbiddenException("StudyMaterial", _currentUserService.UserId ?? "unknown");
            }

            // Soft-delete the domain record.
            material.SoftDelete(_currentUserService.UserId ?? "system");
            material.IsPublished = false;
            await _lectureNoteRepository.UpdateAsync(material, cancellationToken);

            // Soft-delete the underlying file metadata (central upload pipeline).
            if (material.UploadFileId.HasValue)
            {
                await _uploadService.DeleteAsync(material.UploadFileId.Value, _currentUserService.UserId ?? "system");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Study material {MaterialId} deleted from unit {UnitId} by {User}",
                request.MaterialId, request.UnitId, _currentUserService.UserId);

            return true;
        }
    }
}