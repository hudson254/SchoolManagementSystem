import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, fireEvent, cleanup } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { SnackbarProvider } from 'notistack';
import { RequestsList } from './RequestsList';
import {
  omsRequestsService,
  OmsRequestStatus,
  OmsRequestPriority,
  OmsRequestListItem,
  OmsRequestType,
  type OmsPagedResult,
} from '../../services/requests.service';

vi.mock('../../services/requests.service', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../services/requests.service')>();
  return {
    ...actual,
    omsRequestsService: {
      getRequests: vi.fn(),
      getRequestTypes: vi.fn(),
    },
  };
});

const listItem = (overrides: Partial<OmsRequestListItem> = {}): OmsRequestListItem => ({
  id: 'req-1',
  requestNumber: 'REQ-2026-000001',
  requestType: 'GENERAL_REQUEST',
  title: 'Need more lab time',
  priority: OmsRequestPriority.Normal,
  status: OmsRequestStatus.Submitted,
  requesterUserId: 'student-1',
  assignedUserId: null,
  createdAt: '2026-09-01T10:00:00Z',
  updatedAt: '2026-09-02T10:00:00Z',
  dueDate: null,
  ...overrides,
});

const paged = (
  items: OmsRequestListItem[],
  overrides: Partial<OmsPagedResult<OmsRequestListItem>> = {},
): OmsPagedResult<OmsRequestListItem> => ({
  items,
  totalCount: items.length,
  pageNumber: 1,
  pageSize: 20,
  totalPages: items.length === 0 ? 0 : 1,
  hasPreviousPage: false,
  hasNextPage: false,
  ...overrides,
});

const requestTypes: OmsRequestType[] = [
  {
    id: 'rt-1',
    code: 'GENERAL_REQUEST',
    displayName: 'General Request',
    description: 'A general purpose request',
    isActive: true,
    defaultPriority: OmsRequestPriority.Low,
    createdAt: '2026-09-01T00:00:00Z',
  },
  {
    id: 'rt-2',
    code: 'INACTIVE_TYPE',
    displayName: 'Inactive Type',
    description: null,
    isActive: false,
    defaultPriority: OmsRequestPriority.Normal,
    createdAt: '2026-09-01T00:00:00Z',
  },
];

function renderList(initialEntry = '/oms/requests/list', canViewAll = true) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <SnackbarProvider>
          {/* Rendered directly inside the router: RequestsList only consumes
              router context (useNavigate / useSearchParams / useLocation) and
              derives its scope from the pathname, so no <Routes> matcher is
              needed — and none can mount the list more than once. */}
          <RequestsList canViewAll={canViewAll} />
        </SnackbarProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const getRequests = omsRequestsService.getRequests as unknown as ReturnType<typeof vi.fn>;
const getRequestTypes = omsRequestsService.getRequestTypes as unknown as ReturnType<typeof vi.fn>;

beforeEach(() => {
  // Unmount anything left over from a previous case before clearing mocks, so
  // each assertion queries a single, isolated DOM tree.
  cleanup();
  vi.clearAllMocks();
  getRequestTypes.mockResolvedValue(requestTypes);
});

afterEach(() => {
  cleanup();
});

describe('RequestsList â€” rendering', () => {
  it('renders the rows returned in the paginated envelope', async () => {
    getRequests.mockResolvedValue(paged([listItem()]));
    renderList();

    await waitFor(() => {
      expect(screen.getByText('REQ-2026-000001')).toBeInTheDocument();
    });
    expect(screen.getByText('Need more lab time')).toBeInTheDocument();
    // Rows are read from `items`; the envelope is never treated as an array.
    expect(screen.getByRole('table', { name: 'requests table' })).toBeInTheDocument();
  });

  it('renders a status chip and priority chip per row', async () => {
    getRequests.mockResolvedValue(
      paged([
        listItem({ status: OmsRequestStatus.PendingApproval, priority: OmsRequestPriority.Urgent }),
      ]),
    );
    renderList();

    await waitFor(() => {
      expect(screen.getByText('Pending Approval')).toBeInTheDocument();
    });
    expect(screen.getByText('Urgent')).toBeInTheDocument();
  });
});

