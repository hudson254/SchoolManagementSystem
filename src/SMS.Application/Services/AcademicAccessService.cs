using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SMS.Application.Common.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Services
{
    /// <inheritdoc cref="IAcademicAccessService"/>
    public class AcademicAccessService : IAcademicAccessService
    {
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IStudentRepository _studentRepository;

        public AcademicAccessService(
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            ILecturerRepository lecturerRepository,
            IStudentRepository studentRepository)
        {
            _currentUserService = currentUserService;
            _lecturerRepository = lecturerRepository;
            _studentRepository = studentRepository;
        }

        public async Task<Lecturer?> GetCurrentLecturerAsync(CancellationToken cancellationToken = default)
        {
            var email = _currentUserService.Email;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var byEmail = await _lecturerRepository.GetLecturerByEmailAsync(email).ConfigureAwait(false);
                if (byEmail != null)
                {
                    return byEmail;
                }
            }

            var userId = _currentUserService.UserId;
            if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var userGuid))
            {
                return await _lecturerRepository.GetByUserIdAsync(userGuid, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        public async Task<Student?> GetCurrentStudentAsync(CancellationToken cancellationToken = default)
        {
            var email = _currentUserService.Email;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var byEmail = await _studentRepository.GetStudentByEmailAsync(email).ConfigureAwait(false);
                if (byEmail != null)
                {
                    return byEmail;
                }
            }

            var userId = _currentUserService.UserId;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var students = await _studentRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
                return students.FirstOrDefault(s => s.UserId == userId);
            }

            return null;
        }

        public bool IsAdminOrCoordinator()
        {
            var roles = _currentUserService.Roles ?? Enumerable.Empty<string>();
            return roles.Any(r =>
                string.Equals(r, "SystemAdministrator", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r, "Coordinator", StringComparison.OrdinalIgnoreCase));
        }

        public bool IsLecturerRole()
        {
            var roles = _currentUserService.Roles ?? Enumerable.Empty<string>();
            return roles.Any(r => string.Equals(r, "Lecturer", StringComparison.OrdinalIgnoreCase));
        }

        public bool IsStudentRole()
        {
            var roles = _currentUserService.Roles ?? Enumerable.Empty<string>();
            return roles.Any(r => string.Equals(r, "Student", StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> LecturerTeachesUnitAsync(Guid unitId, CancellationToken cancellationToken = default)
        {
            if (!IsLecturerRole() && !IsAdminOrCoordinator())
            {
                return false;
            }

            var lecturer = await GetCurrentLecturerAsync(cancellationToken).ConfigureAwait(false);
            if (lecturer == null)
            {
                return false;
            }

            var taughtUnitIds = await _lecturerRepository.GetTaughtUnitIdsAsync(lecturer.Id, cancellationToken).ConfigureAwait(false);
            return taughtUnitIds.Contains(unitId);
        }

        public async Task<bool> LecturerTeachesUnitAsync(Guid lecturerId, Guid unitId, CancellationToken cancellationToken = default)
        {
            var taughtUnitIds = await _lecturerRepository.GetTaughtUnitIdsAsync(lecturerId, cancellationToken).ConfigureAwait(false);
            return taughtUnitIds.Contains(unitId);
        }

        public async Task<bool> StudentEnrolledInUnitAsync(Guid unitId, CancellationToken cancellationToken = default)
        {
            if (!IsStudentRole())
            {
                return false;
            }

            var student = await GetCurrentStudentAsync(cancellationToken).ConfigureAwait(false);
            if (student == null)
            {
                return false;
            }

            var enrolledUnitIds = await _studentRepository.GetEnrolledUnitIdsAsync(student.Id, cancellationToken).ConfigureAwait(false);
            return enrolledUnitIds.Contains(unitId);
        }
    }
}