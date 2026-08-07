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
        public Guid? AcademicYearId { get; set; }
        public Guid? SemesterId { get; set; }
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

            if (request.AcademicYearId.HasValue)
            {
                var byYear = await _courseOfferingRepository.GetByAcademicYearAsync(request.AcademicYearId.Value, cancellationToken);
                return byYear.Select(MapToDto).ToList();
            }

            if (request.SemesterId.HasValue)
            {
                var bySemester = await _courseOfferingRepository.GetBySemesterAsync(request.SemesterId.Value, cancellationToken);
                return bySemester.Select(MapToDto).ToList();
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
                AcademicYearId = o.AcademicYearId,
                AcademicYearName = o.AcademicYear?.Name,
                SemesterId = o.SemesterId,
                SemesterName = o.Semester?.Name,
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
