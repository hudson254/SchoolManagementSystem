import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { PasswordStrengthMeter } from './PasswordStrengthMeter';

describe('PasswordStrengthMeter', () => {
  it('renders the default label', () => {
    render(<PasswordStrengthMeter score={50} level="Medium" />);
    expect(screen.getByText(/password strength/i)).toBeInTheDocument();
  });

  it('renders a custom label', () => {
    render(<PasswordStrengthMeter score={75} level="Strong" label="Strength" />);
    expect(screen.getByText('Strength')).toBeInTheDocument();
  });

  it('displays the strength level label', () => {
    render(<PasswordStrengthMeter score={95} level="Very Strong" />);
    expect(screen.getByText('Very Strong')).toBeInTheDocument();
  });

  it('renders an accessible progress bar with aria-label', () => {
    render(<PasswordStrengthMeter score={55} level="Medium" />);
    const progress = screen.getByRole('progressbar');
    expect(progress).toHaveAttribute('aria-label', 'Password strength: Medium');
  });

  it('clamps score to 0-100', () => {
    render(<PasswordStrengthMeter score={150} level="Very Strong" />);
    const progress = screen.getByRole('progressbar');
    // MUI renders value as aria-valuenow
    expect(progress).toHaveAttribute('aria-valuenow', '100');
  });
});
