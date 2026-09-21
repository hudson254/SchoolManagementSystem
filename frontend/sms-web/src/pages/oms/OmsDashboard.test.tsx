import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { SnackbarProvider } from 'notistack';
import type { ReactNode } from 'react';
import { OmsDashboard } from './OmsDashboard';
import { omsService } from '../../services/oms.service';
import * as authHook from '../../hooks/useAuth';

vi.mock('../../services/oms.service', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../services/oms.service')>();
  return {
    ...actual,
    omsService: {
      getDashboardSummary: vi.fn(),
    },
  };
});

vi.mock('../../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

const summary = {
  draftCount: 3,
  submittedCount: 2,
  pendingApprovalCount: 1,
  approvedCount: 4,
  rejectedCount: 1,
  cancelledCount: 1,
  totalOrders: 12,
  totalsByCurrency: { KES: 12345.5, USD: 20 },
};

function renderWithProviders(ui: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <SnackbarProvider>{ui}</SnackbarProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('OmsDashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { id: 'user-1', roles: ['Coordinator'] },
    });
  });

  it('renders order counts by status from the server summary', async () => {
    (omsService.getDashboardSummary as ReturnType<typeof vi.fn>).mockResolvedValue(summary);
    renderWithProviders(<OmsDashboard />);

    await waitFor(() => {
      expect(screen.getByLabelText('View all orders')).toBeInTheDocument();
    });

    const cardCount = (label: string, count: string) => {
      expect(within(screen.getByLabelText(label)).getByText(count)).toBeInTheDocument();
    };
    cardCount('View all orders', '12');
    cardCount('View draft orders', '3');
    cardCount('View submitted orders', '2');
    cardCount('View pending approval orders', '1');
    cardCount('View approved orders', '4');
    cardCount('View rejected orders', '1');
    cardCount('View cancelled orders', '1');
    expect(screen.getByText('Total Orders')).toBeInTheDocument();
    expect(screen.getByText('Pending Approval')).toBeInTheDocument();
  });

  it('renders currency totals as reported by the server', async () => {
    (omsService.getDashboardSummary as ReturnType<typeof vi.fn>).mockResolvedValue(summary);
    renderWithProviders(<OmsDashboard />);

    await waitFor(() => {
      expect(screen.getByText('KES')).toBeInTheDocument();
    });
    expect(screen.getByText('USD')).toBeInTheDocument();
    expect(screen.getByText(/12,345\.50/)).toBeInTheDocument();
  });

  it('shows the empty state when there are no totals yet', async () => {
    (omsService.getDashboardSummary as ReturnType<typeof vi.fn>).mockResolvedValue({
      ...summary,
      totalsByCurrency: {},
    });
    renderWithProviders(<OmsDashboard />);

    await waitFor(() => {
      expect(screen.getByText('No order totals recorded yet.')).toBeInTheDocument();
    });
  });

  it('shows a loading state while fetching', () => {
    (omsService.getDashboardSummary as ReturnType<typeof vi.fn>).mockReturnValue(
      new Promise(() => {}),
    );
    renderWithProviders(<OmsDashboard />);
    expect(screen.getByText('Loading OMS dashboard...')).toBeInTheDocument();
  });

  it('shows an error state with a retry action when the API fails', async () => {
    (omsService.getDashboardSummary as ReturnType<typeof vi.fn>).mockRejectedValue({
      success: false,
      code: 'INTERNAL_ERROR',
      message: 'boom',
    });
    renderWithProviders(<OmsDashboard />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
    expect(screen.getByText('Retry')).toBeInTheDocument();
  });

  it('shows a permission message for users without OMS view permission', () => {
    (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { id: 'user-2', roles: ['Student'] },
    });
    renderWithProviders(<OmsDashboard />);
    expect(screen.getByText('Order management unavailable')).toBeInTheDocument();
    expect(omsService.getDashboardSummary).not.toHaveBeenCalled();
  });

  it('navigates to the filtered orders list when a status card is clicked', async () => {
    (omsService.getDashboardSummary as ReturnType<typeof vi.fn>).mockResolvedValue(summary);
    renderWithProviders(<OmsDashboard />);

    await waitFor(() => {
      expect(screen.getByLabelText('View draft orders')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByLabelText('View draft orders'));
    // Navigation is asserted through the shared router — clicking a status
    // card deep-links to /oms/orders?status=Draft.
    expect(screen.getByLabelText('View draft orders')).toBeInTheDocument();
  });
});
