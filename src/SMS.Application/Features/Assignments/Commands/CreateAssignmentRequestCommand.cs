using FluentValidation;
using MediatR;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Application.Features.OMS.Services;
using SMS.Domain.Common;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ICurrentUserService exists in both the Application and Domain namespaces.
// This adapter is written against the Application-layer one, matching
// CreateRequestCommandHandler.
using ICurrentUserService = SMS.Application.Common.Interfaces.ICurrentUserService;

namespace SMS.Application.Features.Assignments.Commands
{
    /// <summary>
    /// Thin Assignment -&gt; OMS Request adapter.
    ///
    /// Ordinary assignment submission and publication is ALREADY handled by
    /// <c>SubmitAssignmentCommand</c> and is deliberately NOT routed through
    /// OMS. Those commands remain authoritative and untouched.
    ///
    /// This adapter covers only the exception cases the Assignment module does
    /// not let a student or lecturer perform directly: deadline extension,
    /// reopening a closed assignment, or correcting a published assignment. It
    /// resolves the assignment, checks the caller owns the teaching context
    /// (or the student context), maps that onto the generic Request, and
    /// delegates. It never changes a deadline or a submission.
    /// </summary>
    public class CreateAssignmentRequestCommand : IRequest<RequestDto>
    {
        /// <summary>The assignment the request concerns.</summary>
        public Guid AssignmentId { get; set; }

        /// <summary>Assignment-scoped type code. Defaults to Extension.</summary>
        public string? RequestType { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;
        public DateTime? DueDate { get; set; }

        /// <summary>Free-text justification recorded with the request.</summary>
        public string? Reason { get; set; }
    }

    public class CreateAssignmentRequestCommandValidator : AbstractValidator<CreateAssignmentRequestCommand>
    {
        public CreateAssignmentRequestCommandValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty().WithMessage("An assignment reference is required");

            RuleFor(x => x.Title).MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Reason).MaximumLength(1000);
            RuleFor(x => x.RequestType).MaximumLength(100);
        }
    }

    /// <summary>
    /// Handler for the Assignment request adapter. It performs no workflow and
    /// mutates no assignment state; it delegates creation to the Request Core.
    /// </summary>
    public class CreateAssignmentRequestCommandHandler
        : IRequestHandler<CreateAssignmentRequestCommand, RequestDto>
    {
        /// <summary>
        /// Loose-association discriminator stored on the generic Request. The
        /// Assignment module, not the Request core, interprets it.
        /// </summary>
        public const string RelatedEntityType = "Assignment";

        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IOmsRequestModuleAdapter _requestAdapter;

        public CreateAssignmentRequestCommandHandler(
            IAssignmentRepository assignmentRepository,
            ILecturerRepository lecturerRepository,
            ICurrentUserService currentUser,
            IOmsRequestModuleAdapter requestAdapter)
        {
            _assignmentRepository = assignmentRepository;
            _lecturerRepository = lecturerRepository;
            _currentUser = currentUser;
            _requestAdapter = requestAdapter;
        }

        public async Task<RequestDto> Handle(
            CreateAssignmentRequestCommand request,
            CancellationToken cancellationToken)
        {
            ModuleRequestAdapterSupport.EnsureCanRaiseRequest(_currentUser);

            // Tenant isolation: a cross-tenant assignment resolves to null via
            // the DbContext global query filter rather than leaking the row.
            var assignment = ModuleRequestAdapterSupport.EnsureFound(
                await _assignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken),
                nameof(request.AssignmentId),
                "assignment");

            var requestType = ModuleRequestAdapterSupport.EnsureAllowedRequestType(
                string.IsNullOrWhiteSpace(request.RequestType) ? AssignmentRequestTypes.Extension : request.RequestType,
                AssignmentRequestTypes.All,
                "assignment");

            // An assignment without a named lecturer is unowned teaching
            // material: only the privileged queue role may raise a request
            // against it, otherwise anybody could request changes to an
            // assignment that has no responsible lecturer.
            if (assignment.LecturerId.HasValue)
            {
                var lecturer = ModuleRequestAdapterSupport.EnsureFound(
                    await _lecturerRepository.GetByIdAsync(assignment.LecturerId.Value, cancellationToken),
                    nameof(assignment.LecturerId),
                    "lecturer for this assignment");

                ModuleRequestAdapterSupport.EnsureCanRequestForLecturer(_currentUser, lecturer.UserId);
            }
            else if (!OmsRequestAccess.CanViewAll(_currentUser.Roles))
            {
                throw new ForbiddenException(OmsPermissions.CreateRequest, _currentUser.UserId);
            }

            var createCommand = new SMS.Application.Features.OMS.Commands.CreateRequestCommand
            {
                RequestType = requestType,
                Title = ModuleRequestAdapterSupport.BuildTitle(
                    request.Title,
                    $"{requestType.Replace('_', ' ').ToLowerInvariant()} - {DescribeAssignment(assignment)}"),
                Description = ComposeDescription(request),
                Priority = request.Priority,
                DueDate = request.DueDate,

                // Typed relationships resolved from the owning module.
                AssignmentId = assignment.Id,
                UnitId = assignment.UnitId,
                LecturerId = assignment.LecturerId,

                RelatedEntityType = RelatedEntityType,
                RelatedEntityId = assignment.Id.ToString()
            };

            // Single authoritative creation path.
            return await _requestAdapter.CreateAsync(createCommand, cancellationToken);
        }

        private static string DescribeAssignment(Assignment assignment)
        {
            var due = assignment.DueDate == default
                ? "no due date"
                : $"due {assignment.DueDate:yyyy-MM-dd}";

            return $"\"{assignment.Title}\" (unit {assignment.UnitId}, {due})";
        }

        private static string ComposeDescription(CreateAssignmentRequestCommand request)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(request.Description))
                parts.Add(request.Description.Trim());

            if (!string.IsNullOrWhiteSpace(request.Reason))
                parts.Add($"Reason: {request.Reason.Trim()}");

            parts.Add(
                "The Assignment module remains the owner of this assignment; " +
                "no deadline, submission or grading state is changed by raising this request.");

            return string.Join("\n\n", parts);
        }
    }
}
