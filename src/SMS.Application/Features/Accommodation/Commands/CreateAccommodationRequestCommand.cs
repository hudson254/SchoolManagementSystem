using FluentValidation;
using MediatR;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Application.Features.OMS.Services;
using SMS.Domain.Common;
using SMS.Domain.Entities;
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

// The namespace "SMS.Application.Features.Accommodation" shadows the domain
// entity type of the same name, so the entity is referenced through an alias.
using AccommodationEntity = SMS.Domain.Entities.Accommodation;

namespace SMS.Application.Features.Accommodation.Commands
{
    /// <summary>
    /// Thin Accommodation -&gt; OMS Request adapter.
    ///
    /// Accommodation allocation IS genuinely administrative: TransferRoomCommand
    /// and ReassignHouseCommand already move occupants and both are privileged
    /// operations. Those commands remain the ONLY thing that mutates
    /// house/lane/occupancy data and are left untouched.
    ///
    /// This adapter exists so a student (or staff acting for one) can ask for an
    /// allocation or transfer they cannot perform themselves. It resolves the
    /// allocation, checks entitlement, maps module context onto the generic
    /// Request's typed references, and delegates. Once approved, a coordinator
    /// performs the actual move through the existing Accommodation commands.
    /// </summary>
    public class CreateAccommodationRequestCommand : IRequest<RequestDto>
    {
        /// <summary>The accommodation allocation the request concerns.</summary>
        public Guid AccommodationId { get; set; }

        /// <summary>Accommodation-scoped type code. Defaults to Transfer.</summary>
        public string? RequestType { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;
        public DateTime? DueDate { get; set; }

        /// <summary>Free-text justification recorded with the request.</summary>
        public string? Reason { get; set; }
    }

    public class CreateAccommodationRequestCommandValidator : AbstractValidator<CreateAccommodationRequestCommand>
    {
        public CreateAccommodationRequestCommandValidator()
        {
            RuleFor(x => x.AccommodationId)
                .NotEmpty().WithMessage("An accommodation reference is required");

            RuleFor(x => x.Title).MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Reason).MaximumLength(1000);
            RuleFor(x => x.RequestType).MaximumLength(100);
        }
    }

    /// <summary>
    /// Handler for the Accommodation request adapter. It performs no workflow:
    /// it resolves module context, authorizes the requester, and hands a
    /// populated CreateRequestCommand to the single authoritative Request Core.
    /// </summary>
    public class CreateAccommodationRequestCommandHandler
        : IRequestHandler<CreateAccommodationRequestCommand, RequestDto>
    {
        /// <summary>
        /// Loose-association discriminator stored on the generic Request. The
        /// Accommodation module, not the Request core, interprets it.
        /// </summary>
        public const string RelatedEntityType = "Accommodation";

        private readonly IAccommodationRepository _accommodationRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IOmsRequestModuleAdapter _requestAdapter;

        public CreateAccommodationRequestCommandHandler(
            IAccommodationRepository accommodationRepository,
            IStudentRepository studentRepository,
            ICurrentUserService currentUser,
            IOmsRequestModuleAdapter requestAdapter)
        {
            _accommodationRepository = accommodationRepository;
            _studentRepository = studentRepository;
            _currentUser = currentUser;
            _requestAdapter = requestAdapter;
        }

        public async Task<RequestDto> Handle(
            CreateAccommodationRequestCommand request,
            CancellationToken cancellationToken)
        {
            ModuleRequestAdapterSupport.EnsureCanRaiseRequest(_currentUser);

            // Tenant isolation: the repository reads through the DbContext global
            // query filter, so a cross-tenant reference resolves to null rather
            // than leaking another tenant's allocation.
            var accommodation = ModuleRequestAdapterSupport.EnsureFound(
                await _accommodationRepository.GetByIdAsync(request.AccommodationId, cancellationToken),
                nameof(request.AccommodationId),
                "accommodation allocation");

            var requestType = ModuleRequestAdapterSupport.EnsureAllowedRequestType(
                string.IsNullOrWhiteSpace(request.RequestType) ? AccommodationRequestTypes.Transfer : request.RequestType,
                AccommodationRequestTypes.All,
                "accommodation");

            await EnsureMayActForOccupantAsync(accommodation, cancellationToken);

            var createCommand = new SMS.Application.Features.OMS.Commands.CreateRequestCommand
            {
                RequestType = requestType,
                Title = ModuleRequestAdapterSupport.BuildTitle(
                    request.Title,
                    $"{requestType.Replace('_', ' ').ToLowerInvariant()} - {DescribeOccupant(accommodation)}"),
                Description = ComposeDescription(request),
                Priority = request.Priority,
                DueDate = request.DueDate,

                // Typed relationships resolved from the owning module.
                AccommodationId = accommodation.Id,
                StudentId = accommodation.StudentId,
                LecturerId = accommodation.LecturerId,

                RelatedEntityType = RelatedEntityType,
                RelatedEntityId = accommodation.Id.ToString()
            };

            // Single authoritative creation path.
            return await _requestAdapter.CreateAsync(createCommand, cancellationToken);
        }

        /// <summary>
        /// A student may only raise a request about their own allocation. Staff
        /// allocations are not self-service and are reserved for the privileged
        /// queue role.
        /// </summary>
        private async Task EnsureMayActForOccupantAsync(
            AccommodationEntity accommodation,
            CancellationToken cancellationToken)
        {
            if (accommodation.StudentId.HasValue)
            {
                var student = ModuleRequestAdapterSupport.EnsureFound(
                    await _studentRepository.GetByIdAsync(accommodation.StudentId.Value, cancellationToken),
                    nameof(accommodation.StudentId),
                    "student for this accommodation allocation");

                ModuleRequestAdapterSupport.EnsureCanRequestForStudent(_currentUser, student);
                return;
            }

            if (!OmsRequestAccess.CanViewAll(_currentUser.Roles))
                throw new ForbiddenException(OmsPermissions.CreateRequest, _currentUser.UserId);
        }

        private static string DescribeOccupant(AccommodationEntity accommodation)
        {
            if (accommodation.StudentId.HasValue)
                return $"student {accommodation.StudentId.Value}";

            if (accommodation.LecturerId.HasValue)
                return $"staff member {accommodation.LecturerId.Value}";

            return $"allocation {accommodation.Id}";
        }

        private static string ComposeDescription(CreateAccommodationRequestCommand request)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(request.Description))
                parts.Add(request.Description.Trim());

            if (!string.IsNullOrWhiteSpace(request.Reason))
                parts.Add($"Reason: {request.Reason.Trim()}");

            parts.Add(
                "The Accommodation module remains the owner of this allocation; " +
                "no house, lane or occupancy data is changed by raising this request.");

            return string.Join("\n\n", parts);
        }
    }
}
