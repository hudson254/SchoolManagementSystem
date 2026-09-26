using SMS.Domain.Common;
using System.Collections.Generic;

namespace SMS.Application.Features.Enrollments.Commands
{
    /// <summary>
    /// Stable machine-readable request type codes owned by the Enrollment
    /// adapter. These are the only codes the enrollment request endpoint
    /// accepts, so a request raised through the Enrollment module can never be
    /// mis-typed as an unrelated workflow.
    /// </summary>
    public static class EnrollmentRequestTypes
    {
        /// <summary>Move a student to a different course.</summary>
        public const string CourseChange = "ENROLLMENT_COURSE_CHANGE";

        /// <summary>General enrollment exception (e.g. late add, overload).</summary>
        public const string Exception = "ENROLLMENT_EXCEPTION";

        /// <summary>Correct the unit/semester attached to an enrollment.</summary>
        public const string UnitEnrollmentCorrection = "ENROLLMENT_UNIT_CORRECTION";

        /// <summary>Request to cancel an enrollment that has not yet been actioned.</summary>
        public const string Cancellation = "ENROLLMENT_CANCELLATION";

        public static readonly IReadOnlyCollection<string> All = new[]
        {
            CourseChange, Exception, UnitEnrollmentCorrection, Cancellation
        };
    }
}