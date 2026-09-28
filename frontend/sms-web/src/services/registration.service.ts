import { apiClient } from "./api";
import { RegistrationUnit } from "../types/user.types";

/**
 * API access for the public registration wizard.
 *
 * The registration page runs BEFORE the registrant has an account, so it cannot
 * use the authenticated surfaces:
 *   - GET /courses/{id}/units                    -> ModeratorAccess (403)
 *   - GET /enrollment/available-courses/{id}/units -> StudentAccess (401/403)
 *
 * `GET /auth/active-courses/{id}/units` is the anonymous counterpart used by the
 * verification step. It applies the same active / not-deleted, tenant-scoped
 * filter as the enrollment command, so the units rendered on the review step are
 * exactly the units the backend will persist.
 */
const AUTH_BASE = "/auth";

export const registrationService = {
  getActiveCourses: () => apiClient.get(`${AUTH_BASE}/active-courses`),

  /**
   * Active units of a course, for the course/unit verification step.
   *
   * The endpoint returns a bare JSON array (not a paged envelope). It is
   * normalized defensively so an unexpected envelope yields an empty list
   * rather than a crash, and the caller surfaces a real error state instead of
   * silently rendering an empty selection.
   */
  getCourseUnits: async (courseId: string): Promise<RegistrationUnit[]> => {
    const result = await apiClient.get(
      `${AUTH_BASE}/active-courses/${courseId}/units`
    );

    if (Array.isArray(result)) return result as RegistrationUnit[];

    // Tolerate a paged envelope should the endpoint shape ever change.
    if (result && typeof result === "object") {
      const items = (result as { items?: unknown }).items;
      if (Array.isArray(items)) return items as RegistrationUnit[];
    }

    return [];
  },
};
