using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
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

            return new AccommodationAssignmentDto
            {
                Id = assignment.Id,
                StudentId = assignment.StudentId,
                RoomId = assignment.RoomId ?? Guid.Empty,
                SemesterId = assignment.SemesterId,
                AssignmentDate = assignment.AssignmentDate,
                MoveInDate = assignment.MoveInDate,
                MoveOutDate = assignment.MoveOutDate,
                Status = assignment.Status,
                Remarks = assignment.Remarks,
                StudentName = assignment.Student?.User.FullName ?? string.Empty,
                StudentNumber = assignment.Student?.StudentNumber ?? string.Empty,
                RoomNumber = assignment.Room?.RoomNumber ?? string.Empty,
                BlockName = assignment.Room?.Block?.Name ?? string.Empty,
                BuildingName = assignment.Room?.Block?.Building ?? string.Empty,
                SemesterName = assignment.Semester?.Name ?? string.Empty
            };
        }
    }
}




