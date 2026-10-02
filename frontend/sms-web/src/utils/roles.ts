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

// ─────────────────────────────────────────────────────────────────────────────
// Study Materials — Academics → Study Materials.
//
// Lectures read/upload materials for units they are APPOINTED TO TEACH; students
// read/download materials for units they are ENROLLED TO STUDY. Both roles get
// the menu entry. Administrator/Coordinator/Coordinator-tier staff already reach
// any unit's materials through the course-offering Units tab, and Receptionist
// has no study-material workflow, so neither is granted this shortcut.
//
// PRESENTATION ONLY. The API re-derives entitlement per request from persisted
// teaching/enrollment relationships; this list decides what is worth rendering.
// ─────────────────────────────────────────────────────────────────────────────

/** Roles shown the Academics → Study Materials entry. */
export const STUDY_MATERIALS_ROLES: string[] = [LECTURER, STUDENT];

// ─────────────────────────────────────────────────────────────────────────────
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

// ─────────────────────────────────────────────────────────────────────────────
// OMS Request permissions — mirrors
// SMS.Application.Common.OmsAuthorization role mappings and the Oms.* request
// policies wired in Program.cs.
//
// These helpers are PRESENTATION ONLY. They decide which affordances are worth
// rendering so a user is not shown buttons that the API will certainly reject.
// They are never a security control: every one of these actions is independently
// re-checked server-side by role AND by object-level ownership
// (OmsRequestAccess.CanView / CanAttach / CanDeleteAttachment).
// ─────────────────────────────────────────────────────────────────────────────

/** Oms.CanViewRequests — Admin, Coordinator, Lecturer. */
export const OMS_REQUEST_VIEW_ROLES: string[] = [
  SYSTEM_ADMINISTRATOR,
  ADMINISTRATOR,
  COORDINATOR,
  LECTURER,
];

/** Oms.ViewAllRequests — Admin, Coordinator (the privileged "all" queue). */
export const OMS_REQUEST_VIEW_ALL_ROLES: string[] = [
  SYSTEM_ADMINISTRATOR,
  ADMINISTRATOR,
  COORDINATOR,
];

/**
 * Oms.CreateRequest / Oms.SubmitRequest / Oms.CancelOwnRequest — the roles that
 * may raise and progress their own requests. Mirrors CreateRequestRoles.
 */
export const OMS_REQUEST_SELF_ROLES: string[] = [
  SYSTEM_ADMINISTRATOR,
  ADMINISTRATOR,
  COORDINATOR,
  LECTURER,
  STUDENT,
];

/** Oms.ViewOwnRequests — includes Receptionist, which cannot create requests. */
export const OMS_REQUEST_VIEW_OWN_ROLES: string[] = [
  SYSTEM_ADMINISTRATOR,
  ADMINISTRATOR,
  COORDINATOR,
  LECTURER,
  STUDENT,
  RECEPTIONIST,
];

/** Oms.AssignRequest / Oms.ReassignRequest / Oms.ApproveRequest / Oms.CompleteRequest. */
export const OMS_REQUEST_DECISION_ROLES: string[] = [
  SYSTEM_ADMINISTRATOR,
  ADMINISTRATOR,
  COORDINATOR,
];

/** Oms.ManageRequestTypes — Administrator tier. */
export const OMS_REQUEST_ADMIN_ROLES: string[] = [SYSTEM_ADMINISTRATOR, ADMINISTRATOR];

/** Any role that may read the request queue at all. */
export const canViewOmsRequests = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_REQUEST_VIEW_OWN_ROLES);

/** The privileged full-tenant queue. Drives visibility of the "all" scope. */
export const canViewAllOmsRequests = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_REQUEST_VIEW_ALL_ROLES);

/** May raise a request (Oms.CreateRequest). */
export const canCreateOmsRequest = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_REQUEST_SELF_ROLES);

/** Oms.AssignRequest / Oms.ReassignRequest. */
export const canAssignOmsRequests = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_REQUEST_DECISION_ROLES);

/** Oms.ApproveRequest / Oms.RejectRequest. */
export const canDecideOmsRequests = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_REQUEST_DECISION_ROLES);

/** Oms.CompleteRequest / Oms.EscalateRequest. */
export const canCompleteOmsRequests = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_REQUEST_DECISION_ROLES);

/** Oms.ManageRequestTypes. */
export const canManageOmsRequestTypes = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_REQUEST_ADMIN_ROLES);
