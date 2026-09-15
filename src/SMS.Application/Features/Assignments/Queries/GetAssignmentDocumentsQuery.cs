using System;
using System.Linq;
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

namespace SMS.Application.Features.Assignments.Queries
{
    /// <summary>
    /// Lists the question documents attached to an assignment. Access is enforced
    /// server-side: the owning lecturer, a lecturer teaching the unit,
    /// admin/coordinator roles, or a student enrolled in the unit.
    /// </summary>
    public class GetAssignmentDocumentsQuery : IRequest<IEnumerable<AssignmentDocumentDto>>
    {
        public Guid AssignmentId { get; set; }
    }

    public class GetAssignmentDocumentsQueryValidator : AbstractValidator<GetAssignmentDocumentsQuery>
    {
        public GetAssignmentDocumentsQueryValidator()
        {
            RuleFor(x => x.AssignmentId).NotEmpty().WithMessage("Assignment ID is required");
        }
    }

    public class GetAssignmentDocumentsQueryHandler
        : IRequestHandler<GetAssignmentDocumentsQuery, IEnumerable<AssignmentDocumentDto>>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUploadRepository _uploadRepository;
        private readonly IAcademicAccessService _academicAccessService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILogger<GetAssignmentDocumentsQueryHandler> _logger;

        public GetAssignmentDocumentsQueryHandler(
            IAssignmentRepository assignmentRepository,
            IUploadRepository uploadRepository,
            IAcademicAccessService academicAccessService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILogger<GetAssignmentDocumentsQueryHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _uploadRepository = uploadRepository;
            _academicAccessService = academicAccessService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<IEnumerable<AssignmentDocumentDto>> Handle(
            GetAssignmentDocumentsQuery request, CancellationToken cancellationToken)
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
                    "User {User} denied access to documents of assignment {AssignmentId}",
                    _currentUserService.UserId, request.AssignmentId);
                throw new ForbiddenException("AssignmentDocument", _currentUserService.UserId ?? "unknown");
            }

            var files = await _uploadRepository.GetByAssignmentAsync(request.AssignmentId);
            return files
                .Where(f => f.Category == UploadCategory.AssignmentBrief &&
                            f.Status == "Active" &&
                            !f.IsDeleted)
                .Select(AssignmentDocumentMappings.ToDto)
                .ToList();
        }
    }
}
