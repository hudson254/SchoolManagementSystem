import { api } from './api';

/**
 * OMS Request API client.
 *
 * Mirrors the generic v1 Request contracts implemented by
 *   src/SMS.API/Controllers/v1/Oms/OmsRequestsController.cs
 * and the DTOs in
 *   src/SMS.Application/Features/OMS/Dtos/Request*.cs
 *
 * The BACKEND IS AUTHORITATIVE for everything that matters:
 *   - which requests a caller may see (scope + object-level authorization),
 *   - which lifecycle transitions are currently valid,
 *   - request numbers, tenant ids, requester/assignee identity,
 *   - attachment authorization.
 *
 * This client therefore never invents a transition, never computes a request
 * number, and never filters a list client-side to "fix" access. It relays
 * server values only.
 */

/** Mirrors SMS.Domain.Enums.RequestStatus. */
export const OmsRequestStatus = {
  Draft: 'Draft',
  Submitted: 'Submitted',
  PendingReview: 'PendingReview',
  Assigned: 'Assigned',
  Returned: 'Returned',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
  Completed: 'Completed',
  Escalated: 'Escalated',
} as const;

export type OmsRequestStatus = (typeof OmsRequestStatus)[keyof typeof OmsRequestStatus];

/** Mirrors SMS.Domain.Enums.RequestPriority. */
export const OmsRequestPriority = {
  Low: 0,
  Normal: 1,
  High: 2,
  Urgent: 3,
} as const;

export type OmsRequestPriority = (typeof OmsRequestPriority)[keyof typeof OmsRequestPriority];

/**
 * Server-authoritative queue scope.
 *
 * `mine`    - requests the caller raised (forced server-side, never client-filtered)
 * `assigned`- requests assigned to the caller (forced server-side)
 * `all`     - full tenant queue; requires a privileged role and is REJECTED
 *             server-side for everyone else, so a non-privileged caller cannot
 *             widen their own access by asking for it.
 */
export type OmsRequestScope = 'mine' | 'assigned' | 'all';

/** Mirrors SMS.Application.Features.OMS.Dtos.RequestDto. */
export type OmsRequest = {
  id: string;
  requestNumber: string;
  requestType: string;
  typeDisplayName: string;
  title: string;
  description?: string | null;
  priority: OmsRequestPriority;
  status: OmsRequestStatus;
  requesterUserId: string;
  assignedUserId?: string | null;
  relatedEntityId?: string | null;
  relatedEntityType?: string | null;
  createdAt: string;
  updatedAt: string;
  submittedAt?: string | null;
  completedAt?: string | null;
  dueDate?: string | null;
  tenantId: string;
  rowVersion?: string | null;
  studentId?: string | null;
  lecturerId?: string | null;
  courseId?: string | null;
  unitId?: string | null;
  enrollmentId?: string | null;
  accommodationId?: string | null;
  assignmentId?: string | null;
  certificateId?: string | null;
};

