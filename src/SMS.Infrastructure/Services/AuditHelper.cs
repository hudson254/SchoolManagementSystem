using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SMS.Domain.Interfaces;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Helper service for easy audit logging from command/query handlers.
    /// Provides convenience methods for common audit scenarios.
    /// </summary>
    public class AuditHelper
    {
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditHelper> _logger;

        public AuditHelper(
            IAuditService auditService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditHelper> logger)
        {
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Logs a user creation audit event.
        /// </summary>
        public async Task LogUserCreatedAsync(string userId, string username, string createdBy)
        {
            await _auditService.LogActivityAsync("UserCreated", "User", userId,
                $"User '{username}' was created by {createdBy}");
        }

        /// <summary>
        /// Logs a user modification audit event.
        /// </summary>
        public async Task LogUserModifiedAsync(string userId, string username, string modifiedBy, string changes)
        {
            await _auditService.LogDataChangeAsync("User", userId, "UserModified", changes);
        }

        /// <summary>
        /// Logs a user deletion audit event.
        /// </summary>
        public async Task LogUserDeletedAsync(string userId, string username, string deletedBy)
        {
            await _auditService.LogActivityAsync("UserDeleted", "User", userId,
                $"User '{username}' was deleted by {deletedBy}");
        }

        /// <summary>
        /// Logs a role assignment audit event.
        /// </summary>
        public async Task LogRoleAssignedAsync(string userId, string username, string role, string assignedBy)
        {
            await _auditService.LogActivityAsync("RoleAssigned", "UserRole", userId,
                $"Role '{role}' was assigned to user '{username}' by {assignedBy}");
        }

        /// <summary>
        /// Logs a permission change audit event.
        /// </summary>
        public async Task LogPermissionChangedAsync(string roleId, string roleName, string permission, string changedBy)
        {
            await _auditService.LogActivityAsync("PermissionChanged", "RolePermission", roleId,
                $"Permission '{permission}' was changed for role '{roleName}' by {changedBy}");
        }

        /// <summary>
        /// Logs a student registration audit event.
        /// </summary>
        public async Task LogStudentRegisteredAsync(string studentId, string studentName, string registeredBy)
        {
            await _auditService.LogActivityAsync("StudentRegistered", "Student", studentId,
                $"Student '{studentName}' was registered by {registeredBy}");
        }

        /// <summary>
        /// Logs a student profile update audit event.
        /// </summary>
        public async Task LogStudentUpdatedAsync(string studentId, string studentName, string changes)
        {
            await _auditService.LogDataChangeAsync("Student", studentId, "StudentUpdated", changes);
        }

        /// <summary>
        /// Logs a marks entry audit event.
        /// </summary>
        public async Task LogMarksEnteredAsync(string gradeId, string studentName, string unitName, string enteredBy)
        {
            await _auditService.LogActivityAsync("MarksEntered", "Grade", gradeId,
                $"Marks for student '{studentName}' in unit '{unitName}' were entered by {enteredBy}");
        }

        /// <summary>
        /// Logs a marks modification audit event.
        /// </summary>
        public async Task LogMarksModifiedAsync(string gradeId, string studentName, string unitName, string oldScore, string newScore)
        {
            await _auditService.LogDataChangeAsync("Grade", gradeId, "MarksModified",
                $"Marks for student '{studentName}' in unit '{unitName}' changed from {oldScore} to {newScore}");
        }

        /// <summary>
        /// Logs a grade publication audit event.
        /// </summary>
        public async Task LogGradePublishedAsync(string unitId, string unitName, string publishedBy)
        {
            await _auditService.LogActivityAsync("GradePublished", "Grade", unitId,
                $"Grades for unit '{unitName}' were published by {publishedBy}");
        }

        /// <summary>
        /// Logs an enrollment creation audit event.
        /// </summary>
        public async Task LogEnrollmentCreatedAsync(string enrollmentId, string studentName, string courseName, string enrolledBy)
        {
            await _auditService.LogActivityAsync("EnrollmentCreated", "Enrollment", enrollmentId,
                $"Student '{studentName}' was enrolled in course '{courseName}' by {enrolledBy}");
        }

        /// <summary>
        /// Logs an enrollment status change audit event.
        /// </summary>
        public async Task LogEnrollmentStatusChangedAsync(string enrollmentId, string studentName, string oldStatus, string newStatus)
        {
            await _auditService.LogDataChangeAsync("Enrollment", enrollmentId, "EnrollmentStatusChanged",
                $"Enrollment status for student '{studentName}' changed from '{oldStatus}' to '{newStatus}'");
        }

        /// <summary>
        /// Logs a course creation audit event.
        /// </summary>
        public async Task LogCourseCreatedAsync(string courseId, string courseName, string createdBy)
        {
            await _auditService.LogActivityAsync("CourseCreated", "Course", courseId,
                $"Course '{courseName}' was created by {createdBy}");
        }

        /// <summary>
        /// Logs a course modification audit event.
        /// </summary>
        public async Task LogCourseModifiedAsync(string courseId, string courseName, string changes)
        {
            await _auditService.LogDataChangeAsync("Course", courseId, "CourseModified", changes);
        }

        /// <summary>
        /// Logs an examination scheduling audit event.
        /// </summary>
        public async Task LogExamScheduledAsync(string examId, string examName, string scheduledBy)
        {
            await _auditService.LogActivityAsync("ExamScheduled", "Examination", examId,
                $"Examination '{examName}' was scheduled by {scheduledBy}");
        }

        /// <summary>
        /// Logs a configuration change audit event.
        /// </summary>
        public async Task LogConfigurationChangedAsync(string configKey, string oldValue, string newValue, string changedBy)
        {
            await _auditService.LogDataChangeAsync("Configuration", configKey, "ConfigurationChanged",
                $"Configuration '{configKey}' was changed from '{oldValue}' to '{newValue}' by {changedBy}");
        }

        /// <summary>
        /// Logs a report generation audit event.
        /// </summary>
        public async Task LogReportGeneratedAsync(string reportType, string generatedBy, string parameters)
        {
            await _auditService.LogActivityAsync("ReportGenerated", "Report", reportType,
                $"Report '{reportType}' was generated by {generatedBy} with parameters: {parameters}");
        }

        /// <summary>
        /// Logs a data export audit event.
        /// </summary>
        public async Task LogDataExportedAsync(string exportType, string exportedBy, string details)
        {
            await _auditService.LogActivityAsync("DataExported", "DataExport", exportType,
                $"Data export '{exportType}' was performed by {exportedBy}. Details: {details}");
        }

        /// <summary>
        /// Logs a data import audit event.
        /// </summary>
        public async Task LogDataImportedAsync(string importType, string importedBy, int recordCount)
        {
            await _auditService.LogActivityAsync("DataImported", "DataImport", importType,
                $"Data import '{importType}' was performed by {importedBy}. Records imported: {recordCount}");
        }

        /// <summary>
        /// Logs a bulk operation audit event.
        /// </summary>
        public async Task LogBulkOperationAsync(string operationType, string performedBy, int affectedRecords)
        {
            await _auditService.LogActivityAsync("BulkOperation", "BulkOperation", operationType,
                $"Bulk operation '{operationType}' was performed by {performedBy}. Records affected: {affectedRecords}");
        }

        /// <summary>
        /// Logs a backup initiation audit event.
        /// </summary>
        public async Task LogBackupInitiatedAsync(string backupType, string initiatedBy)
        {
            await _auditService.LogActivityAsync("BackupInitiated", "Backup", backupType,
                $"Backup '{backupType}' was initiated by {initiatedBy}");
        }

        /// <summary>
        /// Logs a restore operation audit event.
        /// </summary>
        public async Task LogRestorePerformedAsync(string backupId, string performedBy)
        {
            await _auditService.LogActivityAsync("RestorePerformed", "Restore", backupId,
                $"Restore from backup '{backupId}' was performed by {performedBy}");
        }

        /// <summary>
        /// Logs a security policy update audit event.
        /// </summary>
        public async Task LogSecurityPolicyUpdatedAsync(string policyName, string changes, string updatedBy)
        {
            await _auditService.LogDataChangeAsync("SecurityPolicy", policyName, "SecurityPolicyUpdated",
                $"Security policy '{policyName}' was updated by {updatedBy}. Changes: {changes}");
        }

        /// <summary>
        /// Logs an API key creation audit event.
        /// </summary>
        public async Task LogApiKeyCreatedAsync(string keyName, string createdBy)
        {
            await _auditService.LogActivityAsync("ApiKeyCreated", "ApiKey", keyName,
                $"API key '{keyName}' was created by {createdBy}");
        }

        /// <summary>
        /// Logs an integration configuration change audit event.
        /// </summary>
        public async Task LogIntegrationConfiguredAsync(string integrationName, string configuredBy)
        {
            await _auditService.LogActivityAsync("IntegrationConfigured", "Integration", integrationName,
                $"Integration '{integrationName}' was configured by {configuredBy}");
        }

        /// <summary>
        /// Logs a timetable update audit event.
        /// </summary>
        public async Task LogTimetableUpdatedAsync(string timetableId, string details, string updatedBy)
        {
            await _auditService.LogActivityAsync("TimetableUpdated", "Timetable", timetableId,
                $"Timetable was updated by {updatedBy}. Details: {details}");
        }

        /// <summary>
        /// Logs an attendance change audit event.
        /// </summary>
        public async Task LogAttendanceChangedAsync(string attendanceId, string studentName, string date, string status)
        {
            await _auditService.LogActivityAsync("AttendanceChanged", "Attendance", attendanceId,
                $"Attendance for student '{studentName}' on {date} was marked as '{status}'");
        }
    }
}
