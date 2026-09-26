using SMS.Application.Common;
using SMS.Domain.Entities;
using System;
using System.Collections.Generic;

// ICurrentUserService exists in both SMS.Application.Common.Interfaces and
// SMS.Domain.Interfaces. The Application-layer alias is the one this helper is
// written against, matching CreateRequestCommandHandler.
using ICurrentUserService = SMS.Application.Common.Interfaces.ICurrentUserService;

namespace SMS.Application.Features.OMS.Services
{
    /// <summary>
    /// Shared validation/authorization primitives for thin module request
    /// adapters.
    ///
    /// These helpers deliberately do *not* implement workflow. They only
    /// answer three questions an adapter must ask before delegating to the
    /// generic Request create command:
    ///
    ///   1. Is the caller authenticated and allowed to raise requests at all?
    ///   2. Does the referenced module entity exist inside the caller's tenant?
    ///   3. Is the caller entitled to raise a request *about* that entity?
    ///
    /// Everything after that (numbering, lifecycle, history, notifications,
    /// audit) belongs to the Request core.
    /// </summary>
    public static class ModuleRequestAdapterSupport
    {
        /// <summary>
        /// Ensures the caller is authenticated and holds a role permitted to
        /// raise OMS requests. The Request core repeats this check inside
        /// <c>CreateRequestCommand</c>; doing it here as well means an adapter
        /// fails fast with a precise error before touching module data.
        /// </summary>
        public static void EnsureCanRaiseRequest(ICurrentUserService currentUser)
        {
            if (currentUser == null || !currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
                throw new Exceptions.UnauthorizedException("An authenticated user is required to raise a request.");

            if (!OmsAuthorization.HasAnyRole(currentUser.Roles, OmsAuthorization.CreateRequestRoles))
                throw new Exceptions.ForbiddenException(OmsPermissions.CreateRequest, currentUser.UserId);
        }

        /// <summary>
        /// Throws a validation error when a referenced module entity is absent.
        /// Absence is reported as a validation problem rather than leaking a
        /// 404, because the adapter deliberately does not expose the module's
        /// own lookup API surface.
        /// </summary>
        public static T EnsureFound<T>(T? entity, string field, string code) where T : class
        {
            if (entity == null)
                throw new Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    [field] = new[] { $"No {code} was found for the supplied reference in your institution." }
                });

            return entity;
        }

        /// <summary>
        /// Ensures the caller may raise a request that concerns a specific
        /// student.
        ///
        /// A Student may only raise requests about their own record. Staff
        /// holding a privileged OMS queue role may raise requests about any
        /// student (for example a coordinator handling an exception raised at
        /// the front office). Ownership is matched on the linked identity user
        /// id, falling back to the student's email, which is how the existing
        /// authentication model binds a student login to their record.
        /// </summary>
        public static void EnsureCanRequestForStudent(ICurrentUserService currentUser, Student student)
        {
            if (student == null)
                throw new ArgumentNullException(nameof(student));

            if (OmsRequestAccess.CanViewAll(currentUser.Roles))
                return;

            var isOwnerByUserId =
                !string.IsNullOrWhiteSpace(student.UserId) &&
                string.Equals(student.UserId, currentUser.UserId, StringComparison.OrdinalIgnoreCase);

            var isOwnerByEmail =
                !string.IsNullOrWhiteSpace(student.Email) &&
                !string.IsNullOrWhiteSpace(currentUser.Email) &&
                string.Equals(student.Email.Trim(), currentUser.Email.Trim(), StringComparison.OrdinalIgnoreCase);

            if (!isOwnerByUserId && !isOwnerByEmail)
                throw new Exceptions.ForbiddenException(OmsPermissions.CreateRequest, currentUser.UserId);
        }

        /// <summary>
        /// Ensures the caller may raise a request that concerns a specific
        /// lecturer's own academic material (assignments, assessments).
        ///
        /// Mirrors <see cref="EnsureCanRequestForStudent"/> for staff-owned
        /// context: a lecturer may only raise requests about their own teaching
        /// material, while administrators/coordinators may act on any
        /// lecturer's behalf.
        /// </summary>
        public static void EnsureCanRequestForLecturer(ICurrentUserService currentUser, string? lecturerUserId)
        {
            if (currentUser == null)
                throw new ArgumentNullException(nameof(currentUser));

            if (OmsRequestAccess.CanViewAll(currentUser.Roles))
                return;

            var isSelf =
                !string.IsNullOrWhiteSpace(lecturerUserId) &&
                string.Equals(lecturerUserId, currentUser.UserId, StringComparison.OrdinalIgnoreCase);

            if (!isSelf)
                throw new Exceptions.ForbiddenException(OmsPermissions.CreateRequest, currentUser.UserId);
        }

        /// <summary>
        /// Ensures a supplied RequestType code belongs to the allow-list owned by
        /// the calling adapter. Without this, a client could raise an
        /// "enrollment" request that is actually typed as, say, a certificate
        /// correction, and the typed relationships on the request would be
        /// misleading. The generic create command still validates that the type
        /// exists and is active.
        /// </summary>
        public static string EnsureAllowedRequestType(string? requestType, IReadOnlyCollection<string> allowed, string module)
        {
            var code = (requestType ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(code))
                throw new Exceptions.ValidationException(new Dictionary<string, string[]>
                {
                    ["RequestType"] = new[] { "A request type is required." }
                });

            foreach (var candidate in allowed)
            {
                if (string.Equals(candidate, code, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            throw new Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["RequestType"] = new[]
                {
                    $"'{code}' is not a valid {module} request type. Allowed values: {string.Join(", ", allowed)}."
                }
            });
        }

        /// <summary>
        /// Builds a request title when the caller did not supply one, using the
        /// module context the adapter has already resolved. Keeps module entry
        /// points free of mandatory title fields while still producing a
        /// meaningful queue entry.
        /// </summary>
        public static string BuildTitle(string? suppliedTitle, string fallback)
        {
            var title = (suppliedTitle ?? string.Empty).Trim();
            return string.IsNullOrEmpty(title) ? fallback : title;
        }
    }
}