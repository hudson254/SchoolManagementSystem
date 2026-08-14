using MediatR;
using SMS.Application.Features.Moderation.DTOs;

namespace SMS.Application.Features.Moderation.Queries
{
    public class GetPendingModerationQuery : IRequest<IEnumerable<ModerationRecordDto>> { }
}

