using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;

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
            var totalStudents = await _studentRepository.CountStudentsAsync(cancellationToken);
            var totalLecturers = await _lecturerRepository.CountLecturersAsync(cancellationToken);
            var totalRooms = await _accommodationRepository.GetAllRoomsAsync(cancellationToken);
            var occupiedRooms = 0;

            var allGrades = await _gradeRepository.GetAllGradesAsync(cancellationToken);
            var averageGPA = allGrades.Any() ? (decimal)(allGrades.Average(g => (double?)g.Score ?? 0) / 20.0) : 0.0m;

            var occupancyRate = totalRooms.Count() > 0 ? (decimal)occupiedRooms / totalRooms.Count() * 100 : 0;

            var studentsByProgramme = new Dictionary<string, int>();
            var gradesDistribution = new Dictionary<string, int>();
            var monthlyEnrollments = new List<MonthlyEnrollmentDto>();

            return new DashboardStatisticsDto
            {
                TotalStudents = totalStudents,
                TotalLecturers = totalLecturers,
                ActiveCourses = 0,
                PendingAssignments = 0,
                TotalEnrollments = 0,
                TotalGrades = 0,
                TotalAssignments = 0,
                TotalRooms = totalRooms.Count(),
                OccupiedRooms = occupiedRooms,
                PendingVerifications = 0,
                RecentActivities = 0,
                AverageGPA = averageGPA,
                OccupancyRate = (decimal)occupancyRate,
                StudentsByProgramme = studentsByProgramme,
                GradesDistribution = gradesDistribution,
                MonthlyEnrollments = monthlyEnrollments
            };
        }
    }
}
