import { api } from './api';

/**
 * Study material (lecture note) metadata for a unit.
 * Mirrors SMS.Application.DTOs.StudyMaterialDto. The listing endpoint returns
 * a plain array (NOT a paged envelope); downloads return the raw file stream.
 */
export interface StudyMaterial {
  id: string;
  unitId: string;
  title: string;
  description?: string | null;
  lecturerId: string;
  lecturerName: string;
  uploadFileId?: string | null;
  originalFileName: string;
  extension: string;
  contentType: string;
  fileSizeBytes: number;
  version: number;
  uploadedAt: string;
  isPublished: boolean;
}

/** A unit the signed-in user may open in Study Materials. */
export interface StudyMaterialUnit {
  unitId: string;
  code: string;
  name: string;
  credits: number;
  courseId: string;
  courseName: string;
  /** "Lecturer" = appointed to teach (may upload); "Student" = enrolled (read only). */
  accessRole: string;
}

/**
 * Accepted study-material file types. Mirrors the backend `LecturerNotes`
 * category, which resolves to UploadSettings.AllowedDocumentExtensions
 * (src/SMS.Infrastructure/Options/UploadSettings.cs). The server is
 * authoritative — it re-validates extension, MIME and magic bytes — this list
 * only avoids showing a file picker rejection the API would not have raised.
 */
export const STUDY_MATERIAL_EXTENSIONS = [
  '.pdf', '.doc', '.docx', '.ppt', '.pptx',
  '.xls', '.xlsx', '.odt', '.odp', '.ods',
  '.rtf', '.txt', '.csv',
];

/** Backend LecturerNotes category limit (50 MB). */
export const STUDY_MATERIAL_MAX_SIZE_MB = 50;

export const studyMaterialService = {
  /**
   * The units the signed-in user may open: those they are appointed to teach
   * (lecturers) or enrolled to study (students). Resolved server-side from the
   * same relationships that authorize the endpoints below, so this list can
   * never offer a unit the API would reject.
   */
  getMyUnits: () => api.get<StudyMaterialUnit[]>('/study-materials/my-units'),

  /** List published materials for a unit (plain array). */
  getUnitMaterials: (unitId: string) =>
    api.get<StudyMaterial[]>(`/study-materials/unit/${unitId}`),

  /** Upload a material for a unit (multipart/form-data, LecturerAccess). */
  uploadMaterial: (
    unitId: string,
    file: File,
    title: string,
    description?: string,
    onProgress?: (progress: number) => void
  ) => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('title', title);
    if (description) {
      formData.append('description', description);
    }
    return api.post<StudyMaterial>(`/study-materials/unit/${unitId}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
      onUploadProgress: (progressEvent: any) => {
        if (onProgress && progressEvent?.total) {
          onProgress(Math.round((progressEvent.loaded * 100) / progressEvent.total));
        }
      },
    });
  },

  /** Download/open a material through the authenticated endpoint (blob). */
  downloadMaterial: (materialId: string) =>
    api.get<Blob>(`/study-materials/${materialId}/download`, {
      responseType: 'blob',
    }),

  /** Delete (soft-delete) a material (LecturerAccess; server-side authorization). */
  deleteMaterial: (materialId: string, unitId: string) =>
    api.delete<void>(`/study-materials/${materialId}`, { params: { unitId } }),
};

/** Formats a byte size for display (e.g. "1.4 MB"). */
export const formatFileSize = (bytes: number): string => {
  if (!bytes || bytes <= 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB'];
  const i = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  const value = bytes / Math.pow(1024, i);
  return `${value >= 10 || i === 0 ? Math.round(value) : value.toFixed(1)} ${units[i]}`;
};

/** Triggers a browser download of a blob response. */
export const saveBlob = (blob: Blob, fileName: string): void => {
  const url = window.URL.createObjectURL(new Blob([blob]));
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName || 'download';
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
};
