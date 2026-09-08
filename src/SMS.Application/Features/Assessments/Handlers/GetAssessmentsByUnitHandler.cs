using MediatR;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Application.Features.Assessments.Queries;
using SMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Returns all assessments for a unit.
    /// </summary>
    public class GetAssessmentsByUnitHandler : IRequestHandler<GetAssessmentsByUnitQuery, IEnumerable<AssessmentDto>>
    {
        private readonly IAssessmentRepository _assessmentRepository;

        public GetAssessmentsByUnitHandler(IAssessmentRepository assessmentRepository)
        {
            _assessmentRepository = assessmentRepository;
        }

        public async Task<IEnumerable<AssessmentDto>> Handle(GetAssessmentsByUnitQuery request, CancellationToken cancellationToken)
        {
            var assessments = await _assessmentRepository.GetByUnitAsync(request.UnitId, cancellationToken);
            return assessments.Where(a => !a.IsDeleted).Select(GetAssessmentHandler.Map).ToList();
        }
    }
}