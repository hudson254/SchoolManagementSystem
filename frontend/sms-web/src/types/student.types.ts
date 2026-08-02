import { User } from './user.types';

export interface Student {
  id: string;
  userId: string;
  studentNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  gender?: string;
  address?: string;
  enrollmentDate: string;
  programmeId?: string;
  programmeName?: string;
  academicStatus: string;
  isEnrolled: boolean;
  cumulativeGPA?: number;
  totalCreditsEarned: number;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelation?: string;
  createdAt: string;
  user?: User;
}

export interface StudentDetails extends Student {
  currentSemesterId?: string;
  currentSemesterName?: string;
  totalEnrollments: number;
  completedUnits: number;
  inProgressUnits: number;
  enrollments: EnrollmentSummary[];
  grades: GradeSummary[];
}

export interface EnrollmentSummary {
  id: string;
  unitId: string;
  unitName: string;
  unitCode: string;
  credits: number;
  status: string;
  semesterId: string;
  semesterName: string;
  enrollmentDate: string;
}

export interface GradeSummary {
  id: string;
  unitId: string;
  unitName: string;
  unitCode: string;
  credits: number;
  grade?: string;
  score?: number;
  remarks?: string;
  semesterId: string;
  semesterName: string;
}

export interface CreateStudentRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  organization?: string;
  dateOfBirth: string;
  gender?: string;
  address?: string;
  programmeId?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelation?: string;
}

export interface UpdateStudentRequest {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  dateOfBirth: string;
  gender?: string;
  address?: string;
  programmeId?: string;
  academicStatus?: string;
  isEnrolled: boolean;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelation?: string;
}

export interface Transcript {
  studentId: string;
  studentName: string;
  studentNumber: string;
  programmeName: string;
  totalCreditsEarned: number;
  cumulativeGPA: number;
  semesterGPA: number;
  semesters: SemesterTranscript[];
  allGrades: GradeSummary[];
}

export interface SemesterTranscript {
  semesterName: string;
  semesterNumber: number;
  credits: number;
  gpa: number;
  grades: GradeSummary[];
}