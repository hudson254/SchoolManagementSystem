using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Reports.Queries
{
    public class GetStudentEnrollmentReportQuery : IRequest<StudentReportDto>
    {
        public Guid? StudentId { get; set; }
        public Guid? CourseId { get; set; }
        public Guid? SemesterId { get; set; }
        public Guid? ProgrammeId { get; set; }
    }

    public class GetStudentEnrollmentReportHandler : IRequestHandler<GetStudentEnrollmentReportQuery, StudentReportDto>
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ILogger<GetStudentEnrollmentReportHandler> _logger;

        public GetStudentEnrollmentReportHandler(
            IEnrollmentRepository enrollmentRepository,
            ILogger<GetStudentEnrollmentReportHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _logger = logger;
        }

        public async Task<StudentReportDto> Handle(GetStudentEnrollmentReportQuery request, CancellationToken cancellationToken)
        {
            var enrollments = await _enrollmentRepository.GetEnrollmentsAsync(cancellationToken);

            if (request.StudentId.HasValue)
                enrollments = enrollments.Where(e => e.StudentId == request.StudentId.Value);
            if (request.CourseId.HasValue)
                enrollments = enrollments.Where(e => e.CourseId == request.CourseId.Value);
            if (request.SemesterId.HasValue)
                enrollments = enrollments.Where(e => e.SemesterId == request.SemesterId.Value);

            var enrollmentList = enrollments.ToList();

            var report = new StudentReportDto
            {
                TotalStudents = enrollmentList.Count,
                ActiveStudents = enrollmentList.Count(e => e.Status == "Active"),
                Enrollments = enrollmentList.Select(e => new StudentEnrollmentReportDto
                {
                    StudentName = e.Student != null ? $"{e.Student.FirstName} {e.Student.LastName}" : "Unknown",
                    StudentNumber = e.Student?.StudentNumber ?? "",
                    ProgrammeName = e.Course?.Programme?.Name ?? "",
                    Status = e.Status
                }).ToList()
            };

            _logger.LogInformation("Generated enrollment report with {Count} records", report.TotalStudents);
            return report;
        }
    }

    public class GetLecturerWorkloadReportQuery : IRequest<IEnumerable<LecturerWorkloadReportDto>>
    {
        public Guid? LecturerId { get; set; }
        public Guid SemesterId { get; set; }
    }

    public class GetLecturerWorkloadReportHandler : IRequestHandler<GetLecturerWorkloadReportQuery, IEnumerable<LecturerWorkloadReportDto>>
    {
        private readonly IUnitAllocationRepository _unitAllocationRepository;
        private readonly ILogger<GetLecturerWorkloadReportHandler> _logger;

        public GetLecturerWorkloadReportHandler(
            IUnitAllocationRepository unitAllocationRepository,
            ILogger<GetLecturerWorkloadReportHandler> logger)
        {
            _unitAllocationRepository = unitAllocationRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<LecturerWorkloadReportDto>> Handle(GetLecturerWorkloadReportQuery request, CancellationToken cancellationToken)
        {
            var allocations = await _unitAllocationRepository.GetBySemesterAsync(request.SemesterId);

            if (request.LecturerId.HasValue)
                allocations = allocations.Where(a => a.LecturerId == request.LecturerId.Value);

            var workload = allocations
                .GroupBy(a => a.Lecturer)
                .Select(g => new LecturerWorkloadReportDto
                {
                    LecturerName = g.Key != null ? $"{g.Key.FirstName} {g.Key.LastName}" : "Unknown",
                    TotalUnits = g.Count(),
                    TotalStudents = g.Sum(a => a.Unit?.Enrollments?.Count ?? 0),
                    Units = g.Select(a => new UnitWorkloadDto
                    {
                        UnitName = a.Unit?.Name ?? "",
                        UnitCode = a.Unit?.Code ?? "",
                        StudentCount = a.Unit?.Enrollments?.Count ?? 0
                    }).ToList()
                })
                .ToList();

            _logger.LogInformation("Generated lecturer workload report with {Count} lecturers", workload.Count);
            return workload;
        }
    }

    public class GetCourseStatisticsReportQuery : IRequest<IEnumerable<CourseStatisticsDto>>
    {
        public Guid? CourseId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    public class GetCourseStatisticsReportHandler : IRequestHandler<GetCourseStatisticsReportQuery, IEnumerable<CourseStatisticsDto>>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ILogger<GetCourseStatisticsReportHandler> _logger;

        public GetCourseStatisticsReportHandler(
            ICourseRepository courseRepository,
            ILogger<GetCourseStatisticsReportHandler> logger)
        {
            _courseRepository = courseRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<CourseStatisticsDto>> Handle(GetCourseStatisticsReportQuery request, CancellationToken cancellationToken)
        {
            var courses = await _courseRepository.GetAllAsync();

            if (request.CourseId.HasValue)
                courses = courses.Where(c => c.Id == request.CourseId.Value);
            if (request.SemesterId.HasValue)
                courses = courses.Where(c => c.SemesterId == request.SemesterId.Value);

            var statistics = courses.Select(c => new CourseStatisticsDto
            {
                CourseName = c.Name,
                CourseCode = c.Code,
                TotalStudents = c.Enrollments?.Count ?? 0,
                TotalUnits = c.Units?.Count ?? 0
            }).ToList();

            _logger.LogInformation("Generated course statistics report with {Count} courses", statistics.Count);
            return statistics;
        }
    }

    public class GetAssignmentCompletionReportQuery : IRequest<IEnumerable<AssignmentCompletionReportDto>>
    {
        public Guid AssignmentId { get; set; }
    }

    public class GetAssignmentCompletionReportHandler : IRequestHandler<GetAssignmentCompletionReportQuery, IEnumerable<AssignmentCompletionReportDto>>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILogger<GetAssignmentCompletionReportHandler> _logger;

        public GetAssignmentCompletionReportHandler(
            IAssignmentRepository assignmentRepository,
            ILogger<GetAssignmentCompletionReportHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<AssignmentCompletionReportDto>> Handle(GetAssignmentCompletionReportQuery request, CancellationToken cancellationToken)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId);
            if (assignment == null)
                throw new NotFoundException("Assignment", request.AssignmentId);

            var submissions = await _assignmentRepository.GetSubmissionsAsync(request.AssignmentId, cancellationToken);
            var submissionList = submissions.ToList();

            var submitted = submissionList.Count;
            var graded = submissionList.Count(s => s.Score.HasValue);
            var averageScore = graded > 0 ? (double)submissionList.Where(s => s.Score.HasValue).Average(s => s.Score!.Value) : 0.0;

            var report = new List<AssignmentCompletionReportDto>
            {
                new AssignmentCompletionReportDto
                {
                    TotalAssignments = 1,
                    Submitted = submitted,
                    Graded = graded,
                    AverageScore = Math.Round(averageScore, 2),
                    CompletionRate = 0
                }
            };

            _logger.LogInformation("Generated assignment completion report for {AssignmentId}", request.AssignmentId);
            return report;
        }
    }

    public class GetGradeDistributionReportQuery : IRequest<GradeDistributionReportDto>
    {
        public Guid? SemesterId { get; set; }
        public Guid? UnitId { get; set; }
    }

    public class GetGradeDistributionReportHandler : IRequestHandler<GetGradeDistributionReportQuery, GradeDistributionReportDto>
    {
        private readonly IGradeRepository _gradeRepository;
        private readonly ILogger<GetGradeDistributionReportHandler> _logger;

        public GetGradeDistributionReportHandler(
            IGradeRepository gradeRepository,
            ILogger<GetGradeDistributionReportHandler> logger)
        {
            _gradeRepository = gradeRepository;
            _logger = logger;
        }

        public async Task<GradeDistributionReportDto> Handle(GetGradeDistributionReportQuery request, CancellationToken cancellationToken)
        {
            var allGrades = await _gradeRepository.GetAllGradesAsync(cancellationToken);

            var grades = allGrades.AsQueryable();
            if (request.SemesterId.HasValue)
                grades = grades.Where(g => g.SemesterId == request.SemesterId.Value);
            if (request.UnitId.HasValue)
                grades = grades.Where(g => g.UnitId == request.UnitId.Value);

            var gradeList = grades.ToList();
            var distribution = new Dictionary<string, int>();
            foreach (var grade in gradeList)
            {
                var letterGrade = !string.IsNullOrEmpty(grade.LetterGrade) ? grade.LetterGrade : "N/A";
                if (distribution.ContainsKey(letterGrade))
                    distribution[letterGrade]++;
                else
                    distribution[letterGrade] = 1;
            }

            var passed = gradeList.Count(g => g.Score >= 40);

            var report = new GradeDistributionReportDto
            {
                TotalStudents = gradeList.Select(g => g.StudentId).Distinct().Count(),
                GradeDistribution = distribution,
                AverageGPA = gradeList.Any() ? Math.Round((double)gradeList.Average(g => (double)g.Score), 2) : 0,
                PassRate = gradeList.Any() ? Math.Round((double)passed / gradeList.Count * 100, 2) : 0
            };

            _logger.LogInformation("Generated grade distribution report with {Count} grades", gradeList.Count);
            return report;
        }
    }

    public class GetUserActivityReportQuery : IRequest<UserActivityReportDto>
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class GetUserActivityReportHandler : IRequestHandler<GetUserActivityReportQuery, UserActivityReportDto>
    {
        private readonly ILoginHistoryRepository _loginHistoryRepository;
        private readonly ILogger<GetUserActivityReportHandler> _logger;

        public GetUserActivityReportHandler(
            ILoginHistoryRepository loginHistoryRepository,
            ILogger<GetUserActivityReportHandler> logger)
        {
            _loginHistoryRepository = loginHistoryRepository;
            _logger = logger;
        }

        public async Task<UserActivityReportDto> Handle(GetUserActivityReportQuery request, CancellationToken cancellationToken)
        {
            var logs = await _loginHistoryRepository.GetAllAsync();

            if (request.FromDate.HasValue)
                logs = logs.Where(l => l.LoginTime >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                logs = logs.Where(l => l.LoginTime <= request.ToDate.Value);

            var logList = logs.ToList();
            var loginsByDay = logList
                .GroupBy(l => l.LoginTime.Date)
                .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

            var report = new UserActivityReportDto
            {
                TotalUsers = logList.Select(l => l.UserId).Distinct().Count(),
                ActiveUsers = logList.Select(l => l.UserId).Distinct().Count(),
                NewRegistrations = 0,
                LoginsByDay = loginsByDay
            };

            _logger.LogInformation("Generated user activity report with {Count} login records", logList.Count);
            return report;
        }
    }

    public class GetTimetableUtilizationReportQuery : IRequest<TimetableUtilizationReportDto>
    {
        public Guid SemesterId { get; set; }
    }

    public class GetTimetableUtilizationReportHandler : IRequestHandler<GetTimetableUtilizationReportQuery, TimetableUtilizationReportDto>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly ILogger<GetTimetableUtilizationReportHandler> _logger;

        public GetTimetableUtilizationReportHandler(
            ITimetableRepository timetableRepository,
            ILogger<GetTimetableUtilizationReportHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _logger = logger;
        }

        public async Task<TimetableUtilizationReportDto> Handle(GetTimetableUtilizationReportQuery request, CancellationToken cancellationToken)
        {
            var timetables = await _timetableRepository.GetTimetableByDateRangeAsync(
                DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow, cancellationToken);
            var timetableList = timetables.ToList();

            var usageByRoom = timetableList
                .Where(t => t.Room != null && !string.IsNullOrEmpty(t.Room.RoomNumber))
                .GroupBy(t => t.Room!.RoomNumber)
                .ToDictionary(g => g.Key, g => g.Count());

            var report = new TimetableUtilizationReportDto
            {
                TotalSlots = timetableList.Count,
                UsedSlots = timetableList.Count(t => t.Room != null),
                UtilizationRate = timetableList.Count > 0
                    ? Math.Round((double)timetableList.Count(t => t.Room != null) / timetableList.Count * 100, 2)
                    : 0,
                UsageByRoom = usageByRoom
            };

            _logger.LogInformation("Generated timetable utilization report with {Count} slots", timetableList.Count);
            return report;
        }
    }

    public class GetVacantRoomsReportQuery : IRequest<VacantRoomsReportDto>
    {
        public Guid? BuildingId { get; set; }
    }

    public class GetVacantRoomsReportHandler : IRequestHandler<GetVacantRoomsReportQuery, VacantRoomsReportDto>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ILogger<GetVacantRoomsReportHandler> _logger;

        public GetVacantRoomsReportHandler(
            IAccommodationRepository accommodationRepository,
            ILogger<GetVacantRoomsReportHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _logger = logger;
        }

        public async Task<VacantRoomsReportDto> Handle(GetVacantRoomsReportQuery request, CancellationToken cancellationToken)
        {
            var rooms = await _accommodationRepository.GetAllRoomsAsync(cancellationToken);

            if (request.BuildingId.HasValue)
                rooms = rooms.Where(r => r.BlockId == request.BuildingId.Value);

            var roomList = rooms.ToList();
            var vacantRooms = roomList.Where(r => r.IsAvailable || r.Status == "Vacant").ToList();

            var report = new VacantRoomsReportDto
            {
                TotalVacant = vacantRooms.Count,
                VacantRooms = vacantRooms.Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    RoomType = r.RoomType ?? "",
                    Capacity = r.Capacity,
                    BuildingName = r.Block?.Building ?? ""
                }).ToList()
            };

            _logger.LogInformation("Generated vacant rooms report with {Count} vacant rooms", vacantRooms.Count);
            return report;
        }
    }

    public class ExportReportQuery : IRequest<ReportFileResult>
    {
        public string Format { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public Guid? SemesterId { get; set; }
        public Dictionary<string, string>? Parameters { get; set; }
    }

    public class ExportReportHandler : IRequestHandler<ExportReportQuery, ReportFileResult>
    {
        private readonly ILogger<ExportReportHandler> _logger;

        public ExportReportHandler(ILogger<ExportReportHandler> logger)
        {
            _logger = logger;
        }

        public async Task<ReportFileResult> Handle(ExportReportQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Exporting report: {ReportType} in {Format} format", request.ReportType, request.Format);

            var reportData = request.ReportType switch
            {
                "StudentEnrollment" => $"Student Enrollment Report (Semester: {request.SemesterId})",
                "LecturerWorkload" => $"Lecturer Workload Report (Semester: {request.SemesterId})",
                "CourseStatistics" => $"Course Statistics Report (Semester: {request.SemesterId})",
                "GradeDistribution" => $"Grade Distribution Report (Semester: {request.SemesterId})",
                "UserActivity" => $"User Activity Report",
                "TimetableUtilization" => $"Timetable Utilization Report (Semester: {request.SemesterId})",
                _ => $"Report: {request.ReportType}"
            };

            var fileContent = System.Text.Encoding.UTF8.GetBytes(reportData);
            var fileName = $"{request.ReportType}_{DateTime.UtcNow:yyyyMMddHHmmss}";

            return request.Format.ToUpper() switch
            {
                "PDF" => new ReportFileResult
                {
                    FileContent = fileContent,
                    FileName = $"{fileName}.pdf",
                    ContentType = "application/pdf"
                },
                "EXCEL" => new ReportFileResult
                {
                    FileContent = fileContent,
                    FileName = $"{fileName}.xlsx",
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                },
                "CSV" => new ReportFileResult
                {
                    FileContent = fileContent,
                    FileName = $"{fileName}.csv",
                    ContentType = "text/csv"
                },
                _ => new ReportFileResult
                {
                    FileContent = fileContent,
                    FileName = $"{fileName}.txt",
                    ContentType = "text/plain"
                }
            };
        }
    }
}
