using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    /// <summary>
    /// Query to get a list of all lecturers with their accommodation assignments.
    /// </summary>
    public class GetLecturerAccommodationListQuery : IRequest<IEnumerable<LecturerAccommodationDto>>
    {
        public Guid? LaneId { get; set; }
        public Guid? SemesterId { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class GetLecturerAccommodationListHandler : IRequestHandler<GetLecturerAccommodationListQuery, IEnumerable<LecturerAccommodationDto>>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetLecturerAccommodationListHandler> _logger;

        public GetLecturerAccommodationListHandler(
            IAccommodationRepository repository,
            ILogger<GetLecturerAccommodationListHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<LecturerAccommodationDto>> Handle(GetLecturerAccommodationListQuery request, CancellationToken cancellationToken)
        {
            var assignments = await _repository.GetAssignmentsWithDetailsAsync(cancellationToken);

            var query = assignments.Where(a => a.OccupantType == SMS.Domain.Enums.OccupantType.Lecturer).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower();
                query = query.Where(a =>
                    (a.Lecturer?.FirstName ?? "").ToLower().Contains(search) ||
                    (a.Lecturer?.LastName ?? "").ToLower().Contains(search) ||
                    (a.Lecturer?.EmployeeNumber ?? "").ToLower().Contains(search));
            }

            var results = query.Select(a => new LecturerAccommodationDto
            {
                LecturerId = a.LecturerId ?? Guid.Empty,
                LecturerName = a.Lecturer != null ? BuildDisplayName(a.Lecturer) : "Unknown",
                EmployeeNumber = a.Lecturer?.EmployeeNumber ?? "N/A",
                HouseId = a.HouseId,
                HouseNumber = a.House?.HouseNumber,
                LaneName = a.Lane?.LaneName ?? a.House?.Lane?.LaneName,
                AssignmentStatus = a.Status,
                AssignedDate = a.AssignedDate,
                MoveInDate = a.MoveInDate,
                MoveOutDate = a.MoveOutDate,
                Remarks = a.Remarks
            });

            _logger.LogInformation("Lecturer accommodation list generated with {Count} entries", results.Count());
            return results;
        }

        private static string BuildDisplayName(SMS.Domain.Entities.Lecturer lecturer)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(lecturer.Title))
                parts.Add(lecturer.Title);
            parts.Add(lecturer.FirstName);
            if (!string.IsNullOrWhiteSpace(lecturer.MiddleName))
                parts.Add(lecturer.MiddleName);
            parts.Add(lecturer.LastName);
            return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        }
    }
}
