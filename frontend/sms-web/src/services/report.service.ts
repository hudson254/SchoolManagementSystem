import { api } from './api';

export interface ReportParameterValue {
  [key: string]: string | number | undefined;
}

export interface GenerateReportRequest {
  reportType: string;
  [key: string]: any;
}

export interface GenerateReportResponse {
  url: string;
  fileName: string;
  reportId: string;
  token?: string;
}

export interface ReportStatus {
  id: string;
  reportType: string;
  status: 'completed' | 'processing' | 'failed';
  fileName: string;
  createdAt: string;
  generatedBy: string;
}

export interface VerificationRecord {
  id: string;
  reportId: string;
  token: string;
  verifiedAt: string;
  verifiedBy: string;
  ipAddress?: string;
  status: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const reportService = {
  generateReport: (data: GenerateReportRequest) =>
    api.post<GenerateReportResponse>('/reports/generate', data),

  getReportStatus: (reportId: string) =>
    api.get<ReportStatus>(`/reports/${reportId}/status`),

  downloadReport: (reportId: string, format: 'pdf' | 'excel' | 'csv') =>
    api.get<Blob>(`/reports/${reportId}/download`, {
      params: { format },
      responseType: 'blob',
    }),

  getReports: (params?: { page?: number; pageSize?: number; reportType?: string }) =>
    api.get<PagedResponse<ReportStatus>>('/reports', { params }),

  getMyReports: (params?: { page?: number; pageSize?: number }) =>
    api.get<PagedResponse<ReportStatus>>('/reports/mine', { params }),

  // Report authentication / verification
  generateAuthentication: (reportId: string) =>
    api.post<{ token: string; qrCodeUrl?: string }>(`/reports/${reportId}/authenticate`),

  verifyReport: (token: string) =>
    api.post<VerificationRecord>('/reports/verify', { token }),

  getVerificationHistory: (reportId: string) =>
    api.get<VerificationRecord[]>(`/reports/${reportId}/verifications`),

  revokeReport: (reportId: string) =>
    api.post(`/reports/${reportId}/revoke`),

  restoreReport: (reportId: string) =>
    api.post(`/reports/${reportId}/restore`),

  searchReports: (params: { searchTerm?: string; status?: string; reportType?: string; page?: number; pageSize?: number }) =>
    api.get<PagedResponse<ReportStatus>>('/reports/search', { params }),
};

