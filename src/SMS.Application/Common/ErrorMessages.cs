using System.Collections.Generic;

namespace SMS.Application.Common
{
    /// <summary>
    /// Provides user-friendly, localized error messages for the application.
    /// Messages explain what happened and what the user can do next.
    /// Supports localization for future multilingual deployments.
    /// </summary>
    public static class ErrorMessages
    {
        // Authentication Messages
        public const string InvalidCredentials = "The email or password you entered is incorrect. Please check your credentials and try again.";
        public const string AccessDenied = "You do not have permission to perform this action. Please contact your administrator if you believe this is an error.";
        public const string SessionExpired = "Your session has expired. Please log in again to continue.";
        public const string AccountLocked = "Your account has been temporarily locked due to multiple failed login attempts. Please try again in 15 minutes.";
        public const string AccountDisabled = "Your account has been disabled. Please contact your administrator for assistance.";
        public const string TokenExpired = "Your session has expired. Please log in again to continue.";
        public const string TokenInvalid = "Your authentication token is invalid. Please log in again.";
        public const string PasswordResetFailed = "Unable to reset your password. The reset link may have expired or is invalid. Please request a new password reset.";
        public const string PasswordChangeFailed = "Unable to change your password. Please ensure your current password is correct and try again.";
        public const string EmailNotConfirmed = "Your email address has not been confirmed. Please check your inbox for the confirmation email.";
        public const string MfaRequired = "Additional verification is required. Please complete the multi-factor authentication process.";

        // Resource Messages
        public const string RecordNotFound = "The requested record was not found. It may have been deleted or you may not have permission to view it.";
        public const string DuplicateRecord = "A record with this information already exists. Please use unique values and try again.";
        public const string RecordInUse = "This record is currently in use and cannot be deleted. Please remove all references before attempting to delete.";
        public const string RecordAlreadyExists = "A record with these details already exists in the system. Please check for duplicates.";

        // Validation Messages
        public const string ValidationFailure = "One or more fields have invalid values. Please review the highlighted fields and correct them.";
        public const string RequiredField = "This field is required. Please provide a value.";
        public const string InvalidEmail = "The email address you entered is not valid. Please enter a valid email address.";
        public const string InvalidPhoneNumber = "The phone number you entered is not valid. Please enter a valid phone number.";
        public const string InvalidDate = "The date you entered is not valid. Please enter a valid date.";
        public const string InvalidFormat = "The value you entered is not in the correct format. Please check and try again.";
        public const string ValueTooLong = "The value you entered is too long. Please shorten it and try again.";
        public const string ValueTooShort = "The value you entered is too short. Please provide more information.";
        public const string InvalidFileType = "The file type you selected is not supported. Please select a file with an allowed extension.";
        public const string FileTooLarge = "The file you selected is too large. Please select a file smaller than {0} MB.";

        // Business Rule Messages
        public const string BusinessRuleViolation = "This operation violates a business rule. Please review the details and try again.";
        public const string EnrollmentClosed = "Enrollment is currently closed. Please contact the registrar's office for assistance.";
        public const string GradeAlreadyPublished = "Grades have already been published and cannot be modified. Please contact the academic office.";
        public const string DuplicateEnrollment = "This student is already enrolled in this course. Duplicate enrollments are not allowed.";
        public const string PrerequisiteNotMet = "You have not met the prerequisites for this course. Please complete the required courses first.";
        public const string CapacityExceeded = "This course has reached its maximum capacity. Please select another section or course.";
        public const string TimetableConflict = "The scheduled time conflicts with an existing class. Please choose a different time slot.";
        public const string RoomUnavailable = "The selected room is not available at the requested time. Please choose a different room or time.";
        public const string LecturerUnavailable = "The selected lecturer is not available at the requested time. Please choose a different lecturer or time.";
        public const string StudentNotEligible = "This student is not eligible for the requested operation. Please check the student's status.";
        public const string AcademicYearClosed = "The academic year is closed. No changes can be made at this time.";

        // System Messages
        public const string InternalError = "An unexpected error occurred while processing your request. Please try again later. If the problem persists, contact support.";
        public const string ServiceUnavailable = "The service is temporarily unavailable. Please try again in a few minutes.";
        public const string DatabaseUnavailable = "The database is currently unavailable. Please try again later. If the problem persists, contact your system administrator.";
        public const string NetworkUnavailable = "A network error occurred. Please check your internet connection and try again.";
        public const string TimeoutError = "The operation timed out. Please try again. If the problem persists, contact support.";
        public const string ExternalServiceError = "An external service is currently unavailable. Please try again later.";
        public const string FileSystemError = "A file operation failed. Please try again or contact support if the problem persists.";
        public const string UploadFailed = "The file upload failed. Please check the file and try again.";
        public const string DownloadFailed = "The file download failed. Please try again.";
        public const string SaveFailed = "Unable to save your changes. Please try again. If the problem persists, contact support.";
        public const string OperationFailed = "The operation failed. Please try again. If the problem persists, contact support.";
        public const string ReportGenerationFailed = "Unable to generate the report. Please try again. If the problem persists, contact support.";
        public const string ExportFailed = "Unable to export the data. Please try again.";
        public const string ImportFailed = "Unable to import the data. Please check the file format and try again.";
        public const string BulkOperationFailed = "The bulk operation completed with errors. Please review the results for details.";
        public const string BackupFailed = "The backup operation failed. Please check the backup configuration and try again.";
        public const string RestoreFailed = "The restore operation failed. Please check the backup file and try again.";
        public const string ConfigurationError = "A configuration error occurred. Please contact your system administrator.";
        public const string MaintenanceMode = "The system is currently undergoing maintenance. Please try again later.";

