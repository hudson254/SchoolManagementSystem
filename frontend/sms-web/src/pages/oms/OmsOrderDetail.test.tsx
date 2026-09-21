import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { SnackbarProvider } from 'notistack';
import type { ReactNode } from 'react';
import { OmsOrderDetail } from './OmsOrderDetail';
import { omsService, OmsOrder } from '../../services/oms.service';
import * as authHook from '../../hooks/useAuth';

vi.mock('../../services/oms.service', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../services/oms.service')>();
  return {
    ...actual,
    omsService: {
      getOrder: vi.fn(),
      addOrderItem: vi.fn(),
      removeOrderItem: vi.fn(),
      submitOrder: vi.fn(),
      cancelOwnOrder: vi.fn(),
      cancelAnyOrder: vi.fn(),
    },
  };
});

vi.mock('../../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

const buildOrder = (overrides: Partial<OmsOrder> = {}): OmsOrder => ({
  id: 'order-1',
  orderNumber: 'ORD-2026-000001',
  title: 'Exam stationery',
  description: 'Supplies for exams',
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
  totalAmount: 250,
  currency: 'KES',
  createdAt: '2026-01-15T08:00:00Z',
  updatedAt: '2026-01-15T08:00:00Z',
  itemCount: 1,
  items: [
    {
      id: 'item-1',
      orderId: 'order-1',
      rowNumber: 1,
      itemCode: 'ITM-001',
      description: 'Chalk boxes',
      quantity: 10,
      unitPrice: 25,
      lineTotal: 250,
    },
  ],
  ...overrides,
});

const getOrderMock = omsService.getOrder as unknown as ReturnType<typeof vi.fn>;
const addItemMock = omsService.addOrderItem as unknown as ReturnType<typeof vi.fn>;
const removeItemMock = omsService.removeOrderItem as unknown as ReturnType<typeof vi.fn>;
const submitMock = omsService.submitOrder as unknown as ReturnType<typeof vi.fn>;
const cancelOwnMock = omsService.cancelOwnOrder as unknown as ReturnType<typeof vi.fn>;
const cancelAnyMock = omsService.cancelAnyOrder as unknown as ReturnType<typeof vi.fn>;

function renderWithProviders(
  ui: ReactNode,
  options: {
    roles?: string[];
    userId?: string;
    order?: OmsOrder;
    orderError?: unknown;
  } = {},
) {
  const { roles = ['Coordinator'], userId = 'user-1', order, orderError } = options;
  (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
    user: { id: userId, roles },
  });
  if (orderError !== undefined) {
    getOrderMock.mockRejectedValue(orderError);
  } else {
    getOrderMock.mockResolvedValue(order ?? buildOrder());
  }
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/oms/orders/order-1']}>
        <SnackbarProvider>
          <Routes>
            <Route path="/oms/orders/:id" element={ui} />
          </Routes>
        </SnackbarProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('OmsOrderDetail — states', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the server-issued order number, status and items', async () => {
    renderWithProviders(<OmsOrderDetail />);
    await waitFor(() => {
      expect(screen.getByTestId('order-number')).toHaveTextContent('ORD-2026-000001');
    });
    expect(screen.getByTestId('order-status')).toHaveTextContent('Draft');
    const itemsTable = screen.getByRole('table', { name: 'order items table' });
    expect(within(itemsTable).getByText('Chalk boxes')).toBeInTheDocument();
    expect(within(itemsTable).getByText(/250\.00/)).toBeInTheDocument();
  });

  it('renders lifecycle information when present', async () => {
    renderWithProviders(<OmsOrderDetail />, {
      roles: ['Coordinator'],
      order: buildOrder({
        status: 'Cancelled',
        statusName: 'Cancelled',
        cancelledAtUtc: '2026-02-01T10:00:00Z',
        cancelledByUserId: 'admin-1',
        cancellationReason: 'No longer needed',
      }),
    });
    await waitFor(() => {
      expect(screen.getByTestId('order-status')).toHaveTextContent('Cancelled');
    });
    expect(screen.getByText('Cancellation Reason')).toBeInTheDocument();
    expect(screen.getByText('No longer needed')).toBeInTheDocument();
  });

  it('shows no editing actions for view-only users (Lecturer)', async () => {
    renderWithProviders(<OmsOrderDetail />, { roles: ['Lecturer'] });
    await waitFor(() => {
      expect(screen.getByTestId('order-number')).toHaveTextContent('ORD-2026-000001');
    });
    expect(screen.queryByRole('button', { name: /Add Item/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Submit$/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Cancel Order/i })).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/Remove item/i)).not.toBeInTheDocument();
  });

  it('shows Draft actions for the coordinator who created the order', async () => {
    renderWithProviders(<OmsOrderDetail />, { roles: ['Coordinator'], userId: 'user-1' });
    await waitFor(() => {
      expect(screen.getByTestId('order-number')).toHaveTextContent('ORD-2026-000001');
    });
    expect(screen.getByRole('button', { name: /Add Item/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^Submit$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Cancel Order/i })).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Cancel \(Administrator\)/i }),
    ).not.toBeInTheDocument();
  });

  it('hides item editing once the order is no longer a Draft', async () => {
    renderWithProviders(<OmsOrderDetail />, {
      roles: ['Coordinator'],
      order: buildOrder({
        status: 'Submitted',
        statusName: 'Submitted',
        submittedAtUtc: '2026-01-16T09:00:00Z',
      }),
    });
    await waitFor(() => {
      expect(screen.getByTestId('order-status')).toHaveTextContent('Submitted');
    });
    expect(screen.queryByRole('button', { name: /Add Item/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Submit$/i })).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/Remove item/i)).not.toBeInTheDocument();
  });

  it('shows no actions for terminal states even for administrators', async () => {
    renderWithProviders(<OmsOrderDetail />, {
      roles: ['Administrator'],
      order: buildOrder({ status: 'Approved', statusName: 'Approved' }),
    });
    await waitFor(() => {
      expect(screen.getByTestId('order-status')).toHaveTextContent('Approved');
    });
    expect(screen.queryByRole('button', { name: /Add Item/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Cancel Order/i })).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Cancel \(Administrator\)/i }),
    ).not.toBeInTheDocument();
  });

  it('shows the administrator-only cancel-any action for another user’s order', async () => {
    renderWithProviders(<OmsOrderDetail />, {
      roles: ['Administrator'],
      userId: 'admin-1',
      order: buildOrder({ requestedByUserId: 'user-9' }),
    });
    await waitFor(() => {
      expect(screen.getByTestId('order-number')).toHaveTextContent('ORD-2026-000001');
    });
    expect(screen.getByRole('button', { name: /Cancel \(Administrator\)/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Cancel Order/i })).not.toBeInTheDocument();
  });

  it('shows a not-found state on 404', async () => {
    renderWithProviders(<OmsOrderDetail />, {
      orderError: { success: false, code: 'NOT_FOUND', message: 'Order not found.', statusCode: 404 },
    });
    await waitFor(() => {
      expect(screen.getByText('Order not found')).toBeInTheDocument();
    });
  });

  it('shows an access-denied state on 403', async () => {
    renderWithProviders(<OmsOrderDetail />, {
      orderError: { success: false, code: 'FORBIDDEN', message: 'Access denied.', statusCode: 403 },
    });
    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
    expect(screen.getByText(/permission/i)).toBeInTheDocument();
  });
});

