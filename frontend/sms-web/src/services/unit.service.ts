import { api } from './api';

interface Unit {
  id: string;
  name: string;
  code: string;
  description?: string;
  credits: number;
  contactHours: number;
  isActive: boolean;
  courseId: string;
  courseName: string;
  courseCode: string;
  prerequisiteUnitId?: string;
  prerequisiteCode?: string;
  prerequisiteName?: string;
  createdDate: string;
}

interface UnitDetails extends Unit {
  learningOutcomes?: string;
  assessmentMethods?: string;
  recommendedTextbooks?: string;
  totalEnrollments: number;
  totalAllocations: number;
  totalAssignments: number;
  totalLectureNotes: number;
  allocatedLecturers: LecturerSummary[];
}

interface LecturerSummary {
  id: string;
  fullName: string;
  employeeNumber: string;
  specialization?: string;
  isPrimary: boolean;
}

interface GetUnitsParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  courseId?: string;
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

export const unitService = {
  getUnits: (params: GetUnitsParams) =>
    api.get<PagedResponse<Unit>>('/units', { params }),

  getUnit: (id: string) =>
    api.get<UnitDetails>(`/units/${id}`),

  createUnit: (data: any) =>
    api.post<Unit>('/units', data),

  updateUnit: (id: string, data: any) =>
    api.put<Unit>(`/units/${id}`, data),

  deleteUnit: (id: string) =>
    api.delete(`/units/${id}`),

  getLecturers: (unitId: string) =>
    api.get<LecturerSummary[]>(`/units/${unitId}/lecturers`),

  getStudents: (unitId: string) =>
    api.get<StudentSummary[]>(`/units/${unitId}/students`),
};

interface StudentSummary {
  id: string;
  studentNumber: string;
  fullName: string;
  email: string;
  status: string;
}