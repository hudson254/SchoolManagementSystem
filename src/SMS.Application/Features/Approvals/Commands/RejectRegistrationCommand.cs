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
    /// Rejects a student or lecturer registration.
    /// Sets RegistrationStatus to Rejected with a rejection reason.
    /// </summary>
    public class RejectRegistrationCommand : IRequest<ApprovalResultDto>
    {
        public Guid UserId { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class RejectRegistrationCommandValidator : AbstractValidator<RejectRegistrationCommand>
    {
        public RejectRegistrationCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.UserType)
                .NotEmpty().Must(x => x == "Student" || x == "Lecturer");
            RuleFor(x => x.Reason)
                .NotEmpty().MaximumLength(500);
        }
    }

    public class RejectRegistrationCommandHandler
        : IRequestHandler<RejectRegistrationCommand, ApprovalResultDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IAuditService _auditService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RejectRegistrationCommandHandler> _logger;

        public RejectRegistrationCommandHandler(
            IStudentRepository studentRepository,
            ILecturerRepository lecturerRepository,
            IAuditService auditService,
            IUnitOfWork unitOfWork,
            ILogger<RejectRegistrationCommandHandler> logger)
        {
            _studentRepository = studentRepository;
            _lecturerRepository = lecturerRepository;
            _auditService = auditService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApprovalResultDto> Handle(
            RejectRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            if (request.UserType == "Student")
            {
                var student = await _studentRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (student == null)
                    throw new NotFoundException("Student", request.UserId);

                student.RegistrationStatus = RegistrationStatus.Rejected;
                student.IsEnrolled = false;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _auditService.LogAsync("RejectRegistration", student.Id.ToString(),
                    $"Student registration rejected. Reason: {request.Reason}");
            }
            else if (request.UserType == "Lecturer")
            {
                var lecturer = await _lecturerRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (lecturer == null)
                    throw new NotFoundException("Lecturer", request.UserId);

                lecturer.RegistrationStatus = RegistrationStatus.Rejected;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _auditService.LogAsync("RejectRegistration", lecturer.Id.ToString(),
                    $"Lecturer registration rejected. Reason: {request.Reason}");
            }
            else
            {
                throw new SMS.Application.Exceptions.ValidationException("Invalid user type");
            }

            _logger.LogInformation("{UserType} {UserId} registration rejected: {Reason}",
                request.UserType, request.UserId, request.Reason);

            return new ApprovalResultDto
            {
                UserId = request.UserId,
                UserType = request.UserType,
                Status = "Rejected",
                Message = $"Registration rejected. Reason: {request.Reason}"
            };
        }
    }
}
