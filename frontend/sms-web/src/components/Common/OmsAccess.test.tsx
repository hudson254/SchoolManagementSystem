import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { Sidebar } from '../Layout/Sidebar';
import { OMS_VIEW_ROLES, OMS_MANAGE_ROLES } from '../../utils/roles';
import * as authHook from '../../hooks/useAuth';

vi.mock('../../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

type AuthUser = {
  id: string;
  firstName?: string;
  lastName?: string;
  roles: string[];
  permissions?: string[];
};

const mockAuth = (
  user: Partial<AuthUser> | null,
  overrides: Partial<{ isAuthenticated: boolean; isLoading: boolean }> = {},
) => {
  (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
    user,
    isAuthenticated: overrides.isAuthenticated ?? !!user,
    isLoading: overrides.isLoading ?? false,
    logout: vi.fn(),
  });
};

const renderGuardedRoute = (roles: string[], path = '/oms/orders/new') =>
  render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/login" element={<div>Login Page</div>} />
        <Route path="/dashboard" element={<div>Dashboard Page</div>} />
        <Route
          path={path}
          element={
            <ProtectedRoute roles={roles}>
              <div>OMS Protected Page</div>
            </ProtectedRoute>
          }
        />
      </Routes>
    </MemoryRouter>,
  );

const renderSidebar = () =>
  render(
    <MemoryRouter initialEntries={['/oms/orders']}>
      <Sidebar />
    </MemoryRouter>,
  );

describe('OMS route guards', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('allows a Coordinator onto the OMS order-creation route', () => {
    mockAuth({ id: 'user-1', roles: ['Coordinator'] });
    renderGuardedRoute(OMS_MANAGE_ROLES);

    expect(screen.getByText('OMS Protected Page')).toBeInTheDocument();
  });

  it('allows a Lecturer (view tier) onto the view-only OMS route', () => {
    mockAuth({ id: 'user-2', roles: ['Lecturer'] });
    renderGuardedRoute(OMS_VIEW_ROLES, '/oms/orders');

    expect(screen.getByText('OMS Protected Page')).toBeInTheDocument();
  });

  it('denies a Lecturer the OMS management route and redirects away', () => {
    mockAuth({ id: 'user-2', roles: ['Lecturer'] });
    renderGuardedRoute(OMS_MANAGE_ROLES);

    expect(screen.queryByText('OMS Protected Page')).not.toBeInTheDocument();
    expect(screen.getByText('Dashboard Page')).toBeInTheDocument();
  });

  it('denies a user without any OMS permission the view route', () => {
    mockAuth({ id: 'user-3', roles: ['Student', 'Receptionist'] });
    renderGuardedRoute(OMS_VIEW_ROLES, '/oms/orders');

    expect(screen.queryByText('OMS Protected Page')).not.toBeInTheDocument();
    expect(screen.getByText('Dashboard Page')).toBeInTheDocument();
  });

  it('redirects unauthenticated visitors to login', () => {
    mockAuth(null, { isAuthenticated: false });
    renderGuardedRoute(OMS_VIEW_ROLES, '/oms/orders');

    expect(screen.getByText('Login Page')).toBeInTheDocument();
    expect(screen.queryByText('OMS Protected Page')).not.toBeInTheDocument();
  });

  it('shows a loading state while authentication is unresolved', () => {
    mockAuth(null, { isAuthenticated: false, isLoading: true });
    renderGuardedRoute(OMS_VIEW_ROLES, '/oms/orders');

    expect(screen.getByText('Loading...')).toBeInTheDocument();
    expect(screen.queryByText('OMS Protected Page')).not.toBeInTheDocument();
  });
});

describe('OMS navigation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows the Order Management group for OMS view roles', () => {
    mockAuth({ id: 'user-1', firstName: 'Ada', lastName: 'Lovelace', roles: ['Coordinator'] });
    renderSidebar();

    expect(screen.getByText('Order Management')).toBeInTheDocument();
  });

  it('hides the Order Management group from users without OMS permissions', () => {
    mockAuth({ id: 'user-3', firstName: 'Sam', lastName: 'Student', roles: ['Student'] });
    renderSidebar();

    expect(screen.queryByText('Order Management')).not.toBeInTheDocument();
    expect(screen.queryByText('OMS Dashboard')).not.toBeInTheDocument();
  });

  it('exposes dashboard, orders and new-order links to a Coordinator', () => {
    mockAuth({ id: 'user-1', firstName: 'Ada', lastName: 'Lovelace', roles: ['Coordinator'] });
    renderSidebar();

    fireEvent.click(screen.getByText('Order Management'));

    expect(screen.getByText('OMS Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Orders')).toBeInTheDocument();
    expect(screen.getByText('New Order')).toBeInTheDocument();
  });

  it('hides the New Order link from view-only Lecturers', () => {
    mockAuth({ id: 'user-2', firstName: 'Lee', lastName: 'Turer', roles: ['Lecturer'] });
    renderSidebar();

    fireEvent.click(screen.getByText('Order Management'));

    expect(screen.getByText('OMS Dashboard')).toBeInTheDocument();
    expect(screen.queryByText('New Order')).not.toBeInTheDocument();
  });
});

