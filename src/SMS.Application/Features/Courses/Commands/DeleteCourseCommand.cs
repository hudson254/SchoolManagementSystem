using FluentValidation;
using SMS.Shared.DTOs;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Courses.Commands
{
    public class DeleteCourseCommand : IRequest
    {
        public Guid CourseId { get; set; }
    }

    public class DeleteCourseCommandHandler : IRequestHandler<DeleteCourseCommand>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteCourseCommandHandler> _logger;

        public DeleteCourseCommandHandler(
            ICourseRepository courseRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DeleteCourseCommandHandler> logger)
        {
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
        {
            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null)
            {
                throw new NotFoundException("Course", request.CourseId);
            }

            // Check if course has any active units
            var hasActiveUnits = await _courseRepository.HasActiveUnitsAsync(request.CourseId, cancellationToken);
            if (hasActiveUnits)
            {
                throw new BusinessRuleException(
                    "Cannot delete course",
                    "Course has active units. Please remove all units before deleting the course.");
            }

            await _auditService.LogActivityAsync("Course", "Delete", course.Id.ToString(), request.CourseId.ToString());
            await _courseRepository.DeleteAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("Course", "Delete", course.Id.ToString(), "Delete-Course");

            _logger.LogInformation("Course deleted: {CourseCode}", course.Code);
        }
    }
}





