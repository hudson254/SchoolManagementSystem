using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    /// <summary>
    /// Query to get a list of all students with their accommodation assignments.
    /// </summary>
    public class GetStudentAccommodationListQuery : IRequest<IEnumerable<StudentAccommodationDto>>
    {
        public Guid? LaneId { get; set; }
        public Guid? SemesterId { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class GetStudentAccommodationListHandler : IRequestHandler<GetStudentAccommodationListQuery, IEnumerable<StudentAccommodationDto>>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetStudentAccommodationListHandler> _logger;

        public GetStudentAccommodationListHandler(
            IAccommodationRepository repository,
            ILogger<GetStudentAccommodationListHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<StudentAccommodationDto>> Handle(GetStudentAccommodationListQuery request, CancellationToken cancellationToken)
        {
            var assignments = await _repository.GetAssignmentsWithDetailsAsync(cancellationToken);

            var query = assignments.Where(a => a.OccupantType == OccupantType.Student).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower();
                query = query.Where(a =>
                    (a.Student?.FirstName ?? "").ToLower().Contains(search) ||
                    (a.Student?.LastName ?? "").ToLower().Contains(search) ||
                    (a.Student?.StudentNumber ?? "").ToLower().Contains(search));
            }

            var results = query.Select(a => new StudentAccommodationDto
            {
                StudentId = a.StudentId ?? Guid.Empty,
                StudentName = a.Student != null ? $"{a.Student.FirstName} {a.Student.LastName}" : "Unknown",
                StudentNumber = a.Student?.StudentNumber ?? "N/A",
                HouseId = a.HouseId,
                HouseNumber = a.House?.HouseNumber,
                LaneName = a.Lane?.LaneName ?? a.House?.Lane?.LaneName,
                AssignmentStatus = a.Status,
                AssignedDate = a.AssignedDate,
                MoveInDate = a.MoveInDate,
                MoveOutDate = a.MoveOutDate,
                Remarks = a.Remarks
            });

            _logger.LogInformation("Student accommodation list generated with {Count} entries", results.Count());
            return results;
        }
    }
}

