using MediatR;
using SMS.Application.Exceptions;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Application.Features.Assessments.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Lists the active assessment types. Assessment types are institutionally
    /// configurable; new types can be created without modifying source code.
    /// </summary>
    public class GetAssessmentTypesHandler : IRequestHandler<GetAssessmentTypesQuery, IEnumerable<AssessmentTypeDto>>
    {
        private readonly IAssessmentTypeRepository _typeRepository;

        public GetAssessmentTypesHandler(IAssessmentTypeRepository typeRepository)
        {
            _typeRepository = typeRepository;
        }

        public async Task<IEnumerable<AssessmentTypeDto>> Handle(GetAssessmentTypesQuery request, CancellationToken cancellationToken)
        {
            var types = await _typeRepository.GetActiveAsync(cancellationToken);
            return types.Select(t => Map(t)).ToList();
        }

        internal static AssessmentTypeDto Map(AssessmentType t)
        {
            return new AssessmentTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                Description = t.Description,
                Category = t.Category,
                IsActive = t.IsActive,
                IsSystemDefined = t.IsSystemDefined,
                SortOrder = t.SortOrder
            };
        }
    }

    /// <summary>
    /// Returns a single assessment type.
    /// </summary>
    public class GetAssessmentTypeHandler : IRequestHandler<GetAssessmentTypeQuery, AssessmentTypeDto>
    {
        private readonly IAssessmentTypeRepository _typeRepository;

        public GetAssessmentTypeHandler(IAssessmentTypeRepository typeRepository)
        {
            _typeRepository = typeRepository;
        }

        public async Task<AssessmentTypeDto> Handle(GetAssessmentTypeQuery request, CancellationToken cancellationToken)
        {
            var type = await _typeRepository.GetByIdAsync(request.Id, cancellationToken);
            if (type == null || type.IsDeleted)
                throw new NotFoundException("AssessmentType", request.Id);

            return GetAssessmentTypesHandler.Map(type);
        }
    }
}