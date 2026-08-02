using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Lecturers.Queries
{
    public class GetLecturerQuery : IRequest<LecturerDto>
    {
        public Guid LecturerId { get; set; }
    }

    public class GetLecturerQueryHandler : IRequestHandler<GetLecturerQuery, LecturerDto>
    {
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ILogger<GetLecturerQueryHandler> _logger;

        public GetLecturerQueryHandler(
            ILecturerRepository lecturerRepository,
            ILogger<GetLecturerQueryHandler> logger)
        {
            _lecturerRepository = lecturerRepository;
            _logger = logger;
        }

        public async Task<LecturerDto> Handle(GetLecturerQuery request, CancellationToken cancellationToken)
        {
            var lecturer = await _lecturerRepository.GetByIdAsync(request.LecturerId, cancellationToken);

            if (lecturer == null)
                throw new NotFoundException("Lecturer", request.LecturerId);

            return new LecturerDto
            {
                Id = lecturer.Id,
                FirstName = lecturer.FirstName,
                LastName = lecturer.LastName,
                Email = lecturer.Email,
                PhoneNumber = lecturer.PhoneNumber,
                EmployeeNumber = lecturer.EmployeeNumber,
                DepartmentId = lecturer.DepartmentId,
                DepartmentName = lecturer.Department?.Name,
                IsActive = lecturer.IsActive,
                UserId = lecturer.UserId?.ToString(),
                CreatedDate = lecturer.CreatedDate ?? DateTime.UtcNow
            };
        }
    }
}

