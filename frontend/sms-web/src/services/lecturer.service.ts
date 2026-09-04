import { api } from './api';

interface Lecturer {
  id: string;
  userId: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  specialization?: string;
  qualifications?: string;
  isVerified: boolean;
  isActive: boolean;
  hireDate: string;
}

interface LecturerDetails extends Lecturer {
  biography?: string;
  officeLocation?: string;
  totalUnitsAllocated: number;
  currentUnitsCount: number;
  units: UnitSummary[];
  assignments: AssignmentSummary[];
}

interface UnitSummary {
  id: string;
  name: string;
  code: string;
  credits: number;
  semesterName: string;
}

interface AssignmentSummary {
  id: string;
  title: string;
  dueDate: string;
  submissionCount: number;
  status: string;
}

interface GetLecturersParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  isVerified?: boolean;
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

export const lecturerService = {
  getLecturers: (params: GetLecturersParams) =>
    api.get<PagedResponse<Lecturer>>('/lecturer', { params }),

  getLecturer: (id: string) =>
    api.get<LecturerDetails>(`/lecturer/${id}`),

  createLecturer: (data: any) =>
    api.post<Lecturer>('/lecturer', data),

  updateLecturer: (id: string, data: any) =>
    api.put<Lecturer>(`/lecturer/${id}`, data),

  deleteLecturer: (id: string) =>
    api.delete(`/lecturer/${id}`),

  verifyLecturer: (id: string) =>
    api.post<Lecturer>(`/lecturer/${id}/verify`),

  getUnits: (lecturerId: string) =>
    api.get<UnitSummary[]>(`/lecturer/${lecturerId}/units`),

  allocateUnit: (lecturerId: string, unitId: string, semesterId: string, isPrimary?: boolean) =>
    api.post(`/lecturer/${lecturerId}/allocate-unit`, { unitId, semesterId, isPrimary }),
};