using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;
using SMS.Shared.DTOs;

namespace SMS.Application.Features.Units.Queries
{
    public class GetUnitsQuery : IRequest<PagedResult<UnitDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public Guid? CourseId { get; set; }
        public int? Semester { get; set; }
        public bool? IsActive { get; set; }
        public string SortBy { get; set; } = "Name";
        public bool SortDescending { get; set; }
    }

    public class GetUnitsQueryHandler : IRequestHandler<GetUnitsQuery, PagedResult<UnitDto>>
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<GetUnitsQueryHandler> _logger;

        public GetUnitsQueryHandler(IUnitRepository unitRepository, ILogger<GetUnitsQueryHandler> logger)
        {
            _unitRepository = unitRepository;
            _logger = logger;
        }

        public async Task<PagedResult<UnitDto>> Handle(GetUnitsQuery request, CancellationToken cancellationToken)
        {
            var units = await _unitRepository.GetUnitsAsync(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                request.CourseId,
                request.Semester,
                request.IsActive,
                request.SortBy,
                request.SortDescending,
                cancellationToken);

            var totalCount = await _unitRepository.CountUnitsAsync(
                request.SearchTerm,
                request.CourseId,
                request.Semester,
                request.IsActive,
                cancellationToken);

            return new PagedResult<UnitDto>
            {
                Items = units.Select(u => new UnitDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Code = u.Code,
                    Description = u.Description,
                    Credits = u.Credits,
                    ContactHours = u.ContactHours,
                    IsActive = u.IsActive,
                    CourseId = u.CourseId,
                    CourseName = u.Course?.Name,
                    CourseCode = u.Course?.Code,
                    Semester = u.Semester,
                    SemesterName = u.Semester.ToString(),
                    PrerequisiteUnitId = u.PrerequisiteUnitId,
                    PrerequisiteCode = u.Prerequisite?.Code,
                    PrerequisiteName = u.Prerequisite?.Name,
                    CreatedDate = u.CreatedAt
                }).ToList(),
                TotalCount = totalCount,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
