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
    api.get<PagedResponse<Course>>('/courses', { params }),

  getCourse: (id: string) =>
    api.get<CourseDetails>(`/courses/${id}`),

  createCourse: (data: any) =>
    api.post<Course>('/courses', data),

  updateCourse: (id: string, data: any) =>
    api.put<Course>(`/courses/${id}`, data),

  deleteCourse: (id: string) =>
    api.delete(`/courses/${id}`),

  getUnits: (courseId: string) =>
    api.get<UnitSummary[]>(`/courses/${courseId}/units`),

  getDepartments: () =>
    api.get<any[]>('/departments'),

  getProgrammes: (courseId?: string) =>
    courseId
      ? api.get<ProgrammeSummary[]>(`/courses/${courseId}/programmes`)
      : api.get<any[]>('/programmes'),
};
