namespace SMS.Domain.Common
{
    public static class DomainConstants
    {
        public const int MaxNameLength = 100;
        public const int MaxCodeLength = 20;
        public const int MaxDescriptionLength = 1000;
        public const int MaxPhoneLength = 20;
        public const int MaxEmailLength = 100;
        public const int MaxAddressLength = 500;
        public const int MaxNotesLength = 500;
        public const int MaxFilePathLength = 500;

        public static class StudentStatus
        {
            public const string Active = "Active";
            public const string Suspended = "Suspended";
            public const string Graduated = "Graduated";
            public const string Withdrawn = "Withdrawn";
            public const string Probation = "Probation";
        }

        public static class EnrollmentStatus
        {
            public const string Enrolled = "Enrolled";
            public const string Dropped = "Dropped";
            public const string Completed = "Completed";
            public const string InProgress = "InProgress";
        }

        public static class AssignmentStatus
        {
            public const string Draft = "Draft";
            public const string Published = "Published";
            public const string Closed = "Closed";
            public const string Archived = "Archived";
        }

        public static class SubmissionStatus
        {
            public const string Pending = "Pending";
            public const string Submitted = "Submitted";
            public const string Graded = "Graded";
            public const string Late = "Late";
        }

        public static class GradeValues
        {
            public const string A = "A";
            public const string A_minus = "A-";
            public const string B_plus = "B+";
            public const string B = "B";
            public const string B_minus = "B-";
            public const string C_plus = "C+";
            public const string C = "C";
            public const string C_minus = "C-";
            public const string D = "D";
            public const string F = "F";

            public static readonly Dictionary<string, decimal> GradePoints = new()
            {
                { A, 4.0m },
                { A_minus, 3.7m },
                { B_plus, 3.3m },
                { B, 3.0m },
                { B_minus, 2.7m },
                { C_plus, 2.3m },
                { C, 2.0m },
                { C_minus, 1.7m },
                { D, 1.0m },
                { F, 0.0m }
            };
        }

        public static class RoomTypes
        {
            public const string Single = "Single";
            public const string Double = "Double";
            public const string Dormitory = "Dormitory";
        }

        public static class AccommodationStatus
        {
            public const string Active = "Active";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
        }

        public static class NotificationTypes
        {
            public const string Info = "Info";
            public const string Warning = "Warning";
            public const string Success = "Success";
            public const string Error = "Error";
        }
    }
}