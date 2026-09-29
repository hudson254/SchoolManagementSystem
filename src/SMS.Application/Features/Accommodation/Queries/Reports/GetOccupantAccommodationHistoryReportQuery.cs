using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

using ICurrentUserService = SMS.Domain.Interfaces.ICurrentUserService;

namespace SMS.Application.Features.Accommodation.Queries.Reports
{
    /// <summary>
    /// G. Occupant Accommodation History — search for an occupant (student or
    /// lecturer) then inspect their complete accommodation history.
    ///
    /// Search mode: a search term (name, student number or employee number) is
    /// required. Detail mode: <see cref="OccupantId"/> is supplied and the
    /// occupant type is taken from <see cref="AccommodationReportQueryBase.OccupantType"/>,
    /// defaulting to Student when omitted.
    /// </summary>
    public class GetOccupantAccommodationHistoryReportQuery : AccommodationReportQueryBase,
        IRequest<OccupantAccommodationHistoryReportDto>
    {
        /// <summary>Selected occupant; when supplied the report returns their history.</summary>
        public Guid? OccupantId { get; set; }
    }

    public class GetOccupantAccommodationHistoryReportHandler
        : IRequestHandler<GetOccupantAccommodationHistoryReportQuery, OccupantAccommodationHistoryReportDto>
    {
        private readonly IAccommodationReportRepository _reportRepository;
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<GetOccupantAccommodationHistoryReportHandler> _logger;

        public GetOccupantAccommodationHistoryReportHandler(
            IAccommodationReportRepository reportRepository,
            IAccommodationRepository accommodationRepository,
            ISemesterRepository semesterRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ILogger<GetOccupantAccommodationHistoryReportHandler> logger)
        {
            _reportRepository = reportRepository;
            _accommodationRepository = accommodationRepository;
            _semesterRepository = semesterRepository;
            _auditService = auditService;
            _currentUser = currentUserService;
            _logger = logger;
        }

        public async Task<OccupantAccommodationHistoryReportDto> Handle(
            GetOccupantAccommodationHistoryReportQuery request,
            CancellationToken cancellationToken)
        {
            var filters = request.ToFilters();
            var generatedBy = AccommodationReportSupport.ResolveGeneratedBy(_currentUser);
            var labels = await AccommodationReportSupport.ResolveFilterLabelsAsync(
                _accommodationRepository, _semesterRepository, filters, cancellationToken);

            var isDetail = request.OccupantId.HasValue && request.OccupantId.Value != Guid.Empty;
            var dto = isDetail
                ? await BuildDetailAsync(request, filters, cancellationToken)
                : await BuildSearchAsync(request, filters, cancellationToken);

            AccommodationReportSupport.ApplyMeta(
                dto,
                "occupant-history",
                isDetail
                    ? $"Occupant Accommodation History — {dto.SelectedOccupant?.OccupantName}"
                    : "Occupant Accommodation History — Search",
                filters, generatedBy, DateTime.UtcNow, labels);

            await AccommodationReportSupport.AuditAsync(_auditService, "occupant-history", filters, generatedBy);
            return dto;
        }

        private async Task<OccupantAccommodationHistoryReportDto> BuildSearchAsync(
            GetOccupantAccommodationHistoryReportQuery request,
            Domain.Reporting.AccommodationReportFilters filters,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SearchTerm))
                throw new ValidationException("Enter a student number, employee number or name to search for an occupant.");

            var candidates = await _reportRepository.SearchOccupantsAsync(filters, cancellationToken);

            _logger.LogInformation(
                "Occupant history search '{Term}' returned {Count} matches", request.SearchTerm, candidates.Count);

            return new OccupantAccommodationHistoryReportDto
            {
                Mode = "Search",
                Candidates = candidates,
                Pagination = AccommodationReportSupport.BuildPagination(filters, candidates.Count)
            };
        }

        private async Task<OccupantAccommodationHistoryReportDto> BuildDetailAsync(
            GetOccupantAccommodationHistoryReportQuery request,
            Domain.Reporting.AccommodationReportFilters filters,
            CancellationToken cancellationToken)
        {
            var occupantId = request.OccupantId!.Value;
            var occupantType = request.OccupantType ?? OccupantType.Student;

            var occupant = await _reportRepository.GetOccupantAsync(occupantId, occupantType, cancellationToken);
            if (occupant == null)
                throw new NotFoundException("Occupant", occupantId);

            var stays = await _reportRepository.GetOccupantStaysAsync(occupantId, occupantType, cancellationToken);

            _logger.LogInformation(
                "Occupant history generated for {Occupant} ({Type}): {Count} stays",
                occupant.OccupantName, occupantType, stays.Count);

            return new OccupantAccommodationHistoryReportDto
            {
                Mode = "Detail",
                SelectedOccupant = occupant,
                Stays = stays,
                CurrentHouse = occupant.IsCurrent && !string.IsNullOrWhiteSpace(occupant.CurrentHouseNumber)
                    ? occupant.CurrentHouseNumber!
                    : string.Empty,
                Pagination = AccommodationReportSupport.BuildPagination(filters, stays.Count)
            };
        }
    }
}
