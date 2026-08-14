using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
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
    /// Repository for managing configurable title records in the database.
    /// </summary>
    public class TitleRepository : BaseRepository<Title>, ITitleRepository
    {
        public TitleRepository(ApplicationDbContext context, ILogger<TitleRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Title>> GetActiveTitlesAsync(string language = "en", CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.IsActive && t.Language == language)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Code)
                .ToListAsync(cancellationToken);
        }

        public async Task<Title?> GetByCodeAsync(string code, string language = "en", CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            return await _dbSet
                .Where(t => t.IsActive &&
                            t.NormalizedCode == code.ToUpperInvariant() &&
                            t.Language == language)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Dictionary<string, List<Title>>> GetTitlesByCategoryAsync(string language = "en", CancellationToken cancellationToken = default)
        {
            var titles = await _dbSet
                .Where(t => t.IsActive && t.Language == language)
                .ToListAsync(cancellationToken);

            return titles
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.SortOrder).ThenBy(t => t.Code).ToList());
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync(string language = "en", CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.IsActive && t.Language == language)
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsRecognizedTitleAsync(string code, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return await _dbSet
                .AnyAsync(t => t.IsActive && t.NormalizedCode == code.ToUpperInvariant(), cancellationToken);
        }
    }
}
