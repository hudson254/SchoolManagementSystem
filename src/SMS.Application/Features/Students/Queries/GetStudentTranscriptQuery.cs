using MediatR;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

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

            var allGrades = await _gradeRepository.GetStudentGradesAsync(request.StudentId, null, null, cancellationToken);

            var semesterGroups = allGrades
                .Where(g => g.GradeValue != null)
                .GroupBy(g => new { g.Enrollment.Semester.Id, g.Enrollment.Semester.Name, g.Enrollment.Semester.SemesterNumber });

            var semesterTranscripts = semesterGroups.Select(g => new SemesterTranscriptDto
            {
                SemesterName = g.Key.Name,
                SemesterNumber = g.Key.SemesterNumber,
                Credits = g.Sum(x => x.Enrollment.Unit.Credits),
                GPA = CalculateGPA(g),
                Grades = g.Select(x => new GradeSummaryDto
                {
                    Id = x.Id,
                    UnitId = x.Enrollment.UnitId,
                    UnitName = x.Enrollment.Unit.Name,
                    UnitCode = x.Enrollment.Unit.Code,
                    Credits = x.Enrollment.Unit.Credits,
                    Grade = x.GradeValue,
                    Score = x.Score,
                    SemesterId = x.Enrollment.SemesterId,
                    SemesterName = x.Enrollment.Semester.Name
                }).ToList()
            }).ToList();

            var allGradeSummaries = allGrades
                .Where(g => g.GradeValue != null)
                .Select(g => new GradeSummaryDto
                {
                    Id = g.Id,
                    UnitId = g.Enrollment.UnitId,
                    UnitName = g.Enrollment.Unit.Name,
                    UnitCode = g.Enrollment.Unit.Code,
                    Credits = g.Enrollment.Unit.Credits,
                    Grade = g.GradeValue,
                    Score = g.Score,
                    SemesterId = g.Enrollment.SemesterId,
                    SemesterName = g.Enrollment.Semester.Name
                }).ToList();

            return new TranscriptDto
            {
                StudentId = student.Id,
                StudentName = student.User.FullName,
                StudentNumber = student.StudentNumber,
                ProgrammeName = student.Programme?.Name ?? "Not Enrolled",
                TotalCreditsEarned = allGrades
                    .Where(g => g.GradeValue != null && g.GradeValue != "F")
                    .Sum(g => g.Enrollment.Unit.Credits),
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
                g.Enrollment.Unit.Credits *
                Domain.Common.DomainConstants.GradeValues.GradePoints.GetValueOrDefault(g.GradeValue, 0));

            var totalCredits = gradedGrades.Sum(g => g.Enrollment.Unit.Credits);

            return totalCredits > 0 ? totalPoints / totalCredits : 0;
        }

        private decimal CalculateCumulativeGPA(IEnumerable<Domain.Entities.Grade> grades)
        {
            var gradedGrades = grades.Where(g => g.GradeValue != null).ToList();
            if (!gradedGrades.Any()) return 0;

            var totalPoints = gradedGrades.Sum(g =>
                g.Enrollment.Unit.Credits *
                Domain.Common.DomainConstants.GradeValues.GradePoints.GetValueOrDefault(g.GradeValue, 0));

            var totalCredits = gradedGrades.Sum(g => g.Enrollment.Unit.Credits);

            return totalCredits > 0 ? totalPoints / totalCredits : 0;
        }
    }
}