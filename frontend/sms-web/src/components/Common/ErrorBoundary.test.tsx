import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ErrorBoundary } from './ErrorBoundary';

// A component that throws an error to trigger the boundary
function ThrowingComponent({ shouldThrow = true }: { shouldThrow?: boolean }) {
  if (shouldThrow) {
    throw new Error('Critical failure: at SMS.API.Controllers.StudentController line 42 password=secret123');
  }
  return <div>Recovered Content</div>;
}

describe('ErrorBoundary (Phase 7 - never expose stack traces)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    // Silence expected console.error from the boundary
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  it('renders children when no error occurs', () => {
    render(
      <ErrorBoundary>
        <div>Normal Content</div>
      </ErrorBoundary>
    );
    expect(screen.getByText('Normal Content')).toBeInTheDocument();
  });

  it('shows a friendly fallback message when an error is thrown', () => {
    render(
      <ErrorBoundary>
        <ThrowingComponent />
      </ErrorBoundary>
    );

    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
    expect(screen.getByText(/couldn't complete your request/i)).toBeInTheDocument();
    expect(screen.getByText('Try Again')).toBeInTheDocument();
  });

  it('never exposes the raw error message or stack trace to users', () => {
    render(
      <ErrorBoundary>
        <ThrowingComponent />
      </ErrorBoundary>
    );

    // The raw error message with sensitive details must NOT appear
    expect(screen.queryByText(/Critical failure/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/at SMS\.API\.Controllers/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/password=secret123/i)).not.toBeInTheDocument();
    // No pre/code blocks with stack traces
    expect(document.querySelector('pre')).not.toBeInTheDocument();
  });

  it('recovers after clicking Try Again when the child no longer throws', () => {
    let shouldThrow = true;
    const { rerender } = render(
      <ErrorBoundary>
        <ThrowingComponent shouldThrow={shouldThrow} />
      </ErrorBoundary>
    );

    expect(screen.getByText('Something went wrong')).toBeInTheDocument();

    // Fix the child and click Try Again
    shouldThrow = false;
    rerender(
      <ErrorBoundary>
        <ThrowingComponent shouldThrow={shouldThrow} />
      </ErrorBoundary>
    );

    fireEvent.click(screen.getByText('Try Again'));
    expect(screen.getByText('Recovered Content')).toBeInTheDocument();
  });
});
