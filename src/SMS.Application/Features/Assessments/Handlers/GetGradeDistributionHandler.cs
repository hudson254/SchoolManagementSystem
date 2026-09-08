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
    /// Grade distribution report for a unit, computed exclusively from the
    /// authoritative UnitResult data persisted by the centralized engine.
    /// </summary>
    public class GetGradeDistributionHandler : IRequestHandler<GetGradeDistributionQuery, SMS.Application.Features.Reporting.DTOs.GradeDistributionReportDto>
    {
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ICertificateRuleRepository _certificateRuleRepository;

        public GetGradeDistributionHandler(
            IUnitResultRepository unitResultRepository,
            IUnitRepository unitRepository,
            ICertificateRuleRepository certificateRuleRepository)
        {
            _unitResultRepository = unitResultRepository;
            _unitRepository = unitRepository;
            _certificateRuleRepository = certificateRuleRepository;
        }

        public async Task<SMS.Application.Features.Reporting.DTOs.GradeDistributionReportDto> Handle(GetGradeDistributionQuery request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null || unit.IsDeleted)
                throw new NotFoundException("Unit", request.UnitId);

            var results = (await _unitResultRepository.GetByUnitAsync(request.UnitId, cancellationToken))
                .Where(r => !r.IsDeleted)
                .ToList();

            var rule = await _certificateRuleRepository.GetActiveRuleAsync(cancellationToken);
            var minPass = rule?.MinimumPassingPercentage ?? 50m;

            var passed = 0;
            decimal sum = 0m;
            decimal highest = decimal.MinValue;
            decimal lowest = decimal.MaxValue;

            foreach (var r in results)
            {
                if (r.FinalPercentage >= minPass)
                    passed++;
                sum += r.FinalPercentage;
                if (r.FinalPercentage > highest)
                    highest = r.FinalPercentage;
                if (r.FinalPercentage < lowest)
                    lowest = r.FinalPercentage;
            }

            var distribution = results.GroupBy(r => r.GradeLetter)
                .ToDictionary(g => g.Key, g => g.Count());

            var count = results.Count;
            return new SMS.Application.Features.Reporting.DTOs.GradeDistributionReportDto
            {
                UnitId = request.UnitId,
                UnitName = unit.Name,
                TotalStudents = count,
                GradeDistribution = distribution,
                GradeCounts = distribution,
                AverageScore = count > 0 ? Math.Round(sum / count, 2) : 0m,
                HighestScore = count > 0 ? highest : 0m,
                LowestScore = count > 0 ? lowest : 0m,
                PassRate = count > 0 ? Math.Round((passed * 100m) / count, 2) : 0m,
                FailRate = count > 0 ? Math.Round(((count - passed) * 100m) / count, 2) : 0m
            };
        }
    }
}