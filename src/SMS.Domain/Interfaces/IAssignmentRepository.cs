using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IAssignmentRepository : IRepository<Assignment>
    {
        Task<IEnumerable<Assignment>> GetAssignmentsByUnitAsync(Guid unitId);
        Task<IEnumerable<Assignment>> GetAssignmentsByStudentAsync(Guid studentId);
        Task<IEnumerable<Assignment>> GetUpcomingAssignmentsAsync(int days);
        Task<Assignment> GetAssignmentWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Assignment>> GetAssignmentsAsync(int page, int pageSize, Guid? unitId, Guid? studentId, CancellationToken cancellationToken = default);
        Task<int> CountAssignmentsAsync(Guid? unitId, Guid? studentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<AssignmentSubmission>> GetSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken = default);
        Task<AssignmentSubmission> GetSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default);
        Task<AssignmentSubmission> GetSubmissionWithDetailsAsync(Guid submissionId, CancellationToken cancellationToken = default);
        Task<bool> HasSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken = default);
        Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid unitId, CancellationToken cancellationToken = default);
        Task<AssignmentSubmission> AddSubmissionAsync(AssignmentSubmission submission, CancellationToken cancellationToken = default);
        Task UpdateSubmission(AssignmentSubmission submission, CancellationToken cancellationToken = default);
        Task<IEnumerable<Assignment>> GetUpcomingDeadlinesAsync(Guid studentId, int days, CancellationToken cancellationToken = default);
        Task<int> CountAllAsync(CancellationToken cancellationToken = default);
    }
}
