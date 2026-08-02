using MediatR;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Students.Queries
{
    public class GetStudentsQuery : IRequest<PagedResult<StudentDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? AcademicStatus { get; set; }
        public Guid? ProgrammeId { get; set; }
        public bool? IsEnrolled { get; set; }
        public string SortBy { get; set; } = "CreatedDate";
        public bool SortDescending { get; set; } = false;
    }

    public class GetStudentsQueryHandler : IRequestHandler<GetStudentsQuery, PagedResult<StudentDto>>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<GetStudentsQueryHandler> _logger;

        public GetStudentsQueryHandler(
            IStudentRepository studentRepository,
            ILogger<GetStudentsQueryHandler> logger)
        {
            _studentRepository = studentRepository;
            _logger = logger;
        }

        public async Task<PagedResult<StudentDto>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
        {
            var students = await _studentRepository.GetStudentsAsync(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                request.AcademicStatus,
                request.ProgrammeId,
                request.IsEnrolled,
                request.SortBy,
                request.SortDescending,
                cancellationToken);

            var totalCount = await _studentRepository.CountStudentsAsync(
                request.SearchTerm,
                request.AcademicStatus,
                request.ProgrammeId,
                request.IsEnrolled,
                cancellationToken);

            var dtos = students.Select(s => new StudentDto
            {
                Id = s.Id,
                UserId = s.UserId,
                StudentNumber = s.StudentNumber,
                FirstName = s.User.FirstName,
                LastName = s.User.LastName,
                Email = s.User.Email ?? string.Empty,
                PhoneNumber = s.User.PhoneNumber ?? string.Empty,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                Address = s.Address,
                EnrollmentDate = s.EnrollmentDate,
                ProgrammeId = s.ProgrammeId,
                ProgrammeName = s.Programme?.Name,
                AcademicStatus = s.AcademicStatus,
                IsEnrolled = s.IsEnrolled,
                CumulativeGPA = s.CumulativeGPA,
                TotalCreditsEarned = s.TotalCreditsEarned,
                CreatedDate = s.CreatedDate
            }).ToList();

            return new PagedResult<StudentDto>
            {
                Items = dtos,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
    }
}