using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using SMS.Shared.DTOs;

namespace SMS.Application.Features.Units.Queries
{
    public class GetUnitQuery : IRequest<UnitDetailsDto>
    {
        public Guid Id { get; set; }
    }

    public class GetUnitQueryHandler : IRequestHandler<GetUnitQuery, UnitDetailsDto>
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<GetUnitQueryHandler> _logger;

        public GetUnitQueryHandler(IUnitRepository unitRepository, ILogger<GetUnitQueryHandler> logger)
        {
            _unitRepository = unitRepository;
            _logger = logger;
        }

        public async Task<UnitDetailsDto> Handle(GetUnitQuery request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetUnitWithDetailsAsync(request.Id, cancellationToken);
            if (unit == null)
            {
                throw new NotFoundException("Unit", request.Id);
            }

            return new UnitDetailsDto
            {
                Id = unit.Id,
                Name = unit.Name,
                Code = unit.Code,
                Description = unit.Description,
                Credits = unit.Credits,
                ContactHours = unit.ContactHours,
                IsActive = unit.IsActive,
                CourseId = unit.CourseId,
                CourseName = unit.Course?.Name,
                CourseCode = unit.Course?.Code,
                Semester = unit.Semester,
                SemesterName = unit.Semester.ToString(),
                PrerequisiteUnitId = unit.PrerequisiteUnitId,
                PrerequisiteCode = unit.Prerequisite?.Code,
                PrerequisiteName = unit.Prerequisite?.Name,
                LearningOutcomes = unit.LearningOutcomes,
                AssessmentMethods = unit.AssessmentMethods,
                RecommendedTextbooks = unit.RecommendedTextbooks,
                CreatedDate = unit.CreatedAt,
                UpdatedDate = unit.UpdatedAt
            };
        }
    }
}
