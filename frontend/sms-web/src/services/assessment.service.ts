import { api } from './api'  
import type {
  Assessment,
  AssessmentType,
  AuditLogEntry,
  BulkMarkImportResult,
  CertificateEligibility,
  GradingScale,
  LecturerDashboard,
  StudentAssessmentMark,
  StudentResult,
  WeightValidationResult,
} from '../types/assessment.types'  

export interface CreateAssessmentRequest {
  name: string  
  description?: string  
  assessmentTypeId: string  
  unitId: string  
  courseOfferingId?: string  
  lecturerId?: string  
  weight: number  
  maxMarks?: number  
  dueDate?: string  
  templateId?: string  
}

export interface EnterMarkRequest {
  assessmentId: string  
  studentId: string  
  score: number  
  maxScore: number  
  feedback?: string  
  isDraft?: boolean  
  reason?: string  
}

export interface UpdateMarkRequest {
  markId: string  
  score: number  
  maxScore: number  
  feedback?: string  
  isDraft?: boolean  
  reason?: string  
}

export interface ImportMarksRequest {
  assessmentId: string  
  records: { studentId: string; score: number; maxScore: number; feedback?: string }[];
  importBatchId?: string  
}

export const assessmentService = {
  getTypes: () => api.get<AssessmentType[]>('/assessment/types'),
  createType: (data: Partial<AssessmentType>) => api.post<AssessmentType>('/assessment/types', data),
  updateType: (id: string, data: Partial<AssessmentType>) => api.put<AssessmentType>(`/assessment/types/${id}`, data),
  getGradingScales: () => api.get<GradingScale[]>('/assessment/grading-scales'),
  getGradingScale: (id: string) => api.get<GradingScale>(`/assessment/grading-scales/${id}`),
  getAssessmentsByUnit: (unitId: string) => api.get<Assessment[]>(`/assessment/unit/${unitId}`),
  getAssessment: (id: string) => api.get<Assessment>(`/assessment/${id}`),
  createAssessment: (data: CreateAssessmentRequest) => api.post<Assessment>('/assessment', data),
  getWeightValidation: (unitId: string) => api.get<WeightValidationResult>(`/assessment/unit/${unitId}/weights`),
  enterMark: (data: EnterMarkRequest) => api.post<StudentAssessmentMark>('/assessment/marks', data),
  updateMark:(data: UpdateMarkRequest) => api.put<StudentAssessmentMark>(`/assessment/marks/${data.markId}`, data),
  importMarks: (data: ImportMarksRequest) => api.post<BulkMarkImportResult>('/assessment/marks/import', data),
  submitForReview:(unitId: string, courseOfferingId: string, comments?: string) =>
    api.post('/assessment/results/submit', { unitId, courseOfferingId, comments }),
  approveResults:(unitId: string, courseOfferingId: string, comments?: string) =>
    api.post('/assessment/results/approve', { unitId, courseOfferingId, comments }),
  publishResults:(unitId: string, courseOfferingId: string, comments?: string) =>
    api.post('/assessment/results/publish', { unitId, courseOfferingId, comments }),
  getPendingModeration: () => api.get<StudentResult[]>('/assessment/moderation/pending'),
  reviewMarks: (data: any) => api.post('/assessment/moderation/review', data),
  getAssessmentAuditLog: (unitId?: string, assessmentId?: string) =>
    api.get<AuditLogEntry[]>('/assessment/audit-log', { params: { unitId, assessmentId } }),
  getGradeDistribution: (unitId: string) => api.get(`/assessment/reports/grade-distribution/${unitId}`),
  getPassFailRates: (unitId: string) => api.get(`/assessment/reports/pass-fail-rates/${unitId}`),
  getAssessmentSummary: (unitId: string) => api.get(`/assessment/reports/assessment-summary/${unitId}`),
  getLecturerDashboard:(lecturerId: string) => api.get<LecturerDashboard>(`/assessment/dashboard/lecturer/${lecturerId}`),
  calculateResults:(unitId: string) => api.post<StudentResult[]>(`/assessment/results/calculate?unitId=${unitId}`),
  lockUnit:(unitId: string) => api.post(`/assessment/units/${unitId}/lock`, { reason: undefined }),
  unlockUnit:(unitId: string) => api.post(`/assessment/units/${unitId}/unlock`, { reason: undefined }),
  getStudentResults:(studentId: string) => api.get<StudentResult[]>(`/assessment/student/${studentId}/results`),
  getCertificateEligibility:(studentId: string) => api.get<CertificateEligibility>(`/assessment/student/${studentId}/certificate-eligibility`),
}  
