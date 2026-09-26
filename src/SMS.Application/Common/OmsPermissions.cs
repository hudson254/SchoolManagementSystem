using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Application.Common
{
    /// <summary>
    /// Stable machine-readable request type codes owned by the OMS Request Core.
    ///
    /// Module adapters reference these codes rather than inventing their own
    /// strings, so a request type configured by an administrator always matches
    /// what the adapter expects. These are the ONLY sanctioned codes; free-form
    /// type strings remain available on the generic endpoint for types an
    /// administrator creates outside the known set.
    /// </summary>
    public static class OmsRequestTypeCodes
    {
        // --- Enrollment (module: Enrollments) ---
        public const string EnrollmentCourseChange = "ENROLLMENT_COURSE_CHANGE";
        public const string EnrollmentException = "ENROLLMENT_EXCEPTION";

        // --- Accommodation (module: Accommodation) ---
        public const string AccommodationAllocation = "ACCOMMODATION_ALLOCATION";
        public const string AccommodationChange = "ACCOMMODATION_CHANGE";

        // --- Assignment (module: Assignments) ---
        public const string AssignmentExtension = "ASSIGNMENT_EXTENSION";
        public const string AssignmentCorrection = "ASSIGNMENT_CORRECTION";

        // --- Assessment (module: Assessments) ---
        public const string AssessmentGradeReview = "ASSESSMENT_GRADE_REVIEW";
        public const string AssessmentCorrection = "ASSESSMENT_CORRECTION";

        // --- Certificate (module: Certificates) ---
        public const string CertificateReissue = "CERTIFICATE_REISSUE";
        public const string CertificateCorrection = "CERTIFICATE_CORRECTION";

        /// <summary>
        /// Every code the seeded module adapters rely on. Used by the
        /// idempotent request-type seeder to guarantee a configured type exists
        /// for each adapter.
        /// </summary>
        public static readonly IReadOnlyList<string> AllModuleCodes = new[]
        {
            EnrollmentCourseChange,
            EnrollmentException,
            AccommodationAllocation,
            AccommodationChange,
            AssignmentExtension,
            AssignmentCorrection,
            AssessmentGradeReview,
            AssessmentCorrection,
            CertificateReissue,
            CertificateCorrection
        };
    }

    /// <summary>
    /// OMS permission constants (Documentation/OMS/OMS_ARCHITECTURE.md section 5).
    /// Enforced as defense-in-depth inside OMS handlers; HTTP-level policy
    /// wiring arrives with the API controllers.
    /// </summary>
    public static class OmsPermissions
    {
        public const string ViewOrders = "Oms.ViewOrders";
        public const string CreateOrder = "Oms.CreateOrder";
        public const string EditOrder = "Oms.EditOrder";
        public const string SubmitOrder = "Oms.SubmitOrder";
        public const string ApproveOrder = "Oms.ApproveOrder";
        public const string RejectOrder = "Oms.RejectOrder";
        public const string CancelOrder = "Oms.CancelOrder";
        public const string ManageRoadAccounting = "Oms.ManageRoadAccounting";
        public const string ViewReports = "Oms.ViewReports";

        // OMS Request permissions (Phase 2C)
        public const string ViewRequests = "Oms.ViewRequests";
        public const string ViewOwnRequests = "Oms.ViewOwnRequests";
        public const string ViewAssignedRequests = "Oms.ViewAssignedRequests";
        public const string ViewAllRequests = "Oms.ViewAllRequests";
        public const string CreateRequest = "Oms.CreateRequest";
        public const string UpdateRequest = "Oms.UpdateRequest";
        public const string SubmitRequest = "Oms.SubmitRequest";
        public const string AssignRequest = "Oms.AssignRequest";
        public const string ReassignRequest = "Oms.ReassignRequest";
        public const string ReviewRequest = "Oms.ReviewRequest";
        public const string ApproveRequest = "Oms.ApproveRequest";
        public const string RejectRequest = "Oms.RejectRequest";
        public const string ReturnRequest = "Oms.ReturnRequest";
        public const string CancelRequest = "Oms.CancelRequest";
        public const string CancelAnyRequest = "Oms.CancelAnyRequest";
        public const string CompleteRequest = "Oms.CompleteRequest";
        public const string EscalateRequest = "Oms.EscalateRequest";
        public const string CommentOnRequest = "Oms.CommentOnRequest";
        public const string ViewRequestHistory = "Oms.ViewRequestHistory";
        public const string ManageRequestTypes = "Oms.ManageRequestTypes";
    }

    /// <summary>
    /// Role-based authorization rules for OMS handlers (architecture section 5
    /// permission matrix). Roles are matched case-insensitively against the
    /// existing SMS roles - no new identity system is introduced.
    /// </summary>
    public static class OmsAuthorization
    {
        private static readonly string[] AdminRoles = { "SystemAdministrator", "Administrator" };

        private static readonly string[] CoordinatorRoles = { "SystemAdministrator", "Administrator", "Coordinator" };

        /// <summary>View orders: Admin, Coordinator, Lecturer (view-only).</summary>
        public static readonly string[] ViewOrdersRoles = CoordinatorRoles.Concat(new[] { "Lecturer" }).ToArray();

        /// <summary>Create / edit (own, Draft) / submit orders.</summary>
        public static readonly string[] CreateOrderRoles = CoordinatorRoles;

        /// <summary>Approve / reject orders (creator self-approval also blocked in the domain).</summary>
        public static readonly string[] ApproveOrderRoles = AdminRoles;

        /// <summary>Cancel: Admin (any non-final) or Coordinator (own only).</summary>
        public static readonly string[] CancelAnyOrderRoles = AdminRoles;
        public static readonly string[] CancelOwnOrderRoles = CoordinatorRoles;

        /// <summary>Manage road accounts / view OMS reports.</summary>
        public static readonly string[] ManageRoadAccountingRoles = AdminRoles;
        public static readonly string[] ViewReportsRoles = CoordinatorRoles;

        public static bool HasAnyRole(IEnumerable<string> roles, string[] allowed) =>
            roles.Any(r => allowed.Contains(r, StringComparer.OrdinalIgnoreCase));

        // --- OMS Request role mappings (Phase 2C) ---

        /// <summary>View requests: Admin, Coordinator, Lecturer.</summary>
        public static readonly string[] ViewRequestsRoles = CoordinatorRoles.Concat(new[] { "Lecturer" }).ToArray();

        /// <summary>View own requests: all roles can view their own requests.</summary>
        public static readonly string[] ViewOwnRequestsRoles = new[] { "SystemAdministrator", "Administrator", "Coordinator", "Lecturer", "Student", "Receptionist" };

        /// <summary>View assigned requests: Admin, Coordinator, Lecturer.</summary>
        public static readonly string[] ViewAssignedRequestsRoles = ViewRequestsRoles;

        /// <summary>View all requests: Admin, Coordinator.</summary>
        public static readonly string[] ViewAllRequestsRoles = CoordinatorRoles;

        /// <summary>Create requests: all roles.</summary>
        public static readonly string[] CreateRequestRoles = new[] { "SystemAdministrator", "Administrator", "Coordinator", "Lecturer", "Student" };

        /// <summary>Update own draft/returned requests.</summary>
        public static readonly string[] UpdateRequestRoles = CreateRequestRoles;

        /// <summary>Submit own requests: all roles.</summary>
        public static readonly string[] SubmitRequestRoles = CreateRequestRoles;

        /// <summary>Assign requests: Admin, Coordinator.</summary>
        public static readonly string[] AssignRequestRoles = CoordinatorRoles;

        /// <summary>Reassign requests: Admin, Coordinator.</summary>
        public static readonly string[] ReassignRequestRoles = CoordinatorRoles;

        /// <summary>Review requests: Admin, Coordinator, Lecturer.</summary>
        public static readonly string[] ReviewRequestRoles = ViewRequestsRoles;

        /// <summary>Approve requests: Admin, Coordinator.</summary>
        public static readonly string[] ApproveRequestRoles = CoordinatorRoles;

        /// <summary>Reject requests: Admin, Coordinator.</summary>
        public static readonly string[] RejectRequestRoles = ApproveRequestRoles;

        /// <summary>Return requests for correction: Admin, Coordinator, Lecturer.</summary>
        public static readonly string[] ReturnRequestRoles = ReviewRequestRoles;

        /// <summary>Cancel own request: all roles.</summary>
        public static readonly string[] CancelOwnRequestRoles = CreateRequestRoles;

        /// <summary>Cancel any request: Admin.</summary>
        public static readonly string[] CancelAnyRequestRoles = AdminRoles;

        /// <summary>Complete requests: Admin, Coordinator.</summary>
        public static readonly string[] CompleteRequestRoles = CoordinatorRoles;

        /// <summary>Escalate requests: Admin, Coordinator.</summary>
        public static readonly string[] EscalateRequestRoles = CoordinatorRoles;

        /// <summary>Comment on requests: all roles with view access.</summary>
        public static readonly string[] CommentOnRequestRoles = ViewRequestsRoles;

        /// <summary>Manage request types: Admin only.</summary>
        public static readonly string[] ManageRequestTypesRoles = AdminRoles;
    }
}
