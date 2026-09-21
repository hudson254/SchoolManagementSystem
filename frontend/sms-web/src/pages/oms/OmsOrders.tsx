import React, { useState } from 'react';
import {
  Box,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TextField,
  Typography,
  Alert,
  Checkbox,
  CircularProgress,
  Chip,
  Tooltip,
} from '@mui/material';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  Search as SearchIcon,
  Visibility as ViewIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  omsService,
  OmsOrder,
  OmsOrderStatus,
  OmsPagedResult,
} from '../../services/oms.service';
import { useAuth } from '../../hooks/useAuth';
import { canViewOmsOrders, canManageOmsOrders } from '../../utils/roles';
import { normalizeError } from '../../utils/errors';
import { EmptyState } from '../../components/Common/EmptyState';
import { formatOmsMoney } from './OmsDashboard';

/** Chip colors per OMS order status (feeds StatusBadge). */
export const omsStatusColorMap: Record<string, 'default' | 'primary' | 'info' | 'warning' | 'success' | 'error'> = {
  Draft: 'default',
  Submitted: 'info',
  PendingApproval: 'warning',
  Approved: 'success',
  Rejected: 'error',
  Cancelled: 'default',
};

/** Truncated creator id — the list API exposes only the creator's user id. */
const shortUserId = (id: string | null | undefined): string => {
  if (!id) return '—';
  return id.length <= 12 ? id : `${id.slice(0, 8)}…`;
};

const ALL_STATUSES: OmsOrderStatus[] = [
  OmsOrderStatus.Draft,
  OmsOrderStatus.Submitted,
  OmsOrderStatus.PendingApproval,
  OmsOrderStatus.Approved,
  OmsOrderStatus.Rejected,
  OmsOrderStatus.Cancelled,
];

const EMPTY_PAGE: OmsPagedResult<OmsOrder> = {
  items: [],
  totalCount: 0,
  pageNumber: 1,
  pageSize: 10,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false,
};

