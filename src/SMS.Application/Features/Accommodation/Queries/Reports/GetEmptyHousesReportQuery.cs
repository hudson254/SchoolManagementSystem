using MediatR;
using SMS.Application.DTOs;
using SMS.Domain.Enums;
using SMS.Domain.Reporting;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

using ICurrentUserService = SMS.Domain.Interfaces.ICurrentUserService;

namespace SMS.Application.Features.Accommodation.Queries.Reports
{
    /// <summary>
    /// C. Empty Houses report — houses with no active occupants, enriched with
    /// the last occupant and last occupancy end date from history.
    /// </summary>
    public class GetEmptyHousesReportQuery : AccommodationReportQueryBase, IRequest<AccommodationHouseOccupancyReportDto>
    {
    }

    public class GetEmptyHousesReportHandler : HouseOccupancyReportHandlerBase<GetEmptyHousesReportQuery>,
        IRequestHandler<GetEmptyHousesReportQuery, AccommodationHouseOccupancyReportDto>
    {
        public GetEmptyHousesReportHandler(
            IAccommodationReportRepository reportRepository,
            IAccommodationRepository accommodationRepository,
            ISemesterRepository semesterRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService)
            : base(reportRepository, accommodationRepository, semesterRepository, auditService, currentUserService)
        {
        }

        protected override string ReportKey => "empty-houses";
        protected override string ReportTitle => "Empty Houses";
        protected override HouseOccupancyScope Scope => HouseOccupancyScope.Empty;

        public Task<AccommodationHouseOccupancyReportDto> Handle(GetEmptyHousesReportQuery request, CancellationToken cancellationToken)
            => ExecuteAsync(request, cancellationToken);
    }
}
