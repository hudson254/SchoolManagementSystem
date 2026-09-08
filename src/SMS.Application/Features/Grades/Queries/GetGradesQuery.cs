using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Features.Grades;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Grades.Queries
{
    public class GetGradesQuery : IRequest<PagedResult<GradeDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? StudentId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? SemesterId { get; set; }
        public bool? IsPublished { get; set; }
    }

    /// <summary>
    /// Staff-facing paged grades feed backed by the authoritative grading engine
    /// (UnitResults.( No independent grade calculation or hard-coded grade points
    /// exist on this path; every row is mapped through <see cref="AuthoritativeGradeMapper"/>.
    /// </summary>
    public class GetGradesQueryHandler : IRequestHandler<GetGradesQuery, PagedResult<GradeDto>>
    {
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly ILogger<GetGradesQueryHandler> _logger;

        public GetGradesQueryHandler(IUnitResultRepository unitResultRepository, ILogger<GetGradesQueryHandler> logger)
        {
            _unitResultRepository = unitResultRepository;
            _logger = logger;
        }

        public async Task<PagedResult<GradeDto>> Handle(GetGradesQuery request, CancellationToken cancellationToken)
        {
            var all = (await _unitResultRepository.GetAllWithDetailsAsync(cancellationToken)).ToList();

            var query = all.AsQueryable();
            if (request.StudentId.HasValue)
                query = query.Where(g => g.StudentId == request.StudentId.Value);
            if (request.UnitId.HasValue)
                query = query.Where(g => g.UnitId == request.UnitId.Value);
            if (request.SemesterId.HasValue)
                query = query.Where(g => g.SemesterId == request.SemesterId.Value);
            if (request.IsPublished.HasValue)
                query = query.Where(g => g.IsPublished == request.IsPublished.Value);

            var list = query.ToList();
            var totalCount = list.Count;

            var pagedItems = list
                .OrderByDescending(g => g.CreatedDate)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
.Select(g => AuthoritativeGradeMapper.MapToGradeDto(g, g.Unit, g.Student))
.ToList();

            return new PagedResult<GradeDto>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                Page = request.Page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
    }
}
