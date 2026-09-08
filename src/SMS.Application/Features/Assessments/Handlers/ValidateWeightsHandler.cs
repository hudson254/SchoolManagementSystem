using MediatR;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Application.Features.Assessments.Queries;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Validates that the total configured assessment weighting for a unit
    /// equals exactly 100%. Invalid totals cannot be published.
    /// </summary>
    public class ValidateWeightsHandler : IRequestHandler<ValidateWeightsQuery, WeightValidationResult>
    {
        private readonly IAssessmentRepository _assessmentRepository;

        public ValidateWeightsHandler(IAssessmentRepository assessmentRepository)
        {
            _assessmentRepository = assessmentRepository;
        }

        public async Task<WeightValidationResult> Handle(ValidateWeightsQuery request, CancellationToken cancellationToken)
        {
            var assessments = (await _assessmentRepository.GetByUnitAsync(request.UnitId, cancellationToken))
                .Where(a => !a.IsDeleted && a.IsActive)
                .ToList();

            var result = new WeightValidationResult
            {
                IsValid = true,
                TotalWeight = 0m,
                Weights = new(),
                Errors = new()
            };

            foreach (var a in assessments)
            {
                result.TotalWeight += a.Weight;
                result.Weights.Add(new AssessmentWeightDto
                {
                    AssessmentId = a.Id,
                    AssessmentName = a.Title,
                    Weight = a.Weight
                });

                if (a.Weight < 0 || a.Weight > 100)
                {
                    result.IsValid = false;
                    result.Errors.Add(new WeightValidationError
                    {
                        Message = $"Assessment '{a.Title}' has invalid weight: {a.Weight}%",
                        Field = $"Assessment_{a.Id}"
                    });
                }
            }

            // Hundred-point parity: 99.99 / 100.00 / 100.01 are the boundary cases.
            // Exact 100.00 is required; decimal drift beyond +/- 0.01 is rejected.
            if (Math.Abs(result.TotalWeight - 100m) >= 0.0001m)
            {
                result.IsValid = false;
                result.Errors.Add(new WeightValidationError
                {
                    Message = $"Total weight is {result.TotalWeight}%. Must equal exactly 100%.",
                    Field = "TotalWeight"
                });
            }

            return result;
        }
    }
}