using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IAssignmentIssueReportRepository : IRepository<AssignmentIssueReport>
    {
        Task<IEnumerable<AssignmentIssueReport>> GetPendingAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<AssignmentIssueReport>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
        Task<IEnumerable<AssignmentIssueReport>> GetByReporterAsync(Guid reporterUserId, CancellationToken cancellationToken = default);
        Task<IEnumerable<AssignmentIssueReport>> GetByOfferingAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);
        Task<IEnumerable<AssignmentIssueReport>> GetPendingByTypeAsync(string assignmentType, CancellationToken cancellationToken = default);
    }
}
