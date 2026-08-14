using MediatR;
using SMS.Application.Features.Reporting.DTOs;

namespace SMS.Application.Features.Reporting.Queries
{
    public class GetGradeDistributionQuery : IRequest<SMS.Application.Features.Reporting.DTOs.GradeDistributionReportDto> { public Guid UnitId { get; set; } }
    public class GetPassFailRateQuery : IRequest<PassFailRateReportDto> { public Guid UnitId { get; set; } }
    public class GetAssessmentSummaryQuery : IRequest<AssessmentSummaryReportDto> { public Guid UnitId { get; set; } }
}


