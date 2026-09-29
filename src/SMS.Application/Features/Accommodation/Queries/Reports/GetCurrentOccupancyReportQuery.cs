using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Domain.Reporting;
using System;
using System.Threading;
using System.Threading.Tasks;

using ICurrentUserService = SMS.Domain.Interfaces.ICurrentUserService;

namespace SMS.Application.Features.Accommodation.Queries.Reports
{
    /// <summary>
    /// Shared executor for the current / occupied / empty house reports. The
    /// three reports differ only by report key, title and house scope.
    /// </summary>
    public abstract class HouseOccupancyReportHandlerBase<TQuery>
        where TQuery : AccommodationReportQueryBase
    {
        protected readonly IAccommodationReportRepository ReportRepository;
        protected readonly IAccommodationRepository AccommodationRepository;
        protected readonly ISemesterRepository SemesterRepository;
        protected readonly IAuditService AuditService;
        protected readonly ICurrentUserService CurrentUser;

        protected HouseOccupancyReportHandlerBase(
            IAccommodationReportRepository reportRepository,
            IAccommodationRepository accommodationRepository,
            ISemesterRepository semesterRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService)
        {
            ReportRepository = reportRepository;
            AccommodationRepository = accommodationRepository;
            SemesterRepository = semesterRepository;
            AuditService = auditService;
            CurrentUser = currentUserService;
        }

        protected abstract string ReportKey { get; }
        protected abstract string ReportTitle { get; }
        protected abstract HouseOccupancyScope Scope { get; }

        protected async Task<AccommodationHouseOccupancyReportDto> ExecuteAsync(TQuery request, CancellationToken cancellationToken)
        {
            request.ValidatePeriod();
            var filters = request.ToFilters();

            var (page, summary) = await ReportRepository.GetHouseOccupancyReportAsync(filters, Scope, cancellationToken);
            var labels = await AccommodationReportSupport.ResolveFilterLabelsAsync(
                AccommodationRepository, SemesterRepository, filters, cancellationToken);

            var generatedBy = AccommodationReportSupport.ResolveGeneratedBy(CurrentUser);
            var dto = new AccommodationHouseOccupancyReportDto
            {
                Summary = summary,
                Rows = page.Items,
                Pagination = AccommodationReportSupport.BuildPagination(filters, page.TotalCount)
            };

            AccommodationReportSupport.ApplyMeta(
                dto, ReportKey, ReportTitle, filters, generatedBy, DateTime.UtcNow, labels);

            await AccommodationReportSupport.AuditAsync(AuditService, ReportKey, filters, generatedBy);
            return dto;
        }
    }

    /// <summary>A. Current House Occupancy report — all houses with current occupants.</summary>
    public class GetCurrentOccupancyReportQuery : AccommodationReportQueryBase, IRequest<AccommodationHouseOccupancyReportDto>
    {
    }

    public class GetCurrentOccupancyReportHandler : HouseOccupancyReportHandlerBase<GetCurrentOccupancyReportQuery>,
        IRequestHandler<GetCurrentOccupancyReportQuery, AccommodationHouseOccupancyReportDto>
    {
        public GetCurrentOccupancyReportHandler(
            IAccommodationReportRepository reportRepository,
            IAccommodationRepository accommodationRepository,
            ISemesterRepository semesterRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService)
            : base(reportRepository, accommodationRepository, semesterRepository, auditService, currentUserService)
        {
        }

        protected override string ReportKey => "current-occupancy";
        protected override string ReportTitle => "Current House Occupancy";
        protected override HouseOccupancyScope Scope => HouseOccupancyScope.All;

        public Task<AccommodationHouseOccupancyReportDto> Handle(GetCurrentOccupancyReportQuery request, CancellationToken cancellationToken)
            => ExecuteAsync(request, cancellationToken);
    }
}
