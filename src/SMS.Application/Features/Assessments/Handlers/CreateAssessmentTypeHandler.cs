using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Creates a new assessment type (System Administrator / Administrator).
    /// </summary>
    public class CreateAssessmentTypeHandler : IRequestHandler<CreateAssessmentTypeCommand, AssessmentTypeDto>
    {
        private readonly IAssessmentTypeRepository _typeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateAssessmentTypeHandler> _logger;

        public CreateAssessmentTypeHandler(
            IAssessmentTypeRepository typeRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateAssessmentTypeHandler> logger)
        {
            _typeRepository = typeRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AssessmentTypeDto> Handle(CreateAssessmentTypeCommand request, CancellationToken cancellationToken)
        {
            var code = request.Code.Trim().ToUpper();
            var existing = await _typeRepository.GetByCodeAsync(code, cancellationToken);
            if (existing != null && !existing.IsDeleted)
                throw new InvalidOperationException($"Assessment type with code '{code}' already exists.");

            var type = new AssessmentType
            {
                Code = code,
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                IsActive = request.IsActive,
                IsSystemDefined = false,
                SortOrder = request.SortOrder
            };

            await _typeRepository.AddAsync(type, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("AssessmentTypeCreated", "AssessmentType", type.Id.ToString(),
                $"Created assessment type: {type.Name} ({type.Code})");

            _logger.LogInformation("Assessment type created: {Code} ({Name})", type.Code, type.Name);

            return GetAssessmentTypesHandler.Map(type);
        }
    }
}