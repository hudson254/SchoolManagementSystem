import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { Register } from './Register';
import * as authHook from '../hooks/useAuth';
import { apiClient } from '../services/api';

// Mock the API client
vi.mock('../services/api', () => ({
  apiClient: {
    get: vi.fn(),
  },
}));

// Mock the auth hook
vi.mock('../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

// Mock the password strength utility to isolate the gating logic
vi.mock('../utils/passwordStrength', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../utils/passwordStrength')>();
  return {
    ...actual,
    getPasswordStrength: vi.fn((pwd: string) => {
      // Deterministic: weak unless it's a "strong" test password
      if (pwd && pwd.length >= 12 && /[A-Z]/.test(pwd) && /[0-9]/.test(pwd) && /[^a-zA-Z0-9]/.test(pwd)) {
        return { score: 90, level: 'Very Strong' as const, blacklistHits: [], requirements: {
          minLength: true, hasUpper: true, hasLower: true, hasNumber: true, hasSpecial: true, min12: true,
        } };
      }
      return { score: 20, level: 'Weak' as const, blacklistHits: [], requirements: {
        minLength: false, hasUpper: false, hasLower: false, hasNumber: false, hasSpecial: false, min12: false,
      } };
    }),
    checkBreachedPassword: vi.fn().mockResolvedValue(false),
  };
});

function renderRegister() {
  return render(
    <MemoryRouter>
      <Register />
    </MemoryRouter>
  );
}

describe('Register page — password strength gating', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (authHook.useAuth as any).mockReturnValue({
      register: vi.fn().mockResolvedValue({}),
    });
    (apiClient.get as any).mockResolvedValue([]);
  });

  it('renders the role selection screen', () => {
    renderRegister();
    expect(screen.getByText('Create Account')).toBeInTheDocument();
    expect(screen.getByText('Register as Student')).toBeInTheDocument();
    expect(screen.getByText('Register as Lecturer')).toBeInTheDocument();
  });

  it('disables Next until a strong password is entered', async () => {
    renderRegister();

    // Select Student role
    fireEvent.click(screen.getAllByRole('button', { name: /register as student/i })[0]);

    // Fill personal details (step 0)
    fireEvent.change(screen.getByLabelText(/first name/i), { target: { value: 'John' } });
    fireEvent.change(screen.getByLabelText(/last name/i), { target: { value: 'Doe' } });
    fireEvent.change(screen.getByLabelText(/organization/i), { target: { value: 'Test Org' } });
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));

    // Fill contact details (step 1)
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'john@example.com' } });
    fireEvent.change(screen.getByLabelText(/phone/i), { target: { value: '+254712345678' } });
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));

    // Account details (step 2) — weak password should keep Next disabled
    const passwordInput = screen.getByLabelText('Password *');
    const confirmInput = screen.getByLabelText('Confirm Password *');
    const usernameInput = screen.getByLabelText('Username *');

    fireEvent.change(passwordInput, { target: { value: 'weak' } });
    fireEvent.change(confirmInput, { target: { value: 'weak' } });

    const nextButton = screen.getByRole('button', { name: 'Next' });
    expect(nextButton).toBeDisabled();

    // Now enter a strong password and a valid username
    fireEvent.change(usernameInput, { target: { value: 'john.doe' } });
    fireEvent.change(passwordInput, { target: { value: 'Xk9#mQ2$vL7!rT' } });
    fireEvent.change(confirmInput, { target: { value: 'Xk9#mQ2$vL7!rT' } });

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Next' })).toBeEnabled();
    });
  });

  it('shows the password requirements checklist', async () => {
    renderRegister();

    fireEvent.click(screen.getAllByRole('button', { name: /register as student/i })[0]);
    fireEvent.change(screen.getByLabelText(/first name/i), { target: { value: 'John' } });
    fireEvent.change(screen.getByLabelText(/last name/i), { target: { value: 'Doe' } });
    fireEvent.change(screen.getByLabelText(/organization/i), { target: { value: 'Test Org' } });
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'john@example.com' } });
    fireEvent.change(screen.getByLabelText(/phone/i), { target: { value: '+254712345678' } });
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));

    fireEvent.change(screen.getByLabelText('Password *'), { target: { value: 'Xk9#mQ2$vL7!rT' } });

    await waitFor(() => {
      expect(screen.getByText(/minimum 8 characters/i)).toBeInTheDocument();
      expect(screen.getByText(/uppercase letter/i)).toBeInTheDocument();
      expect(screen.getByText(/lowercase letter/i)).toBeInTheDocument();
      expect(screen.getByText(/number/i)).toBeInTheDocument();
      expect(screen.getByText(/special character/i)).toBeInTheDocument();
    });
  });

  it('keeps Next disabled when passwords do not match', async () => {
    renderRegister();

    fireEvent.click(screen.getAllByRole('button', { name: /register as student/i })[0]);
    fireEvent.change(screen.getByLabelText(/first name/i), { target: { value: 'John' } });
    fireEvent.change(screen.getByLabelText(/last name/i), { target: { value: 'Doe' } });
    fireEvent.change(screen.getByLabelText(/organization/i), { target: { value: 'Test Org' } });
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'john@example.com' } });
    fireEvent.change(screen.getByLabelText(/phone/i), { target: { value: '+254712345678' } });
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));

    fireEvent.change(screen.getByLabelText('Password *'), { target: { value: 'Xk9#mQ2$vL7!rT' } });
    fireEvent.change(screen.getByLabelText('Confirm Password *'), { target: { value: 'Different!' } });

    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
  });
});
