import { describe, it, expect, vi, beforeEach } from 'vitest';
import { api } from './api';
import {
  omsRequestsService,
  normalizeOmsRequestsPagedResult,
  OmsRequestStatus,
  OmsRequestPriority,
  OmsRequestType,
  OmsRequestListItem,
  type OmsPagedResult,
} from './requests.service';

vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}));

const mocked = api as unknown as Record<'get' | 'post' | 'put' | 'delete', ReturnType<typeof vi.fn>>;

const REQUEST_ID = '11111111-1111-1111-1111-111111111111';
const ATTACHMENT_ID = '22222222-2222-2222-2222-222222222222';

const listItem: OmsRequestListItem = {
  id: REQUEST_ID,
  requestNumber: 'REQ-2026-000001',
  requestType: 'GENERAL_REQUEST',
  title: 'Need more lab time',
  priority: OmsRequestPriority.Normal,
  status: OmsRequestStatus.Submitted,
  requesterUserId: 'student-1',
  assignedUserId: null,
  createdAt: '2026-09-01T10:00:00Z',
  updatedAt: '2026-09-02T10:00:00Z',
  dueDate: null,
};

beforeEach(() => {
  vi.clearAllMocks();
});

describe('requests.service â€” endpoints match the backend controller', () => {
  it('GET /oms/requests returns a normalized PagedResult envelope', async () => {
    const envelope: OmsPagedResult<OmsRequestListItem> = {
      items: [listItem],
      totalCount: 1,
      pageNumber: 1,
      pageSize: 20,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    };
    mocked.get.mockResolvedValueOnce(envelope);

    const result = await omsRequestsService.getRequests({ scope: 'mine', pageNumber: 1 });

    expect(mocked.get).toHaveBeenCalledWith('/oms/requests', {
      params: { scope: 'mine', pageNumber: 1 },
    });
    expect(result.items).toHaveLength(1);
    expect(result.totalCount).toBe(1);
  });

  it('GET /oms/requests/{id} targets the detail endpoint', async () => {
    mocked.get.mockResolvedValueOnce({});
    await omsRequestsService.getRequest(REQUEST_ID);
    expect(mocked.get).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}`);
  });

  it('GET /oms/requests/{id}/history targets the history endpoint', async () => {
    mocked.get.mockResolvedValueOnce([]);
    await omsRequestsService.getHistory(REQUEST_ID);
    expect(mocked.get).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}/history`);
  });

  it('POST /oms/requests creates and takes the generic payload', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.createRequest({ requestType: 'GENERAL_REQUEST', title: 'Hello' });
    expect(mocked.post).toHaveBeenCalledWith('/oms/requests', {
      requestType: 'GENERAL_REQUEST',
      title: 'Hello',
    });
  });

  it('submit posts to /submit with no body', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.submitRequest(REQUEST_ID);
    // SubmitRequestCommand takes only the route id, so no body is sent.
    expect(mocked.post).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}/submit`);
  });

  it('startReview posts to /start-review, NOT /review, with no body', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.startReview(REQUEST_ID);
    expect(mocked.post).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}/start-review`);
    // The controller action takes no [FromBody]; the earlier '/review' path
    // with a { notes } payload did not exist server-side.
    expect(mocked.post.mock.calls[0][0]).not.toContain('/review"');
  });

  it('assign sends assignedUserId and assignmentReason', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.assignRequest(REQUEST_ID, {
      assignedUserId: 'coordinator-2',
      assignmentReason: 'owner of the unit',
    });
    expect(mocked.post).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}/assign`, {
      assignedUserId: 'coordinator-2',
      assignmentReason: 'owner of the unit',
    });
  });

  it('reassign sends newAssignedUserId, not assignedUserId', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.reassignRequest(REQUEST_ID, {
      newAssignedUserId: 'coordinator-3',
      reassignmentReason: 'conflict of interest',
    });
    const body = mocked.post.mock.calls[0][1];
    expect(body).toEqual({
      newAssignedUserId: 'coordinator-3',
      reassignmentReason: 'conflict of interest',
    });
    expect(body).not.toHaveProperty('assignedUserId');
  });

  it('approve sends approvalNotes', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.approveRequest(REQUEST_ID, 'looks good');
    expect(mocked.post).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}/approve`, {
      approvalNotes: 'looks good',
    });
  });

  it('reject sends rejectionReason', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.rejectRequest(REQUEST_ID, 'insufficient justification');
    expect(mocked.post).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}/reject`, {
      rejectionReason: 'insufficient justification',
    });
  });

  it('returnForCorrection posts to /return-for-correction, NOT /return, with correctionReason', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.returnForCorrection(REQUEST_ID, 'attach the transcript');
    expect(mocked.post).toHaveBeenCalledWith(
      `/oms/requests/${REQUEST_ID}/return-for-correction`,
      { correctionReason: 'attach the transcript' },
    );
    // There is no `/return` route on the controller.
    expect(mocked.post.mock.calls[0][0]).not.toBe(`/oms/requests/${REQUEST_ID}/return`);
  });

  it('cancel sends cancellationReason', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.cancelRequest(REQUEST_ID, 'no longer needed');
    expect(mocked.post).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}/cancel`, {
      cancellationReason: 'no longer needed',
    });
  });

  it('complete sends completionNotes', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.completeRequest(REQUEST_ID, 'lab time granted');
    expect(mocked.post).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}/complete`, {
      completionNotes: 'lab time granted',
    });
  });

  it('escalate sends escalationReason and the optional target', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.escalateRequest(REQUEST_ID, {
      escalationReason: 'needs the dean',
      escalateToUserId: 'dean-1',
    });
    expect(mocked.post).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}/escalate`, {
      escalationReason: 'needs the dean',
      escalateToUserId: 'dean-1',
    });
  });
});

