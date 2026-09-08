// Assessment module types - mirror the verificaed backend AssessmentController / DTO contracts.
// All calculations remain server-side these types are pure display contracts.

export type AssessmentStatus = 'Draft' | 'Active' | 'GradingInProgress' | 'Completed' | 'Archived'
export type PublicationStatus = 'Draft' | 'PendingReview' | 'Approved' | 'Published'
export type ModerationStatus = 'NotRequired' | 'PendingReview' | 'Approved' | 'ReturnedForCorrection' | 'Rejected'
export type MarkEntrySource = 'ManualEntry' | 'BulkImport' | 'OnlineSubmission'

export interface AssessmentType {
  id: string
  name: string
  code: string
  description?: string
  category?: string
  isActive: boolean
  isSystemDefined: boolean
  sortOrder?: number
}

export interface Assessment {
  id: string
  name: string
  description?: string
  unitId: string
  courseOfferingId?: string
  semesterId?: string
  assessmentTypeId: string
  lecturerId?: string
  assessmentTemplateId?: string
  maxScore: number
  weight: number
  dueDate?: string
  closingDate?: string
  allowLateSubmission: boolean
  latePenaltyPercent: number
  gracePeriodDays?: number
  isOnlineSubmission: boolean
  linkedAssignmentId?: string
  isExemptable: boolean
  isMandatory: boolean
  requiresModeration: boolean
  isAnonymousMarking: boolean
  status: AssessmentStatus
  publicationStatus: PublicationStatus
  moderationStatus: ModerationStatus
  isWeightLocked: boolean
  weightLockedDate?: string
  weightLockedBy?: string
  isActive: boolean
}

export interface StudentAssessmentMark {
  id: string
  assessmentId: string
  assessmentName?: string
  studentId: string
  enrollmentId?: string
  courseOfferingId?: string
  score: number
  maxScore: number
  percentage: number
  weightedScore: number
  weight: number
  isDraft: boolean
  isModerated: boolean
  moderatedDate?: string
  moderatedBy?: string
  moderationComment?: string
  originalMark?: number
  revisedMark?: number
  entrySource: MarkEntrySource
  importBatchReference?: string
  isExempt: boolean
  exemptionReason?: string
  feedback?: string
  feedbackPublished: boolean
  gradedBy?: string
  gradedDate?: string
}

export interface StudentResult {
  studentId: string
  studentName: string
  unitId: string
  unitName: string
  assessmentMarks: StudentAssessmentMark[]
  finalScore: number
  totalWeight: number
  finalGrade?: string
  gradeDescription?: string
  gradeColor?: string
  isPassed: boolean
  gradingScaleVersionId?: string
  isEligibleForCertificate: boolean
  isPublished: boolean
  publicationStatus: PublicationStatus
}

export interface WeightValidationResult {
  isValid: boolean
  totalWeight: number
  weights: { assessmentId: string; assessmentName: string; weight: number }[];
  errors: { message: string; field: string }[];
}

export interface WeightValidationError {
  message: string
  field: string
}

export interface BulkMarkImportResult {
  totalRecords: number

 successCount: number
 errorCount: number
 errors: string[]
 importBatchId?: string
}

export interface AuditLogEntry {
  id: string
  timestamp: string
  userId?: string
  userRole?: string
  action?: string
  entityName?: string
  entityId?: string
  previousValue?: string
 newValue?: string
 reason?: string
 ipAddress?: string
 sessionId?: string
 unitId?: string
 assessmentId?: string
 studentId?: string
}

export interface LecturerDashboard {
  lecturerId: string
  lecturerName: string
  assessments: Assessment[]
  studentResults: StudentResult[]
  passRate: number
 gradeDistribution: Record<string, number>
  studentsAtRisk: StudentResult[]
 incompleteAssessments: Assessment[]
  pendingGradingTasks: Assessment[]
  totalAssessments: number
  totalStudents: number
}

export interface GradingScale {
  id: string
  name: string
 description?: string
  version: number
 isDefault: boolean
 isActive: boolean
 effectiveFrom?: string
 effectiveTo?: string
 bands: GradeBand[]
}

export interface GradeBand {
  id: string
  gradingScaleId: string
 gradeLetter: string
 description: string
 minPercentage: number
 maxPercentage: number
 gpaPoints: number
 colorCode: string
 honorsClassification?: string
 sortOrder: number
}

export interface CertificateEligibility {
  studentId: string
  status: 'Eligible' | 'NotEligible' | 'NotDetermined' | 'Pending'
  missingRequirements: string[]
  overallPercentage?: number
 lastEvaluatedAt?: string
}
