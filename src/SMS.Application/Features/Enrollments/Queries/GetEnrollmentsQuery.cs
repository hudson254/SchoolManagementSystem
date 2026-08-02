using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Common;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Enrollments.Queries
{
    public class GetEnrollmentsQuery : IRequest<PagedResult<EnrollmentDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? StudentId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? SemesterId { get; set; }
        public string? Status { get; set; }
    }

    public class GetEnrollmentsQueryHandler : IRequestHandler<GetEnrollmentsQuery, PagedResult<EnrollmentDto>>
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ILogger<GetEnrollmentsQueryHandler> _logger;

        public GetEnrollmentsQueryHandler(
            IEnrollmentRepository enrollmentRepository,
            ILogger<GetEnrollmentsQueryHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _logger = logger;
        }

        public async Task<PagedResult<EnrollmentDto>> Handle(GetEnrollmentsQuery request, CancellationToken cancellationToken)
        {
            var allEnrollments = await _enrollmentRepository.GetEnrollmentsAsync(cancellationToken);
            var query = allEnrollments.AsQueryable();

            if (request.StudentId.HasValue)
                query = query.Where(e => e.StudentId == request.StudentId.Value);
            if (request.UnitId.HasValue)
                query = query.Where(e => e.UnitId == request.UnitId.Value);
            if (request.SemesterId.HasValue)
                query = query.Where(e => e.SemesterId == request.SemesterId.Value);
            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(e => e.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));

            var list = query.ToList();
            var totalCount = list.Count;

            var pagedItems = list
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(e => new EnrollmentDto
                {
                    Id = e.Id,
                    StudentId = e.StudentId,
                    UnitId = e.UnitId ?? Guid.Empty,
                    SemesterId = e.SemesterId ?? Guid.Empty,
                    EnrollmentDate = e.EnrollmentDate,
                    Status = e.Status,
                    DropDate = e.DropDate,
                    StudentName = e.Student != null ? $"{e.Student.FirstName} {e.Student.LastName}" : string.Empty,
                    StudentNumber = e.Student?.StudentNumber ?? string.Empty,
                    UnitName = e.Unit?.Name ?? e.Course?.Name ?? string.Empty,
                    UnitCode = e.Unit?.Code ?? e.Course?.Code ?? string.Empty,
                    Credits = e.Unit?.Credits ?? 0,
                    SemesterName = e.Semester?.Name ?? string.Empty
                })
                .ToList();

            return new PagedResult<EnrollmentDto>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                Page = request.Page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
    }
}

