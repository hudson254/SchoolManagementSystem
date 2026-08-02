using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Lecturers.Commands
{
    public class VerifyLecturerCommand : IRequest<LecturerDto>
    {
        public Guid LecturerId { get; set; }
    }

    public class VerifyLecturerCommandHandler : IRequestHandler<VerifyLecturerCommand, LecturerDto>
    {
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<VerifyLecturerCommandHandler> _logger;

        public VerifyLecturerCommandHandler(
            ILecturerRepository lecturerRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<VerifyLecturerCommandHandler> logger)
        {
            _lecturerRepository = lecturerRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<LecturerDto> Handle(VerifyLecturerCommand request, CancellationToken cancellationToken)
        {
            var lecturer = await _lecturerRepository.GetByIdAsync(request.LecturerId, cancellationToken);
            if (lecturer == null)
                throw new NotFoundException("Lecturer", request.LecturerId);

            lecturer.IsActive = true;

            await _lecturerRepository.UpdateAsync(lecturer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Verify", "Lecturer", $"Lecturer verified: {lecturer.EmployeeNumber}");

            _logger.LogInformation("Lecturer verified: {EmployeeNumber}", lecturer.EmployeeNumber);

            return new LecturerDto
            {
                Id = lecturer.Id,
                FirstName = lecturer.FirstName,
                LastName = lecturer.LastName,
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

