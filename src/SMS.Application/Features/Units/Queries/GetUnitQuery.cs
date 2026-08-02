using MediatR;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Units.Queries
{
    public class GetUnitQuery : IRequest<UnitDetailsDto>
    {
        public Guid UnitId { get; set; }
    }

    public class GetUnitQueryHandler : IRequestHandler<GetUnitQuery, UnitDetailsDto>
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<GetUnitQueryHandler> _logger;

        public GetUnitQueryHandler(
            IUnitRepository unitRepository,
            ILogger<GetUnitQueryHandler> logger)
        {
            _unitRepository = unitRepository;
            _logger = logger;
        }

        public async Task<UnitDetailsDto> Handle(GetUnitQuery request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetUnitWithDetailsAsync(request.UnitId, cancellationToken);

            if (unit == null)
            {
                throw new NotFoundException("Unit", request.UnitId);
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
                CourseName = unit.Course.Name,
                CourseCode = unit.Course.Code,
                PrerequisiteUnitId = unit.PrerequisiteUnitId,
                PrerequisiteCode = unit.Prerequisite?.Code,
                PrerequisiteName = unit.Prerequisite?.Name,
                LearningOutcomes = unit.LearningOutcomes,
                AssessmentMethods = unit.AssessmentMethods,
                RecommendedTextbooks = unit.RecommendedTextbooks,
                TotalEnrollments = unit.Enrollments.Count,
                TotalAllocations = unit.Allocations.Count,
                TotalAssignments = unit.Assignments.Count,
                TotalLectureNotes = unit.LectureNotes.Count,
                AllocatedLecturers = unit.Allocations
                    .Where(a => a.Lecturer != null)
                    .Select(a => new LecturerSummaryDto
                    {
                        Id = a.Lecturer.Id,
                        FullName = a.Lecturer.User.FullName,
                        EmployeeNumber = a.Lecturer.EmployeeNumber,
                        Specialization = a.Lecturer.Specialization,
                        IsPrimary = a.IsPrimary
                    }).ToList(),
                CreatedDate = unit.CreatedDate
            };
        }
    }
}