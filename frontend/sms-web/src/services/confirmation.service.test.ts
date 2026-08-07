import { describe, it, expect, vi, beforeEach } from 'vitest';
import { confirmationService } from './confirmation.service';
import { api } from './api';

vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('confirmationService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getPendingEnrollments calls api.get with the student id', async () => {
    const mockData = [{ id: 'enroll-1' }];
    (api.get as any).mockResolvedValue(mockData);

    const result = await confirmationService.getPendingEnrollments('student-123');

    expect(api.get).toHaveBeenCalledWith('/confirmation/enrollments/pending/student-123');
    expect(result).toEqual(mockData);
  });

  it('confirmEnrollment calls api.post with confirm data', async () => {
    (api.post as any).mockResolvedValue({ success: true });

    const result = await confirmationService.confirmEnrollment('enroll-1', { confirm: true });

    expect(api.post).toHaveBeenCalledWith('/confirmation/enrollments/enroll-1/confirm', { confirm: true });
    expect(result).toEqual({ success: true });
  });

  it('confirmTeaching calls api.post on the teaching endpoint', async () => {
    (api.post as any).mockResolvedValue({ success: true });

    const result = await confirmationService.confirmTeaching('teach-1', { confirm: true, notes: 'ok' });

    expect(api.post).toHaveBeenCalledWith('/confirmation/teaching/teach-1/confirm', {
      confirm: true,
      notes: 'ok',
    });
    expect(result).toEqual({ success: true });
  });

  it('reportIssue calls api.post with the issue payload', async () => {
    (api.post as any).mockResolvedValue({ id: 'issue-1' });

    const payload = {
      reporterUserId: 'user-1',
      assignmentType: 'Enrollment' as const,
      courseOfferingId: 'offering-1',
      courseOfferingEnrollmentId: 'enroll-1',
      reason: 'Wrong course',
    };

    const result = await confirmationService.reportIssue(payload);

    expect(api.post).toHaveBeenCalledWith('/confirmation/issues', payload);
    expect(result).toEqual({ id: 'issue-1' });
  });
});
