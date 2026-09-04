import { api } from './api';

interface Assignment {
  id: string;
  title: string;
  description?: string;
  unitId: string;
  lecturerId: string;
  semesterId: string;
  maxScore: number;
  weight: number;
  dueDate: string;
  publishedDate?: string;
  closingDate?: string;
  instructions?: string;
  attachments?: string;
  status: string;
  isGraded: boolean;
  allowLateSubmission: boolean;
  latePenaltyPercent: number;
  unitName: string;
  unitCode: string;
  lecturerName: string;
  semesterName: string;
  submissionCount: number;
  gradedCount: number;
}

interface AssignmentSubmission {
  id: string;
  assignmentId: string;
  studentId: string;
  submissionDate: string;
  filePath?: string;
  fileName?: string;
  fileSize: number;
  comments?: string;
  score?: number;
  feedback?: string;
  status: string;
  isLate: boolean;
  gradedDate?: string;
  studentName: string;
  studentNumber: string;
  assignmentTitle: string;
  maxScore: number;
}

interface GetAssignmentsParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  unitId?: string;
  lecturerId?: string;
  semesterId?: string;
  status?: string;
  isGraded?: boolean;
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

export const assignmentService = {
  getAssignments: (params: GetAssignmentsParams) =>
    api.get<PagedResponse<Assignment>>('/assignment', { params }),

  getAssignment: (id: string) =>
    api.get<Assignment>(`/assignment/${id}`),

  createAssignment: (data: any) =>
    api.post<Assignment>('/assignment', data),

  updateAssignment: (id: string, data: any) =>
    api.put<Assignment>(`/assignment/${id}`, data),

  deleteAssignment: (id: string) =>
    api.delete(`/assignment/${id}`),

  getSubmissions: (assignmentId: string) =>
    api.get<AssignmentSubmission[]>(`/assignment/${assignmentId}/submissions`),

  getSubmission: (submissionId: string) =>
    api.get<AssignmentSubmission>(`/assignment/submissions/${submissionId}`),

  submitAssignment: (data: { assignmentId: string; studentId: string; filePath: string; fileName: string; fileSize: number; comments?: string }) =>
    api.post<AssignmentSubmission>('/assignment/submit', data),

  gradeSubmission: (submissionId: string, score: number, feedback?: string) =>
    api.put<AssignmentSubmission>(`/assignment/submissions/${submissionId}/grade`, { score, feedback }),

  getStudentAssignments: (studentId: string, semesterId?: string) =>
    api.get<Assignment[]>(`/assignment/student/${studentId}`, { params: { semesterId } }),
};