describe('requests.service â€” comments', () => {
  it('adds a comment with only the message field', async () => {
    mocked.post.mockResolvedValueOnce({});
    await omsRequestsService.addComment(REQUEST_ID, 'Please clarify.');
    // AddRequestCommentCommand has no NotifyRequester property; notification is
    // the server's decision, not the client's.
    expect(mocked.post).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}/comments`, {
      message: 'Please clarify.',
    });
  });

  it('exposes no GET comments endpoint, because none exists server-side', () => {
    // Comments are embedded in RequestDetailDto. Calling GET /{id}/comments
    // would 404 against the real controller.
    expect('getComments' in omsRequestsService).toBe(false);
  });
});

describe('requests.service â€” attachments use the authorized flat routes', () => {
  it('lists attachments nested under the request', async () => {
    mocked.get.mockResolvedValueOnce([]);
    await omsRequestsService.getAttachments(REQUEST_ID);
    expect(mocked.get).toHaveBeenCalledWith(`/oms/requests/${REQUEST_ID}/attachments`);
  });

  it('uploads as multipart form data with a "file" part', async () => {
    mocked.post.mockResolvedValueOnce({});
    const file = new File(['data'], 'evidence.pdf', { type: 'application/pdf' });
    await omsRequestsService.uploadAttachment(REQUEST_ID, file);
    const form = mocked.post.mock.calls[0][1] as FormData;
    expect(form.get('file')).toBe(file);
  });

  it('downloads from /oms/requests/attachments/{id}/download, not nested under the request', async () => {
    mocked.get.mockResolvedValueOnce(new Blob(['x']));
    await omsRequestsService.downloadAttachment(ATTACHMENT_ID);
    // The download route is NOT under /{requestId}. The earlier client built
    // `/{requestId}/attachments/{id}/download`, which is not a real route.
    expect(mocked.get).toHaveBeenCalledWith(`/oms/requests/attachments/${ATTACHMENT_ID}/download`, {
      responseType: 'blob',
    });
  });

  it('deletes from /oms/requests/attachments/{id}, not nested under the request', async () => {
    mocked.delete.mockResolvedValueOnce(undefined);
    await omsRequestsService.deleteAttachment(ATTACHMENT_ID);
    expect(mocked.delete).toHaveBeenCalledWith(`/oms/requests/attachments/${ATTACHMENT_ID}`);
  });
});


describe('requests.service â€” request types and dashboard', () => {
  it('reads request types from /types', async () => {
    const types: OmsRequestType[] = [
      {
        id: 'r1',
        code: 'GENERAL_REQUEST',
        displayName: 'General Request',
        description: null,
        isActive: true,
        defaultPriority: OmsRequestPriority.Low,
        createdAt: '2026-09-01T00:00:00Z',
      },
    ];
    mocked.get.mockResolvedValueOnce(types);
    const result = await omsRequestsService.getRequestTypes();
    expect(mocked.get).toHaveBeenCalledWith('/oms/requests/types');
    expect(result[0].code).toBe('GENERAL_REQUEST');
  });

  it('reads the dashboard summary from /dashboard', async () => {
    mocked.get.mockResolvedValueOnce({ totalRequests: 0, countsByStatus: {} });
    await omsRequestsService.getDashboardSummary();
    expect(mocked.get).toHaveBeenCalledWith('/oms/requests/dashboard');
  });
});

describe('requests.service â€” module adapters', () => {
  it('creates an enrollment request through the adapter endpoint', async () => {
    mocked.post.mockResolvedValueOnce({ id: 'req-1' });
    await omsRequestsService.createEnrollmentRequest({
      enrollmentId: 'enr-1',
      requestType: 'ENROLLMENT_EXCEPTION',
    });
    expect(mocked.post).toHaveBeenCalledWith('/oms/requests/enrollment', {
      enrollmentId: 'enr-1',
      requestType: 'ENROLLMENT_EXCEPTION',
    });
  });

  it('creates an accommodation request through the adapter endpoint', async () => {
    mocked.post.mockResolvedValueOnce({ id: 'req-2' });
    await omsRequestsService.createAccommodationRequest({
      accommodationId: 'acc-1',
      requestType: 'ACCOMMODATION_TRANSFER',
    });
    expect(mocked.post).toHaveBeenCalledWith('/oms/requests/accommodation', {
      accommodationId: 'acc-1',
      requestType: 'ACCOMMODATION_TRANSFER',
    });
  });

  it('creates an assignment request through the adapter endpoint', async () => {
    mocked.post.mockResolvedValueOnce({ id: 'req-3' });
    await omsRequestsService.createAssignmentRequest({
      assignmentId: 'asg-1',
      requestType: 'ASSIGNMENT_EXTENSION',
    });
    expect(mocked.post).toHaveBeenCalledWith('/oms/requests/assignment', {
      assignmentId: 'asg-1',
      requestType: 'ASSIGNMENT_EXTENSION',
    });
  });

  it('exposes no adapter for modules the backend does not support', () => {
    const names = Object.keys(omsRequestsService);
    expect(names).toContain('createEnrollmentRequest');
    expect(names).toContain('createAccommodationRequest');
    expect(names).toContain('createAssignmentRequest');
    // Assessment, Certificate and Timetable deliberately have no backend
    // adapter, so the client must not offer one either.
    expect(names.some((n) => /assessment|certificate|timetable/i.test(n))).toBe(false);
  });
});

describe('normalizeOmsRequestsPagedResult', () => {
  it('accepts a bare array as a single unpaged page', () => {
    const result = normalizeOmsRequestsPagedResult([listItem]);
    expect(result.items).toHaveLength(1);
    expect(result.totalCount).toBe(1);
  });

  it('reads rows from the items array of a paginated envelope', () => {
    // This is the `/units` regression guard: the envelope must never be treated
    // as the array itself.
    const result = normalizeOmsRequestsPagedResult({
      items: [listItem],
      totalCount: 42,
      pageNumber: 2,
      pageSize: 20,
    } as unknown);
    expect(result.items).toHaveLength(1);
    expect(result.totalCount).toBe(42);
    expect(result.totalPages).toBe(3);
    expect(result.hasNextPage).toBe(true);
    expect(result.hasPreviousPage).toBe(true);
  });

  it('throws rather than silently returning an empty list for a malformed payload', () => {
    expect(() => normalizeOmsRequestsPagedResult({ unexpected: true })).toThrow(
      /Unexpected response shape/,
    );
  });
});
