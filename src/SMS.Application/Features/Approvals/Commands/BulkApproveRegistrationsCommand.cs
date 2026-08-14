using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Approvals.Commands
{
    /// <summary>
    /// Bulk approves multiple student or lecturer registrations.
    /// </summary>
    public class BulkApproveRegistrationsCommand : IRequest<BulkApprovalResultDto>
    {
        public List<Guid> UserIds { get; set; } = new();
        public string UserType { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class BulkApproveRegistrationsCommandValidator : AbstractValidator<BulkApproveRegistrationsCommand>
    {
        public BulkApproveRegistrationsCommandValidator()
        {
            RuleFor(x => x.UserIds)
                .NotEmpty().WithMessage("At least one user ID is required");

            RuleFor(x => x.UserType)
                .NotEmpty().WithMessage("User type is required")
                .Must(x => x == "Student" || x == "Lecturer")
                .WithMessage("User type must be 'Student' or 'Lecturer'");
        }
    }

    public class BulkApprovalResultDto
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class BulkApproveRegistrationsCommandHandler
        : IRequestHandler<BulkApproveRegistrationsCommand, BulkApprovalResultDto>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<BulkApproveRegistrationsCommandHandler> _logger;

        public BulkApproveRegistrationsCommandHandler(
            IMediator mediator,
            ILogger<BulkApproveRegistrationsCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<BulkApprovalResultDto> Handle(
            BulkApproveRegistrationsCommand request,
            CancellationToken cancellationToken)
        {
            var result = new BulkApprovalResultDto
            {
                TotalProcessed = request.UserIds.Count
            };

            foreach (var userId in request.UserIds)
            {
                try
                {
                    var approveCommand = new ApproveRegistrationCommand
                    {
                        UserId = userId,
                        UserType = request.UserType,
                        Notes = request.Notes
                    };

                    await _mediator.Send(approveCommand, cancellationToken);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"User {userId}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to approve user {UserId}", userId);
                }
            }

            result.Message = $"Processed {result.TotalProcessed} users: {result.SuccessCount} approved, {result.FailureCount} failed";

            _logger.LogInformation("Bulk approval completed: {SuccessCount} succeeded, {FailureCount} failed",
                result.SuccessCount, result.FailureCount);

            return result;
        }
    }
}
