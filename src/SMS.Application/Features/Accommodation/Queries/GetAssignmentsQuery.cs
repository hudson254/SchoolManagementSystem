using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    /// <summary>
    /// Lists accommodation assignments (default: active) with full occupant,
    /// house and check-in information. Used by the receptionist occupancy view.
    /// </summary>
    public class GetAssignmentsQuery : IRequest<IEnumerable<AccommodationAssignmentDto>>
    {
        public Guid? LaneId { get; set; }
        public Guid? HouseId { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class GetAssignmentsHandler : IRequestHandler<GetAssignmentsQuery, IEnumerable<AccommodationAssignmentDto>>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetAssignmentsHandler> _logger;

        public GetAssignmentsHandler(
            IAccommodationRepository repository,
            ILogger<GetAssignmentsHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<AccommodationAssignmentDto>> Handle(GetAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var assignments = await _repository.GetActiveAssignmentsAsync(
                request.HouseId,
                request.LaneId,
                request.SearchTerm,
                cancellationToken);

            var dtos = assignments.Select(a => AccommodationDtoMappings.ToAssignmentDto(a));

            _logger.LogInformation("Retrieved {Count} active accommodation assignments", dtos.Count());
            return dtos;
        }
    }
}