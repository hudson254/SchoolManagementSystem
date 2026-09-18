namespace SMS.Domain.Enums
{
    /// <summary>
    /// Status of an OMS CSV import batch (two-stage validate/confirm workflow).
    /// Development default per Documentation/OMS/OMS_OPEN_REQUIREMENTS.md (#23-#26).
    /// </summary>
    public enum OrderImportStatus
    {
        /// <summary>Uploaded and parsed; rows are being/have been validated.</summary>
        PendingValidation = 1,

        /// <summary>Validation finished; rows ready for the confirm step.</summary>
        Validated = 2,

        /// <summary>Rows imported transactionally.</summary>
        Imported = 3,

        /// <summary>Validation or import failed (row errors recorded).</summary>
        Failed = 4
    }
}
