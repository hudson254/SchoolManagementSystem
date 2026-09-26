import {
  OmsRequestStatus,
  OmsRequestPriority,
  OmsRequestActionType,
  OmsRequestActionTypeValue,
} from '../services/requests.service';
import {
  hasAnyRole,
  canAssignOmsRequests,
  canDecideOmsRequests,
  canCompleteOmsRequests,
  canCreateOmsRequest,
  canViewAllOmsRequests,
  OMS_REQUEST_ADMIN_ROLES,
  OMS_REQUEST_VIEW_ROLES,
} from './roles';

/**
 * Presentation-only derivation of OMS request affordances.
 *
 * â”€â”€ Scope and limits of this module â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
 * The BACKEND is authoritative. Every rule encoded here mirrors a server-side
 * rule that already exists, so the UI can hide actions that are certainly going
 * to be rejected:
 *
 *   â€¢ status gates      â† RequestLifecycle + each command handler's guard
 *   â€¢ role gates        â† OmsAuthorization.*Roles
 *   â€¢ ownership gates   â† OmsRequestAccess.IsRequester / IsAssignee
 *
 * Nothing here grants access. A caller who bypasses this module still faces the
 * same server-side role check, tenant check and object-level ownership check.
 * The purpose is purely to avoid rendering a button that cannot work.
 *
 * Because it is a pure function of (request, user) it is fully unit-testable
 * without a browser or a network.
 */

/** The lifecycle verbs the backend actually implements. */
export type OmsRequestActionKey =
  | 'submit'
  | 'startReview'
  | 'assign'
  | 'reassign'
  | 'approve'
  | 'reject'
  | 'returnForCorrection'
  | 'cancel'
  | 'complete'
  | 'escalate';

export type OmsRequestRequiredField =
  | 'rejectionReason'
  | 'correctionReason'
  | 'assignedUserId'
  | 'newAssignedUserId'
  | 'escalationReason';

export type OmsRequestOptionalField =
  | 'approvalNotes'
  | 'completionNotes'
  | 'assignmentReason'
  | 'reassignmentReason'
  | 'cancellationReason';

export interface OmsRequestActionDescriptor {
  key: OmsRequestActionKey;
  label: string;
  /** Requires a dialog and user-supplied input. */
  requiresInput: boolean;
  /** Which field the backend validator actually requires. */
  requiredField?: OmsRequestRequiredField;
  /** Optional free-text the handler accepts but does not require. */
  optionalField?: OmsRequestOptionalField;
  variant: 'contained' | 'outlined' | 'text';
  color: 'primary' | 'success' | 'warning' | 'error' | 'inherit';
}

const DESCRIPTORS: Record<OmsRequestActionKey, OmsRequestActionDescriptor> = {
  submit: { key: 'submit', label: 'Submit', requiresInput: false, variant: 'contained', color: 'primary' },
  startReview: { key: 'startReview', label: 'Start Review', requiresInput: false, variant: 'outlined', color: 'primary' },
  assign: {
    key: 'assign', label: 'Assign', requiresInput: true, requiredField: 'assignedUserId',
    optionalField: 'assignmentReason', variant: 'outlined', color: 'primary',
  },
  reassign: {
    key: 'reassign', label: 'Reassign', requiresInput: true, requiredField: 'newAssignedUserId',
    optionalField: 'reassignmentReason', variant: 'outlined', color: 'primary',
  },
  approve: {
    key: 'approve', label: 'Approve', requiresInput: false,
    optionalField: 'approvalNotes', variant: 'contained', color: 'success',
  },
  reject: {
    key: 'reject', label: 'Reject', requiresInput: true, requiredField: 'rejectionReason',
    variant: 'outlined', color: 'error',
  },
  returnForCorrection: {
    key: 'returnForCorrection', label: 'Return for Correction', requiresInput: true,
    requiredField: 'correctionReason', variant: 'outlined', color: 'warning',
  },
  cancel: {
    key: 'cancel', label: 'Cancel Request', requiresInput: false,
    optionalField: 'cancellationReason', variant: 'outlined', color: 'inherit',
  },
  complete: {
    key: 'complete', label: 'Mark Complete', requiresInput: false,
    optionalField: 'completionNotes', variant: 'contained', color: 'success',
  },
  escalate: {
    key: 'escalate', label: 'Escalate', requiresInput: true, requiredField: 'escalationReason',
    variant: 'outlined', color: 'warning',
  },
};

