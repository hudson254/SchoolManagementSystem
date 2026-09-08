using MediatR;
using SMS.Application.Exceptions;
using SMS.Application.Features.Reporting.Queries;
using SMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Assessment summary report: per-assessment completion, average, highest and
    /// lowest scores, together with overall completion for the unit.
    /// </summary>
    public class GetAssessmentSummaryHandler : IRequestHandler<GetAssessmentSummaryQuery, SMS.Application.Features.Reporting.DTOs.AssessmentSummaryReportDto>
    {
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IStudentAssessmentMarkRepository _markRepository;

        public GetAssessmentSummaryHandler(
            IUnitResultRepository unitResultRepository,
            IUnitRepository unitRepository,
            IAssessmentRepository assessmentRepository,
            IStudentAssessmentMarkRepository markRepository)
        {
            _unitResultRepository = unitResultRepository;
            _unitRepository = unitRepository;
            _assessmentRepository = assessmentRepository;
            _markRepository = markRepository;
        }

        public async Task<SMS.Application.Features.Reporting.DTOs.AssessmentSummaryReportDto> Handle(GetAssessmentSummaryQuery request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null || unit.IsDeleted)
                throw new NotFoundException("Unit", request.UnitId);

            var assessments = (await _assessmentRepository.GetByUnitAsync(request.UnitId, cancellationToken))
                .Where(a => !a.IsDeleted)
                .ToList();

            var resultCount = (await _unitResultRepository.GetByUnitAsync(request.UnitId, cancellationToken))
                .Where(r => !r.IsDeleted)
                .Count();

            var summaries = new List<SMS.Application.Features.Reporting.DTOs.AssessmentSummaryDto>();
            decimal overallAvg = 0m;
            var assessedStudents = new HashSet<Guid>();
            var completedAssessments = 0;

            foreach (var assessment in assessments)
            {
                var marks = (await _markRepository.GetByAssessmentAsync(assessment.Id, cancellationToken))
                    .Where(m => !m.IsDraft)
                    .ToList();

                foreach (var mark in marks)
                    assessedStudents.Add(mark.StudentId);

                var total = marks.Count;
                if (total > 0)
                    completedAssessments++;

                decimal sum = 0m;
                decimal highest = decimal.MinValue;
                decimal lowest = decimal.MaxValue;
                foreach (var mark in marks)
                {
                    var percentage = mark.Percentage > 0m ? mark.Percentage : (mark.Mark / assessment.MaxScore) * 100m;
                    sum += percentage;
                    if (percentage > highest)
                        highest = percentage;
                    if (percentage < lowest)
                        lowest = percentage;
                }

                var divisor = resultCount > 0 ? resultCount : assessedStudents.Count;
                summaries.Add(new SMS.Application.Features.Reporting.DTOs.AssessmentSummaryDto
                {
                    AssessmentId = assessment.Id,
                    AssessmentName = assessment.Title,
                    AssessmentTypeName = assessment.AssessmentType?.Name ?? string.Empty,
                    Weight = assessment.Weight,
                    TotalStudents = divisor,
                    GradedStudents = total,
                    AverageScore = total > 0 ? Math.Round(sum / total, 2) : 0m,
                    HighestScore = total > 0 ? highest : 0m,
                    LowestScore = total > 0 ? lowest : 0m,
                    CompletionRate = divisor > 0 ? Math.Round((total * 100m) / divisor, 2) : 0m
                });

                if (total > 0)
                    overallAvg += Math.Round(sum / total, 2);
            }

            overallAvg = summaries.Count > 0 ? Math.Round(overallAvg / summaries.Count, 2) : 0m;

            return new SMS.Application.Features.Reporting.DTOs.AssessmentSummaryReportDto
            {
                UnitId = request.UnitId,
                UnitName = unit.Name,
                Assessments = summaries,
                OverallAverage = overallAvg,
                TotalAssessments = assessments.Count,
                CompletedAssessments = completedAssessments,
                CompletionRate = assessments.Count > 0 ? Math.Round((completedAssessments * 100m) / assessments.Count, 2) : 0m
            };
        }
    }
}