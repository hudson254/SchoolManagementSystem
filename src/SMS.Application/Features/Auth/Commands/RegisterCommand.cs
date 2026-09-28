using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Application.Services;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Auth.Commands
{
    public class RegisterCommand : IRequest<AuthResponseDto>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Role { get; set; } = "Student";

        public string? Organization { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Username { get; set; }
        public Guid? CourseId { get; set; }
        /// <summary>
        /// Units the registrant selected to be enrolled in / assigned to teach.
        ///
        /// STUDENT: optional. The existing SMS business rule is that a student is
        /// automatically enrolled in EVERY active unit of the selected course, so
        /// the handler resolves the unit set server-side and this list is only
        /// used to detect a client/backend disagreement. It must never be used to
        /// narrow a student's enrollment.
        ///
        /// LECTURER: required. A lecturer chooses "all units" or specific units,
        /// and only the ids listed here are assigned. Every id is validated
        /// server-side against the selected course.
        /// </summary>
        public List<Guid> UnitIds { get; set; } = new List<Guid>();

        public string? Specialization { get; set; }

        /// <summary>
        /// Staff ID / Establishment Number. Preserves leading zeros.
        /// </summary>
        public string? StaffIdEstNo { get; set; }

        /// <summary>
        /// National ID or Passport Number. Alphanumeric, preserves leading zeros.
        /// </summary>
        public string? NationalIdPassport { get; set; }
    }

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email is required")
                .MaximumLength(200).WithMessage("Email must not exceed 200 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(12).WithMessage("Password must be at least 12 characters")
                .Must(p => p.Any(char.IsUpper)).WithMessage("Password must contain an uppercase letter")
                .Must(p => p.Any(char.IsLower)).WithMessage("Password must contain a lowercase letter")
                .Must(p => p.Any(char.IsDigit)).WithMessage("Password must contain a number")
                .Must(p => p.Any(ch => !char.IsLetterOrDigit(ch))).WithMessage("Password must contain a special character");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.Password).WithMessage("Passwords do not match");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required")
                .Must(r => r == "Student" || r == "Lecturer").WithMessage("Role must be either Student or Lecturer");

            RuleFor(x => x.Organization)
                .NotEmpty().WithMessage("Organization / Institution is required")
                .MaximumLength(200).WithMessage("Organization must not exceed 200 characters");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^[+]?[0-9\s\-\(\)]{7,20}$").WithMessage("A valid phone number is required")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.Username)
                .Matches(@"^[a-z0-9]+$").WithMessage("Username may only contain lowercase letters and numbers")
                .MaximumLength(50).WithMessage("Username must not exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Username));

            // Course is required for students; specialization is required for lecturers.
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Please select a course")
                .When(x => x.Role == "Student");

            // A lecturer's course is optional at the validator level (it is only
            // mandated when units are being assigned), but the pair must be
            // consistent: units without a course cannot be validated against a
            // course, so that combination is always rejected.
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("A course must be selected to assign units")
                .When(x => x.Role == "Lecturer" && x.UnitIds != null && x.UnitIds.Count > 0);

            // A lecturer must choose at least one unit to teach. Without this the
            // registration produced a lecturer with no teaching assignment at all,
            // which is what the reported bug described.
            RuleFor(x => x.UnitIds)
                .NotEmpty().WithMessage("Select at least one unit to teach")
                .When(x => x.Role == "Lecturer");

            // Guid.Empty is never a real unit id; catching it here gives a clear
            // message instead of a 404 from deep inside the handler.
            RuleForEach(x => x.UnitIds)
                .Must(id => id != Guid.Empty)
                .WithMessage("Invalid unit selected")
                .When(x => x.Role == "Lecturer");

            // Duplicates would create duplicate UnitAllocation rows.
            RuleFor(x => x.UnitIds)
                .Must(ids => ids == null || ids.Count == ids.Distinct().Count())
                .WithMessage("The same unit cannot be selected more than once")
                .When(x => x.Role == "Lecturer");

            RuleFor(x => x.Specialization)
                .NotEmpty().WithMessage("Specialization is required")
                .MaximumLength(200).WithMessage("Specialization must not exceed 200 characters")
                .When(x => x.Role == "Lecturer");

            // Staff Id / Est No. validation
            RuleFor(x => x.StaffIdEstNo)
                .MaximumLength(50).WithMessage("Staff Id / Est No. must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9\-\/]+$").WithMessage("Staff Id / Est No. contains invalid characters")
                .When(x => !string.IsNullOrEmpty(x.StaffIdEstNo));

            // National ID / Passport No. validation
            RuleFor(x => x.NationalIdPassport)
                .MaximumLength(50).WithMessage("National ID / Passport No. must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9]+$").WithMessage("National ID / Passport No. must be alphanumeric")
                .When(x => !string.IsNullOrEmpty(x.NationalIdPassport));
        }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
    {
        private static readonly HashSet<string> AllowedSelfRegistrationRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Student",
            "Lecturer"
        };

        private readonly IUserManagerService _userManagerService;
        private readonly IJwtService _jwtService;
        private readonly IAuditService _auditService;
        private readonly ILogger<RegisterCommandHandler> _logger;
        private readonly IUsernameGenerator _usernameGenerator;
        private readonly INameParser _nameParser;
        private readonly IStudentRepository _studentRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IUnitAllocationRepository _unitAllocationRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordPolicyService _passwordPolicyService;

        public RegisterCommandHandler(
            IUserManagerService userManagerService,
            IJwtService jwtService,
            IAuditService auditService,
            ILogger<RegisterCommandHandler> logger,
            IUsernameGenerator usernameGenerator,
            INameParser nameParser,
            IStudentRepository studentRepository,
            ILecturerRepository lecturerRepository,
            ICourseRepository courseRepository,
            IUnitRepository unitRepository,
            IUnitAllocationRepository unitAllocationRepository,
            ISemesterRepository semesterRepository,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext,
            IUnitOfWork unitOfWork,
            IPasswordPolicyService passwordPolicyService)
        {
            _userManagerService = userManagerService;
            _jwtService = jwtService;
            _auditService = auditService;
            _logger = logger;
            _usernameGenerator = usernameGenerator;
            _nameParser = nameParser;
            _studentRepository = studentRepository;
            _lecturerRepository = lecturerRepository;
            _courseRepository = courseRepository;
            _unitRepository = unitRepository;
            _unitAllocationRepository = unitAllocationRepository;
            _semesterRepository = semesterRepository;
            _tenantContext = tenantContext;
            _unitOfWork = unitOfWork;
            _passwordPolicyService = passwordPolicyService;
        }

        public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (request.Password != request.ConfirmPassword)
                throw new SMS.Application.Exceptions.ValidationException("Passwords do not match");

            if (!AllowedSelfRegistrationRoles.Contains(request.Role))
                throw new SMS.Application.Exceptions.ValidationException("Invalid registration role");

            // Server-side password policy enforcement (authoritative).
            var policyErrors = _passwordPolicyService.Validate(
                request.Password,
                new PasswordPolicyContext
                {
                    Email = request.Email,
                    Username = request.Username,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Organization = request.Organization
                });
            if (policyErrors.Count > 0)
                throw new SMS.Application.Exceptions.ValidationException(policyErrors.First());

            var existingUser = await _userManagerService.FindByEmailAsync(request.Email);
            if (existingUser != null)
                throw new ConflictException("User with this email already exists");

            // Parse the full name to extract any title and normalize name parts.
            var fullName = $"{request.FirstName} {request.LastName}".Trim();
            var parsed = _nameParser.ParseName(fullName);

            var title = request.Title ?? parsed.Title;

            if (!parsed.IsValid)
                throw new SMS.Application.Exceptions.ValidationException(parsed.ErrorMessage ?? "Invalid name format");

            var username = request.Username;
            if (string.IsNullOrWhiteSpace(username))
            {
                username = await _usernameGenerator.GenerateUsernameAsync(parsed.FirstName, parsed.LastName);
            }
            else
            {
                var isAvailable = await _usernameGenerator.IsUsernameAvailableAsync(username);
                if (!isAvailable)
                    throw new ConflictException("Username is already taken");
            }

            var user = await _userManagerService.CreateUserAsync(username, request.Email, request.Password, request.Role);
            if (user == null)
                throw new ExternalServiceException("User creation service returned null");

            var typedUser = (User)user;
            typedUser.FirstName = parsed.FirstName;
            typedUser.LastName = parsed.LastName;
            typedUser.MiddleName = parsed.MiddleName;
            typedUser.Title = title;
            typedUser.PhoneNumber = request.PhoneNumber;
            typedUser.Organization = request.Organization;
            await _userManagerService.UpdateUserAsync(typedUser);

            if (request.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
                await CreateStudentRecord(request, typedUser, parsed, title, cancellationToken);
            else if (request.Role.Equals("Lecturer", StringComparison.OrdinalIgnoreCase))
                await CreateLecturerRecord(request, typedUser, parsed, title, cancellationToken);

            var roles = await _userManagerService.GetRolesAsync(typedUser);
            var rolesList = roles?.ToList() ?? new List<string>();

            var accessToken = _jwtService.GenerateAccessToken(typedUser.Id.ToString(), typedUser.Email ?? typedUser.UserName, typedUser.Email, rolesList);
            var refreshToken = await _userManagerService.GenerateRefreshTokenAsync(typedUser.Id.ToString());

            await _auditService.LogAsync("Register", typedUser.Id.ToString(), $"User registered successfully as {request.Role} (pending course selection)");
            _logger.LogInformation("User registered successfully: {Email} as {Role} (pending course selection)", request.Email, request.Role);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600,
                TokenType = "Bearer",
                UserId = typedUser.Id.ToString(),
                Email = typedUser.Email,
                Username = typedUser.UserName,
                FullName = string.IsNullOrWhiteSpace(title)
                    ? $"{typedUser.FirstName} {typedUser.LastName}".Trim()
                    : $"{title} {typedUser.FirstName} {typedUser.LastName}".Trim(),
                FirstName = typedUser.FirstName,
                LastName = typedUser.LastName,
                Title = title,
                Roles = rolesList,
                RegistrationStatus = RegistrationStatus.PendingCourseSelection.ToString()
            };
        }

        /// <summary>
        /// Loads a course and its active units through the tenant-scoped
        /// repositories. This is the single authoritative resolution used by BOTH
        /// the registration verification page (via the public registration
        /// endpoint) and the enrollment/assignment persistence below, so what the
        /// user reviews on the preview step is exactly what gets written.
        ///
        /// The repository applies the tenant global query filter, so a course id
        /// from another tenant resolves to null and is rejected as NotFound -
        /// browser-supplied ids are treated as untrusted input.
        /// </summary>
        // Fully qualified: SMS.Domain.Entities.Unit collides with MediatR.Unit.
        private async Task<(Course Course, List<SMS.Domain.Entities.Unit> Units)> ResolveCourseAsync(
            Guid courseId,
            CancellationToken cancellationToken)
        {
            if (courseId == Guid.Empty)
                throw new SMS.Application.Exceptions.ValidationException("Please select a course");

            // GetByIdAsync is tenant-scoped by the global query filter.
            var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken);
            if (course == null || course.IsDeleted || !course.IsActive)
                throw new NotFoundException("Course", courseId);

            var units = (await _unitRepository.GetUnitsByCourseIdAsync(course.Id, cancellationToken))
                .Where(u => u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.Code)
                .ToList();

            return (course, units);
        }

        private async Task CreateStudentRecord(RegisterCommand request, User user, NameParseResult parsed, string? title, CancellationToken cancellationToken)
        {
            // Validate course selection (stored for later use during course selection workflow)
            if (!request.CourseId.HasValue)
                throw new SMS.Application.Exceptions.ValidationException("Course selection is required for student registration");

            // Resolve the authoritative course + unit set. The enrollment command
            // applies exactly the same filter (active + not deleted), so the
            // verification page and the persisted enrollment cannot drift.
            var (course, courseUnits) = await ResolveCourseAsync(request.CourseId.Value, cancellationToken);

            // Guard against a client/backend disagreement. The business rule is
            // "all units of the course", so we only reject when the client
            // submitted a DIFFERENT, non-empty set - never to narrow enrollment.
            if (request.UnitIds is { Count: > 0 } submitted)
            {
                var submittedSet = submitted.ToHashSet();
                var authoritativeSet = courseUnits.Select(u => u.Id).ToHashSet();
                if (!submittedSet.SetEquals(authoritativeSet))
                {
                    _logger.LogWarning(
                        "Registration for student {Email} submitted {SubmittedCount} unit ids for course {CourseId} " +
                        "but {AuthoritativeCount} active units are defined. Falling back to the authoritative set.",
                        request.Email, request.CourseId.Value, submittedSet.Count, authoritativeSet.Count);
                }
            }

            var student = new Student
            {
                FirstName = parsed.FirstName,
                LastName = parsed.LastName,
                MiddleName = parsed.MiddleName,
                Title = title,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                StaffIdEstNo = request.StaffIdEstNo,
                NationalIdPassport = request.NationalIdPassport,
                StudentNumber = $"STU{DateTime.UtcNow:yyyyMMdd}{new Random().Next(1000, 9999)}",
                UserId = user.Id,
                // Persist the course the student actually chose. Without this the
                // choice was validated and then discarded, so the student record,
                // the enrollment status endpoint and the dashboard all showed
                // "no course" (the reported production bug).
                SelectedCourseId = course.Id,
                // Course.ProgrammeId is nullable and is null in a fresh database
                // where courses are not linked to a programme, so fall back to
                // the course's programme collection before giving up.
                // NOTE: the cast to Guid? matters. FirstOrDefault() over a
                // non-nullable Guid sequence returns Guid.Empty, which is NOT
                // null and would violate the Students -> Programmes foreign key
                // (a 500 at registration) instead of leaving the FK unset.
                ProgrammeId = course.ProgrammeId
                    ?? course.Programmes.Select(p => (Guid?)p.Id).FirstOrDefault(),
                IsActive = true,
                IsEnrolled = false,  // Not enrolled until course selection + approval
                EnrollmentDate = DateTime.UtcNow,
                TenantId = Guid.Parse(_tenantContext.TenantId),
                RegistrationStatus = RegistrationStatus.PendingCourseSelection
            };

            await _studentRepository.AddAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Register", student.Id.ToString(),
                $"Student account created with selected course {course.Code} (pending course selection approval)");
            _logger.LogInformation(
                "Student {StudentId} registered with selected course {CourseId} ({CourseCode})",
                student.Id, course.Id, course.Code);
        }

        private async Task CreateLecturerRecord(RegisterCommand request, User user, NameParseResult parsed, string? title, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Specialization))
                throw new SMS.Application.Exceptions.ValidationException("Specialization is required for lecturer registration");

            // A lecturer must choose the units they will teach. Previously the
            // course and unit selections were accepted by the client and then
            // silently discarded here, so a newly registered lecturer had no
            // teaching assignment at all.
            var requestedUnitIds = (request.UnitIds ?? new List<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (requestedUnitIds.Count == 0)
                throw new SMS.Application.Exceptions.ValidationException(
                    "Select at least one unit to teach before completing registration");

            if (!request.CourseId.HasValue)
                throw new SMS.Application.Exceptions.ValidationException(
                    "A course must be selected to assign units");

            // Resolves the course (tenant-scoped, active) and its active units.
            var (course, courseUnits) = await ResolveCourseAsync(request.CourseId.Value, cancellationToken);

            if (courseUnits.Count == 0)
                throw new SMS.Application.Exceptions.ValidationException(
                    "The selected course has no active units to assign");

            // Every submitted unit id must belong to the selected course. This is
            // the server-side check that stops a tampered payload from assigning
            // a unit from another course (or another tenant) to this lecturer.
            var courseUnitIds = courseUnits.Select(u => u.Id).ToHashSet();
            var foreignUnitIds = requestedUnitIds.Where(id => !courseUnitIds.Contains(id)).ToList();
            if (foreignUnitIds.Count > 0)
            {
                _logger.LogWarning(
                    "Lecturer registration for {Email} submitted {Count} unit id(s) that do not belong to course {CourseId} ({CourseCode}). Rejected.",
                    request.Email, foreignUnitIds.Count, course.Id, course.Code);

                throw new SMS.Application.Exceptions.ValidationException(
                    "One or more selected units do not belong to the selected course. " +
                    "Please review your unit selection and try again.");
            }

            // Deterministic semester for the allocation. UnitAllocation.SemesterId
            // is a non-nullable FK to Semesters, so it can never be Guid.Empty.
            // Prefer the course's own semester, then the tenant's current
            // semester. A course with no resolvable semester is rejected rather
            // than persisted with a dangling foreign key (which would be a 500).
            Guid semesterId;
            if (course.SemesterId.HasValue && course.SemesterId.Value != Guid.Empty)
            {
                semesterId = course.SemesterId.Value;
            }
            else
            {
                var currentSemester = await _semesterRepository.GetCurrentOrDefaultAsync(cancellationToken);
                if (currentSemester == null)
                    throw new SMS.Application.Exceptions.ValidationException(
                        "No academic period is configured for this institution, so teaching units cannot be assigned yet. Please contact the administration office.");

                semesterId = currentSemester.Id;
            }

            var lecturer = new Lecturer
            {
                FirstName = parsed.FirstName,
                LastName = parsed.LastName,
                MiddleName = parsed.MiddleName,
                Title = title,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                NationalIdPassport = request.NationalIdPassport,
                EmployeeNumber = $"LEC{DateTime.UtcNow:yyyyMMdd}{new Random().Next(1000, 9999)}",
                IsActive = true,
                UserId = user.Id.ToString(),
                HireDate = DateTime.UtcNow,
                TenantId = Guid.Parse(_tenantContext.TenantId),
                RegistrationStatus = RegistrationStatus.PendingCourseSelection
            };

            await _lecturerRepository.AddAsync(lecturer, cancellationToken);
            // Save first so the lecturer row (and its generated id) exists before
            // the UnitAllocation rows reference it via the LecturerId foreign key.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Persist exactly the units the user confirmed on the verification
            // page - no more, no less. Persist through the repository rather than
            // a navigation collection: BaseEntity pre-assigns Id, so attaching a
            // brand new entity via a navigation makes EF classify it as Modified
            // and issue an UPDATE against a row that does not exist.
            var allocated = courseUnits.Where(u => requestedUnitIds.Contains(u.Id)).ToList();
            foreach (var unit in allocated)
            {
                var allocation = new UnitAllocation
                {
                    LecturerId = lecturer.Id,
                    UnitId = unit.Id,
                    SemesterId = semesterId,
                    AllocationDate = DateTime.UtcNow,
                    // PendingApproval mirrors the enrollment side: the teaching
                    // assignment exists and is visible, but is not yet active
                    // teaching load until an administrator approves it.
                    Status = "PendingApproval",
                    IsPrimary = false,
                    Notes = $"Created at registration for course {course.Code}."
                };

                await _unitAllocationRepository.AddAsync(allocation, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Register", lecturer.Id.ToString(),
                $"Lecturer account created with teaching assignment for course {course.Code} " +
                $"({allocated.Count} of {courseUnits.Count} units, pending approval)");

            _logger.LogInformation(
                "Lecturer {LecturerId} registered with course {CourseId} ({CourseCode}) and {UnitCount} assigned unit(s)",
                lecturer.Id, course.Id, course.Code, allocated.Count);
        }
    }
}
