using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Lecturers.Commands
{
    public class CreateLecturerCommand : IRequest<LecturerDto>
    {
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public Guid? DepartmentId { get; set; }
        public string Password { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public string? Qualifications { get; set; }
    }

    public class CreateLecturerCommandValidator : AbstractValidator<CreateLecturerCommand>
    {
        public CreateLecturerCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email is required")
                .MaximumLength(200);

            RuleFor(x => x.EmployeeNumber)
                .NotEmpty().WithMessage("Employee number is required")
                .MaximumLength(50);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters");

            RuleFor(x => x.DepartmentId)
                .NotEmpty().WithMessage("Department is required");
        }
    }

    public class CreateLecturerCommandHandler : IRequestHandler<CreateLecturerCommand, LecturerDto>
    {
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IUserManagerService _userManager;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateLecturerCommandHandler> _logger;
        private readonly INameParser _nameParser;
        private readonly IUsernameGenerator _usernameGenerator;

        public CreateLecturerCommandHandler(
            ILecturerRepository lecturerRepository,
            IDepartmentRepository departmentRepository,
            IUserManagerService userManager,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateLecturerCommandHandler> logger,
            INameParser nameParser,
            IUsernameGenerator usernameGenerator)
        {
            _lecturerRepository = lecturerRepository;
            _departmentRepository = departmentRepository;
            _userManager = userManager;
            _tenantContext = tenantContext;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
            _nameParser = nameParser;
            _usernameGenerator = usernameGenerator;
        }

        public async Task<LecturerDto> Handle(CreateLecturerCommand request, CancellationToken cancellationToken)
        {
            // Check if email already exists
            var existingUser = await _userManager.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
                throw new ConflictException("User with this email already exists");

            // Check if employee number already exists
            var existingLecturer = await _lecturerRepository.FindAsync(l => l.EmployeeNumber == request.EmployeeNumber, cancellationToken);
            if (existingLecturer.Any())
                throw new ConflictException("Lecturer with this employee number already exists");

            // Validate department exists
            if (request.DepartmentId.HasValue)
            {
                var department = await _departmentRepository.GetByIdAsync(request.DepartmentId.Value, cancellationToken);
                if (department == null)
                    throw new NotFoundException("Department", request.DepartmentId.Value);
            }

            // Parse the full name to extract any title and normalize name parts.
            // This ensures titles like "Dr" are stored separately and never leak
            // into usernames or file names.
            var fullName = $"{request.FirstName} {request.LastName}".Trim();
            var parsed = _nameParser.ParseName(fullName);

            if (!parsed.IsValid)
                throw new SMS.Application.Exceptions.ValidationException(parsed.ErrorMessage ?? "Invalid name format");

            var title = request.Title ?? parsed.Title;

            // Create user account
            var username = await _usernameGenerator.GenerateUsernameAsync(parsed.FirstName, parsed.LastName);
            var user = await _userManager.CreateUserAsync(username, request.Email, request.Password, "Lecturer");

            // Sync the User's name fields with the Lecturer's names
            if (user != null)
            {
                user.FirstName = parsed.FirstName;
                user.LastName = parsed.LastName;
                user.MiddleName = parsed.MiddleName;
                user.Title = title;
                user.PhoneNumber = request.PhoneNumber;
                user.Email = request.Email;
                await _userManager.UpdateUserAsync(user);
            }

            // Create lecturer
            var lecturer = new Lecturer
            {
                FirstName = parsed.FirstName,
                LastName = parsed.LastName,
                MiddleName = parsed.MiddleName,
                Title = title,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                EmployeeNumber = request.EmployeeNumber,
                DepartmentId = request.DepartmentId,
                IsActive = true,
                UserId = user.Id.ToString(),
                HireDate = DateTime.UtcNow,
                TenantId = Guid.Parse(_tenantContext.TenantId)
            };

            await _lecturerRepository.AddAsync(lecturer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Create", "Lecturer", $"Lecturer created: {lecturer.EmployeeNumber}");

            _logger.LogInformation("Lecturer created: {EmployeeNumber} ({Email})", lecturer.EmployeeNumber, lecturer.Email);

            return new LecturerDto
            {
                Id = lecturer.Id,
                FirstName = lecturer.FirstName,
                LastName = lecturer.LastName,
                MiddleName = lecturer.MiddleName,
                Title = lecturer.Title,
                Email = lecturer.Email,
                PhoneNumber = lecturer.PhoneNumber,
                EmployeeNumber = lecturer.EmployeeNumber,
                DepartmentId = lecturer.DepartmentId,
                IsActive = lecturer.IsActive,
                UserId = lecturer.UserId?.ToString(),
                CreatedDate = lecturer.CreatedDate ?? DateTime.UtcNow
            };
        }
    }
}

