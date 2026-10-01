import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { ReactNode } from 'react';
import { AccommodationReports } from './AccommodationReports';
import { accommodationService } from '../services/accommodation.service';
import { semesterService } from '../services/semester.service';
import * as authHook from '../hooks/useAuth';
import type {
  AccommodationHouseOccupancyReport,
  AccommodationReportBase,
  HouseOccupantReport,
  HouseUtilizationSummaryReport,
  OccupancyByPeriodReport,
  OccupancyHistoryReportRow,
  OccupancySummaryReport,
  OccupantAccommodationHistoryReport,
  OccupantCandidate,
} from '../types/accommodation.types';

vi.mock('../services/accommodation.service', () => ({
  accommodationService: {
    getLanes: vi.fn(),
    getHouses: vi.fn(),
    getCurrentOccupancyReport: vi.fn(),
    getOccupiedHousesReport: vi.fn(),
    getEmptyHousesReport: vi.fn(),
    getOccupancyHistoryReport: vi.fn(),
    getHouseHistoryReport: vi.fn(),
    getOccupancyByPeriodReport: vi.fn(),
    getOccupantHistoryReport: vi.fn(),
    getUtilizationSummaryReport: vi.fn(),
    exportAccommodationReport: vi.fn(),
  },
}));

vi.mock('../services/semester.service', () => ({
  semesterService: { getSemesters: vi.fn() },
}));

