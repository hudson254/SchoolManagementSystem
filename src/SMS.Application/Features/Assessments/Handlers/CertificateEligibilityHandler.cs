using MediatR;
using SMS.Application.Features.CertificateEligibility.DTOs;
using SMS.Application.Features.CertificateEligibility.Queries;
using SMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Returns the student's certificate eligibility. Eligibility is evaluated by
    /// the centralized assessment engine against the configured certificate rule
    /// and the authoritative published unit results.
    /// </summary>
    public class GetStudentCertificateEligibilityHandler : IRequestHandler<GetStudentCertificateEligibilityQuery, StudentCertificateEligibilityDto>
    {
        private readonly IAssessmentEngine _engine;
        private readonly IStudentCertificateEligibilityRepository _eligibilityRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ICertificateRuleRepository _certificateRuleRepository;

        public GetStudentCertificateEligibilityHandler(
            IAssessmentEngine engine,
            IStudentCertificateEligibilityRepository eligibilityRepository,
            IStudentRepository studentRepository,
            ICertificateRuleRepository certificateRuleRepository)
        {
            _engine = engine;
            _eligibilityRepository = eligibilityRepository;
            _studentRepository = studentRepository;
            _certificateRuleRepository = certificateRuleRepository;
        }

        public async Task<StudentCertificateEligibilityDto> Handle(GetStudentCertificateEligibilityQuery request, CancellationToken cancellationToken)
        {
            var eligibility = await _eligibilityRepository.GetByStudentAsync(request.StudentId, cancellationToken);
            if (eligibility == null)
                eligibility = await _engine.EvaluateCertificateEligibilityAsync(request.StudentId, cancellationToken);

            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            var rule = await _certificateRuleRepository.GetActiveRuleAsync(cancellationToken);
            var minPass = rule?.MinimumPassingPercentage ?? 50m;

            var missing = new List<string>();
            if (eligibility.HasFailedRequiredUnits)
                missing.Add("One or more required units were failed.");
            if (eligibility.HasOutstandingIncomplete)
                missing.Add("There are outstanding incomplete units.");
            if ((eligibility.OverallPercentage ?? 0m) < minPass)
                missing.Add("Overall percentage is below the institution minimum.");

            return new StudentCertificateEligibilityDto
            {
                Id = eligibility.Id,
                StudentId = eligibility.StudentId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}".Trim() : string.Empty,
                Status = eligibility.Status,
                IneligibilityReason = eligibility.Status == SMS.Domain.Enums.CertificateEligibilityStatus.Eligible ? null : string.Join(" ", missing),
                EvaluatedDate = eligibility.EvaluatedDate,
                LastUpdated = eligibility.ModifiedDate,
                MissingRequirements = missing,
                OverallPercentage = eligibility.OverallPercentage ?? 0m,
                OverallGrade = eligibility.OverallGradeLetter ?? string.Empty
            };
        }
    }
}