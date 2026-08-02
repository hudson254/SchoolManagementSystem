import { api } from './api';

export interface TimetableEntry {
  id: string;
  classId: string;
  className: string;
  unitId: string;
  unitName: string;
  unitCode: string;
  lecturerId: string;
  lecturerName: string;
  semesterId: string;
  semesterName: string;
  dayOfWeek: string;
  startTime: string;
  endTime: string;
  venue: string;
  createdDate: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface GetTimetablesParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  classId?: string;
  lecturerId?: string;
  studentId?: string;
  semesterId?: string;
  dayOfWeek?: string;
  venue?: string;
}

export interface CreateTimetableRequest {
  classId: string;
  unitId: string;
  lecturerId: string;
  semesterId: string;
  dayOfWeek: string;
  startTime: string;
  endTime: string;
  venue?: string;
}

export interface UpdateTimetableRequest extends Partial<CreateTimetableRequest> {}

export interface ConflictCheckRequest {
  classId?: string;
  lecturerId?: string;
  venue?: string;
  semesterId?: string;
}

export interface TimetableConflict {
  description: string;
  details: string;
}

export interface ConflictCheckResponse {
  conflicts: TimetableConflict[];
  hasConflicts: boolean;
}

export interface Venue {
  id: string;
  name: string;
  capacity: number;
  type: string;
  isAvailable: boolean;
}

export const timetableService = {
  getTimetables: (params: GetTimetablesParams) =>
    api.get<PagedResponse<TimetableEntry>>('/timetables', { params }),

  getTimetable: (id: string) =>
    api.get<TimetableEntry>(`/timetables/${id}`),

  createTimetable: (data: CreateTimetableRequest) =>
    api.post<TimetableEntry>('/timetables', data),

  updateTimetable: (id: string, data: UpdateTimetableRequest) =>
    api.put<TimetableEntry>(`/timetables/${id}`, data),

  deleteTimetable: (id: string) =>
    api.delete(`/timetables/${id}`),

  getClassTimetable: (classId: string, semesterId?: string) =>
    api.get<TimetableEntry[]>(`/timetables/class/${classId}`, { params: { semesterId } }),

  getLecturerTimetable: (lecturerId: string, semesterId?: string) =>
    api.get<TimetableEntry[]>(`/timetables/lecturer/${lecturerId}`, { params: { semesterId } }),

  getStudentTimetable: (studentId: string, semesterId?: string) =>
    api.get<TimetableEntry[]>(`/timetables/student/${studentId}`, { params: { semesterId } }),

  getWeeklyTimetable: (semesterId: string, weekStart?: string) =>
    api.get<TimetableEntry[]>('/timetables/weekly', { params: { semesterId, weekStart } }),

  getAvailableVenues: (params: { dayOfWeek?: string; startTime?: string; endTime?: string; semesterId?: string }) =>
    api.get<Venue[]>('/timetables/venues/available', { params }),

  checkConflicts: (params: ConflictCheckRequest) =>
    api.get<ConflictCheckResponse>('/timetables/conflicts', { params }),
};

