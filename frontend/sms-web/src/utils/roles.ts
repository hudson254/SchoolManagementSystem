/**
 * Role-based UI helpers.
 *
 * The backend authorizes via named policies (Program.cs):
 *   - SystemAdministratorAccess : SystemAdministrator
 *   - AdministratorAccess      : SystemAdministrator, Administrator
 *   - ModeratorAccess          : SystemAdministrator, Administrator, Coordinator
 *   - LecturerAccess           : SystemAdministrator, Administrator, Coordinator, Lecturer
 *   - StudentAccess            : SystemAdministrator, Administrator, Coordinator, Lecturer, Student
 *   - ReceptionistAccess       : SystemAdministrator, Administrator, Coordinator, Receptionist
 *
 * The UI helpers mirror those policies so the visibility of buttons/actions
 * always agrees with what the backend will accept.
 */

export const SYSTEM_ADMINISTRATOR = 'SystemAdministrator';
export const ADMINISTRATOR = 'Administrator';
export const COORDINATOR = 'Coordinator';
export const LECTURER = 'Lecturer';
export const STUDENT = 'Student';
export const RECEPTIONIST = 'Receptionist';

/** True when the user holds at least one of the given roles. */
export const hasAnyRole = (roles: string[] | undefined | null, ...allowed: string[]): boolean => {
  if (!roles || roles.length === 0) return false;
  return roles.some((role) => allowed.includes(role));
};

/** SystemAdministrator / Administrator — can administrate (users, delete). */
export const canAdministrate = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, SYSTEM_ADMINISTRATOR, ADMINISTRATOR);

/**
 * SystemAdministrator / Administrator / Coordinator — can manage academic
 * operations (create/modify courses, units, offerings, classes, timetable,
 * calendar events). Mirrors ModeratorAccess.
 */
export const canManageAcademic = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, SYSTEM_ADMINISTRATOR, ADMINISTRATOR, COORDINATOR);

// ────────────────────────────────────────────────────────────────────────────
// OMS (Order Management System) — mirrors SMS.Application.Common.OmsAuthorization
// and the Oms.* policies wired in Program.cs (Phase 2C) so UI visibility always
// agrees with what the backend will accept. The API remains authoritative;
// these helpers only decide which affordances are worth rendering.
// ────────────────────────────────────────────────────────────────────────────

/** View orders / OMS dashboard: Admin, Coordinator, Lecturer (view-only). */
export const OMS_VIEW_ROLES: string[] = [
  SYSTEM_ADMINISTRATOR,
  ADMINISTRATOR,
  COORDINATOR,
  LECTURER,
];

/** Create / edit (own, Draft) / submit orders: Coordinator tier. */
export const OMS_MANAGE_ROLES: string[] = [
  SYSTEM_ADMINISTRATOR,
  ADMINISTRATOR,
  COORDINATOR,
];

/** Cancel any non-final order: Administrator tier. */
export const OMS_ADMIN_ROLES: string[] = [SYSTEM_ADMINISTRATOR, ADMINISTRATOR];

/** Oms.CanViewOrders — see the OMS dashboard and order lists. */
export const canViewOmsOrders = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_VIEW_ROLES);

/** Oms.CanCreateOrder / Oms.CanEditOrder / Oms.CanSubmitOrder. */
export const canManageOmsOrders = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_MANAGE_ROLES);

/**
 * Oms.CanCancelOwnOrder — same role set as manage; ownership is enforced by
 * the API handler (the frontend never decides ownership as a security control).
 */
export const canCancelOwnOmsOrder = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_MANAGE_ROLES);

/** Oms.CanCancelAnyOrder — the broader Administrator-only cancellation. */
export const canCancelAnyOmsOrder = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_ADMIN_ROLES);
