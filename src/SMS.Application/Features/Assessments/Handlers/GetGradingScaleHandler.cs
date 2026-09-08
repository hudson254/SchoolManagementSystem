using MediatR;
using SMS.Application.Exceptions;
using SMS.Application.Features.GradingScales.DTOs;
using SMS.Application.Features.GradingScales.Queries;
using SMS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Returns a single grading scale with its bands.
    /// </summary>
    public class GetGradingScaleHandler : IRequestHandler<GetGradingScaleQuery, GradingScaleDto>
    {
        private readonly IGradingScaleRepository _scaleRepository;
        private readonly IGradeBandRepository _bandRepository;

        public GetGradingScaleHandler(IGradingScaleRepository scaleRepository, IGradeBandRepository bandRepository)
        {
            _scaleRepository = scaleRepository;
            _bandRepository = bandRepository;
        }

        public async Task<GradingScaleDto> Handle(GetGradingScaleQuery request, CancellationToken cancellationToken)
        {
            var scale = await _scaleRepository.GetByIdAsync(request.Id, cancellationToken);
            if (scale == null || scale.IsDeleted)
                throw new NotFoundException("GradingScale", request.Id);

            var bands = await _bandRepository.GetByScaleAsync(scale.Id, cancellationToken);
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
                Bands = bands.OrderBy(b => b.MinPercentage).Select(GetGradingScalesHandler.MapBand).ToList()
            };
        }
    }
}