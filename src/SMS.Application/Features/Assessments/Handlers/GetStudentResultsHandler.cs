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
    /// Student portal results: only PUBLISHED results and published assessment
    /// marks are returned. Draft / pending / approved-but-unpublished results
    /// remain invisible to students at both the API and the UI layer.
    /// </summary>
    public class GetStudentResultsHandler : IRequestHandler<GetStudentResultsQuery, IEnumerable<StudentResultDto>>
    {
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IStudentAssessmentMarkRepository _markRepository;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IGradingScaleRepository _gradingScaleRepository;
        private readonly ICertificateRuleRepository _certificateRuleRepository;

        public GetStudentResultsHandler(
            IUnitResultRepository unitResultRepository,
            IUnitRepository unitRepository,
            IStudentRepository studentRepository,
            IStudentAssessmentMarkRepository markRepository,
            IAssessmentRepository assessmentRepository,
            IGradingScaleRepository gradingScaleRepository,
            ICertificateRuleRepository certificateRuleRepository)
        {
            _unitResultRepository = unitResultRepository;
            _unitRepository = unitRepository;
            _studentRepository = studentRepository;
            _markRepository = markRepository;
            _assessmentRepository = assessmentRepository;
            _gradingScaleRepository = gradingScaleRepository;
            _certificateRuleRepository = certificateRuleRepository;
        }

        public async Task<IEnumerable<StudentResultDto>> Handle(GetStudentResultsQuery request, CancellationToken cancellationToken)
        {
            var results = await _unitResultRepository.GetPublishedByStudentAsync(request.StudentId, cancellationToken);

            var dtos = new List<StudentResultDto>();
            foreach (var r in results)
            {
                var unit = await _unitRepository.GetByIdAsync(r.UnitId, cancellationToken);
                dtos.Add(await UnitResultMapper.MapAsync(
                    r, unit, _studentRepository, _markRepository, _assessmentRepository,
                    _gradingScaleRepository, _certificateRuleRepository, true, cancellationToken));
            }

            return dtos;
        }
    }
}