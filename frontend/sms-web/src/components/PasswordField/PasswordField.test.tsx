import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { PasswordField } from './PasswordField';

describe('PasswordField', () => {
  it('renders with default label', () => {
    render(<PasswordField />);
    expect(screen.getByLabelText('Password')).toBeInTheDocument();
  });

  it('renders with custom label', () => {
    render(<PasswordField label="Custom Password" />);
    expect(screen.getByLabelText(/custom password/i)).toBeInTheDocument();
  });

  it('is type=password by default', () => {
    render(<PasswordField />);
    expect(screen.getByLabelText('Password')).toHaveAttribute('type', 'password');
  });

  it('toggles visibility when eye icon clicked', () => {
    render(<PasswordField />);
    const input = screen.getByLabelText('Password');
    const toggle = screen.getByRole('button', { name: /show password/i });

    fireEvent.click(toggle);
    expect(input).toHaveAttribute('type', 'text');
    expect(screen.getByRole('button', { name: /hide password/i })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /hide password/i }));
    expect(input).toHaveAttribute('type', 'password');
  });

  it('uses autocomplete=new-password', () => {
    render(<PasswordField />);
    expect(screen.getByLabelText('Password')).toHaveAttribute('autocomplete', 'new-password');
  });

  it('passes through value and onChange', () => {
    const handleChange = (e: unknown) => {
      const input = e as { target: { value: string } };
      expect(input.target.value).toBe('secret');
    };
    render(<PasswordField value="secret" onChange={handleChange} />);
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'secret' } });
  });
});