describe('OmsOrderDetail — workflow', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('adds an item through the Draft-only dialog', async () => {
    addItemMock.mockResolvedValue({ id: 'item-2' });
    renderWithProviders(<OmsOrderDetail />);
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Add Item/i })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole('button', { name: /Add Item/i }));

    fireEvent.change(screen.getByLabelText('Item Code (optional)'), { target: { value: 'ITM-002' } });
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Markers' } });
    fireEvent.change(screen.getByLabelText(/^Quantity/), { target: { value: '4' } });
    fireEvent.change(screen.getByLabelText(/^Unit Price/), { target: { value: '50' } });
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: /Add Item/i }));

    await waitFor(() => {
      expect(addItemMock).toHaveBeenCalledWith('order-1', {
        itemCode: 'ITM-002',
        description: 'Markers',
        quantity: 4,
        unitPrice: 50,
      });
    });
  });

  it('shows the server validation error in the add-item dialog', async () => {
    addItemMock.mockRejectedValue({
      success: false,
      code: 'VALIDATION_ERROR',
      message: 'Item description is required.',
      statusCode: 400,
    });
    renderWithProviders(<OmsOrderDetail />);
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Add Item/i })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole('button', { name: /Add Item/i }));
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Markers' } });
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: /Add Item/i }));

    await waitFor(() => {
      expect(screen.getByText('Item description is required.')).toBeInTheDocument();
    });
  });

  it('removes an item after confirmation', async () => {
    removeItemMock.mockResolvedValue({ id: 'item-1' });
    renderWithProviders(<OmsOrderDetail />);
    await waitFor(() => {
      expect(screen.getByLabelText('Remove item Chalk boxes')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByLabelText('Remove item Chalk boxes'));

    const dialog = within(screen.getByRole('dialog'));
    expect(dialog.getByText(/Remove “Chalk boxes” from this order/i)).toBeInTheDocument();
    fireEvent.click(dialog.getByRole('button', { name: 'Remove' }));

    await waitFor(() => {
      expect(removeItemMock).toHaveBeenCalledWith('order-1', 'item-1');
    });
  });

  it('submits the order after confirmation', async () => {
    submitMock.mockResolvedValue(buildOrder({ status: 'Submitted', statusName: 'Submitted' }));
    renderWithProviders(<OmsOrderDetail />);
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /^Submit$/i })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole('button', { name: /^Submit$/i }));

    const dialog = within(screen.getByRole('dialog'));
    expect(dialog.getByText('Submit Order for Approval')).toBeInTheDocument();
    fireEvent.click(dialog.getByRole('button', { name: /Submit Order/i }));

    await waitFor(() => {
      expect(submitMock).toHaveBeenCalledWith('order-1');
    });
  });

  it('surfaces the server business error when submission is rejected', async () => {
    submitMock.mockRejectedValue({
      success: false,
      code: 'BUSINESS_RULE_VIOLATION',
      message: 'Only Draft orders can be submitted. Current status: Submitted.',
      statusCode: 400,
    });
    renderWithProviders(<OmsOrderDetail />);
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /^Submit$/i })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole('button', { name: /^Submit$/i }));
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: /Submit Order/i }));

    await waitFor(() => {
      expect(
        screen.getByText('Only Draft orders can be submitted. Current status: Submitted.'),
      ).toBeInTheDocument();
    });
  });

  it('requires a reason before creator cancellation', async () => {
    cancelOwnMock.mockResolvedValue(buildOrder({ status: 'Cancelled', statusName: 'Cancelled' }));
    renderWithProviders(<OmsOrderDetail />);
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Cancel Order/i })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole('button', { name: /Cancel Order/i }));

    const dialog = within(screen.getByRole('dialog'));
    // Confirm stays disabled until a reason is provided.
    expect(dialog.getByRole('button', { name: /Cancel Order/i })).toBeDisabled();
    fireEvent.change(screen.getByLabelText(/Cancellation reason/i), {
      target: { value: 'Duplicate request' },
    });
    fireEvent.click(dialog.getByRole('button', { name: /Cancel Order/i }));

    await waitFor(() => {
      expect(cancelOwnMock).toHaveBeenCalledWith('order-1', 'Duplicate request');
    });
    expect(cancelAnyMock).not.toHaveBeenCalled();
  });

  it('routes administrator cancellation through cancel-any with a reason', async () => {
    cancelAnyMock.mockResolvedValue(buildOrder({ status: 'Cancelled', statusName: 'Cancelled' }));
    renderWithProviders(<OmsOrderDetail />, {
      roles: ['Administrator'],
      userId: 'admin-1',
      order: buildOrder({ requestedByUserId: 'user-9' }),
    });
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Cancel \(Administrator\)/i })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole('button', { name: /Cancel \(Administrator\)/i }));

    const dialog = within(screen.getByRole('dialog'));
    fireEvent.change(screen.getByLabelText(/Cancellation reason/i), {
      target: { value: 'Policy violation' },
    });
    fireEvent.click(dialog.getByRole('button', { name: /Cancel Order/i }));

    await waitFor(() => {
      expect(cancelAnyMock).toHaveBeenCalledWith('order-1', 'Policy violation');
    });
    expect(cancelOwnMock).not.toHaveBeenCalled();
  });

  it('shows a server authorization error when cancellation is no longer permitted', async () => {
    cancelOwnMock.mockRejectedValue({
      success: false,
      code: 'FORBIDDEN',
      message: 'Access denied. You do not have permission to perform this action.',
      statusCode: 403,
    });
    renderWithProviders(<OmsOrderDetail />);
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Cancel Order/i })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole('button', { name: /Cancel Order/i }));

    const dialog = within(screen.getByRole('dialog'));
    fireEvent.change(screen.getByLabelText(/Cancellation reason/i), {
      target: { value: 'Trying again' },
    });
    fireEvent.click(dialog.getByRole('button', { name: /Cancel Order/i }));

    await waitFor(() => {
      expect(within(screen.getByRole('dialog')).getByText(/do not have permission/i)).toBeInTheDocument();
    });
  });

  it('renders the OMS detail view inside the application shell', async () => {
    renderWithProviders(<OmsOrderDetail />);
    await waitFor(() => {
      expect(screen.getByTestId('order-number')).toHaveTextContent('ORD-2026-000001');
    });
    expect(screen.getByRole('button', { name: /Cancel Order/i })).toBeInTheDocument();
    // The title is rendered in both the header and the Order Information card.
    expect(screen.getAllByText(/exam stationery/i).length).toBeGreaterThan(0);
  });
});