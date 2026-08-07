using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Commands
{
    public class DeleteCourseOfferingUnitCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }

    public class DeleteCourseOfferingUnitCommandHandler
        : IRequestHandler<DeleteCourseOfferingUnitCommand, bool>
    {
        private readonly ICourseOfferingUnitRepository _courseOfferingUnitRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteCourseOfferingUnitCommandHandler> _logger;

        public DeleteCourseOfferingUnitCommandHandler(
            ICourseOfferingUnitRepository courseOfferingUnitRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DeleteCourseOfferingUnitCommandHandler> logger)
        {
            _courseOfferingUnitRepository = courseOfferingUnitRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteCourseOfferingUnitCommand request, CancellationToken cancellationToken)
        {
            var unit = await _courseOfferingUnitRepository.GetByIdAsync(request.Id, cancellationToken);
            if (unit == null)
                throw new NotFoundException("CourseOfferingUnit", request.Id);

            // Soft delete
            unit.IsActive = false;
            await _courseOfferingUnitRepository.UpdateAsync(unit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("CourseOfferingUnit", "Delete", unit.Id.ToString());

            _logger.LogInformation("Unit {UnitCode} removed from offering {OfferingId}", unit.Code, unit.CourseOfferingId);

            return true;
        }
    }
}
