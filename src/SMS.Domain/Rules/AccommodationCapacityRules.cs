using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Domain.Rules
{
    /// <summary>
    /// Pure capacity-classification rules shared by every accommodation report
    /// summary.
    ///
    /// <para>
    /// Three summary tiles — "At full capacity", "With free space" and "Never
    /// occupied" — depend on how a single house is classified. Each report counts
    /// them over a different house set and a different occupancy window (active
    /// occupants *now*, or occupants *overlapping the selected period*), but the
    /// classification itself never varies. Defining it once here is what stops the
    /// reports from drifting apart: an earlier revision of the occupancy-by-period
    /// report simply omitted the three assignments and silently rendered 0 on tiles
    /// that the house reports populated correctly.
    /// </para>
    /// </summary>
    public static class AccommodationCapacityRules
    {
        /// <summary>
        /// A house is at full capacity when it has a positive capacity and every one
        /// of its spaces is taken.
        /// </summary>
        /// <remarks>
        /// A house whose capacity is zero (or negative — bad data) is deliberately
        /// excluded: with no spaces to fill, "full" is meaningless, and counting it
        /// would inflate the tile. It still counts towards "with free space" only
        /// when <c>occupiedCount &lt; capacity</c> holds, which a zero-capacity house
        /// with zero occupants satisfies — the two tiles are computed independently
        /// and are not required to sum to the house total.
        /// </remarks>
        public static bool IsAtFullCapacity(int capacity, int occupiedCount)
            => capacity > 0 && occupiedCount >= capacity;

        /// <summary>
        /// A house has free space when it is not filled to capacity. Over-capacity
        /// (occupants outnumbering spaces — possible when capacity is reduced after
        /// allocation) counts as full, not as having free space.
        /// </summary>
        public static bool HasAvailableCapacity(int capacity, int occupiedCount)
            => occupiedCount < capacity;

        /// <summary>
        /// A house was never occupied when it has no assignment record at all. This
        /// is deliberately an all-time question, not a window question, so the
        /// "Never occupied" tile keeps one meaning across every report.
        /// </summary>
        public static bool WasNeverOccupied(int totalRecords) => totalRecords == 0;

        /// <summary>Counts the houses classified as full (see <see cref="IsAtFullCapacity"/>).</summary>
        public static int CountAtFullCapacity<T>(
            IEnumerable<T> houses, Func<T, int> capacity, Func<T, int> occupiedCount)
            => Count(houses, h => IsAtFullCapacity(capacity(h), occupiedCount(h)));

        /// <summary>Counts the houses that still have free space (see <see cref="HasAvailableCapacity"/>).</summary>
        public static int CountWithAvailableCapacity<T>(
            IEnumerable<T> houses, Func<T, int> capacity, Func<T, int> occupiedCount)
            => Count(houses, h => HasAvailableCapacity(capacity(h), occupiedCount(h)));

        /// <summary>Counts the houses with no assignment record at all (see <see cref="WasNeverOccupied"/>).</summary>
        public static int CountNeverOccupied<T>(IEnumerable<T> houses, Func<T, int> totalRecords)
            => Count(houses, h => WasNeverOccupied(totalRecords(h)));

        private static int Count<T>(IEnumerable<T>? houses, Func<T, bool> predicate)
            => houses == null ? 0 : houses.Count(predicate);
    }
}