/** Mirrors RequestLifecycle.IsTerminal. */
export const isTerminalOmsRequestStatus = (status: OmsRequestStatus): boolean =>
  status === OmsRequestStatus.Completed ||
  status === OmsRequestStatus.Rejected ||
  status === OmsRequestStatus.Cancelled;

/** Mirrors RequestLifecycle.CanEdit â€” Draft or Returned. */
export const isEditableOmsRequestStatus = (status: OmsRequestStatus): boolean =>
  status === OmsRequestStatus.Draft || status === OmsRequestStatus.Returned;

/** Human label for a status (identity, just humanised). */
export const omsRequestStatusLabel = (status: string): string =>
  status ? status.replace(/([a-z])([A-Z])/g, '$1 $2') : '';

/** Human label for a priority value (1â€¦4). */
export const omsRequestPriorityLabel = (priority: OmsRequestPriority): string => {
  switch (priority) {
    case OmsRequestPriority.Low: return 'Low';
    case OmsRequestPriority.Normal: return 'Normal';
    case OmsRequestPriority.High: return 'High';
    case OmsRequestPriority.Urgent: return 'Urgent';
    default: return 'Normal';
  }
};

/** Human label for a RequestActionType value. */
export const omsRequestActionLabel = (action: OmsRequestActionTypeValue): string => {
  const entry = Object.entries(OmsRequestActionType).find(([, value]) => value === action);
  if (!entry) return 'Updated';
  switch (entry[0]) {
    case 'ReviewStarted': return 'Review started';
    case 'InProgress': return 'Work started';
    case 'OnHold': return 'Placed on hold';
    default: return omsRequestStatusLabel(entry[0]);
  }
};

export interface OmsRequestActionContext {
  status: OmsRequestStatus;
  /** The authenticated user id, used for the ownership gates. */
  userId?: string | null;
  requesterUserId?: string | null;
  assignedUserId?: string | null;
  roles?: string[] | null;
}

const isSameUser = (a?: string | null, b?: string | null): boolean =>
  !!a && !!b && a.toLowerCase() === b.toLowerCase();

/**
 * Returns the actions that are valid for this request in its current status,
 * for this user, right now.
 *
 * Status rules are copied verbatim from the corresponding command handlers:
 *   submit              â† SubmitRequestCommand:        Draft | Returned
 *   startReview         â† StartRequestReviewCommand:   Submitted | PendingReview | Returned
 *   assign              â† AssignRequestCommand:        PendingReview | Submitted
 *   approve             â† ApproveRequestCommand:       PendingApproval | Assigned
 *   reject              â† RejectRequestCommand:        PendingApproval | Assigned | Submitted
 *   returnForCorrection â† ReturnRequestCommand:        PendingReview | PendingApproval | Assigned
 *   complete            â† CompleteRequestCommand:      Approved | InProgress
 *   escalate            â† EscalateRequestCommand:      PendingApproval | Assigned
 *   cancel              â† CancelRequestCommand:        any non-terminal status
 *
 * Role/ownership rules:
 *   â€¢ approve additionally requires the caller NOT be the requester â€” the server
 *     throws BusinessRuleException on self-approval;
 *   â€¢ cancel requires ownership unless the caller can cancel any request;
 *   â€¢ assign/reassign/approve/reject/complete/escalate require the decision role
 *     set; startReview and returnForCorrection also allow Lecturer.
 */
