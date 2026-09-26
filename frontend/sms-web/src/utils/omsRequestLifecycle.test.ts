import { describe, it, expect } from 'vitest';
import {
  OmsRequestStatus,
  OmsRequestPriority,
  OmsRequestActionType,
  OmsModuleRequestTypeCodes,
} from '../services/requests.service';
import {
  getAvailableOmsRequestActions,
  isTerminalOmsRequestStatus,
  isEditableOmsRequestStatus,
  canAttachToOmsRequest,
  canDeleteOmsRequestAttachment,
  canCommentOnOmsRequest,
  omsRequestPriorityLabel,
  omsRequestStatusLabel,
  type OmsRequestActionContext,
} from './omsRequestLifecycle';

const COORDINATOR = ['Coordinator'];
const ADMIN = ['Administrator'];
const LECTURER = ['Lecturer'];
const STUDENT = ['Student'];

/** A Coordinator acting on someone else's request — the default test context. */
const ctx = (overrides: Partial<OmsRequestActionContext> = {}): OmsRequestActionContext => ({
  status: OmsRequestStatus.Draft,
  userId: 'coordinator-1',
  requesterUserId: 'student-1',
  assignedUserId: null,
  roles: COORDINATOR,
  ...overrides,
});

const keys = (c: OmsRequestActionContext) => getAvailableOmsRequestActions(c).map((a) => a.key);

const descriptorFor = (c: OmsRequestActionContext, key: string) =>
  getAvailableOmsRequestActions(c).find((a) => a.key === key);

describe('omsRequestLifecycle — enum fidelity', () => {
  it('mirrors the backend RequestPriority numbering of 1..4', () => {
    // The wire format is the serialized enum: Low=1 … Urgent=4. Renumbering
    // these would silently change every stored priority.
    expect(OmsRequestPriority.Low).toBe(1);
    expect(OmsRequestPriority.Normal).toBe(2);
    expect(OmsRequestPriority.High).toBe(3);
    expect(OmsRequestPriority.Urgent).toBe(4);
    expect(omsRequestPriorityLabel(1)).toBe('Low');
    expect(omsRequestPriorityLabel(4)).toBe('Urgent');
  });

  it('humanises camel-cased status names', () => {
    expect(omsRequestStatusLabel('PendingApproval')).toBe('Pending Approval');
    expect(omsRequestStatusLabel('InProgress')).toBe('In Progress');
  });

  it('covers every backend RequestStatus and RequestActionType value', () => {
    // 13 statuses in SMS.Domain.Enums.RequestStatus; 14 in RequestActionType.
    expect(Object.keys(OmsRequestStatus)).toHaveLength(13);
    expect(Object.keys(OmsRequestActionType)).toHaveLength(14);
  });
});

describe('omsRequestLifecycle — terminal and editable statuses', () => {
  it('treats Completed, Rejected and Cancelled as terminal', () => {
    expect(isTerminalOmsRequestStatus(OmsRequestStatus.Completed)).toBe(true);
    expect(isTerminalOmsRequestStatus(OmsRequestStatus.Rejected)).toBe(true);
    expect(isTerminalOmsRequestStatus(OmsRequestStatus.Cancelled)).toBe(true);
  });

  it('does not treat in-flight statuses as terminal', () => {
    expect(isTerminalOmsRequestStatus(OmsRequestStatus.Assigned)).toBe(false);
    expect(isTerminalOmsRequestStatus(OmsRequestStatus.Escalated)).toBe(false);
  });

  it('treats only Draft and Returned as editable', () => {
    expect(isEditableOmsRequestStatus(OmsRequestStatus.Draft)).toBe(true);
    expect(isEditableOmsRequestStatus(OmsRequestStatus.Returned)).toBe(true);
    expect(isEditableOmsRequestStatus(OmsRequestStatus.Submitted)).toBe(false);
  });
});

