using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Lecturers.Commands
{
    public class DeleteLecturerCommand : IRequest<MediatR.Unit>
    {
        public Guid LecturerId { get; set; }
    }

    public class DeleteLecturerCommandHandler : IRequestHandler<DeleteLecturerCommand, MediatR.Unit>
    {
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteLecturerCommandHandler> _logger;

        public DeleteLecturerCommandHandler(
            ILecturerRepository lecturerRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DeleteLecturerCommandHandler> logger)
        {
            _lecturerRepository = lecturerRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(DeleteLecturerCommand request, CancellationToken cancellationToken)
        {
            var lecturer = await _lecturerRepository.GetByIdAsync(request.LecturerId, cancellationToken);
            if (lecturer == null)
                throw new NotFoundException("Lecturer", request.LecturerId);

            // Soft delete - base entity handles IsDeleted flag via DbContext
            await _lecturerRepository.DeleteAsync(lecturer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Delete", "Lecturer", $"Lecturer deleted: {lecturer.EmployeeNumber}");

            _logger.LogInformation("Lecturer deleted: {EmployeeNumber} ({Id})", lecturer.EmployeeNumber, request.LecturerId);

            return MediatR.Unit.Value;
        }
    }
}

