using System;
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
    /// Approves a single student or lecturer registration.
    /// Sets RegistrationStatus to Approved and activates enrollments/allocations.
    /// </summary>
    public class ApproveRegistrationCommand : IRequest<ApprovalResultDto>
    {
        public Guid UserId { get; set; }
        public string UserType { get; set; } = string.Empty; // "Student" or "Lecturer"
        public string? Notes { get; set; }
    }

    public class ApproveRegistrationCommandValidator : AbstractValidator<ApproveRegistrationCommand>
    {
        public ApproveRegistrationCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.UserType)
                .NotEmpty().WithMessage("User type is required")
                .Must(x => x == "Student" || x == "Lecturer")
                .WithMessage("User type must be 'Student' or 'Lecturer'");
        }
    }

    public class ApprovalResultDto
    {
        public Guid UserId { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ApproveRegistrationCommandHandler
        : IRequestHandler<ApproveRegistrationCommand, ApprovalResultDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUnitAllocationRepository _unitAllocationRepository;
        private readonly IAuditService _auditService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ApproveRegistrationCommandHandler> _logger;

        public ApproveRegistrationCommandHandler(
            IStudentRepository studentRepository,
            ILecturerRepository lecturerRepository,
            IEnrollmentRepository enrollmentRepository,
            IUnitAllocationRepository unitAllocationRepository,
            IAuditService auditService,
            IUnitOfWork unitOfWork,
            ILogger<ApproveRegistrationCommandHandler> logger)
        {
            _studentRepository = studentRepository;
            _lecturerRepository = lecturerRepository;
            _enrollmentRepository = enrollmentRepository;
            _unitAllocationRepository = unitAllocationRepository;
            _auditService = auditService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApprovalResultDto> Handle(
            ApproveRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            if (request.UserType == "Student")
            {
                var student = await _studentRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (student == null)
                    throw new NotFoundException("Student", request.UserId);

                if (student.RegistrationStatus != RegistrationStatus.PendingApproval)
                    throw new SMS.Application.Exceptions.ValidationException(
                        $"Cannot approve student. Current status: {student.RegistrationStatus}. Expected: PendingApproval");

                // Update student status
                student.RegistrationStatus = RegistrationStatus.Approved;
                student.IsEnrolled = true;

                // Activate all pending enrollments
                var enrollments = await _enrollmentRepository.GetStudentEnrollmentsAsync(student.Id, cancellationToken);
                foreach (var enrollment in enrollments)
                {
                    if (enrollment.Status == "PendingApproval")
                    {
                        enrollment.Status = "Active";
                        enrollment.IsActive = true;
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _auditService.LogAsync("ApproveRegistration", student.Id.ToString(),
                    $"Student registration approved. Notes: {request.Notes ?? "N/A"}");

                _logger.LogInformation("Student {StudentId} registration approved", student.Id);

                return new ApprovalResultDto
                {
                    UserId = student.Id,
                    UserType = "Student",
                    Status = "Approved",
                    Message = $"Student {student.FirstName} {student.LastName} registration approved successfully."
                };
            }
            else if (request.UserType == "Lecturer")
            {
                var lecturer = await _lecturerRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (lecturer == null)
                    throw new NotFoundException("Lecturer", request.UserId);

                if (lecturer.RegistrationStatus != RegistrationStatus.PendingApproval)
                    throw new SMS.Application.Exceptions.ValidationException(
                        $"Cannot approve lecturer. Current status: {lecturer.RegistrationStatus}. Expected: PendingApproval");

                // Update lecturer status
                lecturer.RegistrationStatus = RegistrationStatus.Approved;

                // Activate all pending unit allocations
                var allocations = await _unitAllocationRepository.GetByLecturerAsync(lecturer.Id);
                foreach (var allocation in allocations)
                {
                    if (allocation.Status == "PendingApproval")
                    {
                        allocation.Status = "Active";
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _auditService.LogAsync("ApproveRegistration", lecturer.Id.ToString(),
                    $"Lecturer registration approved. Notes: {request.Notes ?? "N/A"}");

                _logger.LogInformation("Lecturer {LecturerId} registration approved", lecturer.Id);

                return new ApprovalResultDto
                {
                    UserId = lecturer.Id,
                    UserType = "Lecturer",
                    Status = "Approved",
                    Message = $"Lecturer {lecturer.FirstName} {lecturer.LastName} registration approved successfully."
                };
            }

            throw new SMS.Application.Exceptions.ValidationException("Invalid user type");
        }
    }
}
