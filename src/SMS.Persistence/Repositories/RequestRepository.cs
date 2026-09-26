using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Persistence.Repositories
{
    /// <summary>
    /// EF Core persistence for SMS requests. All queries are tenant-scoped via
    /// the global query filter configured in ApplicationDbContext.
    /// </summary>
    public class RequestRepository : BaseRepository<Request>, IRequestRepository
    {
        public RequestRepository(ApplicationDbContext context, ILogger<RequestRepository> logger)
            : base(context, logger)
        {
        }

        public override async Task<Request?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public override Task UpdateAsync(Request entity, CancellationToken cancellationToken = default)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
                _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        public async Task<Request?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(r => r.StatusHistory)
                .Include(r => r.Comments)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<Request?> GetByRequestNumberAsync(string requestNumber, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.RequestNumber == requestNumber, cancellationToken);
        }

        public async Task<IReadOnlyList<RequestStatusHistory>> GetStatusHistoryAsync(Guid requestId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<RequestStatusHistory>()
                .Where(h => h.RequestId == requestId)
                .OrderBy(h => h.PerformedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<RequestType>> GetAllTypesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.RequestTypes
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.DisplayName)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<bool> ExistsByTypeCodeAsync(string code, Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.RequestTypes
                .AsNoTracking()
                .AnyAsync(t => t.Code == code && t.TenantId == tenantId && !t.IsDeleted, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<RequestType?> GetRequestTypeByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.RequestTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<RequestType?> GetByTypeCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.RequestTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Code == code && !t.IsDeleted, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task AddRequestTypeAsync(RequestType type, CancellationToken cancellationToken = default)
        {
            await _context.RequestTypes.AddAsync(type, cancellationToken);
        }

        public async Task UpdateRequestTypeAsync(RequestType type, CancellationToken cancellationToken = default)
        {
            _context.RequestTypes.Update(type);
            await Task.CompletedTask;
        }

        public async Task<IReadOnlyList<RequestComment>> GetCommentsAsync(Guid requestId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<RequestComment>()
                .Where(c => c.RequestId == requestId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<Request> Items, int TotalCount)> GetPagedAsync(
            RequestStatus? status, string? requestType, string? requesterUserId,
            string? assignedUserId, string? search, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(r => r.RequestNumber.Contains(term) || r.Title.Contains(term) ||
                    (r.Description != null && r.Description.Contains(term)));
            }
            if (status.HasValue) query = query.Where(r => r.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(requestType)) query = query.Where(r => r.RequestType == requestType);
            if (!string.IsNullOrWhiteSpace(requesterUserId)) query = query.Where(r => r.RequesterUserId == requesterUserId);
            if (!string.IsNullOrWhiteSpace(assignedUserId)) query = query.Where(r => r.AssignedUserId == assignedUserId);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items.AsReadOnly(), totalCount);
        }

        public async Task<IReadOnlyDictionary<RequestStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
        }

        public async Task<IReadOnlyList<Request>> GetByRequesterUserIdAsync(string requesterUserId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(r => r.RequesterUserId == requesterUserId)
                .OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Request>> GetByAssignedUserIdAsync(string assignedUserId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(r => r.AssignedUserId == assignedUserId)
                .OrderByDescending(r => r.UpdatedAt).ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Request>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.Status == RequestStatus.Submitted || r.Status == RequestStatus.PendingReview ||
                           r.Status == RequestStatus.Assigned || r.Status == RequestStatus.PendingApproval)
                .OrderByDescending(r => r.UpdatedAt).ToListAsync(cancellationToken);
        }

        public async Task<RequestAttachment?> GetAttachmentByIdAsync(Guid attachmentId)
        {
            return await _context.Set<RequestAttachment>()
                .FirstOrDefaultAsync(a => a.Id == attachmentId);
        }

        public async Task<IReadOnlyList<RequestAttachment>> GetAttachmentsByRequestIdAsync(Guid requestId)
        {
            return await _context.Set<RequestAttachment>()
                .Where(a => a.RequestId == requestId)
                .OrderBy(a => a.FileName)
                .ToListAsync();
        }

        public async Task AddAttachmentAsync(RequestAttachment attachment)
        {
            await _context.Set<RequestAttachment>().AddAsync(attachment);
        }

        public void RemoveAttachment(RequestAttachment attachment)
        {
            // Soft delete: the association row is retained (with DeletedAt /
            // DeletedBy populated by the audit interceptor) so attachment
            // history remains auditable. Physical cleanup of the stored blob is
            // a separate, privileged maintenance concern.
            _context.Set<RequestAttachment>().Update(attachment);
        }
    }

    public class RequestTypeRepository : BaseRepository<RequestType>, IRequestTypeRepository
    {
        public RequestTypeRepository(ApplicationDbContext context, ILogger<RequestTypeRepository> logger)
            : base(context, logger) { }

        public async Task<RequestType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(rt => rt.Code == code, cancellationToken);

        public async Task<IReadOnlyList<RequestType>> GetActiveAsync(CancellationToken cancellationToken = default)
            => await _dbSet.Where(rt => rt.IsActive).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// EF Core persistence for request comments. Tenant scoping comes from the
    /// global query filter configured in ApplicationDbContext.
    /// </summary>
    public class RequestCommentRepository : BaseRepository<RequestComment>, IRequestCommentRepository
    {
        public RequestCommentRepository(ApplicationDbContext context, ILogger<RequestCommentRepository> logger)
            : base(context, logger) { }
    }
}
