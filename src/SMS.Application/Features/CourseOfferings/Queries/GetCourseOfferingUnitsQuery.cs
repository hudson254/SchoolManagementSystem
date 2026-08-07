using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Queries
{
    public class GetCourseOfferingUnitsQuery : IRequest<IEnumerable<CourseOfferingUnitDto>>
    {
        public Guid CourseOfferingId { get; set; }
    }

    public class GetCourseOfferingUnitsQueryHandler
        : IRequestHandler<GetCourseOfferingUnitsQuery, IEnumerable<CourseOfferingUnitDto>>
    {
        private readonly ICourseOfferingUnitRepository _courseOfferingUnitRepository;
        private readonly ILogger<GetCourseOfferingUnitsQueryHandler> _logger;

        public GetCourseOfferingUnitsQueryHandler(
            ICourseOfferingUnitRepository courseOfferingUnitRepository,
            ILogger<GetCourseOfferingUnitsQueryHandler> logger)
        {
            _courseOfferingUnitRepository = courseOfferingUnitRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<CourseOfferingUnitDto>> Handle(
            GetCourseOfferingUnitsQuery request,
            CancellationToken cancellationToken)
        {
            var units = await _courseOfferingUnitRepository.GetByOfferingIdAsync(
                request.CourseOfferingId, cancellationToken);

            return units.Select(u => new CourseOfferingUnitDto
            {
                Id = u.Id,
                CourseOfferingId = u.CourseOfferingId,
                UnitId = u.UnitId,
                Name = u.Name,
                Code = u.Code,
                Description = u.Description,
                Credits = u.Credits,
                ContactHours = u.ContactHours,
                Order = u.Order,
                LearningOutcomes = u.LearningOutcomes,
                AssessmentMethods = u.AssessmentMethods,
                AssessmentWeighting = u.AssessmentWeighting,
                IsActive = u.IsActive
            }).OrderBy(u => u.Order).ToList();
        }
    }
}
