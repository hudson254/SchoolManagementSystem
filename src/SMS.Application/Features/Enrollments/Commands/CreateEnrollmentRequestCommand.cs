using FluentValidation;
using MediatR;
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.OMS.Dtos;
using SMS.Application.Features.OMS.Services;
using SMS.Domain.Common;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

// Aliased to match CreateRequestCommandHandler, which uses the Application-layer
// ICurrentUserService rather than the Domain one of the same name.
using ICurrentUserService = SMS.Application.Common.Interfaces.ICurrentUserService;

namespace SMS.Application.Features.Enrollments.Commands
{
    /// <summary>
    /// Thin Enrollment -&gt; OMS Request adapter.
    ///
    /// The Enrollment module remains the owner of enrollment data. This
    /// adapter only:
    ///   * resolves the enrollment/student context,
    ///   * checks the caller is entitled to raise a request about that student,
    ///   * maps module context onto the generic Request's typed references,
    ///   * delegates to the generic OMS Request create command.
    ///
    /// It never mutates enrollment status, never writes oms_requests directly,
    /// and never defines its own approval state. After approval, a coordinator
    /// still performs the actual enrollment change through the existing
    /// Enrollment commands.
    /// </summary>
    public class CreateEnrollmentRequestCommand : IRequest<RequestDto>
    {
        /// <summary>Enrollment the request concerns.</summary>
        public Guid EnrollmentId { get; set; }

        /// <summary>
        /// Enrollment-scoped request type code. Defaults to
        /// <see cref="EnrollmentRequestTypes.CourseChange"/> when omitted.
        /// </summary>
        public string? RequestType { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;
        public DateTime? DueDate { get; set; }

        /// <summary>Free-text justification recorded with the request.</summary>
        public string? Reason { get; set; }
    }

    public class CreateEnrollmentRequestCommandValidator : AbstractValidator<CreateEnrollmentRequestCommand>
    {
        public CreateEnrollmentRequestCommandValidator()
        {
            RuleFor(x => x.EnrollmentId)
                .NotEmpty().WithMessage("An enrollment reference is required");

            RuleFor(x => x.Title)
                .MaximumLength(200);

            RuleFor(x => x.Description)
                .MaximumLength(2000);

            RuleFor(x => x.Reason)
                .MaximumLength(1000);

            RuleFor(x => x.RequestType)
                .MaximumLength(100);
        }
    }

    public class CreateEnrollmentRequestCommandHandler
        : IRequestHandler<CreateEnrollmentRequestCommand, RequestDto>
    {
        /// <summary>
        /// Loose-association discriminator stored on the generic Request. The
        /// Enrollment module, not the Request core, interprets it.
        /// </summary>
        public const string RelatedEntityType = "Enrollment";

        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IOmsRequestModuleAdapter _requestAdapter;

        public CreateEnrollmentRequestCommandHandler(
            IEnrollmentRepository enrollmentRepository,
            IStudentRepository studentRepository,
            ICurrentUserService currentUser,
            IOmsRequestModuleAdapter requestAdapter)
        {
            _enrollmentRepository = enrollmentRepository;
            _studentRepository = studentRepository;
            _currentUser = currentUser;
            _requestAdapter = requestAdapter;
        }

        public async Task<RequestDto> Handle(
            CreateEnrollmentRequestCommand request,
            CancellationToken cancellationToken)
        {
            ModuleRequestAdapterSupport.EnsureCanRaiseRequest(_currentUser);

            // The repository is tenant-filtered, so a cross-tenant reference
            // resolves to null rather than leaking another tenant's row.
            var enrollment = ModuleRequestAdapterSupport.EnsureFound(
                await _enrollmentRepository.GetByIdAsync(request.EnrollmentId, cancellationToken),
                nameof(request.EnrollmentId),
                "enrollment");

            var student = ModuleRequestAdapterSupport.EnsureFound(
                await _studentRepository.GetByIdAsync(enrollment.StudentId, cancellationToken),
                nameof(request.EnrollmentId),
                "student for this enrollment");

            ModuleRequestAdapterSupport.EnsureCanRequestForStudent(_currentUser, student);

            var requestType = ModuleRequestAdapterSupport.EnsureAllowedRequestType(
                string.IsNullOrWhiteSpace(request.RequestType) ? EnrollmentRequestTypes.CourseChange : request.RequestType,
                EnrollmentRequestTypes.All,
                "enrollment");

            var createCommand = new OMS.Commands.CreateRequestCommand
            {
                RequestType = requestType,
                Title = ModuleRequestAdapterSupport.BuildTitle(
                    request.Title,
                    $"{requestType.Replace('_', ' ').ToLowerInvariant()} - {DescribeStudent(student)} ({DescribeEnrollment(enrollment)})"),
                Description = ComposeDescription(request),
                Priority = request.Priority,
                DueDate = request.DueDate,

                // Typed relationships resolved from the owning module.
                EnrollmentId = enrollment.Id,
                StudentId = enrollment.StudentId,
                CourseId = enrollment.CourseId,
                UnitId = enrollment.UnitId,

                // Loose association retained for generic consumers/UI.
                RelatedEntityType = RelatedEntityType,
                RelatedEntityId = enrollment.Id.ToString()
            };

            // Single authoritative creation path.
            return await _requestAdapter.CreateAsync(createCommand, cancellationToken);
        }

        private static string DescribeStudent(Student student)
        {
            var name = string.Join(" ",
                new[] { student.FirstName, student.MiddleName, student.LastName }
                    .Where(p => !string.IsNullOrWhiteSpace(p))).Trim();

            return string.IsNullOrWhiteSpace(name) ? student.StudentNumber : $"{name} ({student.StudentNumber})";
        }

        private static string DescribeEnrollment(Enrollment enrollment)
        {
            var unit = enrollment.UnitId.HasValue ? enrollment.UnitId.Value.ToString() : "course-level";
            return $"course {enrollment.CourseId}, unit {unit}, status {enrollment.Status}";
        }

        private static string ComposeDescription(CreateEnrollmentRequestCommand request)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(request.Description))
                parts.Add(request.Description.Trim());

            if (!string.IsNullOrWhiteSpace(request.Reason))
                parts.Add($"Reason: {request.Reason.Trim()}");

            // State the domain-ownership boundary explicitly on every request,
            // so an approver never mistakes an approved request for an applied
            // enrollment change.
            parts.Add(
                "The Enrollment module remains the owner of this enrollment; " +
                "no enrollment state is changed by raising this request.");

            return string.Join("\n\n", parts);
        }
    }
}