import { apiClient } from "./api";

export interface EnrollmentSubmissionResult {
  studentId: string;
  courseId: string;
  courseName: string;
  unitsEnrolled: number;
  status: string;
  message: string;
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

  submitReturningEnrollment: (courseId: string, semesterId: string) =>
    apiClient.post<ReturningEnrollmentResult>(`${RETURNING_BASE}/enroll`, {
      courseId,
      semesterId,
    }),

  getCourseHistory: () =>
    apiClient.get<CourseHistory>(`${RETURNING_BASE}/course-history`),
};
