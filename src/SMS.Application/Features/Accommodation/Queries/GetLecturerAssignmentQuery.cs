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

            return new AccommodationAssignmentDto
            {
                Id = assignment.Id,
                StudentId = assignment.StudentId,
                LecturerId = assignment.LecturerId,
                OccupantType = assignment.OccupantType,
                RoomId = assignment.RoomId ?? Guid.Empty,
                SemesterId = assignment.SemesterId,
                AssignmentDate = assignment.AssignmentDate,
                MoveInDate = assignment.MoveInDate,
                MoveOutDate = assignment.MoveOutDate,
                Status = assignment.Status,
                Remarks = assignment.Remarks,
                StudentName = assignment.Student != null ? BuildStudentDisplayName(assignment.Student) : string.Empty,
                StudentNumber = assignment.Student?.StudentNumber ?? string.Empty,
                LecturerName = assignment.Lecturer != null ? BuildLecturerDisplayName(assignment.Lecturer) : string.Empty,
                EmployeeNumber = assignment.Lecturer?.EmployeeNumber ?? string.Empty,
                RoomNumber = assignment.Room?.RoomNumber ?? string.Empty,
                BlockName = assignment.Room?.Block?.Name ?? string.Empty,
                BuildingName = assignment.Room?.Block?.Building ?? string.Empty,
                SemesterName = assignment.Semester?.Name ?? string.Empty
            };
        }

        private static string BuildStudentDisplayName(SMS.Domain.Entities.Student student)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(student.Title))
                parts.Add(student.Title);
            parts.Add(student.FirstName);
            if (!string.IsNullOrWhiteSpace(student.MiddleName))
                parts.Add(student.MiddleName);
            parts.Add(student.LastName);
            return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        }

        private static string BuildLecturerDisplayName(SMS.Domain.Entities.Lecturer lecturer)
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
