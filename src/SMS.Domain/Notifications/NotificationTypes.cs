using System;

namespace SMS.Domain.Notifications
{
    /// <summary>
    /// Canonical notification types.
    /// <para>
    /// These values are the persisted <c>Notifications.Type</c> discriminant. They are
    /// deliberately close to the existing free-text values ("System", "Registration",
    /// "OmsRequest") so that existing rows keep rendering, and the frontend maps any
    /// unknown value onto a neutral presentation rather than failing.
    /// </para>
    /// </summary>
    public static class NotificationTypes
    {
        // ── Academic ────────────────────────────────────────────────────────
        public const string Course = "Course";
        public const string Unit = "Unit";
        public const string Enrollment = "Enrollment";
        public const string Assignment = "Assignment";
        public const string AssignmentSubmission = "AssignmentSubmission";
        public const string AssignmentIssue = "AssignmentIssue";
        public const string LectureNotes = "LectureNotes";
        public const string Grade = "Grade";
        public const string Announcement = "Announcement";
        public const string Certificate = "Certificate";

        // ── Accommodation ───────────────────────────────────────────────────
        public const string Accommodation = "Accommodation";

        // ── Requests / workflow ─────────────────────────────────────────────
        public const string PermissionRequest = "PermissionRequest";
        public const string Request = "Request";

        /// <summary>Pre-existing OMS request type, preserved so old rows keep matching.</summary>
        public const string OmsRequest = "OmsRequest";

        // ── Identity / security ─────────────────────────────────────────────
        public const string Registration = "Registration";
        public const string AccountApproval = "AccountApproval";
        public const string Security = "Security";

        // ── System ──────────────────────────────────────────────────────────
        public const string System = "System";
        public const string Maintenance = "Maintenance";

        /// <summary>Fallback used when a caller supplies no type.</summary>
        public const string Default = System;

        /// <summary>Every known type.</summary>
        public static readonly string[] All =
        {
            Course, Unit, Enrollment, Assignment, AssignmentSubmission, AssignmentIssue,
            LectureNotes, Grade, Announcement, Certificate, Accommodation,
            PermissionRequest, Request, OmsRequest, Registration, AccountApproval,
            Security, System, Maintenance
        };

        /// <summary>Coerces an arbitrary value to a known type, defaulting to System.</summary>
        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Default;

            var trimmed = value.Trim();
            foreach (var known in All)
            {
                if (string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase))
                    return known;
            }

            return Default;
        }

        /// <summary>
        /// Default priority for a type. Centralised so that "how urgent is this class
        /// of event" is decided in exactly one place rather than re-invented per call site.
        /// </summary>
        public static string DefaultPriorityFor(string type) => Normalize(type) switch
        {
            AssignmentIssue => NotificationPriorities.Important,
            AssignmentSubmission => NotificationPriorities.Informational,
            Security => NotificationPriorities.Critical,
            AccountApproval => NotificationPriorities.Important,
            Maintenance => NotificationPriorities.Critical,
            Announcement => NotificationPriorities.Important,
            Certificate => NotificationPriorities.Informational,
            Accommodation => NotificationPriorities.Normal,
            PermissionRequest or Request or OmsRequest => NotificationPriorities.Normal,
            Enrollment or Course or Unit => NotificationPriorities.Informational,
            _ => NotificationPriorities.Normal
        };
    }
}