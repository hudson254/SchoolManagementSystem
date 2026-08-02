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
    public class ReportVerificationRepository : BaseRepository<ReportVerification>, IReportVerificationRepository
    {
        public ReportVerificationRepository(ApplicationDbContext context, ILogger<ReportVerificationRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<ReportVerification?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(rv => rv.VerificationToken == token && !rv.IsDeleted, cancellationToken);
        }

        public async Task<ReportVerification?> GetByReportIdAsync(string reportId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(rv => rv.ReportId == reportId && !rv.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<ReportVerification>> GetByReportTypeAsync(string reportType, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(rv => rv.ReportType == reportType && !rv.IsDeleted)
                .OrderByDescending(rv => rv.GeneratedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ReportVerification>> GetByGeneratedByAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(rv => rv.GeneratedByUserId == userId && !rv.IsDeleted)
                .OrderByDescending(rv => rv.GeneratedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<ReportVerification> records, int totalCount)> GetFilteredAsync(
            string? reportType = null,
            string? reportId = null,
            string? generatedBy = null,
            ReportVerificationStatus? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking().Where(rv => !rv.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(reportType))
                query = query.Where(rv => rv.ReportType == reportType);

            if (!string.IsNullOrWhiteSpace(reportId))
                query = query.Where(rv => rv.ReportId.Contains(reportId));

            if (!string.IsNullOrWhiteSpace(generatedBy))
                query = query.Where(rv => rv.GeneratedByUserId == generatedBy);

            if (status.HasValue)
                query = query.Where(rv => rv.Status == status.Value);

            if (startDate.HasValue)
                query = query.Where(rv => rv.GeneratedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(rv => rv.GeneratedDate <= endDate.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var records = await query
                .OrderByDescending(rv => rv.GeneratedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (records, totalCount);
        }

        public async Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(rv => rv.VerificationToken == token && !rv.IsDeleted, cancellationToken);
        }

        public async Task<bool> ReportIdExistsAsync(string reportId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(rv => rv.ReportId == reportId && !rv.IsDeleted, cancellationToken);
        }

        public async Task<int> GetCountAsync(
            ReportVerificationStatus? status = null,
            string? reportType = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking().Where(rv => !rv.IsDeleted).AsQueryable();

            if (status.HasValue)
                query = query.Where(rv => rv.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(reportType))
                query = query.Where(rv => rv.ReportType == reportType);

            if (startDate.HasValue)
                query = query.Where(rv => rv.GeneratedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(rv => rv.GeneratedDate <= endDate.Value);

            return await query.CountAsync(cancellationToken);
        }
    }
}
