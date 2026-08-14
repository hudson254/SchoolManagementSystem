import { api } from './api';
import {
  Certificate,
  GenerateCertificateRequest,
  BulkGenerateRequest,
  BulkGenerationResult,
  RevokeCertificateRequest,
  RegenerateCertificateRequest,
  CertificateTemplate,
  CertificateTemplateRequest,
} from '../types/certificate.types';

export interface VerificationResult {
  isValid: boolean;
  certificateNumber?: string;
  studentName?: string;
  courseName?: string;
  offeringCode?: string;
  issueDate?: string;
  status?: string;
  errorMessage?: string;
}

export const certificateService = {
  // Generate a single certificate for a student
  generate: (data: GenerateCertificateRequest) =>
    api.post<Certificate>('/certificates/generate', data),

  // Get a certificate by ID
  getById: (id: string) =>
    api.get<Certificate>(`/certificates/${id}`),

  // Download certificate PDF (returns blob)
  download: async (id: string): Promise<Blob> => {
    const response = await api.get<Blob>(`/certificates/${id}/download`, {
      responseType: 'blob',
    });
    return response;
  },

  // Get all certificates for a student
  getByStudent: (studentId: string) =>
    api.get<Certificate[]>(`/certificates/student/${studentId}`),

  // Search/list certificates with pagination (admin)
  search: (params?: {
    pageNumber?: number;
    pageSize?: number;
    searchTerm?: string;
    status?: string;
    studentId?: string;
    courseOfferingId?: string;
  }) =>
    api.get<{
      items: Certificate[];
      totalCount: number;
      pageNumber: number;
      pageSize: number;
    }>('/certificates', { params }),

  // Revoke a certificate
  revoke: (id: string, data: RevokeCertificateRequest) =>
    api.post(`/certificates/${id}/revoke`, data),

  // Regenerate a certificate
  regenerate: (id: string, data: RegenerateCertificateRequest) =>
    api.post<Certificate>(`/certificates/${id}/regenerate`, data),

  // Bulk generate certificates for a course offering
  bulkGenerate: (data: BulkGenerateRequest) =>
    api.post<BulkGenerationResult>('/certificates/bulk/generate', data),

  // Bulk generate certificates for all completed offerings
  bulkGenerateAll: () =>
    api.post<BulkGenerationResult>('/certificates/bulk/generate-all'),

  // Public: Verify a certificate by certificate number
  verifyByCertificateNumber: (certificateNumber: string) =>
    api.get<VerificationResult>(`/verify/certificate/${certificateNumber}`),

  // Public: Verify a certificate by verification token
  verifyByToken: (verificationToken: string) =>
    api.get<VerificationResult>(`/verify/token/${verificationToken}`),

  // Public: Verify a certificate by QR code data
  verifyByQrCode: (qrCodeData: string) =>
    api.post<VerificationResult>('/verify/qrcode', { qrCodeData }),

  // Get all certificate templates
  getTemplates: () =>
    api.get<CertificateTemplate[]>('/certificates/templates'),

  // Get a certificate template by ID
  getTemplate: (id: string) =>
    api.get<CertificateTemplate>(`/certificates/templates/${id}`),

  // Create a certificate template
  createTemplate: (data: CertificateTemplateRequest) =>
    api.post<CertificateTemplate>('/certificates/templates', data),

  // Update a certificate template
  updateTemplate: (id: string, data: CertificateTemplateRequest) =>
    api.put<CertificateTemplate>(`/certificates/templates/${id}`, data),

  // Delete a certificate template
  deleteTemplate: (id: string) =>
    api.delete(`/certificates/templates/${id}`),
};
