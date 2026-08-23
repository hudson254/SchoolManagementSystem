using System.Collections.Generic;

namespace SMS.Domain.Common
{
    public static class DomainConstants
    {
        public static class GradeValues
        {
            public static readonly Dictionary<string, int> GradePoints = new()
            {
                { "A", 12 }, { "A-", 11 }, { "B+", 10 }, { "B", 9 }, { "B-", 8 },
                { "C+", 7 }, { "C", 6 }, { "C-", 5 }, { "D+", 4 }, { "D", 3 }, { "D-", 2 },
                { "E", 1 }, { "F", 0 }
            };

            public static readonly Dictionary<string, string> GradeLetters = new()
            {
                { "A", "Excellent" }, { "B+", "Very Good" }, { "B", "Good" },
                { "C+", "Fairly Good" }, { "C", "Average" }, { "D+", "Below Average" },
                { "D", "Poor" }, { "E", "Very Poor" }, { "F", "Fail" }
            };
        }

        public static class Roles
        {
            public const string SystemAdministrator = "SystemAdministrator";
            public const string Administrator = "Administrator";
            public const string Coordinator = "Coordinator";
            public const string Lecturer = "Lecturer";
            public const string Student = "Student";
            public const string Receptionist = "Receptionist";
        }

        public static class AcademicStatuses
        {
            public const string Active = "Active";
            public const string Graduating = "Graduating";
            public const string Suspended = "Suspended";
            public const string Expelled = "Expelled";
            public const string Withdrawn = "Withdrawn";
        }

        public static class EnrollmentStatuses
        {
            public const string Enrolled = "Enrolled";
            public const string Dropped = "Dropped";
            public const string Completed = "Completed";
            public const string Pending = "Pending";
        }

        public static class AssignmentStatuses
        {
            public const string Draft = "Draft";
            public const string Published = "Published";
            public const string Closed = "Closed";
        }

        public static class SubmissionStatuses
        {
            public const string Submitted = "Submitted";
            public const string Graded = "Graded";
            public const string Late = "Late";
            public const string Resubmitted = "Resubmitted";
        }
    }
}