describe('RequestsList â€” query parameters', () => {
  it('sends the route scope, page and page size to the API', async () => {
    getRequests.mockResolvedValue(paged([]));
    renderList('/oms/requests/assigned');

    await waitFor(() => {
      expect(getRequests).toHaveBeenCalledWith(
        expect.objectContaining({ scope: 'assigned', pageNumber: 1, pageSize: 20 }),
      );
    });
  });

  it('locks the scope on the dedicated My Requests route and hides the switcher', async () => {
    getRequests.mockResolvedValue(paged([listItem()]));
    renderList('/oms/requests/mine');

    await waitFor(() => {
      expect(getRequests).toHaveBeenCalledWith(expect.objectContaining({ scope: 'mine' }));
    });
    expect(screen.queryByText('Assigned to Me')).not.toBeInTheDocument();
  });

  it('applies a status filter to the next request', async () => {
    getRequests.mockResolvedValue(paged([listItem()]));
    renderList();
    await waitFor(() => expect(getRequests).toHaveBeenCalled());
    getRequests.mockClear();

    // MUI renders a select as a button + menu, so drive it the way a user does
    // rather than forcing a value onto an element that has no value setter.
    fireEvent.mouseDown(screen.getByLabelText('Status'));
    fireEvent.click(await screen.findByRole('option', { name: 'Approved' }));

    await waitFor(() => {
      expect(getRequests).toHaveBeenCalledWith(
        expect.objectContaining({ status: OmsRequestStatus.Approved, pageNumber: 1 }),
      );
    });
  });

  it('commits a search term only on submit', async () => {
    getRequests.mockResolvedValue(paged([listItem()]));
    renderList();
    await waitFor(() => expect(getRequests).toHaveBeenCalled());
    getRequests.mockClear();

    fireEvent.change(screen.getByLabelText('Search'), {
      target: { value: 'REQ-2026' },
    });
    // Typing alone must not fire a request per keystroke.
    expect(getRequests).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'Search' }));
    await waitFor(() => {
      expect(getRequests).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'REQ-2026' }),
      );
    });
  });
});


describe('RequestsList â€” request type filter', () => {
  it('offers only active request types', async () => {
    getRequests.mockResolvedValue(paged([listItem()]));
    renderList();
    await waitFor(() => expect(getRequests).toHaveBeenCalled());

    fireEvent.mouseDown(screen.getByLabelText('Request Type'));

    await waitFor(() => {
      expect(screen.getAllByText('General Request').length).toBeGreaterThan(0);
    });
    // The inactive type is persisted but must not be selectable for new work.
    expect(screen.queryByText('Inactive Type')).not.toBeInTheDocument();
  });
});

describe('RequestsList â€” scope visibility', () => {
  it('offers the Available Queue only when the caller is authorized for it', async () => {
    getRequests.mockResolvedValue(paged([listItem()]));

    const privileged = renderList();
    await waitFor(() => {
      expect(screen.getByText('Available Queue')).toBeInTheDocument();
    });
    privileged.unmount();

    // Without the privileged role the "all" scope is not offered at all.
    renderList('/oms/requests/mine', false);
    await waitFor(() => {
      expect(screen.getByText('My Requests')).toBeInTheDocument();
    });
    expect(screen.queryByText('Available Queue')).not.toBeInTheDocument();
  });
});

describe('RequestsList â€” loading, empty and error states', () => {

  it('shows a loading state while the list is in flight', () => {
    getRequests.mockReturnValue(new Promise(() => {}));
    renderList();
    expect(screen.getByText('Loading requests...')).toBeInTheDocument();
  });

  it('shows the empty state with a clear action when the queue has no rows', async () => {
    getRequests.mockResolvedValue(paged([]));
    renderList();

    await waitFor(() => {
      expect(screen.getByText('No requests in this queue')).toBeInTheDocument();
    });
    expect(screen.getByText(/Requests raised through the Enrollment/)).toBeInTheDocument();
  });

  it('surfaces an API error with a retry control', async () => {
    getRequests.mockRejectedValue({ message: 'boom', code: 'INTERNAL_ERROR' });
    renderList();

    await waitFor(() => {
      expect(screen.getByText(/Retry/)).toBeInTheDocument();
    });
    expect(screen.getByText(/INTERNAL_ERROR/)).toBeInTheDocument();
  });

  it('explains a 403 as an authorization restriction, not a generic failure', async () => {
    getRequests.mockRejectedValue({
      message: 'forbidden',
      code: 'ACCESS_DENIED',
      statusCode: 403,
    });
    renderList();

    await waitFor(() => {
      expect(
        screen.getByText(/You do not have permission to view this queue/),
      ).toBeInTheDocument();
    });
  });
});
