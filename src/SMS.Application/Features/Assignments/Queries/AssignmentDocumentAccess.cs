using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Assignments.Queries
{
    /// <summary>
    /// Shared access checks for assignment question documents: the owning
    /// lecturer, a lecturer teaching the assignment's unit, admin/coordinator
    /// roles, or a student enrolled in the unit. The chain is resolved from
    /// persisted relationships only.
    /// </summary>
    public static class AssignmentDocumentAccess
    {
        public static async Task<bool> CanAccessAsync(
            SMS.Domain.Entities.Assignment assignment,
            IAcademicAccessService academicAccessService,
            CancellationToken cancellationToken)
        {
            if (academicAccessService.IsAdminOrCoordinator())
            {
                return true;
            }

            if (academicAccessService.IsLecturerRole())
            {
                var lecturer = await academicAccessService.GetCurrentLecturerAsync(cancellationToken);
                if (lecturer == null)
                {
                    return false;
                }

                if (assignment.LecturerId.HasValue && assignment.LecturerId.Value == lecturer.Id)
                {
                    return true;
                }

                return await academicAccessService.LecturerTeachesUnitAsync(
                    lecturer.Id, assignment.UnitId, cancellationToken);
            }

            if (academicAccessService.IsStudentRole())
            {
                return await academicAccessService.StudentEnrolledInUnitAsync(
                    assignment.UnitId, cancellationToken);
            }

            return false;
        }
    }
}
