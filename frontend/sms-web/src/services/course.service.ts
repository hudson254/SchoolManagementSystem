import { api } from './api';

interface Course {
  id: string;
  name: string;
  code: string;
  description?: string;
  duration: number;
  totalCredits: number;
  isActive: boolean;
  departmentId: string;
  departmentName: string;
  departmentCode: string;
  createdDate: string;
}

interface CourseDetails extends Course {
  admissionRequirements?: string;
  objectives?: string;
  totalUnits: number;
  totalProgrammes: number;
  totalStudents: number;
  units: UnitSummary[];
  programmes: ProgrammeSummary[];
}

interface UnitSummary {
  id: string;
  name: string;
  code: string;
  credits: number;
  contactHours: number;
  isActive: boolean;
}

interface ProgrammeSummary {
  id: string;
  name: string;
  code: string;
  duration: number;
  totalCredits: number;
}

interface GetCoursesParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  departmentId?: string;
  isActive?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
}

interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const courseService = {
  getCourses: (params: GetCoursesParams) =>
    api.get<PagedResponse<Course>>('/course', { params }),

  getCourse: (id: string) =>
    api.get<CourseDetails>(`/course/${id}`),

  createCourse: (data: any) =>
    api.post<Course>('/course', data),

  updateCourse: (id: string, data: any) =>
    api.put<Course>(`/course/${id}`, data),

  deleteCourse: (id: string) =>
    api.delete(`/course/${id}`),

  getUnits: (courseId: string) =>
    api.get<UnitSummary[]>(`/course/${courseId}/units`),

  getDepartments: () =>
    api.get<any[]>('/departments'),

  getProgrammes: (courseId?: string) =>
    courseId
      ? api.get<ProgrammeSummary[]>(`/course/${courseId}/programmes`)
      : api.get<any[]>('/programmes'),
};
