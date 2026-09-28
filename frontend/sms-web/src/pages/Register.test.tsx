import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { Register } from './Register';
import * as authHook from '../hooks/useAuth';
import { apiClient } from '../services/api';
import { registrationService } from '../services/registration.service';

// Mock the API client
vi.mock('../services/api', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

// The registration wizard loads courses and course units through this service.
// It must be mocked too, otherwise the real one calls the unmocked apiClient.
vi.mock('../services/registration.service', () => ({
  registrationService: {
    getActiveCourses: vi.fn(),
    getCourseUnits: vi.fn(),
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

const COURSE = {
  id: 'c1',
  name: 'Diploma in Wildlife Management',
  code: 'WLM',
  credits: 120,
  duration: 24,
};

const UNITS = [
  { id: 'u1', code: 'WLM101', name: 'Wildlife Ecology', credits: 4, semester: 1 },
  { id: 'u2', code: 'WLM102', name: 'Conservation Biology', credits: 4, semester: 1 },
  { id: 'u3', code: 'WLM103', name: 'Protected Area Management', credits: 4, semester: 2 },
];

/**
 * Walks the wizard from the role screen to the given step, filling the required
 * fields and selecting the course so the unit-loading effect runs.
 */
async function advanceToStep(stepLabel: RegExp | string, role: 'Student' | 'Lecturer') {
  fireEvent.click(
    screen.getAllByRole('button', {
      name: role === 'Student' ? /register as student/i : /register as lecturer/i,
    })[0]
  );

  fireEvent.change(screen.getByLabelText(/first name/i), { target: { value: 'Test' } });
  fireEvent.change(screen.getByLabelText(/last name/i), { target: { value: 'User' } });
  fireEvent.change(screen.getByLabelText(/organization/i), { target: { value: 'Test Org' } });
  fireEvent.click(screen.getByRole('button', { name: 'Next' }));

  fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 't@example.com' } });
  fireEvent.change(screen.getByLabelText(/phone/i), { target: { value: '+254712345678' } });
  fireEvent.click(screen.getByRole('button', { name: 'Next' }));

  if (role === 'Lecturer') {
    // Professional Info step
    fireEvent.change(screen.getByLabelText(/specialization/i), {
      target: { value: 'Wildlife Conservation' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
  }

  // Account Details step
  fireEvent.change(screen.getByLabelText('Password *'), { target: { value: 'Xk9#mQ2$vL7!rT' } });
  fireEvent.change(screen.getByLabelText('Confirm Password *'), {
    target: { value: 'Xk9#mQ2$vL7!rT' },
  });
  fireEvent.change(screen.getByLabelText('Username *'), { target: { value: 'testuser' } });

  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'Next' })).toBeEnabled();
  });
  fireEvent.click(screen.getByRole('button', { name: 'Next' }));

  // Course step: choose the course from the Autocomplete.
  const courseInput = await screen.findByLabelText(/search for a course/i);
  fireEvent.change(courseInput, { target: { value: COURSE.name } });
  fireEvent.click(await screen.findByText(COURSE.name));

  await waitFor(() => {
    expect(stepLabel).toBeTruthy();
  });
}