vi.mock('../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

const summary: OccupancySummaryReport = {
  totalHouses: 4,
  occupiedHouses: 2,
  emptyHouses: 2,
  totalCapacity: 40,
  occupiedSpaces: 17,
  availableSpaces: 23,
  housesAtFullCapacity: 1,
  housesWithAvailableCapacity: 1,
  housesNeverOccupied: 1,
  occupancyPercentage: 42.5,
};

const meta: AccommodationReportBase = {
  reportKey: 'current-occupancy',
  reportTitle: 'Current House Occupancy',
  generatedAtUtc: '2026-09-28T08:00:00Z',
  generatedBy: 'Jane Receptionist',
  appliedFilters: [{ label: 'Lane', value: 'Boys Lane' }],
};

const pagination = { totalCount: 4, page: 1, pageSize: 50, totalPages: 1 };

const currentOccupants: HouseOccupantReport[] = [
  {
    assignmentId: 'assign-1',
    occupantId: 'student-1',
    occupantType: 'Student',
    occupantName: 'Jane Doe',
    occupantNumber: 'STS/001',
    allocationDate: '2026-01-10T00:00:00Z',
    moveInDate: '2026-01-12T00:00:00Z',
    checkInDate: '2026-01-12T00:00:00Z',
    assignmentStatus: 'Active',
    semesterId: 'sem-1',
    semesterName: 'Semester 1',
  },
];

const houseReport: AccommodationHouseOccupancyReport = {
  ...meta,
  summary,
  pagination,
  rows: [
    {
      houseId: 'house-1',
      houseNumber: 'H-001',
      houseName: 'Block A',
      laneId: 'lane-1',
      laneName: 'Boys Lane',
      status: 'Occupied',
      capacity: 4,
      occupiedCount: 3,
      availableSpaces: 1,
      isOccupied: true,
      isAvailable: true,
      isEnabled: true,
      occupancyStatus: 'Occupied',
      currentOccupants,
      historicalOccupantCount: 7,
    },
    {
      houseId: 'house-2',
      houseNumber: 'H-002',
      houseName: null,
      laneId: 'lane-1',
      laneName: 'Boys Lane',
      status: 'Vacant',
      capacity: 4,
      occupiedCount: 0,
      availableSpaces: 4,
      isOccupied: false,
      isAvailable: true,
      isEnabled: true,
      occupancyStatus: 'Empty',
      currentOccupants: [],
      lastOccupantName: 'Past Occupant',
      lastOccupantNumber: 'STS/099',
      lastOccupantType: 'Student',
      lastOccupancyEndDate: '2025-12-15T00:00:00Z',
      historicalOccupantCount: 2,
    },
  ],
};

const candidate: OccupantCandidate = {
  occupantId: 'student-1',
  occupantType: 'Student',
  occupantName: 'Jane Doe',
  occupantNumber: 'STS/001',
  currentHouseId: 'house-1',
  currentHouseNumber: 'H-001',
  totalStays: 2,
  isCurrent: true,
};

const stay: OccupancyHistoryReportRow = {
  assignmentId: 'assign-1',
  houseId: 'house-1',
  houseNumber: 'H-001',
  houseName: 'Block A',
  laneName: 'Boys Lane',
  occupantId: 'student-1',
  occupantType: 'Student',
  occupantName: 'Jane Doe',
  occupantNumber: 'STS/001',
  occupancyStartDate: '2026-01-12T00:00:00Z',
  occupancyEndDate: null,
  isCurrent: true,
  durationDays: 260,
  status: 'Active',
  semesterName: 'Semester 1',
  academicYearName: '2026',
};

/**
 * DEFECT-02 fixture. The occupancy-by-period summary used to leave
 * housesAtFullCapacity / housesWithAvailableCapacity / housesNeverOccupied at 0, so
 * these three tiles rendered "0" while the house reports showed real numbers. The
 * values below are deliberately distinct from every other number on the page so a
 * regression cannot hide behind an unrelated tile.
 */
const periodSummary: OccupancySummaryReport = {
  totalHouses: 11,
  occupiedHouses: 3,
  emptyHouses: 8,
  totalCapacity: 40,
  occupiedSpaces: 17,
  availableSpaces: 23,
  housesAtFullCapacity: 3,
  housesWithAvailableCapacity: 8,
  housesNeverOccupied: 6,
  occupancyPercentage: 42.5,
};

const periodReport: OccupancyByPeriodReport = {
  ...meta,
  reportKey: 'occupancy-by-period',
  reportTitle: 'Occupancy by Period',
  periodStart: '2026-01-01T00:00:00Z',
  periodEnd: '2026-03-31T00:00:00Z',
  periodLabel: '01 Jan 2026 to 31 Mar 2026',
  summary: periodSummary,
  pagination: { totalCount: 2, page: 1, pageSize: 50, totalPages: 1 },
  rows: [
    {
      houseId: 'house-1',
      houseNumber: 'H-001',
      houseName: 'Block A',
      laneName: 'Boys Lane',
      status: 'Occupied',
      capacity: 4,
      occupiedInPeriod: 4,
      availableInPeriod: 0,
      occupantsInPeriod: 'Jane Doe (STS/001), John Roe (STS/002), Ada Ray (STS/003), Sam Poe (STS/004)',
      wasOccupiedInPeriod: true,
    },
    {
      houseId: 'house-2',
      houseNumber: 'H-002',
      houseName: null,
      laneName: 'Boys Lane',
      status: 'Vacant',
      capacity: 4,
      occupiedInPeriod: 0,
      availableInPeriod: 4,
      occupantsInPeriod: '',
      wasOccupiedInPeriod: false,
    },
  ],
};

/**
 * Reads a summary tile as "label + rendered value". Scoped to the tile's own Paper
 * so a value that happens to appear elsewhere on the page cannot satisfy the
 * assertion.
 */
function summaryTile(label: string): { labelNode: HTMLElement; value: string } {
  const labelNode = screen.getByText(label);
  const paper = labelNode.closest('.MuiPaper-root') as HTMLElement;
  return { labelNode, value: paper.textContent ?? '' };
}

function renderWithProviders(ui: ReactNode) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>,
  );
}

const mock = (fn: unknown) => fn as ReturnType<typeof vi.fn>;

/**
 * MUI renders a Select trigger as role="combobox" whose accessible name comes from the
 * field label, while the native input the label points at stays hidden. mouseDown on the
 * trigger is what opens the menu for a real user, so tests drive it the same way.
 */
