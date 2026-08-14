import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { AssignmentConfirm } from './AssignmentConfirm';
import { confirmationService } from '../services/confirmation.service';
import { ConfirmationStatus } from '../services/course-offering.service';
import * as authHook from '../hooks/useAuth';

vi.mock('../services/confirmation.service', () => ({
  confirmationService: {
    confirmEnrollment: vi.fn(),
    confirmTeaching: vi.fn(),
    reportIssue: vi.fn(),
  },
}));

vi.mock('../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

const mockPending = {
  id: 'enroll-1',
  courseOfferingId: 'offering-1',
  studentId: 'student-1',
  courseName: 'Computer Science',
  courseCode: 'CS101',
  academicYearName: '2025/2026',
  semesterName: '1',
  offeringCode: 'CS-2025-1',
  status: 'Pending',
  enrollmentDate: '2026-01-15T00:00:00Z',
  isActive: true,
  attemptNumber: 1,
  confirmationStatus: ConfirmationStatus.Pending,
};

function renderWithProviders(ui: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('AssignmentConfirm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (authHook.useAuth as any).mockReturnValue({
      user: { id: 'user-1', roles: ['Student'] },
    });
  });

  it('renders enrollment confirmation dialog', () => {
    renderWithProviders(
      <AssignmentConfirm open onClose={vi.fn()} pending={mockPending} type="enrollment" />
    );

    expect(screen.getByText('Enrollment Confirmation')).toBeInTheDocument();
    expect(screen.getByText('Computer Science')).toBeInTheDocument();
    expect(screen.getByText('Confirm Enrollment')).toBeInTheDocument();
    expect(screen.getByText('Report an Issue')).toBeInTheDocument();
  });

  it('renders teaching assignment confirmation dialog', () => {
    renderWithProviders(
      <AssignmentConfirm open onClose={vi.fn()} pending={mockPending} type="teaching" />
    );

    expect(screen.getByText('Teaching Assignment Confirmation')).toBeInTheDocument();
    expect(screen.getByText('Accept Teaching Assignment')).toBeInTheDocument();
  });

  it('calls confirmEnrollment when confirm button is clicked', async () => {
    (confirmationService.confirmEnrollment as any).mockResolvedValue({ success: true });
    const onClose = vi.fn();

    renderWithProviders(
      <AssignmentConfirm open onClose={onClose} pending={mockPending} type="enrollment" />
    );

    fireEvent.click(screen.getByText('Confirm Enrollment'));

    await waitFor(() => {
      expect(confirmationService.confirmEnrollment).toHaveBeenCalledWith('enroll-1', { confirm: true });
    });

    await waitFor(() => {
      expect(onClose).toHaveBeenCalled();
    });
  });

  it('calls confirmTeaching when teaching confirm button is clicked', async () => {
    (confirmationService.confirmTeaching as any).mockResolvedValue({ success: true });
    const onClose = vi.fn();

    renderWithProviders(
      <AssignmentConfirm open onClose={onClose} pending={mockPending} type="teaching" />
    );

    fireEvent.click(screen.getByText('Accept Teaching Assignment'));

    await waitFor(() => {
      expect(confirmationService.confirmTeaching).toHaveBeenCalledWith('enroll-1', { confirm: true });
    });

    await waitFor(() => {
      expect(onClose).toHaveBeenCalled();
    });
  });

  it('shows issue form and calls reportIssue', async () => {
    (confirmationService.reportIssue as any).mockResolvedValue({ id: 'issue-1' });
    const onClose = vi.fn();

    renderWithProviders(
      <AssignmentConfirm open onClose={onClose} pending={mockPending} type="enrollment" />
    );

    fireEvent.click(screen.getByText('Report an Issue'));

    expect(screen.getByText('Describe the issue')).toBeInTheDocument();

    fireEvent.change(screen.getByPlaceholderText('Please describe the issue with this assignment...'), {
      target: { value: 'Wrong course assignment' },
    });

    fireEvent.click(screen.getByText('Submit Issue Report'));

    await waitFor(() => {
      expect(confirmationService.reportIssue).toHaveBeenCalledWith({
        reporterUserId: 'user-1',
        assignmentType: 'Enrollment',
        courseOfferingId: 'offering-1',
        courseOfferingEnrollmentId: 'enroll-1',
        courseOfferingLecturerId: undefined,
        reason: 'Wrong course assignment',
      });
    });

    await waitFor(() => {
      expect(onClose).toHaveBeenCalled();
    });
  });
});
