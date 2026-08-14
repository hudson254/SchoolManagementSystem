using MediatR;
using SMS.Application.Features.GradingScales.DTOs;

namespace SMS.Application.Features.GradingScales.Queries
{
    public class GetGradingScalesQuery : IRequest<IEnumerable<GradingScaleDto>> { }
    public class GetGradingScaleQuery : IRequest<GradingScaleDto> { public Guid Id { get; set; } }
}

