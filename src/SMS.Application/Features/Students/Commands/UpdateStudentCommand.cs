using FluentValidation;
using MediatR;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Students.Commands
{
    public class UpdateStudentCommand : IRequest<StudentDto>
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Guid? ProgrammeId { get; set; }
        public Guid? CurrentSemesterId { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
    {
        public UpdateStudentCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Student ID is required");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required");
        }
    }

    public class UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand, StudentDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IUserManagerService _userManager;
        private readonly IAuditService _auditService;

        public UpdateStudentCommandHandler(
            IStudentRepository studentRepository,
            IUserManagerService userManager,
            IAuditService auditService)
        {
            _studentRepository = studentRepository;
            _userManager = userManager;
            _auditService = auditService;
        }

        public async Task<StudentDto> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetStudentWithDetailsAsync(request.Id, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException("Student", request.Id);
            }

            // Update properties
            student.FirstName = request.FirstName ?? student.FirstName;
            student.LastName = request.LastName ?? student.LastName;
            student.PhoneNumber = request.PhoneNumber ?? student.PhoneNumber;
            student.Address = request.Address ?? student.Address;
            if (request.DateOfBirth.HasValue)
                student.DateOfBirth = request.DateOfBirth.Value;
            if (request.ProgrammeId.HasValue)
                student.ProgrammeId = request.ProgrammeId.Value;
            if (request.CurrentSemesterId.HasValue)
                student.CurrentSemesterId = request.CurrentSemesterId.Value;
            student.IsActive = request.IsActive;

            await _studentRepository.UpdateAsync(student, cancellationToken);

            await _auditService.LogAsync("Update", "Student", $"Student updated: {student.StudentNumber}");

            return new StudentDto
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
                IsActive = student.IsActive
            };
        }
    }
}

