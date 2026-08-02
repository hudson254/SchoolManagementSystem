using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Assignments.Commands
{
    public class DeleteAssignmentCommand : IRequest<MediatR.Unit>
    {
        public Guid AssignmentId { get; set; }
    }

    public class DeleteAssignmentCommandValidator : AbstractValidator<DeleteAssignmentCommand>
    {
        public DeleteAssignmentCommandValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty().WithMessage("Assignment ID is required");
        }
    }

    public class DeleteAssignmentCommandHandler : IRequestHandler<DeleteAssignmentCommand, MediatR.Unit>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteAssignmentCommandHandler> _logger;

        public DeleteAssignmentCommandHandler(
            IAssignmentRepository assignmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DeleteAssignmentCommandHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(DeleteAssignmentCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken);
            if (assignment == null)
                throw new NotFoundException("Assignment", request.AssignmentId);

            await _assignmentRepository.DeleteAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Assignment", "Delete", request.AssignmentId.ToString());

            _logger.LogInformation("Assignment {AssignmentId} deleted", request.AssignmentId);

            return MediatR.Unit.Value;
        }
    }
}
