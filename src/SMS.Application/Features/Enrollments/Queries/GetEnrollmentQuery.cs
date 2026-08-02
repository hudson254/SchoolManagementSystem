using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Enrollments.Queries
{
    public class GetEnrollmentQuery : IRequest<EnrollmentDto>
    {
        public Guid EnrollmentId { get; set; }
    }

    public class GetEnrollmentQueryHandler : IRequestHandler<GetEnrollmentQuery, EnrollmentDto>
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ILogger<GetEnrollmentQueryHandler> _logger;

        public GetEnrollmentQueryHandler(
            IEnrollmentRepository enrollmentRepository,
            ILogger<GetEnrollmentQueryHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _logger = logger;
        }

        public async Task<EnrollmentDto> Handle(GetEnrollmentQuery request, CancellationToken cancellationToken)
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(request.EnrollmentId, cancellationToken);
            if (enrollment == null)
                throw new NotFoundException("Enrollment", request.EnrollmentId);

            return new EnrollmentDto
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId,
                UnitId = enrollment.UnitId ?? Guid.Empty,
                SemesterId = enrollment.SemesterId ?? Guid.Empty,
                EnrollmentDate = enrollment.EnrollmentDate,
                Status = enrollment.Status,
                DropDate = enrollment.DropDate,
                StudentName = enrollment.Student != null ? $"{enrollment.Student.FirstName} {enrollment.Student.LastName}" : string.Empty,
                StudentNumber = enrollment.Student?.StudentNumber ?? string.Empty,
                UnitName = enrollment.Unit?.Name ?? enrollment.Course?.Name ?? string.Empty,
                UnitCode = enrollment.Unit?.Code ?? enrollment.Course?.Code ?? string.Empty,
                Credits = enrollment.Unit?.Credits ?? 0,
                SemesterName = enrollment.Semester?.Name ?? string.Empty
            };
        }
    }
}
