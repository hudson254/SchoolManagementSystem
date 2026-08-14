using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Units.Queries
{
    public class GetUnitLecturersQuery : IRequest<IEnumerable<LecturerDto>>
    {
        public Guid UnitId { get; set; }
    }

    public class GetUnitLecturersHandler : IRequestHandler<GetUnitLecturersQuery, IEnumerable<LecturerDto>>
    {
        private readonly IUnitAllocationRepository _unitAllocationRepository;
        private readonly ILogger<GetUnitLecturersHandler> _logger;

        public GetUnitLecturersHandler(IUnitAllocationRepository unitAllocationRepository, ILogger<GetUnitLecturersHandler> logger)
        {
            _unitAllocationRepository = unitAllocationRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<LecturerDto>> Handle(GetUnitLecturersQuery request, CancellationToken cancellationToken)
        {
            var allocations = await _unitAllocationRepository.GetByUnitAsync(request.UnitId);
            return allocations
                .Where(a => a.Lecturer != null && !a.Lecturer.IsDeleted)
                .Select(a => new LecturerDto
                {
                    Id = a.Lecturer!.Id,
                    FirstName = a.Lecturer.FirstName,
                    MiddleName = a.Lecturer.MiddleName,
                    LastName = a.Lecturer.LastName,
                    Title = a.Lecturer.Title,
                    Email = a.Lecturer.Email,
                    EmployeeNumber = a.Lecturer.EmployeeNumber,
                    PhoneNumber = a.Lecturer.PhoneNumber,
                    DepartmentId = a.Lecturer.DepartmentId,
                    DepartmentName = a.Lecturer.Department?.Name,
                    IsActive = a.Lecturer.IsActive
                })
                .Distinct()
                .ToList();
        }
    }

    public class GetUnitStudentsQuery : IRequest<IEnumerable<StudentDto>>
    {
        public Guid UnitId { get; set; }
        public int? Semester { get; set; }
    }

    public class GetUnitStudentsHandler : IRequestHandler<GetUnitStudentsQuery, IEnumerable<StudentDto>>
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ILogger<GetUnitStudentsHandler> _logger;

        public GetUnitStudentsHandler(IEnrollmentRepository enrollmentRepository, ILogger<GetUnitStudentsHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<StudentDto>> Handle(GetUnitStudentsQuery request, CancellationToken cancellationToken)
        {
            var enrollments = await _enrollmentRepository.GetEnrollmentsAsync(cancellationToken);
            var filtered = enrollments
                .Where(e => e.UnitId == request.UnitId && e.Student != null && !e.Student.IsDeleted)
                .Select(e => e.Student!)
                .Distinct()
                .ToList();

            return filtered.Select(s => new StudentDto
            {
                Id = s.Id,
                StudentNumber = s.StudentNumber,
                FirstName = s.FirstName,
                MiddleName = s.MiddleName,
                LastName = s.LastName,
                Title = s.Title,
                Email = s.Email,
                PhoneNumber = s.PhoneNumber,
                Address = s.Address,
                ProgrammeId = s.ProgrammeId,
                ProgrammeName = s.Programme?.Name,
                IsActive = !s.IsDeleted
            }).ToList();
        }
    }
}
