namespace SMS.Domain.Enums
{
    /// <summary>
    /// Categorizes uploads to determine validation rules, size limits,
    /// storage paths, and naming conventions.
    /// </summary>
    public enum UploadCategory
    {
        /// <summary>Student assignment submission (max 20 MB)</summary>
        StudentAssignment = 1,

        /// <summary>Lecturer course notes (max 50 MB)</summary>
        LecturerNotes = 2,

        /// <summary>Course resources / learning materials (max 100 MB)</summary>
        CourseResources = 3,

        /// <summary>Profile / avatar images (max 5 MB)</summary>
        ProfileImage = 4,

        /// <summary>Administrative documents (max 50 MB)</summary>
        AdminDocument = 5,

        /// <summary>Imported datasets (max 100 MB)</summary>
        Dataset = 6,

        /// <summary>Certificate templates (max 10 MB)</summary>
        CertificateTemplate = 7,

        /// <summary>Lecturer assignment briefs (max 50 MB)</summary>
        AssignmentBrief = 8,

        /// <summary>Student supporting documents / evidence (max 20 MB)</summary>
        SupportingDocument = 9,

        /// <summary>General purpose upload (max 10 MB)</summary>
        Default = 100
    }
}
