using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Application.Features.Assessments.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Returns a single assessment.
    /// </summary>
    public class GetAssessmentHandler : IRequestHandler<GetAssessmentQuery, AssessmentDto>
    {
        private readonly IAssessmentRepository _assessmentRepository;

        public GetAssessmentHandler(IAssessmentRepository assessmentRepository)
        {
            _assessmentRepository = assessmentRepository;
        }

        public async Task<AssessmentDto> Handle(GetAssessmentQuery request, CancellationToken cancellationToken)
        {
            var assessment = await _assessmentRepository.GetByIdAsync(request.Id, cancellationToken);
            if (assessment == null || assessment.IsDeleted)
                throw new NotFoundException("Assessment", request.Id);

            return Map(assessment);
        }

        internal static AssessmentDto Map(Assessment a) => new()
        {
            Id = a.Id,
            Name = a.Title,
            Description = a.Description,
            UnitId = a.UnitId,
            CourseOfferingId = a.CourseOfferingId,
            SemesterId = a.SemesterId,
            AssessmentTypeId = a.AssessmentTypeId,
            LecturerId = a.LecturerId,
            AssessmentTemplateId = a.AssessmentTemplateId,
            MaxScore = a.MaxScore,
            Weight = a.Weight,
            DueDate = a.DueDate,
            ClosingDate = a.ClosingDate,
            AllowLateSubmission = a.AllowLateSubmission,
            LatePenaltyPercent = a.LatePenaltyPercent,
            GracePeriodDays = a.GracePeriodDays,
            IsOnlineSubmission = a.IsOnlineSubmission,
            LinkedAssignmentId = a.LinkedAssignmentId,
            IsExemptable = a.IsExemptable,
            IsMandatory = a.IsMandatory,
            RequiresModeration = a.RequiresModeration,
            IsAnonymousMarking = a.IsAnonymousMarking,
            Status = a.Status,
            PublicationStatus = a.PublicationStatus,
            ModerationStatus = a.ModerationStatus,
            IsWeightLocked = a.IsWeightLocked,
            WeightLockedDate = a.WeightLockedDate,
            WeightLockedBy = a.WeightLockedBy,
            IsActive = a.IsActive,
            SortOrder = a.SortOrder,
            FeedbackTemplate = a.FeedbackTemplate
        };
    }
}