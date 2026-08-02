using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Grades.Commands
{
    public class PublishGradesCommand : IRequest<MediatR.Unit>
    {
        public Guid UnitId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    public class PublishGradesCommandValidator : AbstractValidator<PublishGradesCommand>
    {
        public PublishGradesCommandValidator()
        {
            RuleFor(x => x.UnitId).NotEmpty().WithMessage("Unit ID is required");
        }
    }

    public class PublishGradesCommandHandler : IRequestHandler<PublishGradesCommand, MediatR.Unit>
    {
        private readonly IGradeRepository _gradeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<PublishGradesCommandHandler> _logger;

        public PublishGradesCommandHandler(
            IGradeRepository gradeRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<PublishGradesCommandHandler> logger)
        {
            _gradeRepository = gradeRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(PublishGradesCommand request, CancellationToken cancellationToken)
        {
            var grades = await _gradeRepository.GetGradesByUnitAsync(request.UnitId);

            if (request.SemesterId.HasValue)
                grades = grades.Where(g => g.SemesterId == request.SemesterId.Value);

            var unpublished = grades.Where(g => !g.IsPublished).ToList();
            if (!unpublished.Any())
            {
                _logger.LogInformation("No unpublished grades found for unit {UnitId}", request.UnitId);
                return MediatR.Unit.Value;
            }

            foreach (var grade in unpublished)
            {
                grade.IsPublished = true;
                grade.PublishedDate = DateTime.UtcNow;
                await _gradeRepository.UpdateAsync(grade, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Grade", "Publish", $"Published {unpublished.Count} grades for unit {request.UnitId}");

            _logger.LogInformation("Published {Count} grades for unit {UnitId}", unpublished.Count, request.UnitId);

            return MediatR.Unit.Value;
        }
    }
}
