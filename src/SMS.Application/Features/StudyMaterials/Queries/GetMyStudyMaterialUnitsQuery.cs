using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.StudyMaterials.Queries
{
    /// <summary>
    /// The units the authenticated caller may open in Study Materials.
    ///
    /// Deliberately derived from the SAME persisted relationships the handlers use
    /// to authorize list/upload/download (GetTaughtUnitIdsAsync and
    /// GetEnrolledUnitIdsAsync) rather than from the dashboard payloads. The
    /// dashboard builds its unit lists from a narrower set of tables, so reusing it
    /// would let the selector disagree with the authorization decision: it could hide
    /// a unit the API would allow, or show one the API would reject. Sharing a single
    /// derivation removes that divergence by construction.
    ///
    /// Tenant isolation is inherited from the global EF Core query filter on Unit,
    /// so a unit id belonging to another tenant never appears.
    /// </summary>
    public class GetMyStudyMaterialUnitsQuery : IRequest<IReadOnlyList<StudyMaterialUnitDto>>
    {
    }

    public class GetMyStudyMaterialUnitsQueryHandler
        : IRequestHandler<GetMyStudyMaterialUnitsQuery, IReadOnlyList<StudyMaterialUnitDto>>
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IAcademicAccessService _academicAccessService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILogger<GetMyStudyMaterialUnitsQueryHandler> _logger;

        public GetMyStudyMaterialUnitsQueryHandler(
            IUnitRepository unitRepository,
            ILecturerRepository lecturerRepository,
            IStudentRepository studentRepository,
            IAcademicAccessService academicAccessService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILogger<GetMyStudyMaterialUnitsQueryHandler> logger)
        {
            _unitRepository = unitRepository;
            _lecturerRepository = lecturerRepository;
            _studentRepository = studentRepository;
            _academicAccessService = academicAccessService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<StudyMaterialUnitDto>> Handle(
            GetMyStudyMaterialUnitsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? "unknown";

            // Taught units. An Administrator/Coordinator who also holds a lecturer
            // profile gets exactly the set the upload handler would accept for them.
            var taughtUnitIds = new HashSet<Guid>();
            if (_academicAccessService.IsLecturerRole() || _academicAccessService.IsAdminOrCoordinator())
            {
                var lecturer = await _academicAccessService.GetCurrentLecturerAsync(cancellationToken);
                if (lecturer != null && lecturer.RegistrationStatus == RegistrationStatus.Approved)
                {
                    // Derived from the same persisted relationships the mutation
                    // handlers authorize against, so the selector can never offer a
                    // unit the API would refuse. A lecturer whose registration is
                    // still PendingApproval is offered nothing at all: the selection
                    // is persisted and visible on the dashboard, but teaching has
                    // not been approved yet.
                    var ids = await _lecturerRepository.GetTaughtUnitIdsAsync(lecturer.Id, cancellationToken);
                    taughtUnitIds = new HashSet<Guid>(ids);
                }
                else if (lecturer != null)
                {
                    _logger.LogInformation(
                        "Study material units withheld for {UserId}: lecturer {LecturerId} is {RegistrationStatus}, not approved",
                        userId, lecturer.Id, lecturer.RegistrationStatus);
                }
            }

            // Enrolled units.
            var enrolledUnitIds = new HashSet<Guid>();
            if (_academicAccessService.IsStudentRole())
            {
                var student = await _academicAccessService.GetCurrentStudentAsync(cancellationToken);
                if (student != null)
                {
                    var ids = await _studentRepository.GetEnrolledUnitIdsAsync(student.Id, cancellationToken);
                    enrolledUnitIds = new HashSet<Guid>(ids);
                }
            }

            // A user with neither relationship (e.g. Receptionist) is rejected rather than
            // served an empty list, so the endpoint cannot be used to probe for units
            // the caller has no relationship with.
            if (!_academicAccessService.IsLecturerRole() &&
                !_academicAccessService.IsStudentRole() &&
                !_academicAccessService.IsAdminOrCoordinator())
            {
                _logger.LogWarning(
                    "Study material unit listing refused for {UserId}: role has no academic relationship",
                    userId);
                throw new ForbiddenException("StudyMaterial", userId);
            }

            // Authorized role but no persisted relationship yet: an empty list is the
            // correct, non-disclosing answer.
            if (taughtUnitIds.Count == 0 && enrolledUnitIds.Count == 0)
            {
                return Array.Empty<StudyMaterialUnitDto>();
            }

            var allUnitIds = taughtUnitIds.Union(enrolledUnitIds).ToList();
            var units = await _unitRepository.GetUnitsByIdsAsync(allUnitIds, cancellationToken);

            var result = units
                .Select(u => new StudyMaterialUnitDto
                {
                    UnitId = u.Id,
                    Code = u.Code,
                    Name = u.Name,
                    Credits = u.Credits,
                    CourseId = u.CourseId,
                    CourseName = u.Course?.Name ?? string.Empty,
                    // A unit reachable both ways is still a teaching unit for this
                    // caller, so the upload affordance stays available.
                    AccessRole = taughtUnitIds.Contains(u.Id) ? "Lecturer" : "Student"
                })
                .OrderBy(u => u.Code)
                .ThenBy(u => u.Name)
                .ToList();

            _logger.LogInformation(
                "Study material units listed for {UserId}: {Count} unit(s) ({TaughtCount} taught, {EnrolledCount} enrolled)",
                userId, result.Count, taughtUnitIds.Count, enrolledUnitIds.Count);

            return result;
        }
    }
}