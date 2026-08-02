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
};

interface StudentTop {
  studentId: string;
  studentName: string;
  studentNumber: string;
  programmeName: string;
  gpa: number;
  creditsEarned: number;
}