/** Mirrors SMS.Application.Common.PagedResult<T>. */
export type OmsPagedResult<T> = {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

/** Mirrors RequestStatusHistoryDto. */
export type OmsRequestStatusHistoryEntry = {
  id: string;
  fromStatus?: string | null;
  toStatus: string;
  actionType?: string | null;
  performedByUserId: string;
  performedByUsername?: string | null;
  notes?: string | null;
  createdAt: string;
};

/** Mirrors RequestCommentDto. */
export type OmsRequestComment = {
  id: string;
  requestId: string;
  authorUserId: string;
  message: string;
  createdAt: string;
  editedAtUtc?: string | null;
};

/** Mirrors RequestAttachmentDto. */
export type OmsRequestAttachment = {
  id: string;
  requestId: string;
  fileName: string;
  contentType?: string | null;
  size: number;
  uploadedByUserId: string;
  uploadedByUserName?: string | null;
  createdAt: string;
};

/** Mirrors RequestTypeDto. */
export type OmsRequestType = {
  id: string;
  code: string;
  displayName: string;
  description?: string | null;
  isActive: boolean;
  allowedRequesterRoles?: string | null;
  approverRoles?: string | null;
  workflowSteps?: string | null;
  requiresAttachments: boolean;
  enableComments: boolean;
  defaultPriority: OmsRequestPriority;
  notifyOnSubmit: boolean;
  notifyOnApprove: boolean;
  notifyOnReject: boolean;
  notifyOnReturn: boolean;
};

export type GetOmsRequestsParams = {
  status?: OmsRequestStatus;
  requestType?: string;
  requesterUserId?: string;
  assignedUserId?: string;
  search?: string;
  scope?: OmsRequestScope;
  pageNumber?: number;
  pageSize?: number;
};

/** Payload for the generic create endpoint. */
export type CreateOmsRequest = {
  requestType: string;
  title: string;
  description?: string;
  priority?: OmsRequestPriority;
  dueDate?: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
};

/**
 * Module adapter payloads. Each thin adapter endpoint creates a generic Request
 * pre-populated with module business context; it exposes NO lifecycle verbs.
 */
export type CreateOmsEnrollmentRequest = {
  enrollmentId: string;
  requestType?: string;
  title?: string;
  description?: string;
  reason?: string;
  priority?: OmsRequestPriority;
  dueDate?: string;
};

export type CreateOmsAccommodationRequest = {
  accommodationId: string;
  requestType?: string;
  title?: string;
  description?: string;
  reason?: string;
  priority?: OmsRequestPriority;
  dueDate?: string;
};

export type CreateOmsAssignmentRequest = {
  assignmentId: string;
  requestType?: string;
  title?: string;
  description?: string;
  reason?: string;
  priority?: OmsRequestPriority;
  dueDate?: string;
};

/**
 * Normalizes the paged envelope, so a malformed response renders an error
 * state instead of silently corrupting the list UI.
 */
export function normalizeOmsRequestsPagedResult(
  data: OmsPagedResult<OmsRequest> | OmsRequest[] | unknown,
): OmsPagedResult<OmsRequest> {
  if (Array.isArray(data)) {
    return {
      items: data,
      totalCount: data.length,
      pageNumber: 1,
      pageSize: data.length || 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    };
  }

  if (
    data !== null &&
    typeof data === 'object' &&
    Array.isArray((data as { items?: unknown }).items)
  ) {
    const envelope = data as Partial<OmsPagedResult<OmsRequest>>;
    const totalCount =
      typeof envelope.totalCount === 'number' ? envelope.totalCount : envelope.items!.length;
    const pageNumber = typeof envelope.pageNumber === 'number' ? envelope.pageNumber : 1;
    const pageSize =
      typeof envelope.pageSize === 'number' ? envelope.pageSize : envelope.items!.length || 1;
    const totalPages =
      typeof envelope.totalPages === 'number'
        ? envelope.totalPages
        : pageSize > 0
          ? Math.ceil(totalCount / pageSize)
          : 0;
    return {
      items: envelope.items!,
      totalCount,
      pageNumber,
      pageSize,
      totalPages,
      hasPreviousPage: envelope.hasPreviousPage ?? pageNumber > 1,
      hasNextPage: envelope.hasNextPage ?? pageNumber < totalPages,
    };
  }

  throw new Error('Unexpected response shape from the requests API.');
}

/**
 * Uploads an attachment as multipart form data.
 *
 * The server re-validates extension, size, tenant and object-level access; the
 * browser-side checks exist only to give immediate feedback, never to decide
 * authorization.
 */
function attachmentForm(file: File): FormData {
  const form = new FormData();
  form.append('file', file);
  return form;
}

export const omsRequestsService = {
  // ── Generic lifecycle ───────────────────────────────────────────────────
  getRequests: (params: GetOmsRequestsParams = {}) =>
    api
      .get<OmsPagedResult<OmsRequest>>('/oms/requests', { params })
      .then(normalizeOmsRequestsPagedResult),

  getRequest: (requestId: string) => api.get<OmsRequest>(`/oms/requests/${requestId}`),

  createRequest: (data: CreateOmsRequest) => api.post<OmsRequest>('/oms/requests', data),

  updateRequest: (
    requestId: string,
    data: { title?: string; description?: string; priority?: OmsRequestPriority; dueDate?: string },
  ) => api.put<OmsRequest>(`/oms/requests/${requestId}`, data),

  submitRequest: (requestId: string) => api.post<OmsRequest>(`/oms/requests/${requestId}/submit`),

  startReview: (requestId: string, notes?: string) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/review`, { notes }),

  assignRequest: (requestId: string, data: { assignedUserId: string; notes?: string }) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/assign`, data),

  reassignRequest: (requestId: string, data: { assignedUserId: string; notes?: string }) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/reassign`, data),

  approveRequest: (requestId: string, notes?: string) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/approve`, { notes }),

  rejectRequest: (requestId: string, data: { reason: string; notes?: string }) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/reject`, data),

  returnForCorrection: (requestId: string, data: { reason: string; notes?: string }) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/return`, data),

  cancelRequest: (requestId: string, reason: string) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/cancel`, { reason }),

  completeRequest: (requestId: string, notes?: string) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/complete`, { notes }),

  escalateRequest: (requestId: string, data: { escalateToUserId?: string; reason: string }) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/escalate`, data),

  // ── History and comments ────────────────────────────────────────────────
  getHistory: (requestId: string) =>
    api.get<OmsRequestStatusHistoryEntry[]>(`/oms/requests/${requestId}/history`),

  getComments: (requestId: string) =>
    api.get<OmsRequestComment[]>(`/oms/requests/${requestId}/comments`),

  addComment: (requestId: string, message: string, notifyRequester = false) =>
    api.post<OmsRequestComment>(`/oms/requests/${requestId}/comments`, {
      message,
      notifyRequester,
    }),

  // ── Attachments (authorized server-side; never expose raw paths) ─────────
  getAttachments: (requestId: string) =>
    api.get<OmsRequestAttachment[]>(`/oms/requests/${requestId}/attachments`),

  uploadAttachment: (requestId: string, file: File) =>
    api.post<OmsRequestAttachment>(
      `/oms/requests/${requestId}/attachments`,
      attachmentForm(file),
      { headers: { 'Content-Type': 'multipart/form-data' } },
    ),

  /**
   * Downloads through the AUTHORIZED endpoint. The raw storage key is never
   * known to the browser, so possession of an attachment id is not sufficient
   * to read the file - the server re-checks object-level access.
   */
  downloadAttachment: (requestId: string, attachmentId: string) =>
    api.get<Blob>(`/oms/requests/${requestId}/attachments/${attachmentId}/download`, {
      responseType: 'blob',
    }),

  deleteAttachment: (requestId: string, attachmentId: string) =>
    api.delete<void>(`/oms/requests/${requestId}/attachments/${attachmentId}`),

  // ── Request types (admin-managed configuration) ──────────────────────────
  getRequestTypes: () => api.get<OmsRequestType[]>('/oms/requests/types'),

  // ── Dashboard ───────────────────────────────────────────────────────────
  getDashboardSummary: () => api.get<unknown>('/oms/requests/dashboard'),

  // ── Thin module adapters (create only; no lifecycle verbs here) ──────────
  createEnrollmentRequest: (data: CreateOmsEnrollmentRequest) =>
    api.post<OmsRequest>('/oms/requests/enrollment', data),

  createAccommodationRequest: (data: CreateOmsAccommodationRequest) =>
    api.post<OmsRequest>('/oms/requests/accommodation', data),

  createAssignmentRequest: (data: CreateOmsAssignmentRequest) =>
    api.post<OmsRequest>('/oms/requests/assignment', data),
};
