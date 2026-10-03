using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Common.Interfaces
{
    /// <summary>
    /// Raises in-app notifications for ordinary business events (accommodation,
    /// assignments, units, courses, enrolment, registration approval).
    /// <para>
    /// WHY THIS EXISTS, given <see cref="INotificationDispatcher"/> already exists.
    /// The dispatcher is the low-level <i>write</i> path: it takes an explicit list of
    /// user ids and normalises, de-duplicates, sanitises and persists. It knows
    /// nothing about the academic graph, so every call site would otherwise have to
    /// re-derive "who is affected by an assignment in unit X" - and each derivation
    /// would drift.
    /// </para>
    /// <para>
    /// This service is the thin semantic layer above it. It owns the
    /// domain-relationship-to-recipient mapping in ONE place and delegates the actual
    /// write to <see cref="INotificationDispatcher"/>, so there is still exactly one
    /// code path that can create a notification row.
    /// </para>
    /// <para>
    /// <b>Contract for every method here:</b>
    /// resolve recipients, then call the dispatcher AFTER the caller's own
    /// <c>SaveChangesAsync</c>. Recipients are always de-duplicated by the
    /// dispatcher, blank ids are dropped, and a resolution failure is swallowed so a
    /// notification can never roll back a business transition that already committed.
    /// </para>
    /// </summary>
    public interface IBusinessEventNotifier
    {
        // ── Accommodation ────────────────────────────────────────────────────
        Task NotifyAccommodationAllocatedAsync(
            Guid? studentId, Guid? lecturerId, string description, Guid? referenceId = null,
            CancellationToken cancellationToken = default);

        Task NotifyAccommodationChangedAsync(
            Guid? studentId, Guid? lecturerId, string title, string message, Guid? referenceId = null,
            CancellationToken cancellationToken = default);

        Task NotifyAccommodationEndedAsync(
            Guid? studentId, Guid? lecturerId, string message, Guid? referenceId = null,
            CancellationToken cancellationToken = default);

        // ── Assignments ──────────────────────────────────────────────────────
        /// <summary>New/updated assignment for the unit's enrolled students.</summary>
        Task NotifyAssignmentPublishedAsync(
            Guid assignmentId, Guid unitId, string title, string message, bool includeLecturer = true,
            CancellationToken cancellationToken = default);

        /// <summary>A student submitted work; the assignment's lecturer is told.</summary>
        Task NotifyAssignmentSubmittedAsync(
            Guid assignmentId, Guid lecturerId, string assignmentTitle, string studentName,
            CancellationToken cancellationToken = default);

        /// <summary>A submission was graded; the student is told.</summary>
        Task NotifyAssignmentGradedAsync(
            Guid assignmentId, Guid studentId, string assignmentTitle, int score, int maxScore,
            CancellationToken cancellationToken = default);

        /// <summary>A lecturer raised an issue against a student; the student is told.</summary>
        Task NotifyAssignmentIssueAsync(
            Guid assignmentId, Guid studentId, string title, string message,
            CancellationToken cancellationToken = default);

        // ── Units ────────────────────────────────────────────────────────────
        Task NotifyUnitChangedAsync(
            Guid unitId, string unitCode, string unitName, string message,
            CancellationToken cancellationToken = default);

        // ── Courses ──────────────────────────────────────────────────────────
        Task NotifyCourseChangedAsync(
            Guid courseId, string courseCode, string courseName, string message,
            CancellationToken cancellationToken = default);

        // ── Enrolment ────────────────────────────────────────────────────────
        Task NotifyStudentEnrolledAsync(
            Guid studentId, string unitCode, string unitName, string semesterName,
            CancellationToken cancellationToken = default);

        Task NotifyEnrollmentStatusChangedAsync(
            Guid studentId, string status, string unitName,
            CancellationToken cancellationToken = default);

        // ── Registration / account approval ──────────────────────────────────

        /// <summary>
        /// Raised by the account-creation workflow (registration) for every newly
        /// created student or lecturer that needs an accommodation allocation.
        /// <para>
        /// Recipients are resolved by ROLE (Administrator, Coordinator, Receptionist)
        /// because accommodation is a shared back-office queue rather than one
        /// person's responsibility, and all three roles already hold the existing
        /// accommodation policies. The dispatcher resolves live role membership and
        /// stamps the tenant, so a foreign-tenant admin can never receive it.
        /// </para>
        /// </summary>
        Task NotifyAccommodationRequiredAsync(
            string personName, string role, string? identifier, string? courseName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Raised when a registration has been submitted and is awaiting an
        /// administrator decision. Recipients are the approval roles.
        /// </summary>
        Task NotifyRegistrationAwaitingApprovalAsync(
            string personName, string role, string? courseName, string? unitSummary,
            CancellationToken cancellationToken = default);

        Task NotifyRegistrationDecisionAsync(
            string? userId, string userType, bool approved, string? reason = null,
            CancellationToken cancellationToken = default);
    }
}