using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Grades.Commands
{
    public class DeleteGradeCommand : IRequest<MediatR.Unit>
    {
        public Guid GradeId { get; set; }
    }

    public class DeleteGradeCommandHandler : IRequestHandler<DeleteGradeCommand, MediatR.Unit>
    {
        private readonly IGradeRepository _gradeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteGradeCommandHandler> _logger;

        public DeleteGradeCommandHandler(
            IGradeRepository gradeRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DeleteGradeCommandHandler> logger)
        {
            _gradeRepository = gradeRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(DeleteGradeCommand request, CancellationToken cancellationToken)
        {
            var grade = await _gradeRepository.GetByIdAsync(request.GradeId, cancellationToken);
            if (grade == null)
                throw new NotFoundException("Grade", request.GradeId);

            await _gradeRepository.DeleteAsync(grade, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Grade", "Delete", grade.Id.ToString());

            _logger.LogInformation("Grade {GradeId} deleted", request.GradeId);

            return MediatR.Unit.Value;
        }
    }
}
