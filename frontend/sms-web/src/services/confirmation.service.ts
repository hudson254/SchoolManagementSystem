import { api } from './api';
import { ConfirmationStatus, CourseOfferingEnrollment, CourseOfferingLecturer } from './course-offering.service';

export interface PendingEnrollment extends CourseOfferingEnrollment {
  courseName?: string;
  courseCode?: string;
  academicYearName?: string;
  semesterName?: string;
  offeringCode?: string;
}

export interface ConfirmRequest {
  confirm: boolean;
  notes?: string;
}

export interface ReportIssueRequest {
  reporterUserId: string;
  assignmentType: 'Enrollment' | 'Teaching';
  courseOfferingId: string;
  courseOfferingEnrollmentId?: string;
  courseOfferingLecturerId?: string;
  reason: string;
}

export const confirmationService = {
  getPendingEnrollments: (studentId: string) =>
    api.get<PendingEnrollment[]>(`/confirmation/enrollments/pending/${studentId}`),

  confirmEnrollment: (id: string, data: ConfirmRequest) =>
    api.post(`/confirmation/enrollments/${id}/confirm`, data),

  confirmTeaching: (id: string, data: ConfirmRequest) =>
    api.post(`/confirmation/teaching/${id}/confirm`, data),

  reportIssue: (data: ReportIssueRequest) =>
    api.post('/confirmation/issues', data),
};
