import React from 'react';
import {
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Grid,
  Paper,
  Typography,
  Alert,
  Divider,
  Tooltip,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  ListAlt as ListAltIcon,
  ReceiptLong as ReceiptLongIcon,
  PendingActions as PendingActionsIcon,
  CheckCircle as CheckCircleIcon,
  Cancel as CancelIcon,
  EditNote as EditNoteIcon,
  Schedule as ScheduleIcon,
  Send as SendIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { omsService, OmsOrderStatus, OmsDashboardSummary } from '../../services/oms.service';
import { useAuth } from '../../hooks/useAuth';
import { canViewOmsOrders } from '../../utils/roles';
import { normalizeError } from '../../utils/errors';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';
import { EmptyState } from '../../components/Common/EmptyState';

/** Formats a server-reported amount for display only (never recomputed). */
export const formatOmsMoney = (amount: number, currency: string): string => {
  try {
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency,
      minimumFractionDigits: 2,
    }).format(amount);
  } catch {
    return `${currency} ${amount.toFixed(2)}`;
  }
};

interface StatusCardConfig {
  label: string;
  count: number;
  color: string;
  icon: React.ReactNode;
  filterStatus?: OmsOrderStatus;
}

const buildStatusCards = (summary: OmsDashboardSummary): StatusCardConfig[] => [
  { label: 'Total Orders', count: summary.totalOrders, color: 'primary.main', icon: <ReceiptLongIcon /> },
  { label: 'Draft', count: summary.draftCount, color: 'text.secondary', icon: <EditNoteIcon />, filterStatus: OmsOrderStatus.Draft },
  { label: 'Submitted', count: summary.submittedCount, color: 'info.main', icon: <SendIcon />, filterStatus: OmsOrderStatus.Submitted },
  { label: 'Pending Approval', count: summary.pendingApprovalCount, color: 'warning.main', icon: <PendingActionsIcon />, filterStatus: OmsOrderStatus.PendingApproval },
  { label: 'Approved', count: summary.approvedCount, color: 'success.main', icon: <CheckCircleIcon />, filterStatus: OmsOrderStatus.Approved },
  { label: 'Rejected', count: summary.rejectedCount, color: 'error.main', icon: <CancelIcon />, filterStatus: OmsOrderStatus.Rejected },
  { label: 'Cancelled', count: summary.cancelledCount, color: 'text.disabled', icon: <ScheduleIcon />, filterStatus: OmsOrderStatus.Cancelled },
];

export const OmsDashboard: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const canView = canViewOmsOrders(user?.roles);

  const { data, isLoading, isError, error, refetch, isRefetching } = useQuery({
    queryKey: ['oms-dashboard-summary'],
    queryFn: () => omsService.getDashboardSummary(),
    // Only authorized users trigger the request; the API remains authoritative.
    enabled: canView,
  });

  if (!canView) {
    return (
      <EmptyState
        title="Order management unavailable"
        description="You do not have permission to view OMS orders. Please contact your administrator if you believe this is a mistake."
      />
    );
  }

  if (isLoading) {
    return <LoadingSpinner message="Loading OMS dashboard..." />;
  }

  if (isError || !data) {
    const normalized = normalizeError(error);
    return (
      <Box>
        <Typography variant="h5" fontWeight={600} gutterBottom>
          OMS Dashboard
        </Typography>
        <Alert
          severity="error"
          action={
            <Button color="inherit" size="small" onClick={() => refetch()}>
              Retry
            </Button>
          }
        >
          {normalized.message}
        </Alert>
      </Box>
    );
  }

  const cards = buildStatusCards(data);
  const currencyEntries = Object.entries(data.totalsByCurrency ?? {});

  return (
    <Box>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'center', mb: 3, gap: 2 }}>
        <Box>
          <Typography variant="h5" fontWeight={600}>
            OMS Dashboard
          </Typography>
          <Typography variant="body2" color="textSecondary">
            Order management overview for your school.
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Tooltip title="Refresh summary">
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={() => refetch()}
              disabled={isRefetching}
            >
              Refresh
            </Button>
          </Tooltip>
          <Button variant="contained" startIcon={<ListAltIcon />} onClick={() => navigate('/oms/orders')}>
            View Orders
          </Button>
        </Box>
      </Box>

      <Grid container spacing={2}>
        {cards.map((card) => (
          <Grid item xs={6} sm={4} md={3} lg={1.7143} key={card.label}>
            <Card sx={{ height: '100%', borderTop: 3, borderColor: card.color }}>
              <CardActionArea
                onClick={() =>
                  card.filterStatus
                    ? navigate(`/oms/orders?status=${card.filterStatus}`)
                    : navigate('/oms/orders')
                }
                sx={{ height: '100%' }}
                aria-label={card.filterStatus ? `View ${card.label.toLowerCase()} orders` : 'View all orders'}
              >
                <CardContent sx={{ textAlign: 'center', py: 2, '&:last-child': { pb: 2 } }}>
                  <Box sx={{ color: card.color, mb: 0.5 }}>{card.icon}</Box>
                  <Typography variant="h5" fontWeight={700}>
                    {card.count}
                  </Typography>
                  <Typography variant="caption" color="textSecondary">
                    {card.label}
                  </Typography>
                </CardContent>
              </CardActionArea>
            </Card>
          </Grid>
        ))}

        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 2.5, height: '100%' }}>
            <Typography variant="h6" fontWeight={600} gutterBottom>
              Order Totals by Currency
            </Typography>
            <Divider sx={{ mb: 2 }} />
            {currencyEntries.length === 0 ? (
              <Typography variant="body2" color="textSecondary">
                No order totals recorded yet.
              </Typography>
            ) : (
              currencyEntries
                .sort(([a], [b]) => a.localeCompare(b))
                .map(([currency, amount]) => (
                  <Box
                    key={currency}
                    sx={{ display: 'flex', justifyContent: 'space-between', py: 1 }}
                  >
                    <Typography variant="body1" fontWeight={500}>
                      {currency}
                    </Typography>
                    <Typography variant="body1" fontWeight={600}>
                      {formatOmsMoney(amount, currency)}
                    </Typography>
                  </Box>
                ))
            )}
            <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mt: 1 }}>
              Totals are calculated by the server and shown as reported.
            </Typography>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );

};
