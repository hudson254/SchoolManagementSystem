using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Application.Features.Grades;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Grades.Queries
{
    public class GetUnitGradesQuery : IRequest<IEnumerable<GradeDto>>
    {
        public Guid UnitId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    /// <summary>
    /// Unit grades feed backed by the authoritative grading engine (UnitResults(.
    /// </summary>
    public class GetUnitGradesQueryHandler : IRequestHandler<GetUnitGradesQuery, IEnumerable<GradeDto>>
    {
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<GetUnitGradesQueryHandler> _logger;

        public GetUnitGradesQueryHandler(
            IUnitResultRepository unitResultRepository,
            IUnitRepository unitRepository,
            ILogger<GetUnitGradesQueryHandler> logger)
        {
            _unitResultRepository = unitResultRepository;
            _unitRepository = unitRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<GradeDto>> Handle(GetUnitGradesQuery request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null) throw new NotFoundException("Unit", request.UnitId);

            var results = (await _unitResultRepository.GetByUnitAsync(request.UnitId, cancellationToken)).ToList();

            if (request.SemesterId.HasValue)
                results = results.Where(r => r.SemesterId == request.SemesterId.Value).ToList();

            return results.Select(r => AuthoritativeGradeMapper.MapToGradeDto(r, unit, r.Student)).ToList();
        }
    }
}