export const OmsOrders: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const canView = canViewOmsOrders(user?.roles);

  // MUI TablePagination is 0-based; the API pageNumber is 1-based.
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [statusFilter, setStatusFilter] = useState<string>(searchParams.get('status') ?? '');
  const [searchInput, setSearchInput] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [onlyMine, setOnlyMine] = useState(false);

  const { data, isLoading, isError, error, refetch, isRefetching } = useQuery({
    queryKey: [
      'oms-orders',
      page + 1,
      rowsPerPage,
      statusFilter || undefined,
      searchTerm || undefined,
      onlyMine,
      user?.id,
    ],
    queryFn: () =>
      omsService.getOrders({
        pageNumber: page + 1,
        pageSize: rowsPerPage,
        status: (statusFilter || undefined) as OmsOrderStatus | undefined,
        search: searchTerm || undefined,
        requestedByUserId: onlyMine && user?.id ? user.id : undefined,
      }),
    // Only authorized users trigger the request; the API remains authoritative.
    enabled: canView,
  });

  if (!canView) {
    return (
      <EmptyState
        title="Orders unavailable"
        description="You do not have permission to view OMS orders. Please contact your administrator if you believe this is a mistake."
      />
    );
  }

  const normalized = isError ? normalizeError(error) : null;
  const paged = data ?? EMPTY_PAGE;

  const applyStatusFilter = (next: string) => {
    setStatusFilter(next);
    setPage(0);
    setSearchParams(next ? { status: next } : {}, { replace: true });
  };

  const applySearch = () => {
    setSearchTerm(searchInput.trim());
    setPage(0);
  };

  const safePage = Math.min(page, paged.totalPages > 0 ? paged.totalPages - 1 : 0);

  return (
    <Box>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'center', mb: 3, gap: 2 }}>
        <Box>
          <Typography variant="h5" fontWeight={600}>
            Orders
          </Typography>
          <Typography variant="body2" color="textSecondary">
            Order requests for your school.
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Tooltip title="Refresh list">
            <Button variant="outlined" startIcon={<RefreshIcon />} onClick={() => refetch()} disabled={isRefetching}>
              Refresh
            </Button>
          </Tooltip>
          {canManageOmsOrders(user?.roles) && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate('/oms/orders/new')}>
              New Order
            </Button>
          )}
        </Box>
      </Box>

      <Paper sx={{ p: 2, mb: 2, display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center' }}>
        <TextField
          label="Search orders"
          placeholder="Order number or title"
          size="small"
          sx={{ minWidth: 240, flexGrow: 1, maxWidth: 360 }}
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') applySearch();
          }}
        />
        <Button variant="contained" startIcon={<SearchIcon />} onClick={applySearch}>
          Search
        </Button>
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="oms-status-filter-label">Status</InputLabel>
          <Select
            labelId="oms-status-filter-label"
            label="Status"
            value={statusFilter}
            onChange={(e) => applyStatusFilter(e.target.value)}
          >
            <MenuItem value="">All statuses</MenuItem>
            {ALL_STATUSES.map((status) => (
              <MenuItem key={status} value={status}>
                {status}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <Tooltip title={onlyMine ? 'Showing only orders you requested' : 'Show only orders you requested'}>
          <Box sx={{ display: 'flex', alignItems: 'center' }}>
            <Checkbox
              checked={onlyMine}
              onChange={(e) => {
                setOnlyMine(e.target.checked);
                setPage(0);
              }}
              inputProps={{ 'aria-label': 'Only my orders' }}
            />
            <Typography variant="body2">Only my orders</Typography>
          </Box>
        </Tooltip>
      </Paper>

      {isError && normalized && (
        <Alert
          severity="error"
          sx={{ mb: 2 }}
          action={
            <Button color="inherit" size="small" onClick={() => refetch()}>
              Retry
            </Button>
          }
        >
          {normalized.message}
        </Alert>
      )}

      <Paper sx={{ width: '100%', overflow: 'hidden' }}>
        {isLoading && <Box sx={{ height: 4, display: 'flex' }}><CircularProgress size={16} sx={{ m: 0.5 }} /></Box>}

        <TableContainer>
          <Table stickyHeader aria-label="OMS orders table" size="small">
            <TableHead>
              <TableRow>
                <TableCell>Order Number</TableCell>
                <TableCell>Title</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Items</TableCell>
                <TableCell align="right">Total</TableCell>
                <TableCell>Requested By</TableCell>
                <TableCell>Created</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isError ? null : paged.items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} align="center" sx={{ py: 4 }}>
                    <Typography variant="body1" color="textSecondary">
                      {searchTerm || statusFilter || onlyMine
                        ? 'No orders match the current filters.'
                        : 'No orders yet. Create the first order to get started.'}
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                paged.items.map((order) => (
                  <TableRow
                    hover
                    key={order.id}
                    onClick={() => navigate(`/oms/orders/${order.id}`)}
                    sx={{ cursor: 'pointer', '&:last-child td, &:last-child th': { border: 0 } }}
                  >
                    <TableCell>
                      <Typography variant="body2" fontWeight={600}>
                        {order.orderNumber}
                      </Typography>
                    </TableCell>
                    <TableCell sx={{ maxWidth: 260 }}>
                      <Typography variant="body2" noWrap>
                        {order.title}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={order.statusName}
                        size="small"
                        color={omsStatusColorMap[order.statusName] ?? 'default'}
                        sx={{ fontWeight: 500 }}
                      />
                    </TableCell>
                    <TableCell align="right">{order.itemCount}</TableCell>
                    <TableCell align="right">
                      {formatOmsMoney(order.totalAmount, order.currency)}
                    </TableCell>
                    <TableCell>
                      <Typography variant="caption" color="textSecondary">
                        {shortUserId(order.requestedByUserId)}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="caption" color="textSecondary">
                        {new Date(order.createdAt).toLocaleDateString()}
                      </Typography>
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title="View order">
                        <Button
                          size="small"
                          startIcon={<ViewIcon />}
                          onClick={(e) => {
                            e.stopPropagation();
                            navigate(`/oms/orders/${order.id}`);
                          }}
                        >
                          View
                        </Button>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>

        <TablePagination
          rowsPerPageOptions={[5, 10, 25, 50]}
          component="div"
          count={paged.totalCount}
          rowsPerPage={paged.pageSize || rowsPerPage}
          page={safePage}
          onPageChange={(_event, newPage) => setPage(newPage)}
          onRowsPerPageChange={(event) => {
            setRowsPerPage(parseInt(event.target.value, 10));
            setPage(0);
          }}
        />
      </Paper>

    </Box>
  );

};
