import { api } from './api';

interface DashboardStatistics {
  totalStudents: number;
  totalLecturers: number;
  activeCourses: number;
  pendingAssignments: number;
  totalEnrollments: number;
  totalGrades: number;
  totalAssignments: number;
  totalRooms: number;
  occupiedRooms: number;
  pendingVerifications: number;
  recentActivities: number;
  averageGPA: number;
  occupancyRate: number;
  studentsByProgramme: Record<string, number>;
  gradesDistribution: Record<string, number>;
  monthlyEnrollments: MonthlyEnrollment[];
}

interface MonthlyEnrollment {
  month: string;
  year: number;
  count: number;
  cumulative: number;
}

interface Activity {
  message: string;
  user: string;
  timestamp: string;
  icon?: string;
  color?: string;
  status?: string;
  link?: string;
}

interface EnrollmentTrends {
  enrollmentData: MonthlyEnrollment[];
  programmeDistribution: ProgrammeEnrollment[];
  genderDistribution: GenderDistribution[];
}

interface ProgrammeEnrollment {
  programmeName: string;
  count: number;
  percentage: number;
}

interface GenderDistribution {
  gender: string;
  count: number;
  percentage: number;
}

interface Event {
  title: string;
  description?: string;
  date: string;
  time?: string;
  location?: string;
  eventType?: string;
  color?: string;
}

export const dashboardService = {
  getStatistics: () =>
    api.get<DashboardStatistics>('/dashboard/statistics'),

  getRecentActivities: (count: number = 10) =>
    api.get<Activity[]>('/dashboard/activities', { params: { count } }),

  getEnrollmentTrends: (academicYearId?: number) =>
    api.get<EnrollmentTrends>('/dashboard/enrollment-trends', { params: { academicYearId } }),

  getUpcomingEvents: (days: number = 30) =>
    api.get<Event[]>('/dashboard/upcoming-events', { params: { days } }),

  getTopStudents: (count: number = 10, semesterId?: string) =>
    api.get<StudentTop[]>('/dashboard/top-students', { params: { count, semesterId } }),

  /**
   * Dashboard payload for the currently authenticated lecturer: real course
   * offerings taught (with units), unit allocations, and the accommodation
   * assignment resolved server-side from persisted relationships.
   */
  getMyLecturerDashboard: () =>
    api.get<MyLecturerDashboard>('/dashboard/lecturer-me'),

  /**
   * Dashboard payload for the currently authenticated student: real active
   * course-offering enrollments (with units) and the accommodation assignment.
   */
  getMyStudentDashboard: () =>
    api.get<MyStudentDashboard>('/dashboard/student-me'),
};

// ────────────────────────────────────────────────────────────────────────────
// My Dashboard types — must mirror SMS.Application.DTOs.DashboardMyDtos.
// The endpoints return a plain DTO object (NOT a paged envelope).
// ────────────────────────────────────────────────────────────────────────────

export interface DashboardUnit {
  unitId: string;
  courseOfferingUnitId?: string | null;
  name: string;
  code: string;
  credits: number;
}

export interface LecturerCourse {
  courseOfferingId: string;
  offeringCode: string;
  courseId: string;
  courseName: string;
  courseCode: string;
  academicYearName: string;
  semesterName: string;
  intake?: string | null;
  status: string;
  isPrimary: boolean;
  units: DashboardUnit[];
}

export interface MyLecturerDashboard {
  lecturerId: string;
  fullName: string;
  title?: string | null;
  email: string;
  employeeNumber: string;
  courses: LecturerCourse[];
  unitAllocations: DashboardUnit[];
  accommodation: AccommodationAssignmentSummary | null;
}

export interface StudentCourse {
  courseOfferingId: string;
  offeringCode: string;
  courseId: string;
  courseName: string;
  courseCode: string;
  academicYearName: string;
  semesterName: string;
  status: string;
  confirmationStatus: string;
  attemptNumber: number;
  units: DashboardUnit[];
}

export interface MyStudentDashboard {
  studentId: string;
  fullName: string;
  title?: string | null;
  email: string;
  studentNumber: string;
  academicStatus: string;
  enrollments: StudentCourse[];
  accommodation: AccommodationAssignmentSummary | null;
}

export interface AccommodationAssignmentSummary {
  id: string;
  studentId?: string | null;
  lecturerId?: string | null;
  occupantType?: number | string | null;
  status: string;
  houseId?: string | null;
  houseNumber?: string | null;
  houseName?: string | null;
  houseCapacity?: number;
  houseOccupiedCount?: number;
  laneId?: string | null;
  laneName?: string | null;
  roomNumber?: string | null;
  blockName?: string | null;
  buildingName?: string | null;
  semesterId?: string | null;
  semesterName?: string | null;
  assignmentDate?: string | null;
  moveInDate?: string | null;
  checkInDate?: string | null;
  remarks?: string | null;
}

interface StudentTop {
  studentId: string;
  studentName: string;
  studentNumber: string;
  programmeName: string;
  gpa: number;
  creditsEarned: number;
}