using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    /// <summary>
    /// Query to get a lecturer's accommodation assignment.
    /// </summary>
    public class GetLecturerAssignmentQuery : IRequest<AccommodationAssignmentDto?>
    {
        public Guid LecturerId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    public class GetLecturerAssignmentQueryHandler : IRequestHandler<GetLecturerAssignmentQuery, AccommodationAssignmentDto?>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ILogger<GetLecturerAssignmentQueryHandler> _logger;

        public GetLecturerAssignmentQueryHandler(
            IAccommodationRepository accommodationRepository,
            ILecturerRepository lecturerRepository,
            ILogger<GetLecturerAssignmentQueryHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _lecturerRepository = lecturerRepository;
            _logger = logger;
        }

        public async Task<AccommodationAssignmentDto?> Handle(GetLecturerAssignmentQuery request, CancellationToken cancellationToken)
        {
            var lecturer = await _lecturerRepository.GetByIdAsync(request.LecturerId, cancellationToken);
            if (lecturer == null)
            {
                throw new NotFoundException("Lecturer", request.LecturerId);
            }

            var assignment = await _accommodationRepository.GetAssignmentByLecturerAsync(
                            request.LecturerId,
                            cancellationToken);

            if (assignment == null)
            {
                return null;
            }

            return AccommodationDtoMappings.ToAssignmentDto(assignment);
        }
    }
}
