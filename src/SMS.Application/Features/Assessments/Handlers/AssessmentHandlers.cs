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
/// <summary>
    /// Creates an assessment for a unit. Weight must be within 0-100; the total
    /// weighting is validated against exactly 100% before results can proceed.
    /// </summary>
    public class CreateAssessmentHandler : IRequestHandler<CreateAssessmentCommand, AssessmentDto>
    {
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IAssessmentTypeRepository _typeRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateAssessmentHandler> _logger;

        public CreateAssessmentHandler(
            IAssessmentRepository assessmentRepository,
            IAssessmentTypeRepository typeRepository,
            IUnitRepository unitRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateAssessmentHandler> logger)
        {
            _assessmentRepository = assessmentRepository;
            _typeRepository = typeRepository;
            _unitRepository = unitRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AssessmentDto> Handle(CreateAssessmentCommand request, CancellationToken cancellationToken)
        {
            if (request.Weight < 0 || request.Weight > 100)
                throw new InvalidOperationException("Assessment weight must be between 0 and 100.");

            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null || unit.IsDeleted)
                throw new NotFoundException("Unit", request.UnitId);

            var type = await _typeRepository.GetByIdAsync(request.AssessmentTypeId, cancellationToken);
            if (type == null || type.IsDeleted || !type.IsActive)
                throw new NotFoundException("AssessmentType", request.AssessmentTypeId);

            var assessment = new Assessment
            {
                Title = request.Name,
                Description = request.Description,
                UnitId = request.UnitId,
                CourseOfferingId = request.CourseOfferingId,
                AssessmentTypeId = request.AssessmentTypeId,
                LecturerId = request.LecturerId,
                AssessmentTemplateId = request.TemplateId,
                MaxScore = request.MaxMarks == 0 ? 100 : request.MaxMarks,
                Weight = request.Weight,
                DueDate = request.DueDate,
                AllowLateSubmission = false,
                LatePenaltyPercent = 0m,
                IsOnlineSubmission = false,
                IsExemptable = true,
                IsMandatory = false,
                RequiresModeration = false,
                IsAnonymousMarking = false,
                Status = AssessmentStatus.Active,
                PublicationStatus = ResultPublicationStatus.Draft,
                ModerationStatus = ModerationStatus.NotRequired,
                IsActive = true
            };

            await _assessmentRepository.AddAsync(assessment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("AssessmentCreated", "Assessment", assessment.Id.ToString(),
                $"Created assessment: {assessment.Title} for unit {request.UnitId} with weight {request.Weight}%");

            _logger.LogInformation("Assessment created: {Title} for unit {UnitId}", assessment.Title, request.UnitId);

            return GetAssessmentHandler.Map(assessment);
        }
    }
}