using MediatR;
using SMS.Application.Features.Moderation.DTOs;

namespace SMS.Application.Features.Moderation.Commands
{
    public class ReviewMarksCommand : IRequest
    {
        public Guid AssessmentId { get; set; }
        public Guid StudentId { get; set; }
        public string? Comments { get; set; }
        public bool ReturnForCorrection { get; set; }
    }

    public class ApproveMarksCommand : IRequest
    {
        public Guid AssessmentId { get; set; }
        public Guid StudentId { get; set; }
        public string? Comments { get; set; }
    }
}

