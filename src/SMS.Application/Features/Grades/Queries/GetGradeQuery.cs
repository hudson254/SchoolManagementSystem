using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Application.Features.Grades;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Grades.Queries
{
    public class GetGradeQuery : IRequest<GradeDto>
    {
        public Guid GradeId { get; set; }
    }

    /// <summary>
    /// Resolves a single grade row from the authoritative UnitResult store
    /// (engine-persisted(. Legacy hard-coded grade calculations are not used.
    /// </summary>
    public class GetGradeQueryHandler : IRequestHandler<GetGradeQuery, GradeDto>
    {
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly ILogger<GetGradeQueryHandler> _logger;

        public GetGradeQueryHandler(IUnitResultRepository unitResultRepository, ILogger<GetGradeQueryHandler> logger)
        {
            _unitResultRepository = unitResultRepository;
            _logger = logger;
        }

        public async Task<GradeDto> Handle(GetGradeQuery request, CancellationToken cancellationToken)
        {
            var result = await _unitResultRepository.GetByIdWithDetailsAsync(request.GradeId, cancellationToken);
            if (result == null) throw new NotFoundException("Grade", request.GradeId);

            return AuthoritativeGradeMapper.MapToGradeDto(result, result.Unit, result.Student);
        }
    }
}