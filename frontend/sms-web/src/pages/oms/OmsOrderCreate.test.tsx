import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { SnackbarProvider } from 'notistack';
import type { ReactNode } from 'react';
import { OmsOrderCreate } from './OmsOrderCreate';
import { omsService } from '../../services/oms.service';
import * as authHook from '../../hooks/useAuth';

const mockNavigate = vi.fn();

vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

vi.mock('../../services/oms.service', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../services/oms.service')>();
  return {
    ...actual,
    omsService: {
      createOrder: vi.fn(),
    },
  };
});

vi.mock('../../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

const createdOrder = {
  id: 'order-created-1',
  orderNumber: 'ORD-2026-000777',
  title: 'New lab supplies',
  status: 'Draft',
  statusName: 'Draft',
  totalAmount: 0,
  currency: 'KES',
  itemCount: 0,
  items: [],
};

function renderWithProviders(ui: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/oms/orders/new']}>
        <SnackbarProvider>{ui}</SnackbarProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const createOrderMock = omsService.createOrder as unknown as ReturnType<typeof vi.fn>;

describe('OmsOrderCreate', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    createOrderMock.mockResolvedValue(createdOrder);
    (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { id: 'user-1', roles: ['Coordinator'] },
    });
  });

  it('blocks submission and shows validation errors when the form is empty', async () => {
    renderWithProviders(<OmsOrderCreate />);
    fireEvent.click(screen.getByRole('button', { name: /Create Order/i }));

    await waitFor(() => {
      expect(screen.getByText('Order title is required.')).toBeInTheDocument();
    });
    expect(createOrderMock).not.toHaveBeenCalled();
  });

  it('validates the currency code length', async () => {
    renderWithProviders(<OmsOrderCreate />);
    fireEvent.change(screen.getByLabelText(/Order Title/i), { target: { value: 'Supplies' } });
    fireEvent.change(screen.getByLabelText(/^Currency/i), { target: { value: 'KESX' } });
    fireEvent.click(screen.getByRole('button', { name: /Create Order/i }));

    await waitFor(() => {
      expect(screen.getByText('Currency must be a 3-letter ISO-4217 code.')).toBeInTheDocument();
    });
    expect(createOrderMock).not.toHaveBeenCalled();
  });

  it('creates the order and navigates to the server-created order', async () => {
    renderWithProviders(<OmsOrderCreate />);
    fireEvent.change(screen.getByLabelText(/Order Title/i), { target: { value: 'New lab supplies' } });
    fireEvent.click(screen.getByRole('button', { name: /Create Order/i }));

    await waitFor(() => {
      expect(createOrderMock).toHaveBeenCalledTimes(1);
    });
    expect(createOrderMock).toHaveBeenCalledWith({
      title: 'New lab supplies',
      description: undefined,
      currency: 'KES',
      requiredByDate: undefined,
      items: [],
    });
    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/oms/orders/order-created-1', { replace: true });
    });
  });

  it('submits optional initial items with the creation request', async () => {
    renderWithProviders(<OmsOrderCreate />);
    fireEvent.change(screen.getByLabelText(/Order Title/i), { target: { value: 'New lab supplies' } });
    fireEvent.click(screen.getByRole('button', { name: /Add Item Row/i }));
    const itemRow = within(screen.getByRole('group', { name: 'Item 1' }));
    fireEvent.change(itemRow.getByLabelText(/^Description/), { target: { value: 'Chalk boxes' } });
    fireEvent.change(itemRow.getByLabelText('Quantity'), { target: { value: '10' } });
    fireEvent.change(itemRow.getByLabelText('Unit Price'), { target: { value: '25' } });
    fireEvent.click(screen.getByRole('button', { name: /Create Order/i }));

    await waitFor(() => {
      expect(createOrderMock).toHaveBeenCalledWith(
        expect.objectContaining({
          items: [{ itemCode: undefined, description: 'Chalk boxes', quantity: 10, unitPrice: 25 }],
        }),
      );
    });
  });

  it('shows the server business error when creation is rejected', async () => {
    createOrderMock.mockRejectedValue({
      success: false,
      code: 'BUSINESS_RULE_VIOLATION',
      message: 'A submitted order must contain at least one item.',
      statusCode: 400,
    });
    renderWithProviders(<OmsOrderCreate />);
    fireEvent.change(screen.getByLabelText(/Order Title/i), { target: { value: 'Supplies' } });
    fireEvent.click(screen.getByRole('button', { name: /Create Order/i }));

    await waitFor(() => {
      expect(
        screen.getByText('A submitted order must contain at least one item.'),
      ).toBeInTheDocument();
    });
  });

  it('is not available to users who cannot create orders', () => {
    (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { id: 'user-2', roles: ['Lecturer'] },
    });
    renderWithProviders(<OmsOrderCreate />);
    expect(screen.getByText('Order creation unavailable')).toBeInTheDocument();
    expect(createOrderMock).not.toHaveBeenCalled();
  });
});