        // Data Messages
        public const string NoDataAvailable = "No data is available for the selected criteria. Please adjust your filters and try again.";
        public const string DataExportInProgress = "Your data export is being prepared. You will be notified when it is ready for download.";
        public const string DataImportInProgress = "Your data import is being processed. You will be notified when it is complete.";

        /// <summary>
        /// Gets a dictionary of all error codes mapped to their user-friendly messages.
        /// </summary>
        public static IReadOnlyDictionary<string, string> AllMessages => new Dictionary<string, string>
        {
            ["INVALID_CREDENTIALS"] = InvalidCredentials,
            ["ACCESS_DENIED"] = AccessDenied,
            ["SESSION_EXPIRED"] = SessionExpired,
            ["ACCOUNT_LOCKED"] = AccountLocked,
            ["ACCOUNT_DISABLED"] = AccountDisabled,
            ["TOKEN_EXPIRED"] = TokenExpired,
            ["TOKEN_INVALID"] = TokenInvalid,
            ["PASSWORD_RESET_FAILED"] = PasswordResetFailed,
            ["PASSWORD_CHANGE_FAILED"] = PasswordChangeFailed,
            ["EMAIL_NOT_CONFIRMED"] = EmailNotConfirmed,
            ["MFA_REQUIRED"] = MfaRequired,
            ["NOT_FOUND"] = RecordNotFound,
            ["DUPLICATE_RECORD"] = DuplicateRecord,
            ["RECORD_IN_USE"] = RecordInUse,
            ["RECORD_ALREADY_EXISTS"] = RecordAlreadyExists,
            ["VALIDATION_ERROR"] = ValidationFailure,
            ["REQUIRED_FIELD"] = RequiredField,
            ["INVALID_EMAIL"] = InvalidEmail,
            ["INVALID_PHONE"] = InvalidPhoneNumber,
            ["INVALID_DATE"] = InvalidDate,
            ["INVALID_FORMAT"] = InvalidFormat,
            ["VALUE_TOO_LONG"] = ValueTooLong,
            ["VALUE_TOO_SHORT"] = ValueTooShort,
            ["INVALID_FILE_TYPE"] = InvalidFileType,
            ["FILE_TOO_LARGE"] = FileTooLarge,
            ["BUSINESS_RULE_VIOLATION"] = BusinessRuleViolation,
            ["ENROLLMENT_CLOSED"] = EnrollmentClosed,
            ["GRADE_ALREADY_PUBLISHED"] = GradeAlreadyPublished,
            ["DUPLICATE_ENROLLMENT"] = DuplicateEnrollment,
            ["PREREQUISITE_NOT_MET"] = PrerequisiteNotMet,
            ["CAPACITY_EXCEEDED"] = CapacityExceeded,
            ["TIMETABLE_CONFLICT"] = TimetableConflict,
            ["ROOM_UNAVAILABLE"] = RoomUnavailable,
            ["LECTURER_UNAVAILABLE"] = LecturerUnavailable,
            ["STUDENT_NOT_ELIGIBLE"] = StudentNotEligible,
            ["ACADEMIC_YEAR_CLOSED"] = AcademicYearClosed,
            ["INTERNAL_ERROR"] = InternalError,
            ["SERVICE_UNAVAILABLE"] = ServiceUnavailable,
            ["DATABASE_UNAVAILABLE"] = DatabaseUnavailable,
            ["NETWORK_UNAVAILABLE"] = NetworkUnavailable,
            ["TIMEOUT_ERROR"] = TimeoutError,
            ["EXTERNAL_SERVICE_ERROR"] = ExternalServiceError,
            ["FILE_SYSTEM_ERROR"] = FileSystemError,
            ["UPLOAD_FAILED"] = UploadFailed,
            ["DOWNLOAD_FAILED"] = DownloadFailed,
            ["SAVE_FAILED"] = SaveFailed,
            ["OPERATION_FAILED"] = OperationFailed,
            ["REPORT_GENERATION_FAILED"] = ReportGenerationFailed,
            ["EXPORT_FAILED"] = ExportFailed,
            ["IMPORT_FAILED"] = ImportFailed,
            ["BULK_OPERATION_FAILED"] = BulkOperationFailed,
            ["BACKUP_FAILED"] = BackupFailed,
            ["RESTORE_FAILED"] = RestoreFailed,
            ["CONFIGURATION_ERROR"] = ConfigurationError,
            ["MAINTENANCE_MODE"] = MaintenanceMode,
            ["NO_DATA_AVAILABLE"] = NoDataAvailable,
            ["DATA_EXPORT_IN_PROGRESS"] = DataExportInProgress,
            ["DATA_IMPORT_IN_PROGRESS"] = DataImportInProgress
        };
    }
}
