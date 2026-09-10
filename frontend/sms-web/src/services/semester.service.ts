import { api } from './api';

export interface Semester {
  id: string;
  name: string;
  code: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export const semesterService = {
  getSemesters: (params: { includeInactive?: boolean } = {}) =>
    api.get<Semester[]>('/semesters', { params }),
};