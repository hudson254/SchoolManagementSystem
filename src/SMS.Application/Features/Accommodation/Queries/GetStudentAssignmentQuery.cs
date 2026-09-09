using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
using SMS.Domain.Enums;
namespace SMS.Application.Features.Accommodation.Queries
{
    public class GetStudentAssignmentQuery : IRequest<AccommodationAssignmentDto?>
    {
        public Guid StudentId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    public class GetStudentAssignmentQueryHandler : IRequestHandler<GetStudentAssignmentQuery, AccommodationAssignmentDto?>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<GetStudentAssignmentQueryHandler> _logger;

        public GetStudentAssignmentQueryHandler(
            IAccommodationRepository accommodationRepository,
            IStudentRepository studentRepository,
            ILogger<GetStudentAssignmentQueryHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _studentRepository = studentRepository;
            _logger = logger;
        }

        public async Task<AccommodationAssignmentDto?> Handle(GetStudentAssignmentQuery request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException("Student", request.StudentId);
            }

            var assignment = await _accommodationRepository.GetAssignmentByStudentAsync(
                            request.StudentId,
                            cancellationToken);

            if (assignment == null)
            {
                return null;
            }

            return AccommodationDtoMappings.ToAssignmentDto(assignment);
        }
    }
}




