using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Repository for managing configurable title records.
    /// Titles are stored in the database and can be managed by administrators
    /// without modifying source code.
    /// </summary>
    public interface ITitleRepository : IRepository<Title>
    {
        /// <summary>
        /// Gets all active titles for the specified language.
        /// </summary>
        Task<IEnumerable<Title>> GetActiveTitlesAsync(string language = "en", CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a title by its code (case-insensitive).
        /// </summary>
        Task<Title?> GetByCodeAsync(string code, string language = "en", CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all titles grouped by category for the specified language.
        /// </summary>
        Task<Dictionary<string, List<Title>>> GetTitlesByCategoryAsync(string language = "en", CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all distinct categories for the specified language.
        /// </summary>
        Task<IEnumerable<string>> GetCategoriesAsync(string language = "en", CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a title code is recognized (case-insensitive, any language).
        /// </summary>
        Task<bool> IsRecognizedTitleAsync(string code, CancellationToken cancellationToken = default);
    }
}
