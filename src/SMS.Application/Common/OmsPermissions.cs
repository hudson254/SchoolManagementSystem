using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Application.Common
{
    /// <summary>
    /// OMS permission constants (Documentation/OMS/OMS_ARCHITECTURE.md section 5).
    /// Enforced as defense-in-depth inside OMS handlers; HTTP-level policy
    /// wiring arrives with the API controllers in a later phase.
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
    }
}
