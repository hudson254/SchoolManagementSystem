using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Commands
{
    public class DeleteCourseOfferingCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }

    public class DeleteCourseOfferingCommandHandler : IRequestHandler<DeleteCourseOfferingCommand, bool>
    {
        private readonly ICourseOfferingRepository _courseOfferingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteCourseOfferingCommandHandler> _logger;

        public DeleteCourseOfferingCommandHandler(
            ICourseOfferingRepository courseOfferingRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DeleteCourseOfferingCommandHandler> logger)
        {
            _courseOfferingRepository = courseOfferingRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteCourseOfferingCommand request, CancellationToken cancellationToken)
        {
            var offering = await _courseOfferingRepository.GetByIdAsync(request.Id, cancellationToken);
            if (offering == null)
                throw new NotFoundException("CourseOffering", request.Id);

            // Soft delete - mark as inactive
            offering.IsActive = false;
            offering.Status = SMS.Domain.Enums.CourseOfferingStatus.Cancelled;
            await _courseOfferingRepository.UpdateAsync(offering, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("CourseOffering", "Delete", offering.Id.ToString());

            _logger.LogInformation("Course offering deleted (soft): {OfferingCode}", offering.OfferingCode);

            return true;
        }
    }
}
