namespace SMS.Domain.Enums
{
    /// <summary>
    /// Tracks the registration/approval lifecycle of a user (Student or Lecturer).
    /// </summary>
    public enum RegistrationStatus
    {
        /// <summary>Account created, awaiting course selection and approval.</summary>
        PendingCourseSelection = 0,

        /// <summary>Course/units selected, awaiting admin approval.</summary>
        PendingApproval = 1,

        /// <summary>Registration approved - full access granted.</summary>
        Approved = 2,

        /// <summary>Registration rejected.</summary>
        Rejected = 3
    }
}
