using FluentValidation;
using SMS.Shared.DTOs;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Students.Queries
{
    public class GetStudentTranscriptQuery : IRequest<TranscriptDto>
    {
        public Guid StudentId { get; set; }
    }

    public class GetStudentTranscriptQueryHandler : IRequestHandler<GetStudentTranscriptQuery, TranscriptDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IGradeRepository _gradeRepository;
        private readonly ILogger<GetStudentTranscriptQueryHandler> _logger;

        public GetStudentTranscriptQueryHandler(
            IStudentRepository studentRepository,
            IGradeRepository gradeRepository,
            ILogger<GetStudentTranscriptQueryHandler> logger)
        {
            _studentRepository = studentRepository;
            _gradeRepository = gradeRepository;
            _logger = logger;
        }

        public async Task<TranscriptDto> Handle(GetStudentTranscriptQuery request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetStudentWithDetailsAsync(request.StudentId, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException("Student", request.StudentId);
            }

            var allGrades = await _gradeRepository.GetStudentGradesAsync(request.StudentId);

            var semesterGroups = allGrades
                .Where(g => g.GradeValue != null)
            .GroupBy(g => new { g.Semester?.Id, g.Semester?.Name, g.Semester?.SemesterNumber });

            var semesterTranscripts = semesterGroups.Select(g => new SemesterTranscriptDto
            {
                SemesterName = g.Key.Name,
                SemesterNumber = g.Key.SemesterNumber ?? 0,
                Credits = g.Sum(x => x.Unit?.Credits ?? 0),
                GPA = CalculateGPA(g),
                Grades = g.Select(x => new GradeSummaryDto
                {
                    Id = x.Id,
                    UnitId = x.UnitId,
                    UnitName = x.Unit?.Name,
                    UnitCode = x.Unit?.Code,
                    Credits = x.Enrollment?.Unit?.Credits ?? x.Unit?.Credits ?? 0,
                    Grade = x.GradeValue,
                    Score = x.Score,
                    SemesterId = x.SemesterId ?? Guid.Empty,
                    SemesterName = x.Semester?.Name
                }).ToList()
            }).ToList();

            var allGradeSummaries = allGrades
                .Where(g => g.GradeValue != null)
                .Select(g => new GradeSummaryDto
                {
                    Id = g.Id,
                    UnitId = g.Enrollment != null ? (Guid?)(g.Enrollment.UnitId ?? g.UnitId) ?? Guid.Empty : (Guid?)g.UnitId ?? Guid.Empty,
                    UnitName = (g.Enrollment?.Unit?.Name) ?? g.Unit?.Name ?? "",
                    UnitCode = (g.Enrollment?.Unit?.Code) ?? g.Unit?.Code ?? "",
                    Credits = (g.Enrollment?.Unit?.Credits) ?? g.Unit?.Credits ?? 0,
                    Grade = g.GradeValue,
                    Score = g.Score,
                    SemesterId = g.Enrollment.SemesterId ?? Guid.Empty,
                    SemesterName = g.Enrollment.Semester?.Name ?? g.Semester?.Name ?? ""
                }).ToList();

            return new TranscriptDto
            {
                StudentId = student.Id,
                StudentName = student.User.FullName,
                StudentNumber = student.StudentNumber,
                ProgrammeName = student.Programme?.Name ?? "Not Enrolled",
                TotalCreditsEarned = allGrades
                    .Where(g => g.GradeValue != null && g.GradeValue != "F")
.Sum(g => g.Enrollment?.Unit?.Credits ?? g.Unit?.Credits ?? 0),
                CumulativeGPA = CalculateCumulativeGPA(allGrades),
                SemesterGPA = semesterTranscripts.Any() ? semesterTranscripts.Last().GPA : 0,
                Semesters = semesterTranscripts,
                AllGrades = allGradeSummaries
            };
        }

        private decimal CalculateGPA(IEnumerable<Domain.Entities.Grade> grades)
        {
            var gradedGrades = grades.Where(g => g.GradeValue != null).ToList();
            if (!gradedGrades.Any()) return 0;

            var totalPoints = gradedGrades.Sum(g =>
                (g.Unit?.Credits ?? (g.Enrollment?.Unit?.Credits ?? 0)) *
                GetGradePoints(g.GradeValue));

            var totalCredits = gradedGrades.Sum(g => g.Unit?.Credits ?? (g.Enrollment?.Unit?.Credits ?? 0));

            return totalCredits > 0 ? totalPoints / totalCredits : 0;
        }

        private decimal CalculateCumulativeGPA(IEnumerable<Domain.Entities.Grade> grades)
        {
            var gradedGrades = grades.Where(g => g.GradeValue != null).ToList();
            if (!gradedGrades.Any()) return 0;

            var totalPoints = gradedGrades.Sum(g =>
                (g.Unit?.Credits ?? (g.Enrollment?.Unit?.Credits ?? 0)) *
                GetGradePoints(g.GradeValue));

            var totalCredits = gradedGrades.Sum(g => g.Unit?.Credits ?? (g.Enrollment?.Unit?.Credits ?? 0));

            return totalCredits > 0 ? totalPoints / totalCredits : 0;
        }

        private static int GetGradePoints(string? gradeValue)
        {
            return gradeValue switch
            {
                "A" => 12,
                "A-" => 11,
                "B+" => 10,
                "B" => 9,
                "B-" => 8,
                "C+" => 7,
                "C" => 6,
                "C-" => 5,
                "D+" => 4,
                "D" => 3,
                "D-" => 2,
                "E" => 1,
                "F" => 0,
                _ => 0
            };
        }
    }
}




