using FluentValidation;
using SMS.Shared.DTOs;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Students.Queries
{
    public class GetStudentEnrollmentsQuery : IRequest<IEnumerable<EnrollmentDto>>
    {
        public Guid StudentId { get; set; }
        public Guid? SemesterId { get; set; }
        public string? Status { get; set; }
    }

    public class GetStudentEnrollmentsQueryHandler : IRequestHandler<GetStudentEnrollmentsQuery, IEnumerable<EnrollmentDto>>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ILogger<GetStudentEnrollmentsQueryHandler> _logger;

        public GetStudentEnrollmentsQueryHandler(
            IStudentRepository studentRepository,
            IEnrollmentRepository enrollmentRepository,
            ILogger<GetStudentEnrollmentsQueryHandler> logger)
        {
            _studentRepository = studentRepository;
            _enrollmentRepository = enrollmentRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<EnrollmentDto>> Handle(GetStudentEnrollmentsQuery request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException("Student", request.StudentId);
            }

            var enrollments = await _enrollmentRepository.GetStudentEnrollmentsAsync(request.StudentId);

            return enrollments.Select(e => new EnrollmentDto
            {
                Id = e.Id,
                StudentId = e.StudentId,
                UnitId = e.UnitId ?? Guid.Empty,
                SemesterId = e.SemesterId ?? Guid.Empty,
                EnrollmentDate = e.EnrollmentDate,
                Status = e.Status,
                DropDate = e.DropDate,
                StudentName = e.Student?.User?.FullName ?? "",
                StudentNumber = e.Student?.StudentNumber ?? "",
                UnitName = e.Unit?.Name ?? "",
                UnitCode = e.Unit?.Code ?? "",
                Credits = e.Unit?.Credits ?? 0,
                SemesterName = e.Semester?.Name ?? ""
            });
        }
    }
}




