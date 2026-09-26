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
  PendingApproval: 'PendingApproval',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Returned: 'Returned',
  InProgress: 'InProgress',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
  OnHold: 'OnHold',
  Escalated: 'Escalated',
} as const;

export type OmsRequestStatus = (typeof OmsRequestStatus)[keyof typeof OmsRequestStatus];

/**
 * Mirrors SMS.Domain.Enums.RequestPriority.
 *
 * The wire format is the serialized enum, i.e. the numeric underlying value.
 * The backend declares Low = 1 â€¦ Urgent = 4, so these MUST NOT be renumbered.
 */
export const OmsRequestPriority = {
  Low: 1,
  Normal: 2,
  High: 3,
  Urgent: 4,
} as const;

export type OmsRequestPriority = (typeof OmsRequestPriority)[keyof typeof OmsRequestPriority];

/** Every priority, ordered ascending â€” used for stable dropdown rendering. */
export const OMS_REQUEST_PRIORITIES: OmsRequestPriority[] = [
  OmsRequestPriority.Low,
  OmsRequestPriority.Normal,
  OmsRequestPriority.High,
  OmsRequestPriority.Urgent,
];

/** Every status, in lifecycle order â€” used for stable filter rendering. */
export const OMS_REQUEST_STATUSES: OmsRequestStatus[] = [
  OmsRequestStatus.Draft,
  OmsRequestStatus.Submitted,
  OmsRequestStatus.PendingReview,
  OmsRequestStatus.Assigned,
  OmsRequestStatus.PendingApproval,
  OmsRequestStatus.Approved,
  OmsRequestStatus.Rejected,
  OmsRequestStatus.Returned,
  OmsRequestStatus.InProgress,
  OmsRequestStatus.Completed,
  OmsRequestStatus.Cancelled,
  OmsRequestStatus.OnHold,
  OmsRequestStatus.Escalated,
];

/** Mirrors SMS.Domain.Enums.RequestActionType. */
export const OmsRequestActionType = {
  Created: 1,
  Submitted: 2,
  ReviewStarted: 3,
  Assigned: 4,
  Reassigned: 5,
  Approved: 6,
  Rejected: 7,
  Returned: 8,
  Completed: 9,
  Cancelled: 10,
  Escalated: 11,
  Edited: 12,
  InProgress: 13,
  OnHold: 14,
} as const;

export type OmsRequestActionTypeValue =
  (typeof OmsRequestActionType)[keyof typeof OmsRequestActionType];

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

/**
 * Mirrors RequestListItemDto â€” the row shape actually returned by
 * GET /oms/requests. It is deliberately NOT RequestDto: the list endpoint omits
 * description, tenant and the typed module references.
 */
export type OmsRequestListItem = {
  id: string;
  requestNumber: string;
  requestType: string;
  title: string;
  priority: OmsRequestPriority;
  status: OmsRequestStatus;
  requesterUserId: string;
  assignedUserId?: string | null;
  createdAt: string;
  updatedAt: string;
  dueDate?: string | null;
};

