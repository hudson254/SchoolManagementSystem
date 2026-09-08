using Microsoft.Extensions.Logging;
using MediatR;
using SMS.Application.DTOs;

using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Students.Queries
{
    public class GetStudentTranscriptQuery : IRequest<TranscriptDto>
    {
        public Guid StudentId { get; set; }
    }

    /// <summary>
    /// Transcript handler backed by the authoritative grading engine. Reads
    /// PUBLISHED UnitResults, derives grade points from the grading-scale snapshot
    /// persisted on each result (never hard-coded switches( and uses the configured
    /// certificate rule for pass/credit decisions. Draft / pending / approved-but-
    /// unpublished results are excluded. No legacy Grades-table reads occur here.
    /// </summary>
    public class GetStudentTranscriptQueryHandler : IRequestHandler<GetStudentTranscriptQuery, TranscriptDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IGradingScaleRepository _gradingScaleRepository;
        private readonly ICertificateRuleRepository _certificateRuleRepository;
        private readonly ILogger<GetStudentTranscriptQueryHandler> _logger;

        public GetStudentTranscriptQueryHandler(
            IStudentRepository studentRepository,
            IUnitResultRepository unitResultRepository,
            IUnitRepository unitRepository,
            IGradingScaleRepository gradingScaleRepository,
            ICertificateRuleRepository certificateRuleRepository,
            ILogger<GetStudentTranscriptQueryHandler> logger)
        {
            _studentRepository = studentRepository;
            _unitResultRepository = unitResultRepository;
            _unitRepository = unitRepository;
            _gradingScaleRepository = gradingScaleRepository;
            _certificateRuleRepository = certificateRuleRepository;
            _logger = logger;
        }

        public async Task<TranscriptDto> Handle(
            GetStudentTranscriptQuery request,
            CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetStudentWithDetailsAsync(request.StudentId, cancellationToken);
            if (student == null)
                throw new NotFoundException("Student", request.StudentId);

            var results = (await _unitResultRepository.GetPublishedByStudentAsync(request.StudentId, cancellationToken))
                .Where(r => !r.IsDeleted)
                .ToList();

            _logger.LogInformation("Building transcript for student {StudentId} from {Count} published unit results", request.StudentId, results.Count);

            var unitMap = new Dictionary<Guid, SMS.Domain.Entities.Unit>();
            foreach (var unitId in results.Select(r => r.UnitId).Distinct())
            {
                var unit = await _unitRepository.GetByIdAsync(unitId, cancellationToken);
                if (unit != null && !unit.IsDeleted)
                    unitMap[unitId] = unit;
            }

            var rule = await _certificateRuleRepository.GetActiveRuleAsync(cancellationToken);
            var minPass = rule?.MinimumPassingPercentage ?? 50m;

            var scale = await _gradingScaleRepository.GetActiveVersionAsync(cancellationToken);
            var bandPoints = new Dictionary<string, decimal>();
            if (scale?.Bands != null)
            {
                foreach (var band in scale.Bands.Where(b => !string.IsNullOrWhiteSpace(b.GradeLetter)))
                {
                    bandPoints[band.GradeLetter.Trim().ToUpperInvariant()] = (band.GpaPoints ?? 0m);
 }
            }


            var summaries = new List<GradeSummaryDto>();
            var pointsByResultId = new Dictionary<Guid, decimal>();
            foreach (var r in results)
            {
                var summary = BuildSummary(r, unitMap);
                summaries.Add(summary);
                var letter = (r.GradeLetter ?? string.Empty).Trim().ToUpperInvariant();
                var points = r.GpaPoints ?? (bandPoints.TryGetValue(letter, out var bp) ? bp : 0m);
                pointsByResultId[r.Id] = points;

            }
            var pointsBySummary = summaries.ToDictionary(s => s.Id, s => pointsByResultId[s.Id]);

            var semesterGroups = summaries
                .GroupBy(g => g.SemesterId ?? Guid.Empty)

                .Select(g => new SemesterTranscriptDto
                {
                    SemesterName = string.IsNullOrWhiteSpace(g.First().SemesterName) ? "No Semester" : g.First().SemesterName!,
                    SemesterNumber = g.First().SemesterId.HasValue ? g.First().SemesterId.Value.GetHashCode() : 0,
                    Credits = g.Sum(x => x.Credits),
                    GPA = CalculateGpa(g.ToList(), pointsBySummary, minPass),
                    Grades = g.ToList(),
                }).ToList();

            semesterGroups = semesterGroups
                .OrderBy(sg => sg.Grades.Min(x => x.CreatedDate))
                .ToList();

            var totalCreditsEarned = summaries.Where(s => s.Score >= minPass).Sum(s => s.Credits);
            var cumulativeGpa = CalculateGpa(summaries, pointsBySummary, minPass);

            return new TranscriptDto
            {
                StudentId = student.Id,
                StudentName = student.User.FullName,
                StudentNumber = student.StudentNumber,
                ProgrammeName = student.Programme?.Name ?? "Not Enrolled",
                TotalCreditsEarned = totalCreditsEarned,
                CumulativeGPA = cumulativeGpa,
                SemesterGPA = semesterGroups.Any() ? semesterGroups.Last().GPA : 0m,
                Semesters = semesterGroups,
                AllGrades = summaries
            };
        }

        private static GradeSummaryDto BuildSummary(UnitResult r, IReadOnlyDictionary<Guid, SMS.Domain.Entities.Unit> unitMap)
        {
            var unit = r.Unit ?? (unitMap.TryGetValue(r.UnitId, out var u) ? u : null);
            return new GradeSummaryDto
            {
                Id = r.Id,
                StudentId = r.StudentId,
                UnitId = r.UnitId,
                UnitName = unit?.Name ?? string.Empty,
                UnitCode = unit?.Code ?? string.Empty,
                Score = r.FinalPercentage,
                LetterGrade = r.GradeLetter,
                Grade = r.GradeLetter,
                Credits = unit?.Credits ?? 0,
                Remarks = string.IsNullOrWhiteSpace(r.GradeDescription) ? null : r.GradeDescription,
                SemesterId = r.SemesterId,
                SemesterName = r.Semester?.Name ?? string.Empty,
                CreatedDate = r.CreatedDate ?? r.CreatedAt
            };
        }

        /// <summary>
        /// GPA = sum(points x credits(/ sum(credits( over passing results, using
        /// the grading-band points snapshot persisted on each authoritative result.

        /// </summary>
        private static decimal CalculateGpa(IReadOnlyCollection<GradeSummaryDto> grades, IReadOnlyDictionary<Guid, decimal> pointsBySummary, decimal minPass)
        {


            var graded = grades.Where(g => g.Score >= minPass).ToList();
            var totalCredits = graded.Sum(g => g.Credits);
            if (totalCredits == 0) return 0m;
            decimal totalPoints = graded.Sum(g => (pointsBySummary.TryGetValue(g.Id, out var p) ? p : 0m) * g.Credits);
            return Math.Round(totalPoints / totalCredits, 2);
        }
    }
}