describe('omsRequestLifecycle — terminal requests are frozen', () => {
  it.each([OmsRequestStatus.Completed, OmsRequestStatus.Rejected, OmsRequestStatus.Cancelled])(
    'offers no action for %s, even to an administrator',
    (status) => {
      expect(keys(ctx({ status, roles: ADMIN }))).toEqual([]);
    },
  );

describe('omsRequestLifecycle — submit', () => {
  it('is offered to the requester on a Draft', () => {
    expect(
      keys(ctx({ status: OmsRequestStatus.Draft, userId: 'student-1', roles: STUDENT })),
    ).toContain('submit');
  });

  it('is offered again on a Returned request (resubmission)', () => {
    expect(
      keys(ctx({ status: OmsRequestStatus.Returned, userId: 'student-1', roles: STUDENT })),
    ).toContain('submit');
  });

  it('is not offered once the request has been submitted', () => {
    expect(
      keys(ctx({ status: OmsRequestStatus.Submitted, userId: 'student-1', roles: STUDENT })),
    ).not.toContain('submit');
  });

  it('is offered to a decision-role user even when they did not raise it', () => {
    // SubmitRequestCommand performs NO ownership check — only the role gate, the
    // tenant check and the Draft/Returned status gate. The UI mirrors that
    // rather than inventing a stricter rule the API does not have.
    expect(
      keys(ctx({ status: OmsRequestStatus.Draft, userId: 'someone-else', roles: COORDINATOR })),
    ).toContain('submit');
  });

  it('is not offered to a non-privileged user who did not raise it', () => {
    // A Student lacks both ownership here and the decision role.
    expect(
      keys(ctx({ status: OmsRequestStatus.Draft, userId: 'student-2', roles: STUDENT })),
    ).not.toContain('submit');
  });
});

describe('omsRequestLifecycle — startReview', () => {
  it.each([OmsRequestStatus.Submitted, OmsRequestStatus.PendingReview, OmsRequestStatus.Returned])(
    'is offered to a reviewer in %s',
    (status) => {
      expect(keys(ctx({ status, requesterUserId: 's', roles: COORDINATOR }))).toContain(
        'startReview',
      );
    },
  );

  it('is not offered in a status the handler rejects', () => {
    expect(keys(ctx({ status: OmsRequestStatus.Approved, roles: COORDINATOR }))).not.toContain(
      'startReview',
    );
  });
});

describe('omsRequestLifecycle — assign', () => {
  it('is offered in PendingReview and Submitted', () => {
    expect(
      keys(ctx({ status: OmsRequestStatus.PendingReview, requesterUserId: 's', roles: COORDINATOR })),
    ).toContain('assign');
    expect(
      keys(ctx({ status: OmsRequestStatus.Submitted, requesterUserId: 's', roles: COORDINATOR })),
    ).toContain('assign');
  });

  it('is not offered to a Lecturer, who lacks the decision role', () => {
    expect(
      keys(ctx({ status: OmsRequestStatus.PendingReview, requesterUserId: 's', roles: LECTURER })),
    ).not.toContain('assign');
  });
});

describe('omsRequestLifecycle — reassign', () => {
  it('is offered to a decision role that already holds the assignment', () => {
    expect(
      keys(
        ctx({ status: OmsRequestStatus.Assigned, assignedUserId: 'coordinator-1', roles: COORDINATOR }),
      ),
    ).toContain('reassign');
  });

  it('is not offered when the caller is not the assignee', () => {
    expect(
      keys(
        ctx({ status: OmsRequestStatus.Assigned, assignedUserId: 'someone-else', roles: COORDINATOR }),
      ),
    ).not.toContain('reassign');
  });
});

describe('omsRequestLifecycle — approve', () => {
  it('is offered in PendingApproval and Assigned', () => {
    expect(
      keys(ctx({ status: OmsRequestStatus.PendingApproval, requesterUserId: 's', roles: COORDINATOR })),
    ).toContain('approve');
    expect(
      keys(ctx({ status: OmsRequestStatus.Assigned, requesterUserId: 's', roles: COORDINATOR })),
    ).toContain('approve');
  });

  it('is NEVER offered to the requester, mirroring the server self-approval block', () => {
    expect(
      keys(
        ctx({
          status: OmsRequestStatus.PendingApproval,
          userId: 'coordinator-1',
          requesterUserId: 'coordinator-1',
          roles: COORDINATOR,
        }),
      ),
    ).not.toContain('approve');
  });

  it('is not offered to a Lecturer', () => {
    expect(
      keys(ctx({ status: OmsRequestStatus.PendingApproval, requesterUserId: 's', roles: LECTURER })),
    ).not.toContain('approve');
  });
});

describe('omsRequestLifecycle — reject', () => {
  it('is offered in PendingApproval, Assigned and Submitted', () => {
    for (const status of [
      OmsRequestStatus.PendingApproval,
      OmsRequestStatus.Assigned,
      OmsRequestStatus.Submitted,
    ]) {
      expect(keys(ctx({ status, requesterUserId: 's', roles: COORDINATOR }))).toContain('reject');
    }
  });

  it('is not offered in Approved', () => {
    expect(
      keys(ctx({ status: OmsRequestStatus.Approved, requesterUserId: 's', roles: COORDINATOR })),
    ).not.toContain('reject');
  });
});


describe('omsRequestLifecycle — returnForCorrection', () => {
  it('is offered to a reviewer in the three handler-permitted statuses', () => {
    for (const status of [
      OmsRequestStatus.PendingReview,
      OmsRequestStatus.PendingApproval,
      OmsRequestStatus.Assigned,
    ]) {
      expect(keys(ctx({ status, requesterUserId: 's', roles: LECTURER }))).toContain(
        'returnForCorrection',
      );
    }
  });
});

describe('omsRequestLifecycle — complete', () => {
  it('is offered in Approved and InProgress only', () => {
    expect(
      keys(ctx({ status: OmsRequestStatus.Approved, requesterUserId: 's', roles: COORDINATOR })),
    ).toContain('complete');
    expect(
      keys(ctx({ status: OmsRequestStatus.InProgress, requesterUserId: 's', roles: COORDINATOR })),
    ).toContain('complete');
    expect(
      keys(ctx({ status: OmsRequestStatus.Assigned, requesterUserId: 's', roles: COORDINATOR })),
    ).not.toContain('complete');
  });
});

describe('omsRequestLifecycle — escalate', () => {
  it('is offered in PendingApproval and Assigned only', () => {
    expect(
      keys(ctx({ status: OmsRequestStatus.PendingApproval, requesterUserId: 's', roles: COORDINATOR })),
    ).toContain('escalate');
    expect(
      keys(ctx({ status: OmsRequestStatus.InProgress, requesterUserId: 's', roles: COORDINATOR })),
    ).not.toContain('escalate');
  });
});

describe('omsRequestLifecycle — cancel', () => {
  it('is offered to the requester for a non-terminal request', () => {
    expect(keys(ctx({ status: OmsRequestStatus.Draft, userId: 'student-1', roles: STUDENT }))).toContain(
      'cancel',
    );
  });

  it("is not offered to a non-privileged user acting on another user's request", () => {
    expect(
      keys(ctx({ status: OmsRequestStatus.Draft, userId: 'student-2', roles: STUDENT })),
    ).not.toContain('cancel');
  });

  it("is offered to an Administrator for another user's request", () => {
    expect(
      keys(
        ctx({ status: OmsRequestStatus.Draft, userId: 'admin-1', requesterUserId: 'student-1', roles: ADMIN }),
      ),
    ).toContain('cancel');
  });
});

describe('omsRequestLifecycle — required fields mirror the backend validators', () => {
  it('requires rejectionReason for reject', () => {
    expect(
      descriptorFor(
        ctx({ status: OmsRequestStatus.Assigned, requesterUserId: 's', roles: COORDINATOR }),
        'reject',
      )?.requiredField,
    ).toBe('rejectionReason');
  });

  it('requires correctionReason for returnForCorrection', () => {
    expect(
      descriptorFor(
        ctx({ status: OmsRequestStatus.Assigned, requesterUserId: 's', roles: COORDINATOR }),
        'returnForCorrection',
      )?.requiredField,
    ).toBe('correctionReason');
  });

  it('requires escalationReason for escalate', () => {
    expect(
      descriptorFor(
        ctx({ status: OmsRequestStatus.Assigned, requesterUserId: 's', roles: COORDINATOR }),
        'escalate',
      )?.requiredField,
    ).toBe('escalationReason');
  });

  it('requires an assignee id for assign and a new id for reassign', () => {
    expect(
      descriptorFor(
        ctx({ status: OmsRequestStatus.PendingReview, requesterUserId: 's', roles: COORDINATOR }),
        'assign',
      )?.requiredField,
    ).toBe('assignedUserId');
    expect(
      descriptorFor(
        ctx({
          status: OmsRequestStatus.Assigned,
          assignedUserId: 'coordinator-1',
          roles: COORDINATOR,
        }),
        'reassign',
      )?.requiredField,
    ).toBe('newAssignedUserId');
  });

  it('treats submit, startReview, approve, complete and cancel as input-free', () => {
    // Draft owned by an admin: submit and cancel are the applicable actions.
    const draft = ctx({
      status: OmsRequestStatus.Draft,
      userId: 'admin-1',
      requesterUserId: 'admin-1',
      roles: ADMIN,
    });
    for (const key of ['submit', 'cancel'] as const) {
      expect(descriptorFor(draft, key)?.requiresInput).toBe(false);
    }

    // Approved, so complete is applicable and takes no required input.
    const approved = ctx({
      status: OmsRequestStatus.Approved,
      requesterUserId: 'student-1',
      roles: COORDINATOR,
    });
    expect(descriptorFor(approved, 'complete')?.requiresInput).toBe(false);
  });
});


describe('omsRequestLifecycle — attachment permissions', () => {
  it('permits a privileged role to attach while the request is open', () => {
    expect(
      canAttachToOmsRequest(ctx({ status: OmsRequestStatus.Assigned, roles: COORDINATOR })),
    ).toBe(true);
  });

  it('freezes attachments on a terminal request even for an administrator', () => {
    expect(
      canAttachToOmsRequest(ctx({ status: OmsRequestStatus.Completed, roles: ADMIN })),
    ).toBe(false);
  });

  it('lets an administrator delete any attachment on an open request', () => {
    expect(
      canDeleteOmsRequestAttachment(
        ctx({ status: OmsRequestStatus.Assigned, roles: ADMIN }),
        { uploadedByUserId: 'someone-else' },
      ),
    ).toBe(true);
  });

  it('lets a non-admin delete only their own upload', () => {
    const context = ctx({
      status: OmsRequestStatus.Assigned,
      userId: 'coordinator-1',
      roles: COORDINATOR,
    });
    expect(
      canDeleteOmsRequestAttachment(context, { uploadedByUserId: 'coordinator-1' }),
    ).toBe(true);
    expect(
      canDeleteOmsRequestAttachment(context, { uploadedByUserId: 'another-user' }),
    ).toBe(false);
  });

  it('refuses deletion by a non-uploader once the request is terminal', () => {
    expect(
      canDeleteOmsRequestAttachment(
        ctx({
          status: OmsRequestStatus.Cancelled,
          userId: 'coordinator-1',
          roles: COORDINATOR,
        }),
        { uploadedByUserId: 'coordinator-1' },
      ),
    ).toBe(false);
  });
});

describe('omsRequestLifecycle — comment permission', () => {
  it('follows the Oms.CommentOnRequest role set', () => {
    expect(canCommentOnOmsRequest(LECTURER)).toBe(true);
    expect(canCommentOnOmsRequest(COORDINATOR)).toBe(true);
    // CommentOnRequestRoles == ViewRequestsRoles, which excludes Student.
    expect(canCommentOnOmsRequest(STUDENT)).toBe(false);
  });
});

describe('omsRequestLifecycle — module adapter request type codes', () => {
  it('match the backend *RequestTypes constants exactly', () => {
    expect(OmsModuleRequestTypeCodes.enrollment).toEqual({
      courseChange: 'ENROLLMENT_COURSE_CHANGE',
      exception: 'ENROLLMENT_EXCEPTION',
      unitCorrection: 'ENROLLMENT_UNIT_CORRECTION',
      cancellation: 'ENROLLMENT_CANCELLATION',
    });
    expect(OmsModuleRequestTypeCodes.accommodation).toEqual({
      transfer: 'ACCOMMODATION_TRANSFER',
      allocation: 'ACCOMMODATION_ALLOCATION',
      exception: 'ACCOMMODATION_EXCEPTION',
      correction: 'ACCOMMODATION_CORRECTION',
    });
    expect(OmsModuleRequestTypeCodes.assignment).toEqual({
      extension: 'ASSIGNMENT_EXTENSION',
      reopen: 'ASSIGNMENT_REOPEN',
      correction: 'ASSIGNMENT_CORRECTION',
      exception: 'ASSIGNMENT_EXCEPTION',
    });
  });

  it('exposes no adapter codes for Assessment, Certificate or Timetable', () => {
    const all = JSON.stringify(OmsModuleRequestTypeCodes);
    expect(all).not.toContain('ASSESSMENT_');
    expect(all).not.toContain('CERTIFICATE_');
    expect(all).not.toContain('TIMETABLE_');
  });
});
});
