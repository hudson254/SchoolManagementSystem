using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Updates an existing assessment type (System Administrator / Administrator).
    /// </summary>
    public class UpdateAssessmentTypeHandler : IRequestHandler<UpdateAssessmentTypeCommand, AssessmentTypeDto>
    {
        private readonly IAssessmentTypeRepository _typeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateAssessmentTypeHandler> _logger;

        public UpdateAssessmentTypeHandler(
            IAssessmentTypeRepository typeRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateAssessmentTypeHandler> logger)
        {
            _typeRepository = typeRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AssessmentTypeDto> Handle(UpdateAssessmentTypeCommand request, CancellationToken cancellationToken)
        {
            var type = await _typeRepository.GetByIdAsync(request.Id, cancellationToken);
            if (type == null || type.IsDeleted)
                throw new NotFoundException("AssessmentType", request.Id);

            type.Name = request.Name;
            type.Description = request.Description;
            type.Category = request.Category;
            type.SortOrder = request.SortOrder;
            type.IsActive = request.IsActive;

            await _typeRepository.UpdateAsync(type, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("AssessmentTypeUpdated", "AssessmentType", type.Id.ToString(),
                $"Updated assessment type: {type.Name} ({type.Code})");

            _logger.LogInformation("Assessment type updated: {Id}", type.Id);

            return GetAssessmentTypesHandler.Map(type);
        }
    }
}