describe('Register page â€” password strength gating', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (authHook.useAuth as any).mockReturnValue({
      register: vi.fn().mockResolvedValue({}),
    });
    (apiClient.get as any).mockResolvedValue([]);
    (registrationService.getActiveCourses as any).mockResolvedValue([]);
    (registrationService.getCourseUnits as any).mockResolvedValue([]);
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

    // Account details (step 2) â€” weak password should keep Next disabled
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

describe('Register page â€” course and unit verification', () => {
  const mockRegister = vi.fn().mockResolvedValue({});
  // Each test drives the whole wizard (up to 6 steps) and waits on real async
  // effects, so the default 5s budget is too tight on a loaded machine.
  const TEST_TIMEOUT = 30000;


  beforeEach(() => {
    vi.clearAllMocks();
    (authHook.useAuth as any).mockReturnValue({ register: mockRegister });
    (apiClient.get as any).mockResolvedValue([]);
    (registrationService.getActiveCourses as any).mockResolvedValue([COURSE]);
    (registrationService.getCourseUnits as any).mockResolvedValue(UNITS);
  });

  it('student: loads the course units when a course is selected', async () => {
    renderRegister();
    await advanceToStep('Review & Submit', 'Student');

    await waitFor(() => {
      expect(registrationService.getCourseUnits).toHaveBeenCalledWith(COURSE.id);
    });
  }, TEST_TIMEOUT);

  it('student: verification step shows the course code, name and every unit', async () => {
    renderRegister();
    await advanceToStep('Review & Submit', 'Student');

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /skip to review/i })).toBeEnabled();
    });
    fireEvent.click(screen.getByRole('button', { name: /skip to review/i }));

    expect(await screen.findByText(COURSE.name)).toBeInTheDocument();
    // Use an exact match for the code: the unit codes (WLM101...) also contain
    // the course code, so a loose regex would match several elements.
    expect(
      screen.getByText(new RegExp(`Course Code:\\s*${COURSE.code}\\b`))
    ).toBeInTheDocument();

    // Every unit of the course is listed - this is the assertion that failed
    // before the repair, when the review step showed no units at all.
    for (const unit of UNITS) {
      expect(await screen.findByText(new RegExp(unit.code))).toBeInTheDocument();
      expect(screen.getByText(new RegExp(unit.name))).toBeInTheDocument();
    }
  });

  it('student: submits the full verified unit set', async () => {
    renderRegister();
    await advanceToStep('Review & Submit', 'Student');

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /skip to review/i })).toBeEnabled();
    });
    fireEvent.click(screen.getByRole('button', { name: /skip to review/i }));

    const submit = await screen.findByRole('button', { name: /complete registration/i });
    await waitFor(() => expect(submit).toBeEnabled());
    fireEvent.click(submit);

    await waitFor(() => {
      expect(mockRegister).toHaveBeenCalledWith(
        expect.objectContaining({
          role: 'Student',
          courseId: COURSE.id,
          unitIds: UNITS.map((u) => u.id),
        })
      );
    });
  }, TEST_TIMEOUT);

  it('student: can go back and change the course selection', async () => {
    renderRegister();
    await advanceToStep('Review & Submit', 'Student');

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /skip to review/i })).toBeEnabled();
    });
    fireEvent.click(screen.getByRole('button', { name: /skip to review/i }));
    await screen.findByText(COURSE.name);

    fireEvent.click(screen.getByRole('button', { name: /^back$/i }));

    // Going back must NOT destroy the course selection.
    await waitFor(() => {
      expect(
        (screen.getByLabelText(/search for a course/i) as HTMLInputElement).value
      ).toContain(COURSE.name);
    });
  }, TEST_TIMEOUT);

  it('lecturer: shows all course units and selects them all', async () => {
    renderRegister();
    await advanceToStep('Review & Submit', 'Lecturer');

    const selectAll = await screen.findByLabelText('Select all units');
    expect(selectAll).not.toBeChecked();
    fireEvent.click(selectAll);

    await waitFor(() => {
      for (const unit of UNITS) {
        expect(screen.getByLabelText(`Select unit ${unit.code}`)).toBeChecked();
      }
    });
  }, TEST_TIMEOUT);

  it('lecturer: can select individual units', async () => {
    renderRegister();
    await advanceToStep('Review & Submit', 'Lecturer');

    fireEvent.click(await screen.findByLabelText(`Select unit ${UNITS[0].code}`));

    await waitFor(() => {
      expect(screen.getByLabelText(`Select unit ${UNITS[0].code}`)).toBeChecked();
      expect(screen.getByLabelText(`Select unit ${UNITS[1].code}`)).not.toBeChecked();
    });
  }, TEST_TIMEOUT);

  it('lecturer: verification shows only the selected units and submits exactly those', async () => {
    renderRegister();
    await advanceToStep('Review & Submit', 'Lecturer');

    fireEvent.click(await screen.findByLabelText(`Select unit ${UNITS[0].code}`));
    fireEvent.click(await screen.findByLabelText(`Select unit ${UNITS[2].code}`));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /skip to review/i })).toBeEnabled();
    });
    fireEvent.click(screen.getByRole('button', { name: /skip to review/i }));

    expect(await screen.findByText(new RegExp(UNITS[0].code))).toBeInTheDocument();
    expect(screen.getByText(new RegExp(UNITS[2].code))).toBeInTheDocument();
    // The unselected unit must not appear on the review page.
    expect(screen.queryByText(new RegExp(UNITS[1].code))).not.toBeInTheDocument();

    const submit = await screen.findByRole('button', { name: /complete registration/i });
    await waitFor(() => expect(submit).toBeEnabled());
    fireEvent.click(submit);

    await waitFor(() => {
      expect(mockRegister).toHaveBeenCalledWith(
        expect.objectContaining({
          role: 'Lecturer',
          courseId: COURSE.id,
          unitIds: [UNITS[0].id, UNITS[2].id],
        })
      );
    });
  }, TEST_TIMEOUT);

  it('lecturer: cannot continue while no unit is selected', async () => {
    renderRegister();
    await advanceToStep('Review & Submit', 'Lecturer');

    await screen.findByLabelText('Select all units');

    expect(screen.getByRole('button', { name: /skip to review/i })).toBeDisabled();
  }, TEST_TIMEOUT);

  it('surfaces an error when the course units cannot be loaded', async () => {
    (registrationService.getCourseUnits as any).mockRejectedValue(
      new Error('Network unavailable')
    );

    renderRegister();
    await advanceToStep('Review & Submit', 'Student');

    expect(await screen.findByText(/Network unavailable/i)).toBeInTheDocument();
    // It must not silently proceed as if the course simply had no units.
    expect(screen.getByRole('button', { name: /skip to review/i })).toBeDisabled();
  }, TEST_TIMEOUT);

  it('warns when the selected course has no units', async () => {
    (registrationService.getCourseUnits as any).mockResolvedValue([]);

    renderRegister();
    await advanceToStep('Review & Submit', 'Student');

    expect(await screen.findByText(/no active units/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /skip to review/i })).toBeDisabled();
  }, TEST_TIMEOUT);
});
