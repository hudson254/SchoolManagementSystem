using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Lecturers.Queries
{
    public class GetLecturersQuery : IRequest<PagedResult<LecturerDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public bool? IsVerified { get; set; }
        public string? Department { get; set; }
    }

    public class GetLecturersQueryHandler : IRequestHandler<GetLecturersQuery, PagedResult<LecturerDto>>
    {
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ILogger<GetLecturersQueryHandler> _logger;

        public GetLecturersQueryHandler(
            ILecturerRepository lecturerRepository,
            ILogger<GetLecturersQueryHandler> logger)
        {
            _lecturerRepository = lecturerRepository;
            _logger = logger;
        }

        public async Task<PagedResult<LecturerDto>> Handle(GetLecturersQuery request, CancellationToken cancellationToken)
        {
            var allLecturers = await _lecturerRepository.GetAllAsync(cancellationToken);
            var query = allLecturers.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(l =>
                    (l.FirstName ?? "").ToLower().Contains(term) ||
                    (l.LastName ?? "").ToLower().Contains(term) ||
                    (l.Email ?? "").ToLower().Contains(term) ||
                    (l.EmployeeNumber ?? "").ToLower().Contains(term));
            }

            if (request.IsVerified.HasValue)
            {
                query = query.Where(l => l.IsActive == request.IsVerified.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Department))
            {
                var dept = request.Department.ToLower();
                query = query.Where(l => l.Department != null && l.Department.Name.ToLower().Contains(dept));
            }

            var totalCount = query.Count();

            var lecturers = query
                .OrderBy(l => l.FirstName)
                .ThenBy(l => l.LastName)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var items = lecturers.Select(l => new LecturerDto
            {
                Id = l.Id,
                FirstName = l.FirstName,
                LastName = l.LastName,
                Email = l.Email,
                PhoneNumber = l.PhoneNumber,
                EmployeeNumber = l.EmployeeNumber,
                DepartmentId = l.DepartmentId,
                DepartmentName = l.Department?.Name,
                IsActive = l.IsActive,
                UserId = l.UserId?.ToString(),
                CreatedDate = l.CreatedDate ?? DateTime.UtcNow
            }).ToList();

            return new PagedResult<LecturerDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}

