import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { PasswordRequirementsChecklist } from './PasswordRequirementsChecklist';

describe('PasswordRequirementsChecklist', () => {
  beforeEach(() => {
    // Default: disable HIBP network check in tests for determinism.
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false }));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('renders all requirement items', () => {
    render(<PasswordRequirementsChecklist password="" enableBreachCheck={false} />);
    expect(screen.getByText(/minimum 8 characters/i)).toBeInTheDocument();
    expect(screen.getByText(/uppercase letter/i)).toBeInTheDocument();
    expect(screen.getByText(/lowercase letter/i)).toBeInTheDocument();
    expect(screen.getByText(/number/i)).toBeInTheDocument();
    expect(screen.getByText(/special character/i)).toBeInTheDocument();
  });

  it('shows 12+ character recommendation by default', () => {
    render(<PasswordRequirementsChecklist password="" enableBreachCheck={false} />);
    expect(screen.getByText(/12 or more characters/i)).toBeInTheDocument();
  });

  it('hides recommendation when showRecommendation=false', () => {
    render(
      <PasswordRequirementsChecklist
        password=""
        showRecommendation={false}
        enableBreachCheck={false}
      />
    );
    expect(screen.queryByText(/12 or more characters/i)).not.toBeInTheDocument();
  });

  it('marks all requirements met for a strong password', () => {
    render(
      <PasswordRequirementsChecklist
        password="Xk9#mQ2$vL7!rT"
        enableBreachCheck={false}
      />
    );
    // All items should be met (green check circles).
    const checkIcons = screen.getAllByTestId('CheckCircleIcon');
    expect(checkIcons.length).toBeGreaterThanOrEqual(5);
  });

  it('shows blacklist warning for a common password', () => {
    render(
      <PasswordRequirementsChecklist
        password="Password123!"
        enableBreachCheck={false}
      />
    );
    expect(screen.getByText(/too common/i)).toBeInTheDocument();
  });

  it('shows personal-information warning when context matches', () => {
    render(
      <PasswordRequirementsChecklist
        password="JohnDoe123!"
        context={{ firstName: 'John', lastName: 'Doe' }}
        enableBreachCheck={false}
      />
    );
    expect(screen.getByText(/personal information/i)).toBeInTheDocument();
  });
});
