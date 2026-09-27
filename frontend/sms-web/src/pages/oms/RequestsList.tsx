import React, { useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Grid,
  MenuItem,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
// Named barrel import — see the note in RequestsDashboard.tsx for why deep
// default imports of MUI icons break the production bundle.
import { Refresh as RefreshIcon, Search as SearchIcon } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import {
  omsRequestsService,
  OmsRequestScope,
  OmsRequestStatus,
  OMS_REQUEST_STATUSES,
  OmsRequestListItem,
  OmsRequestType,
} from '../../services/requests.service';
import { normalizeError } from '../../utils/errors';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';
import { EmptyState } from '../../components/Common/EmptyState';
import {
  OmsRequestStatusChip,
  OmsRequestPriorityChip,
} from '../../components/omsRequests/RequestChips';
import { omsRequestStatusLabel } from '../../utils/omsRequestLifecycle';

export type RequestListScope = OmsRequestScope;

interface RequestsListProps {
  title?: string;
  /** Whether the caller may see the privileged "all" scope in the switcher. */
  canViewAll?: boolean;
}

const formatDate = (value?: string | null): string => {
  if (!value) return 'â€”';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'â€”';
  return date.toLocaleDateString();
};

const EMPTY_PAGE = {
  items: [] as OmsRequestListItem[],
  totalCount: 0,
  pageNumber: 1,
  pageSize: 20,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false,
};

/**
 * Reusable, scope-aware request list.
 *
 * All filtering, searching and paging is executed SERVER-SIDE: the component
 * renders `PagedResult<RequestListItemDto>` exactly as returned. It never
 * filters the page client-side, because a client-side filter would silently
 * narrow a result set the server authorized.
 *
 * The scope is read from the URL, which is the single source of truth:
 *   /oms/requests/mine      â†’ forced "mine"
 *   /oms/requests/assigned  â†’ forced "assigned"
 *   /oms/requests/list      â†’ from ?scope=, defaulting to "mine"
 * The server enforces these scopes object-locally regardless of what the
 * client asks for, so the switcher is a convenience, not a control.
 */
export const RequestsList: React.FC<RequestsListProps> = ({
  title,
  canViewAll = false,
}) => {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();

  const pathScope: RequestListScope | null = location.pathname.endsWith('/mine')
    ? 'mine'
    : location.pathname.endsWith('/assigned')
      ? 'assigned'
      : null;

  const queryScope = (searchParams.get('scope') as RequestListScope) || null;
  const effectiveScope: RequestListScope = pathScope ?? queryScope ?? 'mine';
  const isScopeLocked = pathScope !== null;

  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  // Search is a controlled draft committed on submit, so typing does not fire
  // a request per keystroke.
  const [searchDraft, setSearchDraft] = useState(searchParams.get('search') || '');
  const [appliedSearch, setAppliedSearch] = useState(searchParams.get('search') || '');

  const statusFilter = (searchParams.get('status') as OmsRequestStatus) || '';
  const typeFilter = searchParams.get('requestType') || '';
  const requesterFilter = searchParams.get('requesterUserId') || '';
  const assigneeFilter = searchParams.get('assignedUserId') || '';

  const resolvedTitle =
    title ??
    (effectiveScope === 'mine'
      ? 'My Requests'
      : effectiveScope === 'assigned'
        ? 'Assigned to Me'
        : 'Available Queue');

  const setFilter = (key: string, value: string) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(key, value);
    else next.delete(key);
    setSearchParams(next);
    setPage(0);
  };


  const queryParams = useMemo(
    () => ({
      scope: effectiveScope,
      pageNumber: page + 1,
      pageSize,
      ...(statusFilter ? { status: statusFilter } : {}),
      ...(typeFilter ? { requestType: typeFilter } : {}),
      ...(requesterFilter ? { requesterUserId: requesterFilter } : {}),
      // Only meaningful for the privileged "all" queue; the server overwrites
      // it for the object-level scopes regardless.
      ...(assigneeFilter && effectiveScope === 'all' ? { assignedUserId: assigneeFilter } : {}),
      ...(appliedSearch ? { search: appliedSearch } : {}),
    }),
    [
      effectiveScope,
      page,
      pageSize,
      statusFilter,
      typeFilter,
      requesterFilter,
      assigneeFilter,
      appliedSearch,
    ],
  );

  const requestsQuery = useQuery({
    queryKey: ['oms-requests', queryParams],
    queryFn: () => omsRequestsService.getRequests(queryParams),
    // A 403 is the expected, correct answer for a non-privileged caller asking
    // for a queue they may not see. Retrying cannot change that.
    retry: false,
  });

  const typesQuery = useQuery({
    queryKey: ['oms-request-types'],
    queryFn: () => omsRequestsService.getRequestTypes(),
  });

  const data = requestsQuery.data ?? EMPTY_PAGE;
  const activeTypes: OmsRequestType[] = (typesQuery.data ?? []).filter((t) => t.isActive);
  const hasFilters = !!(
    statusFilter ||
    typeFilter ||
    requesterFilter ||
    assigneeFilter ||
    appliedSearch
  );
  const error = requestsQuery.isError ? normalizeError(requestsQuery.error) : null;

  const handleSearchSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    const trimmed = searchDraft.trim();
    setAppliedSearch(trimmed);
    setFilter('search', trimmed);
    setPage(0);
  };

  const clearFilters = () => {
    setSearchDraft('');
    setAppliedSearch('');
    setSearchParams(new URLSearchParams());
    setPage(0);
  };

  const scopeOptions: RequestListScope[] = canViewAll
    ? ['mine', 'assigned', 'all']
    : ['mine', 'assigned'];

  const scopeLabel = (scope: RequestListScope) =>
    scope === 'mine'
      ? 'My Requests'
      : scope === 'assigned'
        ? 'Assigned to Me'
        : 'Available Queue';
  return (
    <Box>
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          justifyContent: 'space-between',
          alignItems: 'center',
          gap: 2,
          mb: 3,
        }}
      >
        <Box>
          <Typography variant="h5" fontWeight={600}>
            {resolvedTitle}
          </Typography>
          <Typography variant="body2" color="textSecondary">
            {data.totalCount} request{data.totalCount === 1 ? '' : 's'} in this queue
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={() => requestsQuery.refetch()}
            disabled={requestsQuery.isFetching}
          >
            Refresh
          </Button>
          <Button variant="contained" onClick={() => navigate('/oms/requests/new')}>
            New Request
          </Button>
        </Box>
      </Box>

      {!isScopeLocked && (
        <Box sx={{ display: 'flex', gap: 1, mb: 2, flexWrap: 'wrap' }}>
          {scopeOptions.map((scope) => (
            <Button
              key={scope}
              size="small"
              variant={effectiveScope === scope ? 'contained' : 'outlined'}
              onClick={() => setFilter('scope', scope)}
              aria-pressed={effectiveScope === scope}
            >
              {scopeLabel(scope)}
            </Button>
          ))}
        </Box>
      )}

      <Paper sx={{ p: 2, mb: 2 }}>
        <Box component="form" onSubmit={handleSearchSubmit}>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                size="small"
                label="Search"
                placeholder="Request number or title"
                value={searchDraft}
                onChange={(event) => setSearchDraft(event.target.value)}
                InputProps={{
                  startAdornment: (
                    <SearchIcon fontSize="small" sx={{ mr: 1, color: 'text.disabled' }} />
                  ),
                }}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                fullWidth
                size="small"
                label="Status"
                value={statusFilter}
                onChange={(event) => setFilter('status', event.target.value)}
              >
                <MenuItem value="">All statuses</MenuItem>
                {OMS_REQUEST_STATUSES.map((status) => (
                  <MenuItem key={status} value={status}>
                    {omsRequestStatusLabel(status)}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                select
                fullWidth
                size="small"
                label="Request Type"
                value={typeFilter}
                onChange={(event) => setFilter('requestType', event.target.value)}
              >
                <MenuItem value="">All types</MenuItem>
                {activeTypes.map((type) => (
                  <MenuItem key={type.id} value={type.code}>
                    {type.displayName}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            {effectiveScope === 'all' && (
              <>
                <Grid item xs={12} sm={6} md={1.5}>
                  <TextField
                    fullWidth
                    size="small"
                    label="Requester"
                    value={requesterFilter}
                    onChange={(event) => setFilter('requesterUserId', event.target.value)}
                  />
                </Grid>
                <Grid item xs={12} sm={6} md={1.5}>
                  <TextField
                    fullWidth
                    size="small"
                    label="Assignee"
                    value={assigneeFilter}
                    onChange={(event) => setFilter('assignedUserId', event.target.value)}
                  />
                </Grid>
              </>
            )}
            <Grid item xs={12} md={3} sx={{ display: 'flex', gap: 1 }}>
              <Button type="submit" variant="outlined">
                Search
              </Button>
              {hasFilters && (
                <Button variant="text" onClick={clearFilters}>
                  Clear
                </Button>
              )}
            </Grid>
          </Grid>
        </Box>
      </Paper>

      {requestsQuery.isLoading && <LoadingSpinner message="Loading requests..." />}

      {error && (
        <Alert
          severity={error.statusCode === 403 ? 'warning' : 'error'}
          sx={{ mb: 2 }}
          action={
            <Button color="inherit" size="small" onClick={() => requestsQuery.refetch()}>
              Retry
            </Button>
          }
        >
          {error.statusCode === 403
            ? 'You do not have permission to view this queue. The server has restricted it to an authorized role.'
            : `${error.serverMessage || error.message} (${error.code})`}
        </Alert>
      )}

      {!requestsQuery.isLoading && !error && (
        <Paper>
          {data.items.length === 0 ? (
            <EmptyState
              title={
                hasFilters ? 'No requests match your filters' : 'No requests in this queue'
              }
              description={
                hasFilters
                  ? 'Try widening your search or clearing the filters.'
                  : 'Requests raised through the Enrollment, Accommodation and Assignment modules will appear here.'
              }
              actionText={hasFilters ? 'Clear Filters' : undefined}
              onAction={hasFilters ? clearFilters : undefined}
            />
          ) : (
            <>
              <TableContainer>
                <Table stickyHeader aria-label="requests table">
                  <TableHead>
                    <TableRow>
                      <TableCell>Request Number</TableCell>
                      <TableCell>Title</TableCell>
                      <TableCell>Type</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell>Priority</TableCell>
                      <TableCell>Requester</TableCell>
                      <TableCell>Assignee</TableCell>
                      <TableCell>Created</TableCell>
                      <TableCell>Due</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {data.items.map((item) => (
                      <TableRow
                        key={item.id}
                        hover
                        sx={{ cursor: 'pointer' }}
                        onClick={() => navigate(`/oms/requests/${item.id}`)}
                      >
                        <TableCell sx={{ fontFamily: 'monospace' }}>
                          {item.requestNumber}
                        </TableCell>
                        <TableCell>{item.title}</TableCell>
                        <TableCell>
                          <Typography variant="caption" color="textSecondary">
                            {item.requestType}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <OmsRequestStatusChip status={item.status} />
                        </TableCell>
                        <TableCell>
                          <OmsRequestPriorityChip priority={item.priority} />
                        </TableCell>
                        <TableCell>
                          <Typography variant="caption">{item.requesterUserId}</Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="caption">
                            {item.assignedUserId || 'â€”'}
                          </Typography>
                        </TableCell>
                        <TableCell>{formatDate(item.createdAt)}</TableCell>
                        <TableCell>{formatDate(item.dueDate)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
              <TablePagination
                component="div"
                count={data.totalCount}
                page={page}
                rowsPerPage={pageSize}
                rowsPerPageOptions={[10, 20, 50, 100]}
                onPageChange={(_event, newPage) => setPage(newPage)}
                onRowsPerPageChange={(event) => {
                  setPageSize(parseInt(event.target.value, 10));
                  setPage(0);
                }}
              />
            </>
          )}
        </Paper>
      )}
    </Box>
  );
};

