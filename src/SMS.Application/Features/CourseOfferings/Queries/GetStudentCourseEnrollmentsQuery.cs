using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Queries
{
    public class GetStudentCourseEnrollmentsQuery : IRequest<IEnumerable<CourseOfferingEnrollmentDto>>
    {
        public Guid StudentId { get; set; }
        public string? Status { get; set; } // "pending", "active", "history"
    }

    public class GetStudentCourseEnrollmentsQueryHandler
        : IRequestHandler<GetStudentCourseEnrollmentsQuery, IEnumerable<CourseOfferingEnrollmentDto>>
    {
        private readonly ICourseOfferingEnrollmentRepository _enrollmentRepository;
        private readonly ILogger<GetStudentCourseEnrollmentsQueryHandler> _logger;

        public GetStudentCourseEnrollmentsQueryHandler(
            ICourseOfferingEnrollmentRepository enrollmentRepository,
            ILogger<GetStudentCourseEnrollmentsQueryHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<CourseOfferingEnrollmentDto>> Handle(
            GetStudentCourseEnrollmentsQuery request,
            CancellationToken cancellationToken)
        {
            IEnumerable<SMS.Domain.Entities.CourseOfferingEnrollment> enrollments;

            switch (request.Status?.ToLower())
            {
                case "pending":
                    enrollments = await _enrollmentRepository.GetPendingConfirmationsByStudentAsync(
                        request.StudentId, cancellationToken);
                    break;
                case "history":
                    enrollments = await _enrollmentRepository.GetHistoryByStudentAsync(
                        request.StudentId, cancellationToken);
                    break;
                default:
                    enrollments = await _enrollmentRepository.GetActiveByStudentAsync(
                        request.StudentId, cancellationToken);
                    break;
            }

            var result = enrollments.Select(MapToDto).ToList();
            _logger.LogInformation("Retrieved {Count} enrollments for student {StudentId} (status: {Status})",
                result.Count, request.StudentId, request.Status ?? "active");
            return result;
        }

        private static CourseOfferingEnrollmentDto MapToDto(SMS.Domain.Entities.CourseOfferingEnrollment e)
        {
            return new CourseOfferingEnrollmentDto
            {
                Id = e.Id,
                CourseOfferingId = e.CourseOfferingId,
                OfferingCode = e.CourseOffering?.OfferingCode,
                StudentId = e.StudentId,
                StudentName = e.Student?.User != null
                    ? $"{e.Student.User.FirstName} {e.Student.User.LastName}"
                    : null,
                StudentNumber = e.Student?.StudentNumber,
                EnrollmentDate = e.EnrollmentDate,
                Status = e.Status,
                IsActive = e.IsActive,
                AttemptNumber = e.AttemptNumber,
                ConfirmationStatus = e.ConfirmationStatus,
                ConfirmedDate = e.ConfirmedDate,
                DropDate = e.DropDate,
                Notes = e.Notes
            };
        }
    }
}
