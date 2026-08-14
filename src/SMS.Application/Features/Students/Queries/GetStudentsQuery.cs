using MediatR;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Students.Queries
{
    public class GetStudentsQuery : IRequest<PagedResult<StudentDto>>
    {
        public string SearchTerm { get; set; }
        public Guid? ProgrammeId { get; set; }
        public Guid? SemesterId { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetStudentsQueryHandler : IRequestHandler<GetStudentsQuery, PagedResult<StudentDto>>
    {
        private readonly IStudentRepository _studentRepository;

        public GetStudentsQueryHandler(IStudentRepository studentRepository)
        {
            _studentRepository = studentRepository;
        }

        public async Task<PagedResult<StudentDto>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<Domain.Entities.Student> students;

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                students = await _studentRepository.SearchStudentsAsync(request.SearchTerm);
            }
            else if (request.ProgrammeId.HasValue)
            {
                students = await _studentRepository.GetStudentsByProgrammeAsync(request.ProgrammeId.Value, cancellationToken);
            }
            else if (request.SemesterId.HasValue)
            {
                students = await _studentRepository.GetStudentsBySemesterAsync(request.SemesterId.Value, cancellationToken);
            }
            else if (request.IsActive.HasValue)
            {
                students = request.IsActive.Value ? await _studentRepository.GetActiveStudentsAsync() : await _studentRepository.GetAllAsync(cancellationToken);
            }
            else
            {
                students = await _studentRepository.GetAllAsync(cancellationToken);
            }

            var allDtos = students.Select(student => new StudentDto
            {
                Id = student.Id,
                UserId = student.UserId,
                StudentNumber = student.StudentNumber,
                FirstName = student.FirstName,
                MiddleName = student.MiddleName,
                LastName = student.LastName,
                Title = student.Title,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                Address = student.Address,
                ProgrammeId = student.ProgrammeId,
                ProgrammeName = student.Programme?.Name,
                IsActive = student.IsActive
            }).ToList();

            var page = Math.Max(1, request.Page);
            var pageSize = Math.Max(1, request.PageSize);

            return new PagedResult<StudentDto>
            {
                Items = allDtos.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                TotalCount = allDtos.Count,
                PageNumber = page,
                PageSize = pageSize
            };
        }
    }
}
