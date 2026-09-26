import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { ReactNode } from 'react';
import { CourseSelectionPage } from './CourseSelectionPage';
import { enrollmentService } from '../services/enrollment.service';
import { courseService } from '../services/course.service';
import * as authHook from '../hooks/useAuth';

vi.mock('../services/enrollment.service', () => ({
  enrollmentService: {
    getMyStatus: vi.fn(),
    getAvailableCourses: vi.fn(),
    getAvailableCourseUnits: vi.fn(),
    submitEnrollment: vi.fn(),
  },
}));

// The page used to call courseService.getCourses / getUnits. Those endpoints are
// ModeratorAccess and return 403 for a Student, which is the reported bug. The
// mock below records any call so the tests can assert it never happens.
vi.mock('../services/course.service', () => ({
  courseService: {
    getCourses: vi.fn(),
    getUnits: vi.fn(),
  },
}));

vi.mock('../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

const CS_COURSE_ID = 'aaaaaaaa-1111-1111-1111-111111111111';
const OTHER_COURSE_ID = 'bbbbbbbb-2222-2222-2222-222222222222';

const availableCourses = [
  {
    id: CS_COURSE_ID,
    name: 'Computer Science',
    code: 'CS101',
    description: 'The CS degree course',
  },
  {
    id: OTHER_COURSE_ID,
    name: 'Business Administration',
    code: 'BA201',
    description: 'The BA degree course',
  },
];

const availableUnits = [
  { id: 'unit-1', code: 'CS101U1', name: 'Programming Fundamentals', credits: 3 },
  { id: 'unit-2', code: 'CS101U2', name: 'Discrete Mathematics', credits: 3 },
];

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

function mockStatus(overrides: Record<string, unknown> = {}) {
  (enrollmentService.getMyStatus as ReturnType<typeof vi.fn>).mockResolvedValue({
    studentId: 'student-1',
    studentNumber: 'STU202601010001',
    fullName: 'John Doe',
    email: 'john.doe@example.com',
    registrationStatus: 'PendingCourseSelection',
    hasSelectedCourse: true,
    selectedCourseId: CS_COURSE_ID,
    selectedCourseName: 'Computer Science',
    unitsCount: 0,
    needsCourseSelection: false,
    isPendingApproval: false,
    isApproved: false,
    message: undefined,
    ...overrides,
  });
}

describe('CourseSelectionPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (authHook.useAuth as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { id: 'u1', roles: ['Student'] },
    });
    (enrollmentService.getAvailableCourses as ReturnType<typeof vi.fn>).mockResolvedValue(
      availableCourses
    );
    (enrollmentService.getAvailableCourseUnits as ReturnType<typeof vi.fn>).mockResolvedValue(
      availableUnits
    );
    (enrollmentService.submitEnrollment as ReturnType<typeof vi.fn>).mockResolvedValue({
      message: 'ok',
    });
  });
  it('renders one card per course returned by the student-authorized endpoint', async () => {
    mockStatus();
    renderWithProviders(<CourseSelectionPage />);

    await waitFor(() => {
      expect(screen.getByText('Computer Science')).toBeInTheDocument();
    });

    expect(screen.getByText('Business Administration')).toBeInTheDocument();
    expect(screen.getByText('CS101')).toBeInTheDocument();
    expect(screen.getByText('BA201')).toBeInTheDocument();
    expect(screen.getByText('The CS degree course')).toBeInTheDocument();
  });

  it('fetches courses from the student-authorized endpoint, never from /courses', async () => {
    mockStatus();
    renderWithProviders(<CourseSelectionPage />);

    await waitFor(() => {
      expect(enrollmentService.getAvailableCourses).toHaveBeenCalledTimes(1);
    });

    // Regression guard for the reported bug: these moderator-only calls are
    // what returned 403 and produced an empty course grid.
    expect(courseService.getCourses).not.toHaveBeenCalled();
    expect(courseService.getUnits).not.toHaveBeenCalled();
  });

  it('pre-selects the course chosen at registration and labels it as such', async () => {
    mockStatus();
    renderWithProviders(<CourseSelectionPage />);

    await waitFor(() => {
      expect(screen.getByText('Selected at registration')).toBeInTheDocument();
    });

    // The registration course is the checked card, the other is still selectable.
    expect(screen.getAllByText('Selected')).toHaveLength(1);
    expect(screen.getAllByText('Select')).toHaveLength(1);
  });

  it('shows a clear error state instead of "no courses" when the request fails', async () => {
    mockStatus();
    (enrollmentService.getAvailableCourses as ReturnType<typeof vi.fn>).mockRejectedValue(
      new Error('Request failed with status code 403')
    );

    renderWithProviders(<CourseSelectionPage />);

    await waitFor(() => {
      expect(screen.getByText(/Could not load available courses/)).toBeInTheDocument();
    });

    // A failed request must never be presented as an empty catalogue.
    expect(
      screen.queryByText(/No courses are currently available for selection/)
    ).not.toBeInTheDocument();
    expect(screen.queryByText('Business Administration')).not.toBeInTheDocument();
  });

  it('shows a distinct empty state when the endpoint returns no courses', async () => {
    mockStatus();
    (enrollmentService.getAvailableCourses as ReturnType<typeof vi.fn>).mockResolvedValue([]);

    renderWithProviders(<CourseSelectionPage />);

    await waitFor(() => {
      expect(
        screen.getByText(/No courses are currently available for selection/)
      ).toBeInTheDocument();
    });
  });
});
