export enum CertificateStatus {
  Pending = 'Pending',
  Issued = 'Issued',
  Revoked = 'Revoked',
  Superseded = 'Superseded',
}

export enum CertificateType {
  Completion = 'Completion',
  Award = 'Award',
  Transcript = 'Transcript',
}

export interface Certificate {
  id: string;
  certificateNumber: string;
  studentId: string;
  studentName?: string;
  studentNumber?: string;
  courseOfferingId: string;
  offeringCode?: string;
  courseName?: string;
  templateId: string;
  templateVersion: string;
  status: CertificateStatus;
  type: CertificateType;
  finalGrade?: string;
  classification?: string;
  issueDate: string;
  expiryDate?: string;
  verificationToken: string;
  verificationUrl: string;
  qrCodePath?: string;
  pdfPath: string;
  hash: string;
  version: number;
  parentCertificateId?: string;
  supersedesCertificateId?: string;
  revocationReason?: string;
  revokedAt?: string;
  createdDate: string;
}

export interface GenerateCertificateRequest {
  studentId: string;
  courseOfferingId: string;
  templateId?: string;
}

export interface BulkGenerateRequest {
  courseOfferingId: string;
}

export interface BulkGenerationResult {
  totalProcessed: number;
  generated: number;
  skipped: number;
  errors: number;
  warnings: number;
  details: string[];
}

export interface RevokeCertificateRequest {
  reason: string;
}

export interface RegenerateCertificateRequest {
  reason: string;
}

export interface CertificateTemplate {
  id: string;
  name: string;
  description?: string;
  version: string;
  type: string;
  status: string;
  courseId?: string;
  filePath: string;
  logoPath?: string;
  watermarkPath?: string;
  fieldMappings: string;
  isDefault: boolean;
  createdAt: string;
  createdBy: string;
  updatedAt: string;
  updatedBy: string;
}

export interface CertificateTemplateRequest {
  name: string;
  description?: string;
  version?: string;
  type: string;
  status?: string;
  courseId?: string;
  filePath: string;
  logoPath?: string;
  watermarkPath?: string;
  fieldMappings?: string;
  isDefault?: boolean;
}
