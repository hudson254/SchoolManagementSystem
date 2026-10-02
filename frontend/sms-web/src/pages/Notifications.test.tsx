import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Notifications } from './Notifications';
import * as authHook from '../hooks/useAuth';
import { notificationService, type Notification } from '../services/notification.service';

vi.mock('../hooks/useAuth', () => ({ useAuth: vi.fn() }));
vi.mock('../services/notification.service', async () => {
  const actual = await vi.importActual<typeof import('../services/notification.service')>(
    '../services/notification.service'
  );
  return {
    ...actual,
    notificationService: {
      getNotifications: vi.fn(),
      markAsRead: vi.fn(),
      markAllAsRead: vi.fn(),
      deleteNotification: vi.fn(),
    },
  };
});

const service = notificationService as unknown as {
  getNotifications: ReturnType<typeof vi.fn>;
  markAsRead: ReturnType<typeof vi.fn>;
  markAllAsRead: ReturnType<typeof vi.fn>;
  deleteNotification: ReturnType<typeof vi.fn>;
};

const make = (overrides: Partial<Notification> = {}): Notification => ({
  id: 'n1',
  title: 'Accommodation allocated',
  message: 'You have been allocated House 12.',
  type: 'Accommodation',
  isRead: false,
  priority: 'Normal',
  isExpired: false,
  createdAt: '2026-01-15T10:30:00Z',
  createdDate: '2026-01-15T10:30:00Z',
  actionUrl: null,
  referenceId: null,
  expiresAt: null,
  readAt: null,
  readDate: null,
  ...overrides,
});

const paged = (items: Notification[]) => ({
  items,
  totalCount: items.length,
  page: 1,
  pageSize: 20,
  totalPages: 1,
});

const renderPage = () =>
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={['/notifications']}>
        <Routes>
          <Route path="/notifications" element={<Notifications />} />
          <Route path="/accommodation" element={<div>Accommodation Page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );

describe('Notifications page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { id: 'u1', roles: ['Student'] },
      isAuthenticated: true,
      isLoading: false,
    });
    service.markAsRead.mockResolvedValue(undefined);
    service.markAllAsRead.mockResolvedValue(undefined);
    service.deleteNotification.mockResolvedValue(undefined);
  });

  it('renders the notification title and message', async () => {
    service.getNotifications.mockResolvedValue(paged([make()]));
    renderPage();

    expect(await screen.findByText('Accommodation allocated')).toBeInTheDocument();
    expect(screen.getByText(/allocated House 12/)).toBeInTheDocument();
  });

  it('renders a real timestamp instead of "Invalid Date"', async () => {
    // The API returns createdAt while the old component read createdDate, so every
    // row rendered "Invalid Date".
    service.getNotifications.mockResolvedValue(paged([make()]));
    renderPage();

    await screen.findByText('Accommodation allocated');
    expect(screen.queryByText('Invalid Date')).toBeNull();
    expect(screen.getByText(/2026/)).toBeInTheDocument();
  });

  it('shows severity as text, not colour alone', async () => {
    service.getNotifications.mockResolvedValue(
      paged([make({ title: 'Assignment issue', type: 'AssignmentIssue', priority: 'Important' })])
    );
    renderPage();

    await screen.findByText('Assignment issue');
    expect(screen.getByText('Important')).toBeInTheDocument();
    expect(screen.getByText('New')).toBeInTheDocument();
  });

  it('marks a notification read from the row action', async () => {
    service.getNotifications.mockResolvedValue(paged([make()]));
    renderPage();

    await screen.findByText('Accommodation allocated');
    await userEvent.click(screen.getByRole('button', { name: /Mark "Accommodation allocated" as read/i }));

    expect(service.markAsRead).toHaveBeenCalledWith('n1');
  });

  it('marks every notification read from the header action', async () => {
    service.getNotifications.mockResolvedValue(paged([make()]));
    renderPage();

    await screen.findByText('Accommodation allocated');
    await userEvent.click(screen.getByRole('button', { name: /mark all read/i }));

    expect(service.markAllAsRead).toHaveBeenCalledTimes(1);
  });

  it('navigates to the action target and marks the notification read', async () => {
    service.getNotifications.mockResolvedValue(
      paged([make({ actionUrl: '/accommodation', type: 'Accommodation' })])
    );
    renderPage();

    await screen.findByText('Accommodation allocated');
    await userEvent.click(screen.getByRole('button', { name: /view accommodation/i }));

    await waitFor(() => {
      expect(service.markAsRead).toHaveBeenCalledWith('n1');
    });
    expect(await screen.findByText('Accommodation Page')).toBeInTheDocument();
  });

  it('does not render an action button when the target is unsafe', async () => {
    // A hostile actionUrl must not become a clickable navigation.
    service.getNotifications.mockResolvedValue(paged([make({ actionUrl: 'https://evil.example' })]));
    renderPage();

    await screen.findByText('Accommodation allocated');
    expect(screen.queryByRole('button', { name: /^(open|view)/i })).toBeNull();
  });

  it('requests a bounded page size', async () => {
    service.getNotifications.mockResolvedValue(paged([make()]));
    renderPage();

    await screen.findByText('Accommodation allocated');
    const requested = service.getNotifications.mock.calls[0][0];
    expect(requested.pageSize).toBeLessThanOrEqual(100);
  });

  it('shows an empty state when there are no notifications', async () => {
    service.getNotifications.mockResolvedValue(paged([]));
    renderPage();

    expect(await screen.findByText(/no notifications yet/i)).toBeInTheDocument();
  });
});