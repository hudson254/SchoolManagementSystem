using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Assignments.Queries
{
    public class GetStudentAssignmentsQuery : IRequest<IEnumerable<AssignmentDto>>
    {
        public Guid StudentId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    public class GetStudentAssignmentsQueryHandler : IRequestHandler<GetStudentAssignmentsQuery, IEnumerable<AssignmentDto>>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<GetStudentAssignmentsQueryHandler> _logger;

        public GetStudentAssignmentsQueryHandler(
            IAssignmentRepository assignmentRepository,
            IStudentRepository studentRepository,
            ILogger<GetStudentAssignmentsQueryHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _studentRepository = studentRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<AssignmentDto>> Handle(GetStudentAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student == null)
                throw new NotFoundException("Student", request.StudentId);

            var assignments = await _assignmentRepository.GetAssignmentsByStudentAsync(request.StudentId);

            if (request.SemesterId.HasValue)
            {
                assignments = assignments.Where(a => a.SemesterId == request.SemesterId.Value);
            }

            return assignments.Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                UnitId = a.UnitId,
                LecturerId = a.LecturerId,
                SemesterId = a.SemesterId,
                MaxScore = a.MaxScore,
                Weight = a.Weight,
                DueDate = a.DueDate,
                PublishedDate = a.PublishedDate,
                ClosingDate = a.ClosingDate,
                Instructions = a.Instructions,
                Attachments = a.Attachments,
                Status = a.Status,
                IsGraded = a.IsGraded,
                AllowLateSubmission = a.AllowLateSubmission,
                LatePenaltyPercent = a.LatePenaltyPercent,
                UnitName = a.Unit?.Name ?? string.Empty,
                UnitCode = a.Unit?.Code ?? string.Empty,
                LecturerName = a.Lecturer != null ? $"{a.Lecturer.FirstName} {a.Lecturer.LastName}" : string.Empty,
                SemesterName = a.Semester?.Name ?? string.Empty,
                SubmissionCount = 0,
                GradedCount = 0
            }).ToList();
        }
    }
}
