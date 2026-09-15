using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Assignments.Commands
{
    /// <summary>
    /// Deletes (soft-deletes) an assignment question document. Only the owning
    /// lecturer, a lecturer teaching the assignment's unit, or an
    /// admin/coordinator may delete documents; students can never delete them.
    /// </summary>
    public class DeleteAssignmentDocumentCommand : IRequest<bool>
    {
        public Guid AssignmentId { get; set; }
        public Guid FileId { get; set; }
    }

    public class DeleteAssignmentDocumentCommandValidator : AbstractValidator<DeleteAssignmentDocumentCommand>
    {
        public DeleteAssignmentDocumentCommandValidator()
        {
            RuleFor(x => x.AssignmentId).NotEmpty().WithMessage("Assignment ID is required");
            RuleFor(x => x.FileId).NotEmpty().WithMessage("File ID is required");
        }
    }

    public class DeleteAssignmentDocumentCommandHandler
        : IRequestHandler<DeleteAssignmentDocumentCommand, bool>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUploadRepository _uploadRepository;
        private readonly IUploadService _uploadService;
        private readonly IAcademicAccessService _academicAccessService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteAssignmentDocumentCommandHandler> _logger;

        public DeleteAssignmentDocumentCommandHandler(
            IAssignmentRepository assignmentRepository,
            IUploadRepository uploadRepository,
            IUploadService uploadService,
            IAcademicAccessService academicAccessService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<DeleteAssignmentDocumentCommandHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _uploadRepository = uploadRepository;
            _uploadService = uploadService;
            _academicAccessService = academicAccessService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(
            DeleteAssignmentDocumentCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _assignmentRepository.GetAssignmentWithDetailsAsync(
                request.AssignmentId, cancellationToken);
            if (assignment == null)
            {
                throw new NotFoundException("Assignment", request.AssignmentId);
            }

            bool allowed;
            if (_academicAccessService.IsAdminOrCoordinator())
            {
                allowed = true;
            }
            else if (_academicAccessService.IsLecturerRole())
            {
                var lecturer = await _academicAccessService.GetCurrentLecturerAsync(cancellationToken);
                allowed = lecturer != null &&
                          (assignment.LecturerId.HasValue && assignment.LecturerId.Value == lecturer.Id ||
                           await _academicAccessService.LecturerTeachesUnitAsync(
                               lecturer.Id, assignment.UnitId, cancellationToken));
            }
            else
            {
                allowed = false;
            }

            if (!allowed)
            {
                _logger.LogWarning(
                    "User {User} denied deletion of file {FileId} for assignment {AssignmentId}",
                    _currentUserService.UserId, request.FileId, request.AssignmentId);
                throw new ForbiddenException("AssignmentDocument", _currentUserService.UserId ?? "unknown");
            }

            var file = await _uploadRepository.GetByIdAsync(request.FileId);
            if (file == null || file.IsDeleted || file.Status != "Active" ||
                file.AssignmentId != request.AssignmentId)
            {
                throw new NotFoundException("AssignmentDocument", request.FileId);
            }

            // Soft-delete through the centralized upload pipeline.
            await _uploadService.DeleteAsync(file.Id, _currentUserService.UserId ?? "system");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Assignment document {FileId} deleted from assignment {AssignmentId} by {User}",
                file.Id, request.AssignmentId, _currentUserService.UserId);

            return true;
        }
    }
}
