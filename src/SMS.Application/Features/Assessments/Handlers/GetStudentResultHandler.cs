using MediatR;
using SMS.Application.Exceptions;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Application.Features.Assessments.Queries;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Returns the calculated result for a single student in a unit. The result
    /// is always recomputed through the centralized engine so the latest marks
    /// and grading scale are reflected.
    /// </summary>
    public class GetStudentResultHandler : IRequestHandler<GetStudentResultQuery, StudentResultDto>
    {
        private readonly IAssessmentEngine _engine;
        private readonly IUnitRepository _unitRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IStudentAssessmentMarkRepository _markRepository;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IGradingScaleRepository _gradingScaleRepository;
        private readonly ICertificateRuleRepository _certificateRuleRepository;

        public GetStudentResultHandler(
            IAssessmentEngine engine,
            IUnitRepository unitRepository,
            IStudentRepository studentRepository,
            IStudentAssessmentMarkRepository markRepository,
            IAssessmentRepository assessmentRepository,
            IGradingScaleRepository gradingScaleRepository,
            ICertificateRuleRepository certificateRuleRepository)
        {
            _engine = engine;
            _unitRepository = unitRepository;
            _studentRepository = studentRepository;
            _markRepository = markRepository;
            _assessmentRepository = assessmentRepository;
            _gradingScaleRepository = gradingScaleRepository;
            _certificateRuleRepository = certificateRuleRepository;
        }

        public async Task<StudentResultDto> Handle(GetStudentResultQuery request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null || unit.IsDeleted)
                throw new NotFoundException("Unit", request.UnitId);

            var result = await _engine.CalculateFinalUnitScoreAsync(request.StudentId, request.UnitId, null, cancellationToken);

            return await UnitResultMapper.MapAsync(
                result, unit, _studentRepository, _markRepository, _assessmentRepository,
                _gradingScaleRepository, _certificateRuleRepository, _engine, false, cancellationToken);
        }
    }
}