/** Mirrors RequestStatusHistoryDto. */
export type OmsRequestStatusHistoryEntry = {
  id: string;
  requestId: string;
  fromStatus?: OmsRequestStatus | null;
  toStatus: OmsRequestStatus;
  action: OmsRequestActionTypeValue;
  performedByUserId: string;
  performedByUsername?: string | null;
  performedAtUtc: string;
  reason?: string | null;
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

/**
 * Mirrors RequestDetailDto â€” RequestDto plus the two collections the detail
 * endpoint embeds. There is NO standalone comments GET endpoint; comments are
 * read from here so the UI never needs a second comment persistence path.
 */
export type OmsRequestDetail = OmsRequest & {
  statusHistory: OmsRequestStatusHistoryEntry[];
  comments: OmsRequestComment[];
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

/**
 * Mirrors RequestTypeDto.
 *
 * Note: the seeded columns (AllowedRequesterRoles, ApproverRoles, WorkflowSteps,
 * RequiresAttachments, EnableComments, NotifyOn*) are persisted but are NOT part
 * of the read DTO, so the client cannot and does not read them. Type/role
 * applicability is therefore enforced server-side at creation time.
 */
export type OmsRequestType = {
  id: string;
  code: string;
  displayName: string;
  description?: string | null;
  isActive: boolean;
  defaultPriority: OmsRequestPriority;
  createdAt: string;
};

/** Mirrors RequestDashboardSummaryDto. */
export type OmsRequestDashboardSummary = {
  totalRequests: number;
  draftCount: number;
  submittedCount: number;
  pendingReviewCount: number;
  assignedCount: number;
  pendingApprovalCount: number;
  approvedCount: number;
  rejectedCount: number;
  returnedCount: number;
  inProgressCount: number;
  completedCount: number;
  cancelledCount: number;
  onHoldCount: number;
  escalatedCount: number;
  countsByStatus: Partial<Record<OmsRequestStatus, number>>;
};

/**
 * Stable request type codes owned by the thin module adapters.
 *
 * These are the ONLY codes each module adapter endpoint accepts. They mirror
 * SMS.Application.Features.{Enrollments,Accommodation,Assignments}.Commands.
 * *RequestTypes exactly. The frontend does not invent or extend this set â€” the
 * adapter validates the code server-side and rejects anything else.
 */
export const OmsModuleRequestTypeCodes = {
  enrollment: {
    courseChange: 'ENROLLMENT_COURSE_CHANGE',
    exception: 'ENROLLMENT_EXCEPTION',
    unitCorrection: 'ENROLLMENT_UNIT_CORRECTION',
    cancellation: 'ENROLLMENT_CANCELLATION',
  },
  accommodation: {
    transfer: 'ACCOMMODATION_TRANSFER',
    allocation: 'ACCOMMODATION_ALLOCATION',
    exception: 'ACCOMMODATION_EXCEPTION',
    correction: 'ACCOMMODATION_CORRECTION',
  },
  assignment: {
    extension: 'ASSIGNMENT_EXTENSION',
    reopen: 'ASSIGNMENT_REOPEN',
    correction: 'ASSIGNMENT_CORRECTION',
    exception: 'ASSIGNMENT_EXCEPTION',
  },
} as const;

export type OmsRequestScopeQuery = OmsRequestScope;

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

export type UpdateOmsRequest = {
  title?: string;
  description?: string;
  priority?: OmsRequestPriority;
  dueDate?: string;
  rowVersion?: string;
};

/**
 * Module adapter payloads. Each thin adapter endpoint creates a generic Request
 * pre-populated with module business context; it exposes NO lifecycle verbs.
 *
 * IMPORTANT: the adapter requires the module entity id. It does NOT accept a
 * request number, a student name, or any free-form identifier â€” the server
 * resolves and authorizes the module record itself.
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
 * Attachment upload constraints, mirrored from
 * SMS.Application.Features.OMS.Commands.RequestAttachmentPolicy so the user gets
 * immediate feedback. The server re-validates all of it; these are never a
 * security control.
 */
export const OMS_REQUEST_ATTACHMENT_MAX_BYTES = 10 * 1024 * 1024;

export const OMS_REQUEST_ATTACHMENT_EXTENSIONS = [
  '.pdf', '.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx',
  '.csv', '.txt', '.rtf', '.odt', '.ods',
  '.png', '.jpg', '.jpeg', '.gif', '.bmp', '.webp', '.tif', '.tiff',
] as const;

/**
 * Normalizes the paged envelope returned by GET /oms/requests, so a malformed
 * response renders an error state instead of silently corrupting the list UI.
 *
 * This exists specifically to avoid the earlier `/units` defect, where a
 * paginated envelope was treated as a bare array: the `items` array is the only
 * accepted source of rows, and a payload that is neither an array nor an
 * envelope throws rather than yielding a phantom empty list.
 */
export function normalizeOmsRequestsPagedResult(
  data: OmsPagedResult<OmsRequestListItem> | OmsRequestListItem[] | unknown,
): OmsPagedResult<OmsRequestListItem> {
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
    const envelope = data as Partial<OmsPagedResult<OmsRequestListItem>>;
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
  // ── Read ─────────────────────────────────────────────────────────────────
  /**
   * GET /oms/requests → PagedResult<RequestListItemDto>.
   *
   * `scope` is honoured server-side: "mine"/"assigned" overwrite any user filter
   * with the authenticated user id, and "all" is rejected outright for anyone
   * without a privileged queue role. A 403 here is therefore the expected,
   * correct answer for a non-privileged caller asking for the full queue.
   */
  getRequests: (params: GetOmsRequestsParams = {}) =>
    api
      .get<OmsPagedResult<OmsRequestListItem>>('/oms/requests', { params })
      .then(normalizeOmsRequestsPagedResult),

  /** GET /oms/requests/{requestId} → RequestDetailDto (embeds history + comments). */
  getRequest: (requestId: string) => api.get<OmsRequestDetail>(`/oms/requests/${requestId}`),

  /** GET /oms/requests/{requestId}/history → RequestStatusHistoryDto[]. */
  getHistory: (requestId: string) =>
    api.get<OmsRequestStatusHistoryEntry[]>(`/oms/requests/${requestId}/history`),

  /** GET /oms/requests/dashboard → RequestDashboardSummaryDto. */
  getDashboardSummary: () => api.get<OmsRequestDashboardSummary>('/oms/requests/dashboard'),

  /** GET /oms/requests/types → RequestTypeDto[] (active and inactive alike). */
  getRequestTypes: () => api.get<OmsRequestType[]>('/oms/requests/types'),

  /** GET /oms/requests/types/{requestTypeId} → RequestTypeDto. */
  getRequestType: (requestTypeId: string) =>
    api.get<OmsRequestType>(`/oms/requests/types/${requestTypeId}`),

  // ── Create / update ──────────────────────────────────────────────────────
  /** POST /oms/requests → RequestDto. The server allocates the request number. */
  createRequest: (data: CreateOmsRequest) => api.post<OmsRequest>('/oms/requests', data),

  /** PUT /oms/requests/{requestId} → RequestDto (Draft/Returned only). */
  updateRequest: (requestId: string, data: UpdateOmsRequest) =>
    api.put<OmsRequest>(`/oms/requests/${requestId}`, data),

  // ── Lifecycle ────────────────────────────────────────────────────────────
  /** POST /oms/requests/{id}/submit. Takes no body (Draft or Returned only). */
  submitRequest: (requestId: string) => api.post<OmsRequest>(`/oms/requests/${requestId}/submit`),

  /** POST /oms/requests/{id}/start-review. Takes no body. */
  startReview: (requestId: string) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/start-review`),

  /** POST /oms/requests/{id}/assign — AssignRequestCommand.assignedUserId. */
  assignRequest: (requestId: string, data: { assignedUserId: string; assignmentReason?: string }) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/assign`, data),

  /** POST /oms/requests/{id}/reassign — ReassignRequestCommand.newAssignedUserId. */
  reassignRequest: (
    requestId: string,
    data: { newAssignedUserId: string; reassignmentReason?: string },
  ) => api.post<OmsRequest>(`/oms/requests/${requestId}/reassign`, data),

  /** POST /oms/requests/{id}/approve — ApproveRequestCommand.approvalNotes. */
  approveRequest: (requestId: string, approvalNotes?: string) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/approve`, { approvalNotes }),

  /** POST /oms/requests/{id}/reject — RejectRequestCommand.rejectionReason (required). */
  rejectRequest: (requestId: string, rejectionReason: string) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/reject`, { rejectionReason }),

  /**
   * POST /oms/requests/{id}/return-for-correction.
   * The verb is `return-for-correction`, NOT `return` (which is not a route), and
   * the payload field is `correctionReason`, required by the validator.
   */
  returnForCorrection: (requestId: string, correctionReason: string) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/return-for-correction`, { correctionReason }),

  /** POST /oms/requests/{id}/cancel — CancelRequestCommand.cancellationReason. */
  cancelRequest: (requestId: string, cancellationReason?: string) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/cancel`, { cancellationReason }),

  /** POST /oms/requests/{id}/complete — CompleteRequestCommand.completionNotes. */
  completeRequest: (requestId: string, completionNotes?: string) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/complete`, { completionNotes }),

  /**
   * POST /oms/requests/{id}/escalate — EscalationReason is required; the target
   * user is optional and, when supplied, also reassigns the request.
   */
  escalateRequest: (requestId: string, data: { escalationReason: string; escalateToUserId?: string }) =>
    api.post<OmsRequest>(`/oms/requests/${requestId}/escalate`, data),

  // ── Comments ─────────────────────────────────────────────────────────────
  /**
   * POST /oms/requests/{id}/comments — AddRequestCommentCommand.message.
   *
   * There is deliberately no GET counterpart: comments are read from the detail
   * payload. The `notifyRequester` flag from the earlier draft of this client
   * does not exist on the command; notification is the server's decision.
   */
  addComment: (requestId: string, message: string) =>
    api.post<OmsRequestComment>(`/oms/requests/${requestId}/comments`, { message }),

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
   * Downloads through the AUTHORIZED endpoint.
   *
   * The route is `/oms/requests/attachments/{attachmentId}/download` — it is NOT
   * nested under the request id. The raw storage key is never known to the
   * browser, so possession of an attachment id is not sufficient to read the
   * file: the server re-checks object-level access on every download.
   */
  downloadAttachment: (attachmentId: string) =>
    api.get<Blob>(`/oms/requests/attachments/${attachmentId}/download`, {
      responseType: 'blob',
    }),

  /** DELETE /oms/requests/attachments/{attachmentId} (not nested under request). */
  deleteAttachment: (attachmentId: string) =>
    api.delete<void>(`/oms/requests/attachments/${attachmentId}`),

  // ── Thin module adapters (create only; no lifecycle verbs here) ──────────
  /**
   * POST /oms/requests/enrollment — CreateEnrollmentRequestCommand.
   * Raises a generic Request against an existing enrollment. The Enrollment
   * module remains authoritative: nothing about the enrollment changes.
   */
  createEnrollmentRequest: (data: CreateOmsEnrollmentRequest) =>
    api.post<OmsRequest>('/oms/requests/enrollment', data),

  /** POST /oms/requests/accommodation — CreateAccommodationRequestCommand. */
  createAccommodationRequest: (data: CreateOmsAccommodationRequest) =>
    api.post<OmsRequest>('/oms/requests/accommodation', data),

  /** POST /oms/requests/assignment — CreateAssignmentRequestCommand. */
  createAssignmentRequest: (data: CreateOmsAssignmentRequest) =>
    api.post<OmsRequest>('/oms/requests/assignment', data),
};

