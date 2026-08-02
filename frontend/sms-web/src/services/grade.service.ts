import { api } from './api';

export interface Grade {
  id: string;
  studentId: string;
  studentName: string;
  studentNumber: string;
  unitId: string;
  unitName: string;
  unitCode: string;
  semesterId: string;
  semesterName: string;
  credits: number;
  score: number | null;
  gradeValue: string | null;
  isPublished: boolean;
  publishedDate?: string;
  createdDate: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface GetGradesParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  studentId?: string;
  unitId?: string;
  semesterId?: string;
  sortBy?: string;
  sortDescending?: boolean;
  isPublished?: boolean;
}

export interface CreateGradeRequest {
  studentId: string;
  unitId: string;
  semesterId: string;
  score?: number;
  gradeValue?: string;
  isPublished?: boolean;
}

export interface UpdateGradeRequest {
  score?: number;
  gradeValue?: string;
  isPublished?: boolean;
}

export const gradeService = {
  getGrades: (params: GetGradesParams) =>
    api.get<PagedResponse<Grade>>('/grades', { params }),

  getGrade: (id: string) =>
    api.get<Grade>(`/grades/${id}`),

  getUnitGrades: (unitId: string, semesterId?: string) =>
    api.get<Grade[]>(`/grades/unit/${unitId}`, { params: { semesterId } }),

  getStudentGrades: (studentId: string, semesterId?: string) =>
    api.get<Grade[]>(`/students/${studentId}/grades`, { params: { semesterId } }),

  createGrade: (data: CreateGradeRequest) =>
    api.post<Grade>('/grades', data),

  updateGrade: (id: string, data: UpdateGradeRequest) =>
    api.put<Grade>(`/grades/${id}`, data),

  deleteGrade: (id: string) =>
    api.delete(`/grades/${id}`),

  publishGrade: (id: string) =>
    api.post<Grade>(`/grades/${id}/publish`),

  publishAll: (unitId: string, semesterId: string) =>
    api.post<{ published: number }>('/grades/publish-all', { unitId, semesterId }),

  exportGrades: (params: GetGradesParams) =>
    api.get<Blob>('/grades/export', { params, responseType: 'blob' }),
};

