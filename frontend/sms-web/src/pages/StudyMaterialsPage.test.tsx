import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { ReactNode } from 'react';
import { StudyMaterialsPage } from './StudyMaterialsPage';
import { studyMaterialService, STUDY_MATERIAL_EXTENSIONS } from '../services/studyMaterial.service';
import * as authHook from '../hooks/useAuth';
import { STUDY_MATERIALS_ROLES, LECTURER, STUDENT, RECEPTIONIST } from '../utils/roles';

vi.mock('../services/studyMaterial.service', async () => {
  const actual = await vi.importActual<typeof import('../services/studyMaterial.service')>(
    '../services/studyMaterial.service'
  );
  return {
    ...actual,
    studyMaterialService: {
      getMyUnits: vi.fn(),
      getUnitMaterials: vi.fn(),
      uploadMaterial: vi.fn(),
      downloadMaterial: vi.fn(),
      deleteMaterial: vi.fn(),
    },
  };
});

vi.mock('../hooks/useAuth', () => ({ useAuth: vi.fn() }));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return { ...actual, useNavigate: () => mockNavigate };
});

const TAUGHT_UNIT = {
  unitId: 'unit-taught',
  code: 'CSC201',
  name: 'Data Structures',
  credits: 3,
  courseId: 'course-1',
  courseName: 'Computer Science',
  accessRole: 'Lecturer',
};

const ENROLLED_UNIT = {
  unitId: 'unit-enrolled',
  code: 'CSC202',
  name: 'Algorithms',
  credits: 4,
  courseId: 'course-1',
  courseName: 'Computer Science',
  accessRole: 'Student',
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <StudyMaterialsPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

function mockUser(roles: string[]) {
  (authHook.useAuth as ReturnType<typeof vi.fn>).mockReturnValue({
    user: { id: 'u1', firstName: 'Test', lastName: 'User', roles },
  });
}

describe('StudyMaterialsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUser([LECTURER]);
    (studyMaterialService.getMyUnits as ReturnType<typeof vi.fn>).mockResolvedValue([
      TAUGHT_UNIT,
      ENROLLED_UNIT,
    ]);
  });

  it('lists exactly the units the server returned, with their access role', async () => {
    renderPage();

    expect(await screen.findByText(/CSC201/)).toBeTruthy();
    expect(screen.getByText(/Data Structures/)).toBeTruthy();
    expect(screen.getByText(/CSC202/)).toBeTruthy();

    expect(screen.getByText('You teach this unit')).toBeTruthy();
    expect(screen.getByText('Enrolled')).toBeTruthy();
  });

  it('navigates to the per-unit materials page when a unit is chosen', async () => {
    renderPage();
    const user = userEvent.setup();

    await user.click(await screen.findByText(/CSC201/));

    expect(mockNavigate).toHaveBeenCalledTimes(1);
    const target = mockNavigate.mock.calls[0][0] as string;
    expect(target).toContain('/units/unit-taught/materials');
    expect(target).toContain('code=CSC201');

    // The unit display name is carried in the query string so the per-unit page
    // works for students, who cannot fetch unit details through the
    // moderator-only unit endpoints. It must survive the round trip intact.
    const query = target.slice(target.indexOf('?') + 1);
    const params = new URLSearchParams(query);
    expect(params.get('name')).toBe('Data Structures');
    expect(params.get('code')).toBe('CSC201');
  });

  it('shows the empty state when the caller has no entitled units', async () => {
    (studyMaterialService.getMyUnits as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    mockUser([STUDENT]);

    renderPage();

    expect(await screen.findByText(/not currently enrolled in any unit/)).toBeTruthy();
  });

  it('shows a permission error and renders no units when the API rejects', async () => {
    (studyMaterialService.getMyUnits as ReturnType<typeof vi.fn>).mockRejectedValue({
      status: 403,
      data: { code: 'FORBIDDEN', message: 'Access denied.' },
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeTruthy();
    });
    expect(screen.queryByText(/CSC201/)).toBeNull();
  });

  it('tells a lecturer they manage materials for units they teach', async () => {
    mockUser([LECTURER]);
    renderPage();

    expect(await screen.findByText(/appointed to teach\./)).toBeTruthy();
  });

  it('tells a student they read materials for units they are enrolled in', async () => {
    mockUser([STUDENT]);
    renderPage();

    expect(await screen.findByText(/enrolled to study/)).toBeTruthy();
  });

  it('filters the unit list as the user types', async () => {
    renderPage();
    const user = userEvent.setup();

    const search = await screen.findByPlaceholderText(/Search by unit code/);
    await user.type(search, 'Algorithms');

    await waitFor(() => {
      expect(screen.queryByText(/CSC201/)).toBeNull();
    });
    expect(screen.getByText(/CSC202/)).toBeTruthy();
  });
});

describe('Study Materials role visibility', () => {
  it('shows the Academics entry to lecturers and students only', () => {
    expect(STUDY_MATERIALS_ROLES).toEqual([LECTURER, STUDENT]);
  });

  it('does not expose the entry to receptionist or admin tiers', () => {
    expect(STUDY_MATERIALS_ROLES).not.toContain(RECEPTIONIST);
    expect(STUDY_MATERIALS_ROLES).not.toContain('Administrator');
    expect(STUDY_MATERIALS_ROLES).not.toContain('SystemAdministrator');
    expect(STUDY_MATERIALS_ROLES).not.toContain('Coordinator');
  });
});

describe('Study material accepted file types', () => {
  it('mirrors the backend LecturerNotes allow-list', () => {
    // Keep in step with UploadSettings.AllowedDocumentExtensions in
    // src/SMS.Infrastructure/Options/UploadSettings.cs.
    expect([...STUDY_MATERIAL_EXTENSIONS].sort()).toEqual(
      [
        '.pdf', '.doc', '.docx', '.ppt', '.pptx', '.xls', '.xlsx',
        '.odt', '.odp', '.ods', '.rtf', '.txt', '.csv',
      ].sort()
    );
  });

  it('never offers an executable extension', () => {
    ['.exe', '.dll', '.bat', '.cmd', '.js', '.php', '.sh'].forEach((ext) => {
      expect(STUDY_MATERIAL_EXTENSIONS).not.toContain(ext);
    });
  });
});