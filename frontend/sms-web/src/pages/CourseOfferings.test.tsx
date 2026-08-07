import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { ReactNode } from 'react';
import { CourseOfferings } from './CourseOfferings';
import { courseOfferingService } from '../services/course-offering.service';
import * as authHook from '../hooks/useAuth';

vi.mock('../services/course-offering.service', () => ({
  courseOfferingService: {
    getCourseOfferings: vi.fn(),
    deleteCourseOffering: vi.fn(),
  },
  CourseOfferingStatus: {
    Draft: 'Draft',
    Scheduled: 'Scheduled',
    Active: 'Active',
    Completed: 'Completed',
    Cancelled: 'Cancelled',
  },
}));

vi.mock('../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

const mockResponse = {
  items: [
    {
      id: 'offering-1',
      offeringCode: 'CS-2025-1',
      courseId: 'course-1',
      courseName: 'Computer Science',
      courseCode: 'CS101',
      status: 'Active',
      totalUnits: 6,
      totalEnrollments: 120,
      totalLecturers: 4,
    },
    {
      id: 'offering-2',
      offeringCode: 'MATH-2025-1',
      courseId: 'course-2',
      courseName: 'Mathematics',
      courseCode: 'MATH101',
      status: 'Scheduled',
      totalUnits: 4,
      totalEnrollments: 0,
      totalLecturers: 2,
    },
  ],
  totalCount: 2,
  page: 0,
  pageSize: 10,
  totalPages: 1,
};

function renderWithProviders(ui: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
}

describe('CourseOfferings page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (courseOfferingService.getCourseOfferings as any).mockResolvedValue(mockResponse);
    (authHook.useAuth as any).mockReturnValue({
      user: { roles: ['SystemAdministrator'] },
    });
  });

  it('renders the page title', async () => {
    renderWithProviders(<CourseOfferings />);

    await waitFor(() => {
      expect(screen.getByText('CS-2025-1')).toBeInTheDocument();
    });

    expect(screen.getByText('Course Offerings')).toBeInTheDocument();
  });

  it('renders course offerings after loading', async () => {
    renderWithProviders(<CourseOfferings />);

    await waitFor(() => {
      expect(screen.getByText('CS-2025-1')).toBeInTheDocument();
    });

    expect(screen.getByText('Computer Science')).toBeInTheDocument();
    expect(screen.getByText('MATH-2025-1')).toBeInTheDocument();
    expect(screen.getByText('Mathematics')).toBeInTheDocument();
  });

  it('shows New Offering button for administrators', async () => {
    renderWithProviders(<CourseOfferings />);

    await waitFor(() => {
      expect(screen.getByText('CS-2025-1')).toBeInTheDocument();
    });

    expect(screen.getByText('New Offering')).toBeInTheDocument();
  });

  it('hides New Offering button for non-admin users', async () => {
    (authHook.useAuth as any).mockReturnValue({
      user: { roles: ['Student'] },
    });
    renderWithProviders(<CourseOfferings />);

    await waitFor(() => {
      expect(screen.getByText('CS-2025-1')).toBeInTheDocument();
    });

    expect(screen.queryByText('New Offering')).not.toBeInTheDocument();
  });

  it('shows empty state when no offerings exist', async () => {
    (courseOfferingService.getCourseOfferings as any).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 0,
      pageSize: 10,
      totalPages: 0,
    });
    renderWithProviders(<CourseOfferings />);

    await waitFor(() => {
      expect(screen.getByText('No course offerings found')).toBeInTheDocument();
    });
  });

  it('shows error state when API fails', async () => {
    (courseOfferingService.getCourseOfferings as any).mockRejectedValue(new Error('API error'));
    renderWithProviders(<CourseOfferings />);

    await waitFor(() => {
      expect(screen.getByText(/Failed to load course offerings/i)).toBeInTheDocument();
    });
  });
});
