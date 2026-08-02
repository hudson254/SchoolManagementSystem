using MediatR;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Dashboard.Queries
{
    public class GetEnrollmentTrendsQuery : IRequest<EnrollmentTrendsDto>
    {
        public int? AcademicYearId { get; set; }
    }

    public class GetEnrollmentTrendsQueryHandler : IRequestHandler<GetEnrollmentTrendsQuery, EnrollmentTrendsDto>
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ILogger<GetEnrollmentTrendsQueryHandler> _logger;

        public GetEnrollmentTrendsQueryHandler(
            IEnrollmentRepository enrollmentRepository,
            ILogger<GetEnrollmentTrendsQueryHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _logger = logger;
        }

        public async Task<EnrollmentTrendsDto> Handle(GetEnrollmentTrendsQuery request, CancellationToken cancellationToken)
        {
            var enrollments = await _enrollmentRepository.GetEnrollmentsByYearAsync(request.AcademicYearId, cancellationToken);

            // Monthly enrollment data
            var monthlyData = new List<MonthlyEnrollmentDto>();
            var last12Months = Enumerable.Range(0, 12)
                .Select(i => DateTime.UtcNow.AddMonths(-i))
                .Reverse()
                .ToList();

            int cumulative = 0;
            foreach (var month in last12Months)
            {
                var count = enrollments.Count(e =>
                    e.EnrollmentDate.Year == month.Year &&
                    e.EnrollmentDate.Month == month.Month);

                cumulative += count;
                monthlyData.Add(new MonthlyEnrollmentDto
                {
                    Month = month.ToString("MMM"),
                    Year = month.Year,
                    Count = count,
                    Cumulative = cumulative
                });
            }

            // Programme distribution
            var programmeDistribution = enrollments
                .GroupBy(e => e.Unit?.Course?.Name ?? "Unknown")
                .Select(g => new ProgrammeEnrollmentDto
                {
                    ProgrammeName = g.Key,
                    Count = g.Count(),
                    Percentage = enrollments.Any() ? (decimal)g.Count() / enrollments.Count * 100 : 0
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Gender distribution (from students)
            var genderDistribution = enrollments
                .Select(e => e.Student)
                .Where(s => s != null)
                .GroupBy(s => s.Gender ?? "Not Specified")
                .Select(g => new GenderDistributionDto
                {
                    Gender = g.Key,
                    Count = g.Count(),
                    Percentage = enrollments.Any() ? (decimal)g.Count() / enrollments.Count * 100 : 0
                })
                .ToList();

            return new EnrollmentTrendsDto
            {
                EnrollmentData = monthlyData,
                ProgrammeDistribution = programmeDistribution,
                GenderDistribution = genderDistribution
            };
        }
    }
}