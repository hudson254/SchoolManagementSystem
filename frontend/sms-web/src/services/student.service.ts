import { api } from './api';
import {
  Student,
  StudentDetails,
  CreateStudentRequest,
  UpdateStudentRequest,
  Transcript,
} from '../types/student.types';

interface GetStudentsParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  academicStatus?: string;
  programmeId?: string;
  isEnrolled?: boolean;
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

export const studentService = {
  getStudents: (params: GetStudentsParams) =>
    api.get<PagedResponse<Student>>('/students', { params }),

  getStudent: (id: string) =>
    api.get<StudentDetails>(`/students/${id}`),

  createStudent: (data: CreateStudentRequest) =>
    api.post<Student>('/students', data),

  updateStudent: (id: string, data: UpdateStudentRequest) =>
    api.put<Student>(`/students/${id}`, data),

  deleteStudent: (id: string) =>
    api.delete(`/students/${id}`),

  getEnrollments: (studentId: string, semesterId?: string) =>
    api.get(`/students/${studentId}/enrollments`, { params: { semesterId } }),

  getGrades: (studentId: string, semesterId?: string) =>
    api.get(`/students/${studentId}/grades`, { params: { semesterId } }),

  getTranscript: (studentId: string) =>
    api.get<Transcript>(`/students/${studentId}/transcript`),

  getTimetable: (studentId: string, semesterId?: string) =>
    api.get(`/students/${studentId}/timetable`, { params: { semesterId } }),

  enrollStudent: (studentId: string, unitId: string, semesterId: string) =>
    api.post(`/students/${studentId}/enroll`, { unitId, semesterId }),

  dropStudent: (studentId: string, unitId: string, semesterId: string, reason?: string) =>
    api.post(`/students/${studentId}/drop`, { unitId, semesterId, reason }),
};