import { api } from './api';

export interface ClassItem {
  id: string;
  name: string;
  code: string;
  description?: string;
  unitId: string;
  unitName: string;
  unitCode: string;
  lecturerId: string;
  lecturerName: string;
  lecturerEmail?: string;
  semesterId: string;
  semesterName: string;
  maxCapacity: number;
  currentEnrollment: number;
  startDate: string;
  endDate: string;
  scheduleDay?: string;
  startTime?: string;
  endTime?: string;
  isActive: boolean;
  createdDate: string;
}

export interface GetClassesParams {
  searchTerm?: string;
  semesterId?: string;
  unitId?: string;
  includeInactive?: boolean;
}

export interface CreateClassRequest {
  name: string;
  code: string;
  description?: string;
  unitId: string;
  lecturerId: string;
  semesterId: string;
  maxCapacity?: number;
  startDate: string;
  endDate: string;
  scheduleDay?: string;
  startTime?: string;
  endTime?: string;
  isActive?: boolean;
}

export interface UpdateClassRequest extends Partial<CreateClassRequest> {}

export const classesService = {
  getClasses: (params: GetClassesParams = {}) =>
    api.get<ClassItem[]>('/classes', { params }),

  getClass: (id: string) =>
    api.get<ClassItem>(`/classes/${id}`),

  createClass: (data: CreateClassRequest) =>
    api.post<ClassItem>('/classes', data),

  updateClass: (id: string, data: UpdateClassRequest) =>
    api.put<ClassItem>(`/classes/${id}`, data),

  deleteClass: (id: string) =>
    api.delete(`/classes/${id}`),
};