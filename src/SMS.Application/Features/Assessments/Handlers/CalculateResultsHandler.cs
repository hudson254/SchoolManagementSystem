using MediatR;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Application.Features.Assessments.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Calculates (and persists) results for every student in a unit using the
    /// centralized assessment engine. This is the only path that produces
    /// final scores and grades.
    /// </summary>
    public class CalculateResultsHandler : IRequestHandler<CalculateResultsQuery, IEnumerable<StudentResultDto>>
    {
        private readonly IAssessmentEngine _engine;
        private readonly IUnitRepository _unitRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IStudentAssessmentMarkRepository _markRepository;
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IGradingScaleRepository _gradingScaleRepository;
        private readonly ICertificateRuleRepository _certificateRuleRepository;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;

        public CalculateResultsHandler(
            IAssessmentEngine engine,
            IUnitRepository unitRepository,
            IStudentRepository studentRepository,
            IStudentAssessmentMarkRepository markRepository,
            IAssessmentRepository assessmentRepository,
            IGradingScaleRepository gradingScaleRepository,
            ICertificateRuleRepository certificateRuleRepository,
            SMS.Domain.Interfaces.ICurrentUserService currentUser)
        {
            _engine = engine;
            _unitRepository = unitRepository;
            _studentRepository = studentRepository;
            _markRepository = markRepository;
            _assessmentRepository = assessmentRepository;
            _gradingScaleRepository = gradingScaleRepository;
            _certificateRuleRepository = certificateRuleRepository;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<StudentResultDto>> Handle(CalculateResultsQuery request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null || unit.IsDeleted)
                throw new SMS.Application.Exceptions.NotFoundException("Unit", request.UnitId);

            var results = await _engine.CalculateAllUnitResultsAsync(request.UnitId, null, cancellationToken);

            var dtos = new List<StudentResultDto>();
            foreach (var r in results)
            {
                dtos.Add(await UnitResultMapper.MapAsync(
                    r, unit, _studentRepository, _markRepository, _assessmentRepository,
                    _gradingScaleRepository, _certificateRuleRepository, false, cancellationToken));
            }

            return dtos;
        }
    }
}
