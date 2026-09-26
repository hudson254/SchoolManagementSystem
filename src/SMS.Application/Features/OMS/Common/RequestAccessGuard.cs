using SMS.Application.Common;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using System;
using System.Collections.Generic;

namespace SMS.Application.Common
{
    /// <summary>
    /// Object-level authorization for the generic OMS Request core
    /// (Phase 2 section 15). Role checks alone are never sufficient: the
    /// requester/assignee relationship on the request itself is evaluated.
    ///
    /// This is a thin, well-named façade over <see cref="OmsRequestAccess"/>,
    /// which holds the single authoritative implementation. The OMS request
    /// engine must not grow a second, competing authorization rule set,
    /// so every method here delegates.
    /// </summary>
    public static class RequestAccessGuard
    {
        public static bool CanView(Request request, string userId, IEnumerable<string> roles) =>
            OmsRequestAccess.CanView(request, userId, roles);

        public static void EnsureCanView(Request request, string? userId, IEnumerable<string> roles) =>
            OmsRequestAccess.EnsureCanView(request, userId, roles);

        public static bool IsOwner(Request request, string userId) =>
            OmsRequestAccess.IsRequester(request, userId);

        public static bool IsAssignee(Request request, string userId) =>
            OmsRequestAccess.IsAssignee(request, userId);

        public static bool IsAdministrator(IEnumerable<string> roles) =>
            OmsAuthorization.HasAnyRole(roles, OmsAuthorization.ManageRequestTypesRoles);

        public static bool CanManageAttachments(Request request, string userId, IEnumerable<string> roles) =>
            OmsRequestAccess.CanAttach(request, userId, roles);

        public static void EnsureCanManageAttachments(Request request, string? userId, IEnumerable<string> roles) =>
            OmsRequestAccess.EnsureCanAttach(request, userId, roles);
    }
}
