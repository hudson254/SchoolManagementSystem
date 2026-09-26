using SMS.Domain.Common;
using System.Collections.Generic;

namespace SMS.Application.Features.Accommodation.Commands
{
    /// <summary>
    /// Stable machine-readable accommodation request type codes owned by the
    /// Accommodation adapter. These are RequestType.Code values seeded by the
    /// OMS request-type seeder, not a parallel configuration system.
    /// </summary>
    public static class AccommodationRequestTypes
    {
        /// <summary>Request a move to a different house or lane.</summary>
        public const string Transfer = "ACCOMMODATION_TRANSFER";

        /// <summary>Request an allocation where none currently exists.</summary>
        public const string Allocation = "ACCOMMODATION_ALLOCATION";

        /// <summary>Administrative exception against an existing allocation.</summary>
        public const string Exception = "ACCOMMODATION_EXCEPTION";

        /// <summary>Correct details on an existing allocation.</summary>
        public const string Correction = "ACCOMMODATION_CORRECTION";

        public static readonly IReadOnlyCollection<string> All = new[]
        {
            Transfer, Allocation, Exception, Correction
        };
    }
}