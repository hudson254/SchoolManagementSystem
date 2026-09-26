using SMS.Domain.Common;
using System.Collections.Generic;

namespace SMS.Application.Features.Assignments.Commands
{
    /// <summary>
    /// Stable machine-readable assignment request type codes owned by the
    /// Assignment adapter. These are RequestType.Code values seeded by the OMS
    /// request-type seeder, not a parallel configuration system.
    ///
    /// Note: there is deliberately NO "submit" code. Ordinary assignment
    /// submission and publication is already handled by the Assignment module's
    /// own workflow and must not be routed through OMS.
    /// </summary>
    public static class AssignmentRequestTypes
    {
        /// <summary>Ask for the deadline to be moved.</summary>
        public const string Extension = "ASSIGNMENT_EXTENSION";

        /// <summary>Ask for an assignment to be reopened for resubmission.</summary>
        public const string Reopen = "ASSIGNMENT_REOPEN";

        /// <summary>Ask for a published assignment's details to be corrected.</summary>
        public const string Correction = "ASSIGNMENT_CORRECTION";

        /// <summary>Administrative exception against an assignment.</summary>
        public const string Exception = "ASSIGNMENT_EXCEPTION";

        public static readonly IReadOnlyCollection<string> All = new[]
        {
            Extension, Reopen, Correction, Exception
        };
    }
}