using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Features.GradingScales.Commands;
using SMS.Application.Features.GradingScales.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Creates a new grading scale (Administrator). The new scale is versioned and
    /// does not alter historical results, which retain the scale version in force
    /// when they were published.
    /// </summary>
    public class CreateGradingScaleHandler : IRequestHandler<CreateGradingScaleCommand, GradingScaleDto>
    {
        private readonly IGradingScaleRepository _scaleRepository;
        private readonly IGradeBandRepository _bandRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUser;
        private readonly ILogger<CreateGradingScaleHandler> _logger;

        public CreateGradingScaleHandler(
            IGradingScaleRepository scaleRepository,
            IGradeBandRepository bandRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Domain.Interfaces.ICurrentUserService currentUser,
            ILogger<CreateGradingScaleHandler> logger)
        {
            _scaleRepository = scaleRepository;
            _bandRepository = bandRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<GradingScaleDto> Handle(CreateGradingScaleCommand request, CancellationToken cancellationToken)
        {
            if (request.Bands == null || request.Bands.Count == 0)
                throw new InvalidOperationException("A grading scale must define at least one grade band.");

            foreach (var band in request.Bands)
            {
                if (band.MinPercentage < 0 || band.MaxPercentage > 100 || band.MinPercentage >= band.MaxPercentage)
                    throw new InvalidOperationException($"Invalid band {band.GradeLetter}: boundaries must satisfy 0 <= min < max <= 100.");
            }

            var scale = new GradingScale
            {
                Name = request.Name,
                Description = request.Description,
                Version = 1,
                IsActive = true,
                IsDefault = request.IsDefault,
                EffectiveFrom = DateTime.UtcNow,
                CreatedBy = _currentUser.Username
            };

            await _scaleRepository.AddAsync(scale, cancellationToken);

            var dtos = new List<GradeBandDto>();
            foreach (var band in request.Bands)
            {
                var entity = new GradeBand
                {
                    GradingScaleId = scale.Id,
                    GradeLetter = band.GradeLetter,
                    Description = band.Description,
                    MinPercentage = band.MinPercentage,
                    MaxPercentage = band.MaxPercentage,
                    GpaPoints = band.GpaPoints,
                    ColorCode = band.ColorCode,
                    HonorsClassification = band.HonorsClassification,
                    SortOrder = band.SortOrder
                };
                await _bandRepository.AddAsync(entity, cancellationToken);
                dtos.Add(GetGradingScalesHandler.MapBand(entity));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("GradingScaleCreated", "GradingScale", scale.Id.ToString(),
                $"Created grading scale: {scale.Name} (v{scale.Version}) with {dtos.Count} bands by {_currentUser.Username}");

            _logger.LogInformation("Grading scale created: {Name} v{Version}", scale.Name, scale.Version);

            return new GradingScaleDto
            {
                Id = scale.Id,
                Name = scale.Name,
                Description = scale.Description,
                Version = scale.Version,
                IsDefault = scale.IsDefault,
                IsActive = scale.IsActive,
                EffectiveFrom = scale.EffectiveFrom,
                EffectiveTo = scale.EffectiveTo,
                Bands = dtos
            };
        }
    }
}