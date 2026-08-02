using MediatR;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Students.Queries
{
    public class GetStudentByIdQuery : IRequest<StudentDetailsDto>
    {
        public Guid Id { get; set; }
    }

    public class GetStudentByIdQueryHandler : IRequestHandler<GetStudentByIdQuery, StudentDetailsDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IUserManagerService _userManager;

        public GetStudentByIdQueryHandler(IStudentRepository studentRepository, IUserManagerService userManager)
        {
            _studentRepository = studentRepository;
            _userManager = userManager;
        }

        public async Task<StudentDetailsDto> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetStudentWithDetailsAsync(request.Id, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException("Student", request.Id);
            }

            var user = await _userManager.GetUserByIdAsync(student.UserId);

            return new StudentDetailsDto
            {
                Id = student.Id,
                UserId = student.UserId,
                StudentNumber = student.StudentNumber,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                Address = student.Address,
                ProgrammeId = student.ProgrammeId,
                ProgrammeName = student.Programme?.Name,
                CurrentSemesterId = student.CurrentSemesterId,
                CurrentSemesterName = student.CurrentSemester?.Name,
                IsActive = student.IsActive,
                TotalEnrollments = student.Enrollments?.Count ?? 0,
                Grades = student.Grades?.Select(g => new GradeSummaryDto
                {
                    Id = g.Id,
                    UnitId = g.UnitId,
                    UnitName = g.Unit?.Name,
                    UnitCode = g.Unit?.Code,
                    Credits = g.Unit?.Credits ?? 0,
                    Grade = g.GradeValue,
                    Score = g.Score,
                    Remarks = g.Remarks,
                    SemesterId = g.SemesterId,
                    SemesterName = g.Semester?.Name
                }).ToList() ?? new List<GradeSummaryDto>(),
                Username = user?.UserName,
                UserEmail = user?.Email,
                IsEmailVerified = user?.EmailConfirmed ?? false
            };
        }
    }
}
