using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Queries
{
    public class GetCourseOfferingsQuery : IRequest<IEnumerable<CourseOfferingDto>>
    {
        public Guid? CourseId { get; set; }
        public string? AcademicYearName { get; set; }
        public string? SemesterName { get; set; }
        public string? SearchTerm { get; set; }
        public bool IncludeInactive { get; set; }
    }

    public class GetCourseOfferingsQueryHandler
        : IRequestHandler<GetCourseOfferingsQuery, IEnumerable<CourseOfferingDto>>
    {
        private readonly ICourseOfferingRepository _courseOfferingRepository;
        private readonly ILogger<GetCourseOfferingsQueryHandler> _logger;

        public GetCourseOfferingsQueryHandler(
            ICourseOfferingRepository courseOfferingRepository,
            ILogger<GetCourseOfferingsQueryHandler> logger)
        {
            _courseOfferingRepository = courseOfferingRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<CourseOfferingDto>> Handle(
            GetCourseOfferingsQuery request,
            CancellationToken cancellationToken)
        {
            // Fetch all offerings with details (repository already includes Course, AcademicYear, Semester)
            var offerings = await _courseOfferingRepository.GetWithDetailsAsync(
                Guid.Empty, cancellationToken);

            // If a specific ID was requested, return only that one
            if (request.CourseId.HasValue)
            {
                var byCourse = await _courseOfferingRepository.GetByCourseIdAsync(request.CourseId.Value, cancellationToken);
                return byCourse.Select(MapToDto).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.AcademicYearName))
            {
                var allByYear = await _courseOfferingRepository.GetAllAsync(cancellationToken);
                return allByYear.Where(o => o.AcademicYearName == request.AcademicYearName).Select(MapToDto).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.SemesterName))
            {
                var allBySemester = await _courseOfferingRepository.GetAllAsync(cancellationToken);
                return allBySemester.Where(o => o.SemesterName == request.SemesterName).Select(MapToDto).ToList();
            }

            // Fallback: return all from the repository (already filtered by tenant & soft-delete)
            var all = await _courseOfferingRepository.GetAllAsync(cancellationToken);

            var query = all.AsQueryable();

            if (!request.IncludeInactive)
                query = query.Where(o => o.IsActive);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(o =>
                    o.OfferingCode.ToLower().Contains(term) ||
                    (o.Course != null && o.Course.Name.ToLower().Contains(term)) ||
                    (o.Course != null && o.Course.Code.ToLower().Contains(term)));
            }

            return query.Select(MapToDto).ToList();
        }

        private static CourseOfferingDto MapToDto(SMS.Domain.Entities.CourseOffering o)
        {
            return new CourseOfferingDto
            {
                Id = o.Id,
                OfferingCode = o.OfferingCode,
                CourseId = o.CourseId,
                CourseName = o.Course?.Name,
                CourseCode = o.Course?.Code,
                AcademicYearName = o.AcademicYearName,
                SemesterName = o.SemesterName,
                Intake = o.Intake,
                StartDate = o.StartDate,
                EndDate = o.EndDate,
                RegistrationStartDate = o.RegistrationStartDate,
                RegistrationEndDate = o.RegistrationEndDate,
                Status = o.Status,
                IsActive = o.IsActive,
                Notes = o.Notes,
                TotalUnits = o.Units?.Count ?? 0,
                TotalEnrollments = o.Enrollments?.Count ?? 0,
                TotalLecturers = o.Lecturers?.Count ?? 0,
                CreatedDate = o.CreatedDate ?? DateTime.UtcNow
            };
        }
    }
}
