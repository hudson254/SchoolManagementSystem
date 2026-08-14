using MediatR;
using SMS.Application.Features.GradingScales.DTOs;

namespace SMS.Application.Features.GradingScales.Commands
{
    public class CreateGradingScaleCommand : IRequest<GradingScaleDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<GradeBandDto> Bands { get; set; } = new();
        public bool IsDefault { get; set; }
    }
}

