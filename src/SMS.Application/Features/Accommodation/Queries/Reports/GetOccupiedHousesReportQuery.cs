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
    /// <summary>B. Occupied Houses report — only houses with active occupants.</summary>
    public class GetOccupiedHousesReportQuery : AccommodationReportQueryBase, IRequest<AccommodationHouseOccupancyReportDto>
    {
    }

    public class GetOccupiedHousesReportHandler : HouseOccupancyReportHandlerBase<GetOccupiedHousesReportQuery>,
        IRequestHandler<GetOccupiedHousesReportQuery, AccommodationHouseOccupancyReportDto>
    {
        public GetOccupiedHousesReportHandler(
            IAccommodationReportRepository reportRepository,
            IAccommodationRepository accommodationRepository,
            ISemesterRepository semesterRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService)
            : base(reportRepository, accommodationRepository, semesterRepository, auditService, currentUserService)
        {
        }

        protected override string ReportKey => "occupied-houses";
        protected override string ReportTitle => "Occupied Houses";
        protected override HouseOccupancyScope Scope => HouseOccupancyScope.Occupied;

        public Task<AccommodationHouseOccupancyReportDto> Handle(GetOccupiedHousesReportQuery request, CancellationToken cancellationToken)
            => ExecuteAsync(request, cancellationToken);
    }
}
