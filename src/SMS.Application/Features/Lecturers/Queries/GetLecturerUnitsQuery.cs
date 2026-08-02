using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Lecturers.Queries
{
    public class GetLecturerUnitsQuery : IRequest<IEnumerable<UnitDto>>
    {
        public Guid LecturerId { get; set; }
    }

    public class GetLecturerUnitsQueryHandler : IRequestHandler<GetLecturerUnitsQuery, IEnumerable<UnitDto>>
    {
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<GetLecturerUnitsQueryHandler> _logger;

        public GetLecturerUnitsQueryHandler(
            ILecturerRepository lecturerRepository,
            IUnitRepository unitRepository,
            ILogger<GetLecturerUnitsQueryHandler> logger)
        {
            _lecturerRepository = lecturerRepository;
            _unitRepository = unitRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<UnitDto>> Handle(GetLecturerUnitsQuery request, CancellationToken cancellationToken)
        {
            var lecturer = await _lecturerRepository.GetByIdAsync(request.LecturerId, cancellationToken);

            if (lecturer == null)
                throw new NotFoundException("Lecturer", request.LecturerId);

            // Get all units for the lecturer's department
            var allUnits = await _unitRepository.GetAllAsync(cancellationToken);

            return allUnits
                .Where(u => u.Course != null && u.Course.DepartmentId == lecturer.DepartmentId)
                .Select(u => new UnitDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Code = u.Code,
                    Description = u.Description ?? string.Empty,
                    Credits = u.Credits,
                    ContactHours = u.ContactHours,
                    CourseId = u.CourseId,
                    CourseName = u.Course?.Name ?? string.Empty,
                    CourseCode = u.Course?.Code ?? string.Empty,
                    Semester = u.Semester,
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate ?? DateTime.UtcNow
                }).ToList();
        }
    }
}

