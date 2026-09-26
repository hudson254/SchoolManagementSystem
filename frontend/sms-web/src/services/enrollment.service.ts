import { apiClient } from "./api";

export interface EnrollmentSubmissionResult {
  studentId: string;
  courseId: string;
  courseName: string;
  unitsEnrolled: number;
  status: string;
  message: string;
  courseOfferingId?: string | null;
  courseOfferingEnrollmentCreated?: boolean;
}

export interface StudentEnrollmentStatus {
  studentId: string;
  studentNumber: string;
  fullName: string;
  email: string;
  registrationStatus: string;
  hasSelectedCourse: boolean;
  selectedCourseId?: string;
  selectedCourseName?: string;
  unitsCount: number;
  needsCourseSelection: boolean;
  isPendingApproval: boolean;
  isApproved: boolean;
  message?: string;
}

export interface CourseOption {
  id: string;
  name: string;
  code: string;
  description?: string | null;
  credits?: number;
  duration?: number;
}

/**
 * A unit of a course the student is choosing from. Mirrors
 * StudentSelectableUnitDto in SMS.Application.
 */
export interface SelectableUnit {
  id: string;
  code: string;
  name: string;
  credits: number;
}

export interface ReturningEnrollmentResult {
  studentId: string;
  courseId: string;
  courseName: string;
  unitsEnrolled: number;
  status: string;
  message: string;
}

export interface CourseHistoryItem {
  courseId: string;
  courseName: string;
  courseCode: string;
  semesterName: string;
  enrolledDate: string;
  status: string;
}

export interface CourseHistory {
  studentId: string;
  studentNumber: string;
  fullName: string;
  message?: string;
  enrollments: CourseHistoryItem[];
  totalCount: number;
}

const ENROLLMENT_BASE = "/enrollment";
const RETURNING_BASE = "/returning-user";

export const enrollmentService = {
  submitEnrollment: (courseId: string, semesterId?: string) =>
    apiClient.post<EnrollmentSubmissionResult>(`${ENROLLMENT_BASE}/submit-enrollment`, {
      courseId,
      semesterId,
    }),

  getMyStatus: () =>
    apiClient.get<StudentEnrollmentStatus>(`${ENROLLMENT_BASE}/my-status`),

  /**
   * Courses a logged-in student may choose from. Uses the dedicated
   * student-authorized endpoint under /enrollment, NOT /courses, which is
   * restricted to moderators and returns 403 for a Student token.
   */
  getAvailableCourses: () =>
    apiClient.get<CourseOption[]>(`${ENROLLMENT_BASE}/available-courses`),

  /**
   * Active units of a selectable course (wizard step 2). Student-authorized
   * counterpart of the moderator-only GET /courses/{id}/units.
   */
  getAvailableCourseUnits: (courseId: string) =>
    apiClient.get<SelectableUnit[]>(`${ENROLLMENT_BASE}/available-courses/${courseId}/units`),

  submitReturningEnrollment: (courseId: string, semesterId: string) =>
    apiClient.post<ReturningEnrollmentResult>(`${RETURNING_BASE}/enroll`, {
      courseId,
      semesterId,
    }),

  getCourseHistory: () =>
    apiClient.get<CourseHistory>(`${RETURNING_BASE}/course-history`),
};
