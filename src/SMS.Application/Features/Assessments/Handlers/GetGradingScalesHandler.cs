using MediatR;
using SMS.Application.Features.GradingScales.DTOs;
using SMS.Application.Features.GradingScales.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Lists grading scales (including version history). Grade bands are loaded
    /// with each scale so the client always receives the authoritative,
    /// configurable boundaries.
    /// </summary>
    public class GetGradingScalesHandler : IRequestHandler<GetGradingScalesQuery, IEnumerable<GradingScaleDto>>
    {
        private readonly IGradingScaleRepository _scaleRepository;
        private readonly IGradeBandRepository _bandRepository;

        public GetGradingScalesHandler(IGradingScaleRepository scaleRepository, IGradeBandRepository bandRepository)
        {
            _scaleRepository = scaleRepository;
            _bandRepository = bandRepository;
        }

        public async Task<IEnumerable<GradingScaleDto>> Handle(GetGradingScalesQuery request, CancellationToken cancellationToken)
        {
            var dtos = new List<GradingScaleDto>();
            var scales = await _scaleRepository.GetAllAsync(cancellationToken);
            foreach (var scale in scales.Where(s => !s.IsDeleted))
            {
                var bands = await _bandRepository.GetByScaleAsync(scale.Id, cancellationToken);
                dtos.Add(new GradingScaleDto
                {
                    Id = scale.Id,
                    Name = scale.Name,
                    Description = scale.Description,
                    Version = scale.Version,
                    IsDefault = scale.IsDefault,
                    IsActive = scale.IsActive,
                    EffectiveFrom = scale.EffectiveFrom,
                    EffectiveTo = scale.EffectiveTo,
                    Bands = bands.OrderBy(b => b.MinPercentage).Select(MapBand).ToList()
                });
            }
            return dtos;
        }

        internal static GradeBandDto MapBand(GradeBand b) => new()
        {
            Id = b.Id,
            GradingScaleId = b.GradingScaleId,
            GradeLetter = b.GradeLetter,
            Description = b.Description,
            MinPercentage = b.MinPercentage,
            MaxPercentage = b.MaxPercentage,
            GpaPoints = b.GpaPoints ?? 0m,
            ColorCode = b.ColorCode,
            HonorsClassification = b.HonorsClassification,
            SortOrder = b.SortOrder
        };
    }
}