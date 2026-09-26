using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// An SMS institutional request (aggregate root). 
    /// Extends the OMS workflow capabilities for academic and administrative processes.
    /// Request numbers use REQ-yyyy-nnnnnn format.
    /// </summary>
    [Table("sms_requests")]
    public class Request : BaseEntity, ITenantAwareEntity
    {
        /// <summary>Tenant-unique, server-generated, immutable request number (REQ-yyyy-nnnnnn).</summary>
        [Required]
        [MaxLength(50)]
        public string RequestNumber { get; private set; } = string.Empty;

        /// <summary>Request type (PermissionRequest, EnrollmentRequest, etc.)</summary>
        [Required]
        [MaxLength(100)]
        public string RequestType { get; private set; } = string.Empty;

        /// <summary>Request type display name for UI</summary>
        [Required]
        [MaxLength(200)]
        public string TypeDisplayName { get; private set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; private set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; private set; }

        /// <summary>Priority of the request</summary>
        public RequestPriority Priority { get; private set; } = RequestPriority.Normal;

        /// <summary>Lifecycle status. Change only via the transition methods.</summary>
        public RequestStatus Status { get; private set; } = RequestStatus.Draft;

        /// <summary>User id of the request creator (requester).</summary>
        [Required]
        [MaxLength(100)]
        public string RequesterUserId { get; private set; } = string.Empty;

        public Guid? StudentId { get; private set; }
        public Guid? LecturerId { get; private set; }
        public Guid? CourseId { get; private set; }
        public Guid? UnitId { get; private set; }
        public Guid? EnrollmentId { get; private set; }
        public Guid? AccommodationId { get; private set; }
        public Guid? AssignmentId { get; private set; }
        public Guid? CertificateId { get; private set; }

        [MaxLength(100)]
        public string? AssignedUserId { get; private set; }
        [MaxLength(100)]
        public string? AssignedByUserId { get; private set; }
        public DateTime? AssignedAtUtc { get; private set; }
        [MaxLength(100)]
        public string? PreviousAssigneeUserId { get; private set; }

        public DateTime? DueDate { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }
        [MaxLength(2000)]
        public string? CompletionNotes { get; private set; }
        public DateTime? SubmittedAtUtc { get; private set; }

        /// <summary>Optional link to a related SMS record (kept generic and finance-free).</summary>
        [MaxLength(100)]
        public string? RelatedEntityId { get; private set; }
        [MaxLength(100)]
        public string? RelatedEntityType { get; private set; }

        /// <summary>Read-model alias kept for API/DTO compatibility.</summary>
        public DateTime? SubmittedAt => SubmittedAtUtc;
        /// <summary>Read-model alias kept for API/DTO compatibility.</summary>
        public DateTime? CompletedAt => CompletedAtUtc;

        [MaxLength(100)]
        public string? ApprovedByUserId { get; private set; }
        public DateTime? ApprovedAtUtc { get; private set; }
        [MaxLength(1000)]
        public string? ApprovalRemarks { get; private set; }

        [MaxLength(100)]
        public string? RejectedByUserId { get; private set; }
        public DateTime? RejectedAtUtc { get; private set; }
        [MaxLength(1000)]
        public string? RejectionRemarks { get; private set; }

        [MaxLength(100)]
        public string? ReturnedByUserId { get; private set; }
        public DateTime? ReturnedAtUtc { get; private set; }
        [MaxLength(1000)]
        public string? ReturnReason { get; private set; }

        [MaxLength(100)]
        public string? CancelledByUserId { get; private set; }
        public DateTime? CancelledAtUtc { get; private set; }
        [MaxLength(1000)]
        public string? CancellationReason { get; private set; }

        [MaxLength(100)]
        public string? EscalatedByUserId { get; private set; }
        public DateTime? EscalatedAtUtc { get; private set; }
        [MaxLength(1000)]
        public string? EscalationReason { get; private set; }

        private void EnsureEditable()
        {
            if (!RequestLifecycle.CanEdit(Status))
                throw new InvalidOperationException($"Request can only be edited while in Draft or Returned status (current: {Status}).");
        }

        public static Request Create(
            string requestNumber,
            string requestType,
            string typeDisplayName,
            string title,
            string? description,
            string requesterUserId,
            RequestPriority priority = RequestPriority.Normal,
            Guid? studentId = null,
            Guid? lecturerId = null,
            Guid? courseId = null,
            Guid? unitId = null,
            string? relatedEntityType = null,
            string? relatedEntityId = null,
            Guid? enrollmentId = null,
            Guid? accommodationId = null,
            Guid? assignmentId = null,
            Guid? certificateId = null)
        {
            if (string.IsNullOrWhiteSpace(requestNumber))
                throw new ArgumentException("Request number is required.", nameof(requestNumber));
            if (string.IsNullOrWhiteSpace(requestType))
                throw new ArgumentException("Request type is required.", nameof(requestType));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required.", nameof(title));
            if (string.IsNullOrWhiteSpace(requesterUserId))
                throw new ArgumentException("Requester is required.", nameof(requesterUserId));

            return new Request
            {
                RequestNumber = requestNumber,
                RequestType = requestType,
                TypeDisplayName = typeDisplayName,
                Title = title.Trim(),
                Description = description?.Trim(),
                RequesterUserId = requesterUserId,
                Priority = priority,
                StudentId = studentId,
                LecturerId = lecturerId,
                CourseId = courseId,
                UnitId = unitId,
                RelatedEntityType = relatedEntityType?.Trim(),
                RelatedEntityId = relatedEntityId?.Trim(),
                EnrollmentId = enrollmentId,
                AccommodationId = accommodationId,
                AssignmentId = assignmentId,
                CertificateId = certificateId
            };
        }

        public void Resubmit(string performedByUserId, string? performedByUsername)
        {
            Submit(performedByUserId, performedByUsername);
        }

        public void StartReview(string performedByUserId, string? performedByUsername, string? notes = null)
        {
            if (string.IsNullOrWhiteSpace(performedByUserId))
                throw new ArgumentException("Performing user is required.", nameof(performedByUserId));
            ApplyTransition(RequestStatus.PendingReview, RequestActionType.ReviewStarted, performedByUserId, performedByUsername, notes);
        }

        public void UpdateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required.", nameof(title));
            EnsureEditable();
            Title = title.Trim();
        }

        public void UpdateDescription(string? description)
        {
            EnsureEditable();
            Description = description?.Trim();
        }

        public void SetPriority(RequestPriority priority)
        {
            EnsureEditable();
            Priority = priority;
        }

        public void SetDueDate(DateTime dueDate)
        {
            EnsureEditable();
            DueDate = dueDate;
        }

        public void ClearDueDate()
        {
            EnsureEditable();
            DueDate = null;
        }

        public void Submit(string performedByUserId, string? performedByUsername)
        {
            if (string.IsNullOrWhiteSpace(performedByUserId))
                throw new ArgumentException("Performing user is required.", nameof(performedByUserId));
            ApplyTransition(RequestStatus.Submitted, RequestActionType.Submitted, performedByUserId, performedByUsername, null);
            SubmittedAtUtc = DateTime.UtcNow;
        }

        public void Assign(string assignedUserId, string assignedByUserId, string? previousAssigneeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(assignedUserId))
                throw new ArgumentException("Assigned user is required.", nameof(assignedUserId));
            if (string.IsNullOrWhiteSpace(assignedByUserId))
                throw new ArgumentException("Assigning user is required.", nameof(assignedByUserId));
            PreviousAssigneeUserId = previousAssigneeUserId;
            AssignedUserId = assignedUserId;
            AssignedByUserId = assignedByUserId;
            AssignedAtUtc = DateTime.UtcNow;
            if (Status == RequestStatus.Submitted || Status == RequestStatus.PendingReview)
                ApplyTransition(RequestStatus.Assigned, RequestActionType.Assigned, assignedByUserId, null, $"Assigned to {assignedUserId}");
        }

        public void Reassign(string newAssigneeUserId, string reassignByUserId, string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(newAssigneeUserId))
                throw new ArgumentException("New assignee is required.", nameof(newAssigneeUserId));
            if (string.IsNullOrWhiteSpace(reassignByUserId))
                throw new ArgumentException("Reassigning user is required.", nameof(reassignByUserId));
            var previousAssignee = AssignedUserId;
            AssignedUserId = newAssigneeUserId;
            AssignedByUserId = reassignByUserId;
            AssignedAtUtc = DateTime.UtcNow;
            PreviousAssigneeUserId = previousAssignee;
            ApplyTransition(RequestStatus.Assigned, RequestActionType.Reassigned, reassignByUserId, null, reason);
        }

        public void ReturnForCorrection(string performedByUserId, string? performedByUsername, string? reason)
        {
            Return(performedByUserId, performedByUsername, reason ?? string.Empty);
        }

        public void Approve(string performedByUserId, string? performedByUsername, string? remarks)
        {
            if (string.IsNullOrWhiteSpace(performedByUserId))
                throw new ArgumentException("Performing user is required.", nameof(performedByUserId));
            ApplyTransition(RequestStatus.Approved, RequestActionType.Approved, performedByUserId, performedByUsername, remarks);
            ApprovedByUserId = performedByUserId;
            ApprovedAtUtc = DateTime.UtcNow;
            ApprovalRemarks = remarks;
        }

        public void Reject(string performedByUserId, string? performedByUsername, string remarks)
        {
            if (string.IsNullOrWhiteSpace(performedByUserId))
                throw new ArgumentException("Performing user is required.", nameof(performedByUserId));
            if (string.IsNullOrWhiteSpace(remarks))
                throw new ArgumentException("A rejection remark is required.", nameof(remarks));
            ApplyTransition(RequestStatus.Rejected, RequestActionType.Rejected, performedByUserId, performedByUsername, remarks);
            RejectedByUserId = performedByUserId;
            RejectedAtUtc = DateTime.UtcNow;
            RejectionRemarks = remarks;
        }

        public void Return(string performedByUserId, string? performedByUsername, string reason)
        {
            if (string.IsNullOrWhiteSpace(performedByUserId))
                throw new ArgumentException("Performing user is required.", nameof(performedByUserId));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A return reason is required.", nameof(reason));
            ApplyTransition(RequestStatus.Returned, RequestActionType.Returned, performedByUserId, performedByUsername, reason);
            ReturnedByUserId = performedByUserId;
            ReturnedAtUtc = DateTime.UtcNow;
            ReturnReason = reason;
        }

        public void Complete(string performedByUserId, string? performedByUsername, string? notes)
        {
            if (string.IsNullOrWhiteSpace(performedByUserId))
                throw new ArgumentException("Performing user is required.", nameof(performedByUserId));
            ApplyTransition(RequestStatus.Completed, RequestActionType.Completed, performedByUserId, performedByUsername, notes);
            CompletedAtUtc = DateTime.UtcNow;
            CompletionNotes = notes;
        }

        public void Cancel(string performedByUserId, string? performedByUsername, string reason)
        {
            if (string.IsNullOrWhiteSpace(performedByUserId))
                throw new ArgumentException("Performing user is required.", nameof(performedByUserId));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A cancellation reason is required.", nameof(reason));
            ApplyTransition(RequestStatus.Cancelled, RequestActionType.Cancelled, performedByUserId, performedByUsername, reason);
            CancelledByUserId = performedByUserId;
            CancelledAtUtc = DateTime.UtcNow;
            CancellationReason = reason;
        }

        public void Escalate(string performedByUserId, string? performedByUsername, string reason)
        {
            if (string.IsNullOrWhiteSpace(performedByUserId))
                throw new ArgumentException("Performing user is required.", nameof(performedByUserId));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("An escalation reason is required.", nameof(reason));
            ApplyTransition(RequestStatus.Escalated, RequestActionType.Escalated, performedByUserId, performedByUsername, reason);
            EscalatedByUserId = performedByUserId;
            EscalatedAtUtc = DateTime.UtcNow;
            EscalationReason = reason;
        }

        private void ApplyTransition(RequestStatus toStatus, RequestActionType action, string performedByUserId, string? performedByUsername, string? remarks)
        {
            if (string.IsNullOrWhiteSpace(performedByUserId))
                throw new ArgumentException("Performing user is required.", nameof(performedByUserId));
            var fromStatus = Status;
            RequestLifecycle.EnsureCanTransition(fromStatus, toStatus);
            Status = toStatus;
            StatusHistory.Add(RequestStatusHistory.Create(fromStatus, toStatus, action, performedByUserId, performedByUsername, remarks));
            UpdatedAt = DateTime.UtcNow;
        }

        public virtual ICollection<RequestStatusHistory> StatusHistory { get; private set; } = new List<RequestStatusHistory>();
        public virtual ICollection<RequestComment> Comments { get; private set; } = new List<RequestComment>();
    }
}