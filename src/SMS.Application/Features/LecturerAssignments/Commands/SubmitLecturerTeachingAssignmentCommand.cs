using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.LecturerAssignments.Commands
{
    /// <summary>
    /// Submits a lecturer's teaching assignment request after initial registration.
    /// Creates UnitAllocation records for selected units and sets
    /// status to PendingApproval.
    /// </summary>
    public class SubmitLecturerTeachingAssignmentCommand : IRequest<TeachingAssignmentResultDto>
    {
        public List<Guid> CourseIds { get; set; } = new();
        public List<Guid> UnitIds { get; set; } = new();
        public Guid? SemesterId { get; set; }
    }

    public class SubmitLecturerTeachingAssignmentCommandValidator
        : AbstractValidator<SubmitLecturerTeachingAssignmentCommand>
    {
        public SubmitLecturerTeachingAssignmentCommandValidator()
        {
            RuleFor(x => x.CourseIds)
                .NotEmpty().WithMessage("At least one course must be selected");

            RuleFor(x => x.UnitIds)
                .NotEmpty().WithMessage("At least one unit must be selected");
        }
    }

    public class TeachingAssignmentResultDto
    {
        public Guid LecturerId { get; set; }
        public int CoursesSelected { get; set; }
        public int UnitsSelected { get; set; }
        public string Status { get; set; } = "PendingApproval";
        public string Message { get; set; } = string.Empty;
    }

    public class SubmitLecturerTeachingAssignmentCommandHandler
        : IRequestHandler<SubmitLecturerTeachingAssignmentCommand, TeachingAssignmentResultDto>
    {
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IUnitAllocationRepository _unitAllocationRepository;
        private readonly IAuditService _auditService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SubmitLecturerTeachingAssignmentCommandHandler> _logger;

        public SubmitLecturerTeachingAssignmentCommandHandler(
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILecturerRepository lecturerRepository,
            ICourseRepository courseRepository,
            IUnitRepository unitRepository,
            IUnitAllocationRepository unitAllocationRepository,
            IAuditService auditService,
            IUnitOfWork unitOfWork,
            ILogger<SubmitLecturerTeachingAssignmentCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _lecturerRepository = lecturerRepository;
            _courseRepository = courseRepository;
            _unitRepository = unitRepository;
            _unitAllocationRepository = unitAllocationRepository;
            _auditService = auditService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<TeachingAssignmentResultDto> Handle(
            SubmitLecturerTeachingAssignmentCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User not authenticated");

            var userEmail = _currentUserService.Email;
            if (string.IsNullOrEmpty(userEmail))
                throw new UnauthorizedAccessException("User email not found");

            var lecturer = await _lecturerRepository.GetLecturerByEmailAsync(userEmail);
            if (lecturer == null)
                throw new NotFoundException("Lecturer record not found for current user");

            // Verify lecturer is in PendingCourseSelection status
            if (lecturer.RegistrationStatus != RegistrationStatus.PendingCourseSelection)
                throw new SMS.Application.Exceptions.ValidationException(
                    $"Cannot submit teaching assignment. Current status: {lecturer.RegistrationStatus}. " +
                    "Expected: PendingCourseSelection");

            // Validate courses exist
            var validCourses = new List<Course>();
            foreach (var courseId in request.CourseIds)
            {
                var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken);
                if (course == null || !course.IsActive)
                    throw new NotFoundException("Course", courseId);
                validCourses.Add(course);
            }

            // Validate units exist and are active
            var validUnits = new List<SMS.Domain.Entities.Unit>();
            foreach (var unitId in request.UnitIds)
            {
                var unit = await _unitRepository.GetByIdAsync(unitId, cancellationToken);
                if (unit == null || !unit.IsActive)
                    throw new NotFoundException("Unit", unitId);
                validUnits.Add(unit);
            }

            // Create UnitAllocation records for each selected unit
            foreach (var unit in validUnits)
            {
                var allocation = new UnitAllocation
                {
                    LecturerId = lecturer.Id,
                    UnitId = unit.Id,
                    SemesterId = request.SemesterId ?? Guid.Empty,
                    AllocationDate = DateTime.UtcNow,
                    Status = "PendingApproval"
                };
                await _unitAllocationRepository.AddAsync(allocation, cancellationToken);
            }

            // Update lecturer status to PendingApproval
            lecturer.RegistrationStatus = RegistrationStatus.PendingApproval;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var courseNames = string.Join(", ", validCourses.Select(c => c.Code));
            await _auditService.LogAsync("SubmitTeachingAssignment", lecturer.Id.ToString(),
                $"Lecturer submitted teaching assignment for courses [{courseNames}] ({validUnits.Count} units)");

            _logger.LogInformation(
                "Lecturer {LecturerId} submitted teaching assignment for {CourseCount} courses with {UnitCount} units",
                lecturer.Id, validCourses.Count, validUnits.Count);

            return new TeachingAssignmentResultDto
            {
                LecturerId = lecturer.Id,
                CoursesSelected = validCourses.Count,
                UnitsSelected = validUnits.Count,
                Status = "PendingApproval",
                Message = $"Teaching assignment submitted for {validUnits.Count} units across {validCourses.Count} courses. Awaiting approval."
            };
        }
    }
}
