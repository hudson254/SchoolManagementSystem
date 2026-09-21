import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { SnackbarProvider } from 'notistack';
import type { ReactNode } from 'react';
import { OmsOrders } from './OmsOrders';
import { omsService, OmsOrder } from '../../services/oms.service';
import * as authHook from '../../hooks/useAuth';

vi.mock('../../services/oms.service', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../services/oms.service')>();
  return {
    ...actual,
    omsService: {
      getOrders: vi.fn(),
    },
  };
});

vi.mock('../../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

const buildOrder = (overrides: Partial<OmsOrder> = {}): OmsOrder => ({
  id: overrides.id ?? 'order-1',
  orderNumber: 'ORD-2026-000001',
  title: 'Exam stationery',
  description: null,
  status: 'Draft',
  statusName: 'Draft',
  requestedByUserId: 'user-1',
  approvedByUserId: null,
  approvedAtUtc: null,
  approvalRemarks: null,
  rejectedByUserId: null,
  rejectedAtUtc: null,
  rejectionRemarks: null,
  cancelledByUserId: null,
  cancelledAtUtc: null,
  cancellationReason: null,
  submittedAtUtc: null,
  requiredByDate: null,
  roadAccountId: null,
  totalAmount: 1500,
  currency: 'KES',
  createdAt: '2026-01-15T08:00:00Z',
  updatedAt: '2026-01-15T08:00:00Z',
  itemCount: 2,
  items: [],
  ...overrides,
});

const page1 = {
  items: [buildOrder()],
  totalCount: 25,
  pageNumber: 1,
  pageSize: 10,
  totalPages: 3,
  hasPreviousPage: false,
  hasNextPage: true,
};

function renderWithProviders(ui: ReactNode, initialEntries: string[] = ['/oms/orders']) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={initialEntries}>
        <SnackbarProvider>{ui}</SnackbarProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const getOrdersMock = omsService.getOrders as unknown as ReturnType<typeof vi.fn>;

describe('OmsOrders', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getOrdersMock.mockResolvedValue(page1);
    (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { id: 'user-1', roles: ['Coordinator'] },
    });
  });

  it('renders orders returned by the API', async () => {
    renderWithProviders(<OmsOrders />);

    await waitFor(() => {
      expect(screen.getByText('ORD-2026-000001')).toBeInTheDocument();
    });
    expect(screen.getByText('Exam stationery')).toBeInTheDocument();
    expect(screen.getByText('Draft')).toBeInTheDocument();
    expect(screen.getByText(/1,500\.00/)).toBeInTheDocument();
  });

  it('shows the empty state when there are no orders', async () => {
    getOrdersMock.mockResolvedValue({ ...page1, items: [], totalCount: 0, totalPages: 0 });
    renderWithProviders(<OmsOrders />);

    await waitFor(() => {
      expect(screen.getByText('No orders yet. Create the first order to get started.')).toBeInTheDocument();
    });
  });

  it('shows the filtered empty state when filters match nothing', async () => {
    getOrdersMock.mockResolvedValue({ ...page1, items: [], totalCount: 0, totalPages: 0 });
    renderWithProviders(<OmsOrders />);

    await waitFor(() => {
      expect(screen.getByLabelText('Search orders')).toBeInTheDocument();
    });
    fireEvent.change(screen.getByLabelText('Search orders'), { target: { value: 'nothing' } });
    fireEvent.click(screen.getByRole('button', { name: 'Search' }));

    await waitFor(() => {
      expect(screen.getByText('No orders match the current filters.')).toBeInTheDocument();
    });
  });

  it('shows an error state when the API fails', async () => {
    getOrdersMock.mockRejectedValue({
      success: false,
      code: 'INTERNAL_ERROR',
      message: 'boom',
    });
    renderWithProviders(<OmsOrders />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
    expect(screen.getByText('Retry')).toBeInTheDocument();
  });

  it('passes the deep-linked status filter to the API', async () => {
    renderWithProviders(<OmsOrders />, ['/oms/orders?status=Draft']);

    await waitFor(() => {
      expect(getOrdersMock).toHaveBeenCalledWith(
        expect.objectContaining({ status: 'Draft', pageNumber: 1, pageSize: 10 }),
      );
    });
  });

  it('requests the next page when paginating', async () => {
    renderWithProviders(<OmsOrders />);

    await waitFor(() => {
      expect(screen.getByText('ORD-2026-000001')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole('button', { name: 'Go to next page' }));

    await waitFor(() => {
      expect(getOrdersMock).toHaveBeenCalledWith(
        expect.objectContaining({ pageNumber: 2, pageSize: 10 }),
      );
    });
  });

  it('passes the search term to the API', async () => {
    renderWithProviders(<OmsOrders />);

    await waitFor(() => {
      expect(screen.getByText('ORD-2026-000001')).toBeInTheDocument();
    });
    fireEvent.change(screen.getByLabelText('Search orders'), { target: { value: 'stationery' } });
    fireEvent.click(screen.getByRole('button', { name: 'Search' }));

    await waitFor(() => {
      expect(getOrdersMock).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'stationery', pageNumber: 1 }),
      );
    });
  });

  it('scopes results to the current user when Only my orders is checked', async () => {
    renderWithProviders(<OmsOrders />);

    await waitFor(() => {
      expect(screen.getByText('ORD-2026-000001')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByLabelText('Only my orders'));

    await waitFor(() => {
      expect(getOrdersMock).toHaveBeenCalledWith(
        expect.objectContaining({ requestedByUserId: 'user-1' }),
      );
    });
  });

  it('shows the New Order button for users who can create orders', async () => {
    renderWithProviders(<OmsOrders />);
    await waitFor(() => {
      expect(screen.getByText('ORD-2026-000001')).toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: /New Order/i })).toBeInTheDocument();
  });

  it('hides the New Order button for view-only users', async () => {
    (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { id: 'user-2', roles: ['Lecturer'] },
    });
    renderWithProviders(<OmsOrders />);
    await waitFor(() => {
      expect(screen.getByText('ORD-2026-000001')).toBeInTheDocument();
    });
    expect(screen.queryByRole('button', { name: /New Order/i })).not.toBeInTheDocument();
  });

  it('shows a permission message for users without OMS view permission', () => {
    (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { id: 'user-2', roles: ['Student'] },
    });
    renderWithProviders(<OmsOrders />);
    expect(screen.getByText('Orders unavailable')).toBeInTheDocument();
    expect(getOrdersMock).not.toHaveBeenCalled();
  });

  it('surfaces the session-expired message on 401', async () => {
    getOrdersMock.mockRejectedValue({
      success: false,
      code: 'SESSION_EXPIRED',
      message: 'Unauthorized',
      statusCode: 401,
    });
    renderWithProviders(<OmsOrders />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
    expect(screen.getByText(/session has expired/i)).toBeInTheDocument();
  });

  it('surfaces the access-denied message on 403', async () => {
    getOrdersMock.mockRejectedValue({
      success: false,
      code: 'FORBIDDEN',
      message: 'Access denied.',
      statusCode: 403,
    });
    renderWithProviders(<OmsOrders />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
    expect(screen.getByText(/do not have permission/i)).toBeInTheDocument();
  });

  it('shows an error state when the API returns an unusable payload', async () => {
    // omsService.getOrders rejects on structurally invalid responses.
    getOrdersMock.mockRejectedValue(new Error('Unexpected response shape from the orders API.'));
    renderWithProviders(<OmsOrders />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
    // Internal error text is never surfaced verbatim to the user.
    expect(screen.queryByText(/Unexpected response shape/i)).not.toBeInTheDocument();
  });

});
