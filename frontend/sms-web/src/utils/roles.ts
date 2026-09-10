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