using MediatR;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Dashboard.Queries
{
    public class GetDashboardStatisticsQuery : IRequest<DashboardStatisticsDto>
    {
    }

    public class GetDashboardStatisticsQueryHandler : IRequestHandler<GetDashboardStatisticsQuery, DashboardStatisticsDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IGradeRepository _gradeRepository;
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ILogger<GetDashboardStatisticsQueryHandler> _logger;

        public GetDashboardStatisticsQueryHandler(
            IStudentRepository studentRepository,
            ILecturerRepository lecturerRepository,
            ICourseRepository courseRepository,
            IAssignmentRepository assignmentRepository,
            IGradeRepository gradeRepository,
            IAccommodationRepository accommodationRepository,
            IEnrollmentRepository enrollmentRepository,
            ILogger<GetDashboardStatisticsQueryHandler> logger)
        {
            _studentRepository = studentRepository;
            _lecturerRepository = lecturerRepository;
            _courseRepository = courseRepository;
            _assignmentRepository = assignmentRepository;
            _gradeRepository = gradeRepository;
            _accommodationRepository = accommodationRepository;
            _enrollmentRepository = enrollmentRepository;
            _logger = logger;
        }

        public async Task<DashboardStatisticsDto> Handle(GetDashboardStatisticsQuery request, CancellationToken cancellationToken)
        {
            var totalStudents = await _studentRepository.CountStudentsAsync(null, null, null, null, cancellationToken);
            var activeStudents = await _studentRepository.CountStudentsAsync(null, "Active", null, true, cancellationToken);
            var totalLecturers = await _lecturerRepository.CountLecturersAsync(null, null, null, cancellationToken);
            var activeCourses = await _courseRepository.CountCoursesAsync(null, null, true, cancellationToken);
            var pendingAssignments = await _assignmentRepository.CountAssignmentsAsync(null, null, null, null, "Published", null, cancellationToken);

            var totalEnrollments = await _enrollmentRepository.CountEnrollmentsAsync(cancellationToken);
            var totalGrades = await _gradeRepository.CountGradesAsync(cancellationToken);
            var totalAssignments = await _assignmentRepository.CountAllAsync(cancellationToken);

            var rooms = await _accommodationRepository.GetAllRoomsAsync(cancellationToken);
            var totalRooms = rooms.Count();
            var occupiedRooms = rooms.Count(r => r.IsOccupied);

            var pendingVerifications = await _lecturerRepository.CountLecturersAsync(null, false, null, cancellationToken);

            var allGrades = await _gradeRepository.GetAllGradesAsync(cancellationToken);
            var averageGPA = allGrades.Any()
                ? allGrades
                    .Where(g => g.GradeValue != null)
                    .Average(g => Domain.Common.DomainConstants.GradeValues.GradePoints.GetValueOrDefault(g.GradeValue, 0))
                : 0;

            var occupancyRate = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms * 100 : 0;

            // Students by programme
            var studentsByProgramme = new Dictionary<string, int>();
            var programmes = await _enrollmentRepository.GetProgrammeEnrollmentCountsAsync(cancellationToken);
            foreach (var p in programmes)
            {
                studentsByProgramme[p.ProgrammeName] = p.Count;
            }

            // Grades distribution
            var gradesDistribution = new Dictionary<string, int>();
            var gradeGroups = allGrades
                .Where(g => g.GradeValue != null)
                .GroupBy(g => g.GradeValue)
                .Select(g => new { Grade = g.Key, Count = g.Count() });

            foreach (var g in gradeGroups)
            {
                gradesDistribution[g.Grade] = g.Count;
            }

            // Monthly enrollments (last 12 months)
            var monthlyEnrollments = new List<MonthlyEnrollmentDto>();
            var enrollments = await _enrollmentRepository.GetEnrollmentsAsync(cancellationToken);
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
                monthlyEnrollments.Add(new MonthlyEnrollmentDto
                {
                    Month = month.ToString("MMM"),
                    Year = month.Year,
                    Count = count,
                    Cumulative = cumulative
                });
            }

            return new DashboardStatisticsDto
            {
                TotalStudents = totalStudents,
                TotalLecturers = totalLecturers,
                ActiveCourses = activeCourses,
                PendingAssignments = pendingAssignments,
                TotalEnrollments = totalEnrollments,
                TotalGrades = totalGrades,
                TotalAssignments = totalAssignments,
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                PendingVerifications = pendingVerifications,
                RecentActivities = 0,
                AverageGPA = averageGPA,
                OccupancyRate = occupancyRate,
                StudentsByProgramme = studentsByProgramme,
                GradesDistribution = gradesDistribution,
                MonthlyEnrollments = monthlyEnrollments
            };
        }
    }
}