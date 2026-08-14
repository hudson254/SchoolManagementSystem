import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ConfirmPasswordField } from './ConfirmPasswordField';

describe('ConfirmPasswordField', () => {
  it('renders with default label', () => {
    render(<ConfirmPasswordField password="" value="" />);
    expect(screen.getByLabelText('Confirm Password')).toBeInTheDocument();
  });

  it('is type=password by default', () => {
    render(<ConfirmPasswordField password="" value="" />);
    expect(screen.getByLabelText('Confirm Password')).toHaveAttribute('type', 'password');
  });

  it('shows match indicator when values match', () => {
    render(<ConfirmPasswordField password="secret" value="secret" />);
    expect(screen.getAllByText(/passwords match/i).length).toBeGreaterThan(0);
    expect(screen.getByText('✓ Passwords match')).toBeInTheDocument();
  });

  it('shows mismatch indicator when values differ', () => {
    render(<ConfirmPasswordField password="secret" value="different" />);
    expect(screen.getAllByText(/passwords do not match/i).length).toBeGreaterThan(0);
    expect(screen.getByText('✗ Passwords do not match')).toBeInTheDocument();
  });

  it('does not show match indicator when empty', () => {
    render(<ConfirmPasswordField password="" value="" />);
    expect(screen.queryByText(/passwords match/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/passwords do not match/i)).not.toBeInTheDocument();
  });

  it('toggles visibility independently', () => {
    render(<ConfirmPasswordField password="" value="abc" />);
    const input = screen.getByLabelText('Confirm Password');
    const toggle = screen.getByRole('button', { name: /show confirm password/i });

    fireEvent.click(toggle);
    expect(input).toHaveAttribute('type', 'text');
    expect(screen.getByRole('button', { name: /hide confirm password/i })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /hide confirm password/i }));
    expect(input).toHaveAttribute('type', 'password');
  });

  it('uses autocomplete=new-password', () => {
    render(<ConfirmPasswordField password="" value="" />);
    expect(screen.getByLabelText('Confirm Password')).toHaveAttribute('autocomplete', 'new-password');
  });
});
