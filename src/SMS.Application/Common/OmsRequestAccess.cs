using System;
using System.Collections.Generic;
using System.Linq;
using SMS.Domain.Common;
using SMS.Domain.Entities;

namespace SMS.Application.Common
{
    /// <summary>
    /// Object-level authorization helpers for the OMS request engine.
    ///
    /// Role membership alone is never sufficient: a caller must additionally be
    /// the requester, the assignee, or hold a privileged queue role in order to
    /// touch a specific request. These helpers centralise that evaluation so
    /// handlers, thin module adapters, and attachment endpoints all apply the
    /// same rule set.
    /// </summary>
    public static class OmsRequestAccess
    {
        /// <summary>True when <paramref name="userId"/> raised the request.</summary>
        public static bool IsRequester(Request request, string? userId) =>
            request != null && !string.IsNullOrWhiteSpace(userId) &&
            string.Equals(request.RequesterUserId, userId, StringComparison.OrdinalIgnoreCase);

        /// <summary>True when <paramref name="userId"/> is the current assignee.</summary>
        public static bool IsAssignee(Request request, string? userId) =>
            request != null && !string.IsNullOrWhiteSpace(userId) &&
            !string.IsNullOrWhiteSpace(request.AssignedUserId) &&
            string.Equals(request.AssignedUserId, userId, StringComparison.OrdinalIgnoreCase);

        /// <summary>True when the caller may see every request in the tenant queue.</summary>
        public static bool CanViewAll(IEnumerable<string>? roles) =>
            OmsAuthorization.HasAnyRole(roles ?? Enumerable.Empty<string>(), OmsAuthorization.ViewAllRequestsRoles);

        /// <summary>True when the caller may see requests assigned to them.</summary>
        public static bool CanViewAssigned(IEnumerable<string>? roles) =>
            OmsAuthorization.HasAnyRole(roles ?? Enumerable.Empty<string>(), OmsAuthorization.ViewAssignedRequestsRoles);

        /// <summary>True when the caller may see requests they raised.</summary>
        public static bool CanViewOwn(IEnumerable<string>? roles) =>
            OmsAuthorization.HasAnyRole(roles ?? Enumerable.Empty<string>(), OmsAuthorization.ViewOwnRequestsRoles);

        /// <summary>
        /// True when the caller may read the request itself (detail, attachments,
        /// history, comments): privileged queue role, or owner, or assignee.
        /// </summary>
        public static bool CanView(Request request, string? userId, IEnumerable<string>? roles)
        {
            if (request == null)
                return false;

            if (CanViewAll(roles))
                return true;

            if (IsRequester(request, userId) && CanViewOwn(roles))
                return true;

            if (IsAssignee(request, userId) && CanViewAssigned(roles))
                return true;

            return false;
        }

        /// <summary>
        /// True when the caller may attach files to the request. Attachments may
        /// be added by anyone who can view the request while it is still open
        /// for work (i.e. not in a terminal status); terminal requests are
        /// frozen so their evidence set cannot change after the fact.
        /// </summary>
        public static bool CanAttach(Request request, string? userId, IEnumerable<string>? roles)
        {
            if (!CanView(request, userId, roles))
                return false;

            return IsOpenForWork(request);
        }

        /// <summary>
        /// True when the caller may delete an attachment row. Uploaders may
        /// remove their own upload while the request is still open for work,
        /// and privileged administrators may remove any attachment.
        /// </summary>
        public static bool CanDeleteAttachment(Request request, string? uploadedByUserId, string? userId, IEnumerable<string>? roles)
        {
            if (!CanView(request, userId, roles))
                return false;

            var isAdmin = OmsAuthorization.HasAnyRole(roles ?? Enumerable.Empty<string>(), OmsAuthorization.ManageRequestTypesRoles);

            if (isAdmin)
                return true;

            // Only the uploader may remove their own attachment, and only while
            // the request is still actionable. Identifier comparison is
            // ordinal-ignore-case because the uploader id is captured from the
            // authenticated principal as a string.
            if (string.IsNullOrWhiteSpace(userId)
                || string.IsNullOrWhiteSpace(uploadedByUserId)
                || !string.Equals(uploadedByUserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return IsOpenForWork(request);
        }

        /// <summary>
        /// Throws <see cref="ForbiddenException"/> unless the caller may read the
        /// request. The single server-side gate for request detail, history,
        /// comments and attachment metadata.
        /// </summary>
        public static void EnsureCanView(Request request, string? userId, IEnumerable<string>? roles)
        {
            if (!CanView(request, userId, roles))
                throw new ForbiddenException(OmsPermissions.ViewRequests, userId);
        }

        /// <summary>
        /// Throws <see cref="ForbiddenException"/> unless the caller may attach a
        /// file to the request.
        /// </summary>
        public static void EnsureCanAttach(Request request, string? userId, IEnumerable<string>? roles)
        {
            if (!CanAttach(request, userId, roles))
                throw new ForbiddenException(OmsPermissions.UpdateRequest, userId);
        }

        /// <summary>
        /// Throws <see cref="ForbiddenException"/> unless the caller may remove
        /// the attachment row.
        /// </summary>
        public static void EnsureCanDeleteAttachment(Request request, string? uploadedByUserId, string? userId, IEnumerable<string>? roles)
        {
            if (!CanDeleteAttachment(request, uploadedByUserId, userId, roles))
                throw new ForbiddenException(OmsPermissions.UpdateRequest, userId);
        }

        /// <summary>
        /// True while the request has not reached a terminal status. Terminal
        /// requests (Completed / Rejected / Cancelled) are immutable.
        /// </summary>
        public static bool IsOpenForWork(Request request) =>
            request != null && !RequestLifecycle.IsTerminal(request.Status);

        /// <summary>
        /// True when the request is in an editable status (Draft or Returned).
        /// Delegates to the single authoritative lifecycle definition.
        /// </summary>
        public static bool IsEditable(Request request) =>
            request != null && RequestLifecycle.CanEdit(request.Status);
    }
}
