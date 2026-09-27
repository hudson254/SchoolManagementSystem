import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { SnackbarProvider } from 'notistack';
import { RequestsDashboard } from './RequestsDashboard';
import { omsRequestsService } from '../../services/requests.service';
import * as authHook from '../../hooks/useAuth';

vi.mock('../../services/requests.service', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../services/requests.service')>();
  return { ...actual, omsRequestsService: { getDashboardSummary: vi.fn() } };
});

vi.mock('../../hooks/useAuth', () => ({ useAuth: vi.fn() }));

const summary = {
  totalRequests: 0,
  draftCount: 0,
  submittedCount: 0,
  pendingReviewCount: 0,
  assignedCount: 0,
  pendingApprovalCount: 0,
  approvedCount: 0,
  rejectedCount: 0,
  returnedCount: 0,
  inProgressCount: 0,
  completedCount: 0,
  cancelledCount: 0,
  onHoldCount: 0,
  escalatedCount: 0,
  countsByStatus: {},
};

const renderIt = () =>
  render(
    <QueryClientProvider
      client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}
    >
      <MemoryRouter>
        <SnackbarProvider>
          <RequestsDashboard />
        </SnackbarProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );

describe('RequestsDashboard render regression (production OMS crash)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { id: 'user-1', roles: ['Coordinator'] },
    });
    (omsRequestsService.getDashboardSummary as ReturnType<typeof vi.fn>).mockResolvedValue(
      summary,
    );
  });

  /**
   * Production behaviour before the fix: every queue card renders a MUI icon
   * that had been imported as a deep default import
   * (`import X from '@mui/icons-material/X'`). In the built bundle those bind to
   * the CommonJS module namespace OBJECT, so React throws error #130
   * ("Element type is invalid ... got: object") and the app-wide ErrorBoundary
   * replaces the page with "Something went wrong".
   *
   * Asserting the card LABEL is reachable proves the whole component tree —
   * including each icon element inside it — actually rendered. If an icon
   * element is invalid again React throws during this render and the labels are
   * never produced, so this test fails rather than passing silently.
   */
  it('renders its queue cards without throwing an invalid element type', async () => {
    renderIt();

    await waitFor(() => {
      expect(screen.getByLabelText('Open My Requests')).toBeInTheDocument();
    });
    expect(screen.getByLabelText('Open Assigned to Me')).toBeInTheDocument();
    // "Available Queue" is the Administrator/Coordinator-only queue.
    expect(screen.getByLabelText('Open Available Queue')).toBeInTheDocument();
    expect(screen.getByText('Request Workspace')).toBeInTheDocument();
  });

  it('renders the status overview without an element-type crash', async () => {
    renderIt();
    await waitFor(() => {
      expect(screen.getByText('Request Status Overview')).toBeInTheDocument();
    });
  });
});