const openSelect = (name: RegExp | string) => {
  const trigger = screen.getByRole('combobox', { name });
  fireEvent.mouseDown(trigger);
  return trigger;
};


beforeEach(() => {
  vi.clearAllMocks();
  (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
    user: { id: 'user-1', roles: ['Receptionist'] },
  });
  mock(accommodationService.getLanes).mockResolvedValue([
    { id: 'lane-1', laneName: 'Boys Lane', description: null, houseCount: 2 },
  ]);
  mock(accommodationService.getHouses).mockResolvedValue([
    { id: 'house-1', houseNumber: 'H-001', houseName: 'Block A', laneId: 'lane-1', laneName: 'Boys Lane' },
  ]);
  mock(semesterService.getSemesters).mockResolvedValue([{ id: 'sem-1', name: 'Semester 1' }]);
  mock(accommodationService.getCurrentOccupancyReport).mockResolvedValue(houseReport);
  mock(accommodationService.getOccupancyByPeriodReport).mockResolvedValue(periodReport);
  mock(accommodationService.getUtilizationSummaryReport).mockResolvedValue({
    ...meta,
    reportTitle: 'House Utilization Summary',
    summary,
  } as HouseUtilizationSummaryReport);
  mock(accommodationService.getOccupantHistoryReport).mockResolvedValue({
    ...meta,
    mode: 'Search',
    selectedOccupant: null,
    candidates: [candidate],
    stays: [],
    currentHouse: '',
    pagination,
  } as OccupantAccommodationHistoryReport);
});

