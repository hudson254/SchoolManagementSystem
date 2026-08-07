import { api } from './api';

export enum CourseOfferingStatus {
  Draft = 'Draft',
  Scheduled = 'Scheduled',
  Active = 'Active',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
}

export enum ConfirmationStatus {
  Pending = 'Pending',
  Confirmed = 'Confirmed',
  Rejected = 'Rejected',
  IssueReported = 'IssueReported',
}

export enum AssignmentIssueStatus {
  Open = 'Open',
  UnderReview = 'UnderReview',
  Resolved = 'Resolved',
  Closed = 'Closed',
}

export interface CourseOffering {
  id: string;
  offeringCode: string;
  courseId: string;
  courseName?: string;
  courseCode?: string;
  academicYearId: string;
  academicYearName?: string;
  semesterId: string;
  semesterName?: string;
  intake?: string;
  startDate: string;
  endDate: string;
  registrationStartDate?: string;
  registrationEndDate?: string;
  status: CourseOfferingStatus;
  isActive: boolean;
  notes?: string;
  totalUnits: number;
  totalEnrollments: number;
  totalLecturers: number;
  createdDate: string;
}

export interface CourseOfferingUnit {
  id: string;
  courseOfferingId: string;
  unitId?: string;
  name: string;
  code: string;
  description?: string;
  credits: number;
  contactHours: number;
  order: number;
  learningOutcomes?: string;
  assessmentMethods?: string;
  assessmentWeighting?: string;
  isActive: boolean;
}

export interface CourseOfferingLecturer {
  id: string;
  courseOfferingId: string;
  lecturerId: string;
  lecturerName?: string;
  lecturerEmail?: string;
  isPrimary: boolean;
  role?: string;
  assignedDate: string;
  isActive: boolean;
}

export interface CourseOfferingEnrollment {
  id: string;
  courseOfferingId: string;
  offeringCode?: string;
  studentId: string;
  studentName?: string;
  studentNumber?: string;
  enrollmentDate: string;
  status: string;
  isActive: boolean;
  attemptNumber: number;
  confirmationStatus: ConfirmationStatus;
  confirmedDate?: string;
  dropDate?: string;
  notes?: string;
}

export interface AssignmentIssueReport {
  id: string;
  courseOfferingId: string;
  offeringCode?: string;
  studentId?: string;
  studentName?: string;
  lecturerId?: string;
  lecturerName?: string;
  issueType: string;
  description: string;
  status: AssignmentIssueStatus;
  resolution?: string;
  resolvedDate?: string;
  reportedDate: string;
}

export interface CourseOfferingDetails extends CourseOffering {
  units: CourseOfferingUnit[];
  lecturers: CourseOfferingLecturer[];
  enrollments: CourseOfferingEnrollment[];
}

export interface GetCourseOfferingsParams {
  courseId?: string;
  academicYearId?: string;
  semesterId?: string;
  searchTerm?: string;
  includeInactive?: boolean;
}

interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const courseOfferingService = {
  getCourseOfferings: (params: GetCourseOfferingsParams = {}) =>
    api.get<PagedResponse<CourseOffering>>('/courseoffering', { params }),

  getCourseOffering: (id: string) =>
    api.get<CourseOfferingDetails>(`/courseoffering/${id}`),

  getUnits: (id: string) =>
    api.get<CourseOfferingUnit[]>(`/courseoffering/${id}/units`),

  createCourseOffering: (data: any) =>
    api.post<CourseOffering>('/courseoffering', data),

  updateCourseOffering: (id: string, data: any) =>
    api.put<CourseOffering>(`/courseoffering/${id}`, data),

  deleteCourseOffering: (id: string) =>
    api.delete(`/courseoffering/${id}`),

  createUnit: (courseOfferingId: string, data: any) =>
    api.post<CourseOfferingUnit>(`/courseoffering/${courseOfferingId}/units`, data),

  updateUnit: (unitId: string, data: any) =>
    api.put<CourseOfferingUnit>(`/courseoffering/units/${unitId}`, data),

  deleteUnit: (unitId: string) =>
    api.delete(`/courseoffering/units/${unitId}`),

  assignStudents: (courseOfferingId: string, data: any) =>
    api.post<CourseOfferingEnrollment[]>(`/courseoffering/${courseOfferingId}/students`, data),

  assignLecturers: (courseOfferingId: string, data: any) =>
    api.post<CourseOfferingLecturer[]>(`/courseoffering/${courseOfferingId}/lecturers`, data),
};