export const getAvailableOmsRequestActions = (
  context: OmsRequestActionContext,
): OmsRequestActionDescriptor[] => {
  const { status, userId, requesterUserId, assignedUserId, roles } = context;

  // Terminal requests are frozen. Mirrors IsTerminal plus the terminal guard in
  // every mutating handler.
  if (isTerminalOmsRequestStatus(status)) return [];

  const isRequester = isSameUser(userId, requesterUserId);
  const isAssignee = isSameUser(userId, assignedUserId);
  const canDecide = canDecideOmsRequests(roles);
  const canAssign = canAssignOmsRequests(roles);
  const canComplete = canCompleteOmsRequests(roles);
  const canReview = hasAnyRole(roles, ...OMS_REQUEST_VIEW_ROLES);
  // CancelAnyRequestRoles is the Administrator tier, not the Coordinator tier.
  const canCancelAny = hasAnyRole(roles, ...OMS_REQUEST_ADMIN_ROLES);
  const canCancelOwn = canCreateOmsRequest(roles) && isRequester;

  const actions: OmsRequestActionDescriptor[] = [];

  if (
    (isRequester || canDecide) &&
    (status === OmsRequestStatus.Draft || status === OmsRequestStatus.Returned)
  ) {
    actions.push(DESCRIPTORS.submit);
  }

  if (
    canReview &&
    (status === OmsRequestStatus.Submitted ||
      status === OmsRequestStatus.PendingReview ||
      status === OmsRequestStatus.Returned)
  ) {
    actions.push(DESCRIPTORS.startReview);
  }

  if (
    canAssign &&
    (status === OmsRequestStatus.PendingReview || status === OmsRequestStatus.Submitted)
  ) {
    actions.push(DESCRIPTORS.assign);
  }

  // Reassign is not status-gated by its handler (it checks role + tenant only),
  // but it is only meaningful once the caller already owns the assignment.
  if (canAssign && isAssignee) {
    actions.push(DESCRIPTORS.reassign);
  }

  if (
    canDecide &&
    !isRequester &&
    (status === OmsRequestStatus.PendingApproval || status === OmsRequestStatus.Assigned)
  ) {
    actions.push(DESCRIPTORS.approve);
  }

  if (
    canDecide &&
    (status === OmsRequestStatus.PendingApproval ||
      status === OmsRequestStatus.Assigned ||
      status === OmsRequestStatus.Submitted)
  ) {
    actions.push(DESCRIPTORS.reject);
  }

  if (
    canReview &&
    (status === OmsRequestStatus.PendingReview ||
      status === OmsRequestStatus.PendingApproval ||
      status === OmsRequestStatus.Assigned)
  ) {
    actions.push(DESCRIPTORS.returnForCorrection);
  }

  if (
    canComplete &&
    (status === OmsRequestStatus.Approved || status === OmsRequestStatus.InProgress)
  ) {
    actions.push(DESCRIPTORS.complete);
  }

  if (
    canComplete &&
    (status === OmsRequestStatus.PendingApproval || status === OmsRequestStatus.Assigned)
  ) {
    actions.push(DESCRIPTORS.escalate);
  }

  if (canCancelAny || canCancelOwn) {
    actions.push(DESCRIPTORS.cancel);
  }

  return actions;
};

/**
 * Whether the caller may attach a file right now.
 * Mirrors OmsRequestAccess.CanAttach: must be able to view the request, and the
 * request must not be terminal (terminal requests are frozen so their evidence
 * set cannot change after the fact).
 */
export const canAttachToOmsRequest = (context: OmsRequestActionContext): boolean => {
  if (isTerminalOmsRequestStatus(context.status)) return false;
  if (canViewAllOmsRequests(context.roles)) return true;
  if (
    isSameUser(context.userId, context.requesterUserId) &&
    hasAnyRole(context.roles, ...OMS_REQUEST_VIEW_ROLES)
  ) {
    return true;
  }
  return isSameUser(context.userId, context.assignedUserId);
};

/**
 * Whether the caller may delete this specific attachment.
 * Mirrors OmsRequestAccess.CanDeleteAttachment: administrators may remove any
 * attachment, otherwise only the uploader may remove their own â€” and only while
 * the request is still open for work.
 */
export const canDeleteOmsRequestAttachment = (
  context: OmsRequestActionContext,
  attachment: { uploadedByUserId?: string | null },
): boolean => {
  if (!canAttachToOmsRequest(context)) return false;
  if (hasAnyRole(context.roles, ...OMS_REQUEST_ADMIN_ROLES)) return true;
  return isSameUser(context.userId, attachment.uploadedByUserId);
};

/** Mirrors AddRequestCommentCommand's role gate (Oms.CommentOnRequest). */
export const canCommentOnOmsRequest = (roles: string[] | undefined | null): boolean =>
  hasAnyRole(roles, ...OMS_REQUEST_VIEW_ROLES);