describe('AccommodationReports', () => {
  it('loads the current occupancy report and renders summary, rows and applied filters', async () => {
    renderWithProviders(<AccommodationReports />);

    await waitFor(() => {
      expect(screen.getByText('Current House Occupancy')).toBeInTheDocument();
    });

    expect(mock(accommodationService.getCurrentOccupancyReport)).toHaveBeenCalledWith(
      expect.objectContaining({ page: 1, pageSize: 50 }),
    );

    // Summary cards driven by the server payload.
    expect(screen.getByText('Total capacity')).toBeInTheDocument();
    expect(screen.getByText('40')).toBeInTheDocument();
    expect(screen.getByText('42.5%')).toBeInTheDocument();

    // House rows plus the filter chip echoed by the report.
    expect(screen.getByText('H-001')).toBeInTheDocument();
    expect(screen.getByText('H-002')).toBeInTheDocument();
    expect(screen.getByText('Lane: Boys Lane')).toBeInTheDocument();
    expect(screen.getByText(/Generated .* by Jane Receptionist/)).toBeInTheDocument();

    // Expanding a house reveals its current occupants.
    const firstRow = screen.getByText('H-001').closest('tr') as HTMLElement;
    fireEvent.click(within(firstRow).getByLabelText('Expand'));
    await waitFor(() => {
      expect(screen.getByText('Jane Doe')).toBeInTheDocument();
    });
    expect(screen.getByText('STS/001')).toBeInTheDocument();
  });

  it('switches the preview when another report is selected', async () => {
    renderWithProviders(<AccommodationReports />);
    await waitFor(() => {
      expect(mock(accommodationService.getCurrentOccupancyReport)).toHaveBeenCalled();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Utilization' }));

    await waitFor(() => {
      expect(mock(accommodationService.getUtilizationSummaryReport)).toHaveBeenCalled();
    });
    expect(await screen.findByText(/These totals cover 4 house/)).toBeInTheDocument();
    expect(screen.getByText('House Utilization Summary')).toBeInTheDocument();
  });

  // DEFECT-02: the occupancy-by-period summary shipped without
  // housesAtFullCapacity / housesWithAvailableCapacity / housesNeverOccupied, so
  // these three tiles silently rendered 0. They must render the payload the API
  // actually returns, and they must render it per report — not inherit the
  // current-occupancy numbers.
  it('renders the occupancy-by-period capacity tiles from the report payload', async () => {
    renderWithProviders(<AccommodationReports />);
    await waitFor(() => {
      expect(mock(accommodationService.getCurrentOccupancyReport)).toHaveBeenCalled();
    });

    fireEvent.click(screen.getByRole('button', { name: 'By period' }));

    await waitFor(() => {
      expect(mock(accommodationService.getOccupancyByPeriodReport)).toHaveBeenCalled();
    });
    expect(await screen.findByText('Occupancy by Period')).toBeInTheDocument();

    // Headline tiles that already worked stay correct.
    expect(summaryTile('Total houses').value).toContain('11');
    expect(summaryTile('Occupied spaces').value).toContain('17');

    // The three tiles that were stuck at 0.
    expect(summaryTile('At full capacity').value).toContain('3');
    expect(summaryTile('With free space').value).toContain('8');
    expect(summaryTile('Never occupied').value).toContain('6');

    // Per-house breakdown rows load for the selected period.
    expect(screen.getByText(/Jane Doe \(STS\/001\)/)).toBeInTheDocument();
    expect(screen.getByText(/John Roe \(STS\/002\)/)).toBeInTheDocument();
  });

  it('shows the empty state, and a real zero, for a period with no occupancy', async () => {
    mock(accommodationService.getOccupancyByPeriodReport).mockResolvedValue({
      ...periodReport,
      periodLabel: '01 Apr 2026 to 30 Apr 2026',
      summary: {
        ...periodSummary,
        occupiedHouses: 0,
        occupiedSpaces: 0,
        availableSpaces: 40,
        housesAtFullCapacity: 0,
        housesWithAvailableCapacity: 11,
      },
      rows: [],
    });

    renderWithProviders(<AccommodationReports />);
    await waitFor(() => {
      expect(mock(accommodationService.getCurrentOccupancyReport)).toHaveBeenCalled();
    });

    fireEvent.click(screen.getByRole('button', { name: 'By period' }));

    expect(await screen.findByText('Nothing occupied in this period')).toBeInTheDocument();
    // A genuine zero still renders as 0 — which is what distinguishes a real empty
    // result from the old "field never assigned" bug.
    expect(summaryTile('At full capacity').value).toContain('0');
    expect(summaryTile('With free space').value).toContain('11');
  });

  it('keeps a required house until it is chosen, then loads that house history', async () => {
    mock(accommodationService.getHouseHistoryReport).mockResolvedValue({
      ...meta,
      reportTitle: 'House Occupancy History',
      houseId: 'house-1',
      houseNumber: 'H-001',
      houseName: 'Block A',
      laneName: 'Boys Lane',
      houseStatus: 'Occupied',
      capacity: 4,
      currentOccupants: 1,
      totalStays: 7,
      pagination,
      rows: [stay],
    });

    renderWithProviders(<AccommodationReports />);
    await waitFor(() => {
      expect(mock(accommodationService.getCurrentOccupancyReport)).toHaveBeenCalled();
    });

    fireEvent.click(screen.getByRole('button', { name: 'House history' }));
    expect(
      await screen.findByText('Choose a house to load its stay-by-stay occupancy history.'),
    ).toBeInTheDocument();
    expect(accommodationService.getHouseHistoryReport).not.toHaveBeenCalled();

    openSelect(/House/);
    fireEvent.click(await screen.findByRole('option', { name: /H-001/ }));

    await waitFor(() => {
      expect(mock(accommodationService.getHouseHistoryReport)).toHaveBeenCalledWith(
        'house-1',
        expect.objectContaining({ houseId: 'house-1' }),
      );
    });
    expect(await screen.findByText('Total stays: 7')).toBeInTheDocument();
  });
  it('searches occupants and then drills into one occupant stay history', async () => {
    mock(accommodationService.getOccupantHistoryReport).mockImplementation((params: any) =>
      Promise.resolve(
        params?.occupantId
          ? {
              ...meta,
              mode: 'Detail',
              selectedOccupant: candidate,
              candidates: [],
              stays: [stay],
              currentHouse: 'H-001',
              pagination,
            }
          : {
              ...meta,
              mode: 'Search',
              selectedOccupant: null,
              candidates: [candidate],
              stays: [],
              currentHouse: '',
              pagination,
            },
      ),
    );

    renderWithProviders(<AccommodationReports />);
    await waitFor(() => {
      expect(mock(accommodationService.getCurrentOccupancyReport)).toHaveBeenCalled();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Occupant' }));
    expect(await screen.findByText(/Type a name, student number or staff number/)).toBeInTheDocument();
    expect(accommodationService.getOccupantHistoryReport).not.toHaveBeenCalled();

    fireEvent.change(screen.getByLabelText('Name, student or staff number'), {
      target: { value: 'Jane' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));

    await waitFor(() => {
      expect(mock(accommodationService.getOccupantHistoryReport)).toHaveBeenCalledWith(
        expect.objectContaining({ searchTerm: 'Jane' }),
      );
    });
    expect(await screen.findByText('View history')).toBeInTheDocument();

    fireEvent.click(screen.getByText('View history'));
    await waitFor(() => {
      expect(mock(accommodationService.getOccupantHistoryReport)).toHaveBeenLastCalledWith(
        expect.objectContaining({ occupantId: 'student-1' }),
      );
    });
    expect(await screen.findByText('Current house: H-001')).toBeInTheDocument();
    expect(screen.getAllByText('Jane Doe').length).toBeGreaterThan(0);
  });

  it('exports the active report without pagination and downloads the file', async () => {
    const downloads: string[] = [];
    const clickSpy = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(function anchorClick(this: HTMLAnchorElement) {
        downloads.push(this.download);
      });
    window.URL.createObjectURL = vi.fn(
      () => 'blob:report',
    ) as unknown as typeof URL.createObjectURL;
    window.URL.revokeObjectURL = vi.fn() as unknown as typeof URL.revokeObjectURL;
    mock(accommodationService.exportAccommodationReport).mockResolvedValue(new Blob(['x']));

    renderWithProviders(<AccommodationReports />);
    await waitFor(() => {
      expect(screen.getByText('Current House Occupancy')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'PDF' }));

    await waitFor(() => {
      expect(mock(accommodationService.exportAccommodationReport)).toHaveBeenCalledWith(
        'current-occupancy',
        'PDF',
        expect.not.objectContaining({ page: expect.anything() }),
      );
    });
    expect(
      await screen.findByText('Current House Occupancy downloaded as PDF.'),
    ).toBeInTheDocument();
    expect(downloads[0]).toMatch(/^Accommodation_current_occupancy_\d{4}-\d{2}-\d{2}\.pdf$/);

    clickSpy.mockRestore();
  });

  it('surfaces an actionable message when the export fails', async () => {
    window.URL.createObjectURL = vi.fn(
      () => 'blob:report',
    ) as unknown as typeof URL.createObjectURL;
    window.URL.revokeObjectURL = vi.fn() as unknown as typeof URL.revokeObjectURL;
    // The api layer rejects with normalizeError() output (src/utils/errors.ts), so the
    // page sees a NormalizedError rather than the raw Axios wrapper.
    mock(accommodationService.exportAccommodationReport).mockRejectedValue({
      code: 'EXPORT_FAILED',
      message: 'Unable to export the data. Please try again.',
      serverMessage: 'No report rows were found for the selected filters.',
      isNetworkError: false,
      isTimeout: false,
    });

    renderWithProviders(<AccommodationReports />);
    await waitFor(() => {
      expect(screen.getByText('Current House Occupancy')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Excel' }));

    expect(await screen.findByText(/No report rows were found/)).toBeInTheDocument();
  });

  it('blocks users without staff access and never calls the report API', () => {
    (authHook.useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      user: { id: 'user-2', roles: ['Student'] },
    });

    renderWithProviders(<AccommodationReports />);

    expect(
      screen.getByText(
        'Accommodation reports are available to reception and administration staff only.',
      ),
    ).toBeInTheDocument();
    expect(accommodationService.getCurrentOccupancyReport).not.toHaveBeenCalled();
  });
});
