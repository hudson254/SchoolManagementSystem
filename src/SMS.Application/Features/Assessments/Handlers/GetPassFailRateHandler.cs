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
    /// Pass / fail rate report for a unit with a per-assessment breakdown.
    /// Uses authoritative UnitResult and mark data; pass thresholds come from
    /// the configured certificate rule.
    /// </summary>
    public class GetPassFailRateHandler : IRequestHandler<GetPassFailRateQuery, SMS.Application.Features.Reporting.DTOs.PassFailRateReportDto>
    {
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IStudentAssessmentMarkRepository _markRepository;
        private readonly ICertificateRuleRepository _certificateRuleRepository;

        public GetPassFailRateHandler(
            IUnitResultRepository unitResultRepository,
            IUnitRepository unitRepository,
            IAssessmentRepository assessmentRepository,
            IStudentAssessmentMarkRepository markRepository,
            ICertificateRuleRepository certificateRuleRepository)
        {
            _unitResultRepository = unitResultRepository;
            _unitRepository = unitRepository;
            _assessmentRepository = assessmentRepository;
            _markRepository = markRepository;
            _certificateRuleRepository = certificateRuleRepository;
        }

        public async Task<SMS.Application.Features.Reporting.DTOs.PassFailRateReportDto> Handle(GetPassFailRateQuery request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null || unit.IsDeleted)
                throw new NotFoundException("Unit", request.UnitId);

            var rule = await _certificateRuleRepository.GetActiveRuleAsync(cancellationToken);
            var minPass = rule?.MinimumPassingPercentage ?? 50m;

            var results = (await _unitResultRepository.GetByUnitAsync(request.UnitId, cancellationToken))
                .Where(r => !r.IsDeleted)
                .ToList();

            var passed = results.Count(r => r.FinalPercentage >= minPass);
            var count = results.Count;

            var breakdown = new List<SMS.Application.Features.Reporting.DTOs.AssessmentPassFailDto>();
            var assessments = (await _assessmentRepository.GetByUnitAsync(request.UnitId, cancellationToken))
                .Where(a => !a.IsDeleted)
                .ToList();

            foreach (var assessment in assessments)
            {
                var marks = (await _markRepository.GetByAssessmentAsync(assessment.Id, cancellationToken))
                    .Where(m => !m.IsDraft)
                    .ToList();
                var assessed = marks.Count(m => m.Percentage >= minPass);
                var total = marks.Count;

                breakdown.Add(new SMS.Application.Features.Reporting.DTOs.AssessmentPassFailDto
                {
                    AssessmentId = assessment.Id,
                    AssessmentName = assessment.Title,
                    Total = total,
                    Passed = assessed,
                    Failed = total - assessed,
                    PassRate = total > 0 ? Math.Round((assessed * 100m) / total, 2) : 0m
                });
            }

            return new SMS.Application.Features.Reporting.DTOs.PassFailRateReportDto
            {
                UnitId = request.UnitId,
                UnitName = unit.Name,
                TotalStudents = count,
                Passed = passed,
                Failed = count - passed,
                PassRatePercentage = count > 0 ? Math.Round((passed * 100m) / count, 2) : 0m,
                FailRatePercentage = count > 0 ? Math.Round(((count - passed) * 100m) / count, 2) : 0m,
                AssessmentBreakdown = breakdown
            };
        }
    }
}