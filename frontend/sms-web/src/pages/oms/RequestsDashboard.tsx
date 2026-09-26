import React from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Grid,
  Paper,
  Typography,
} from '@mui/material';
import AssignmentTurnedInIcon from '@mui/icons-material/AssignmentTurnedIn';
import InboxIcon from '@mui/icons-material/Inbox';
import WorkHistoryIcon from '@mui/icons-material/WorkHistory';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import InsightsIcon from '@mui/icons-material/Insights';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { omsRequestsService, OmsRequestStatus } from '../../services/requests.service';
import { useAuth } from '../../hooks/useAuth';
import {
  canCreateOmsRequest,
  canViewAllOmsRequests,
  canViewOmsRequests,
} from '../../utils/roles';
import { normalizeError } from '../../utils/errors';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';
import { EmptyState } from '../../components/Common/EmptyState';

interface QueueCard {
  title: string;
  description: string;
  path: string;
  icon: React.ReactNode;
  color: string;
}

interface StatCard {
  label: string;
  value: number;
  status?: OmsRequestStatus;
}

/**
 * Request workspace landing page.
 *
 * Queues are role-appropriate: the privileged "Available Queue" (scope=all) is
 * only rendered for a role the backend accepts. The backend still re-checks the
 * scope on every call â€” a non-privileged caller who asks for `all` receives a
 * 403, which the list page surfaces rather than silently emptying.
 */
export const RequestsDashboard: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const roles = user?.roles;

  const canView = canViewOmsRequests(roles);
  const canViewAll = canViewAllOmsRequests(roles);
  const canCreate = canCreateOmsRequest(roles);

  const summaryQuery = useQuery({
    queryKey: ['oms-request-dashboard'],
    queryFn: () => omsRequestsService.getDashboardSummary(),
    enabled: canView,
  });

  if (!canView) {
    return (
      <EmptyState
        title="Request workspace unavailable"
        description="You do not have permission to view requests. Please contact your administrator if you believe this is a mistake."
      />
    );
  }

  const summary = summaryQuery.data;
  const error = summaryQuery.isError ? normalizeError(summaryQuery.error) : null;

  const queues: QueueCard[] = [
    {
      title: 'My Requests',
      description: 'Requests you raised, and their current progress.',
      path: '/oms/requests?scope=mine',
      icon: <AssignmentTurnedInIcon />,
      color: 'primary.main',
    },
    {
      title: 'Assigned to Me',
      description: 'Requests currently assigned to you for action.',
      path: '/oms/requests?scope=assigned',
      icon: <WorkHistoryIcon />,
      color: 'info.main',
    },
  ];

  if (canViewAll) {
    queues.push({
      title: 'Available Queue',
      description: 'The full tenant queue of requests awaiting action.',
      path: '/oms/requests?scope=all',
      icon: <InboxIcon />,
      color: 'warning.main',
    });
  }

  const statCards: StatCard[] = summary
    ? [
        { label: 'Total', value: summary.totalRequests },
        { label: 'Draft', value: summary.draftCount, status: OmsRequestStatus.Draft },
        { label: 'Submitted', value: summary.submittedCount, status: OmsRequestStatus.Submitted },
        { label: 'Pending Review', value: summary.pendingReviewCount, status: OmsRequestStatus.PendingReview },
        { label: 'Assigned', value: summary.assignedCount, status: OmsRequestStatus.Assigned },
        { label: 'Pending Approval', value: summary.pendingApprovalCount, status: OmsRequestStatus.PendingApproval },
        { label: 'Approved', value: summary.approvedCount, status: OmsRequestStatus.Approved },
        { label: 'Returned', value: summary.returnedCount, status: OmsRequestStatus.Returned },
        { label: 'In Progress', value: summary.inProgressCount, status: OmsRequestStatus.InProgress },
        { label: 'Completed', value: summary.completedCount, status: OmsRequestStatus.Completed },
        { label: 'Rejected', value: summary.rejectedCount, status: OmsRequestStatus.Rejected },
        { label: 'Cancelled', value: summary.cancelledCount, status: OmsRequestStatus.Cancelled },
      ]
    : [];

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
            Request Workspace
          </Typography>
          <Typography variant="body2" color="textSecondary">
            Track and action requests raised across Enrollment, Accommodation and Assignment.
          </Typography>
        </Box>
        {canCreate && (
          <Button
            variant="contained"
            startIcon={<AddCircleOutlineIcon />}
            onClick={() => navigate('/oms/requests/new')}
          >
            New Request
          </Button>
        )}
      </Box>

      {summaryQuery.isLoading && <LoadingSpinner message="Loading request dashboard..." />}

      {error && (
        <Alert
          severity={error.statusCode === 403 ? 'warning' : 'error'}
          sx={{ mb: 2 }}
          action={
            <Button color="inherit" size="small" onClick={() => summaryQuery.refetch()}>
              Retry
            </Button>
          }
        >
          {error.statusCode === 403
            ? 'You do not have permission to view the request summary.'
            : `${error.serverMessage || error.message} (${error.code})`}
        </Alert>
      )}

      <Grid container spacing={2} sx={{ mb: 2 }}>
        {queues.map((queue) => (
          <Grid item xs={12} md={4} key={queue.title}>
            <Card sx={{ height: '100%', borderLeft: 4, borderColor: queue.color }}>
              <CardActionArea
                onClick={() => navigate(queue.path)}
                aria-label={`Open ${queue.title}`}
                sx={{ height: '100%' }}
              >
                <CardContent>
                  <Box sx={{ color: queue.color, mb: 1, display: 'flex' }}>{queue.icon}</Box>
                  <Typography variant="h6" fontWeight={600}>
                    {queue.title}
                  </Typography>
                  <Typography variant="body2" color="textSecondary">
                    {queue.description}
                  </Typography>
                </CardContent>
              </CardActionArea>
            </Card>
          </Grid>
        ))}
      </Grid>

      {!summaryQuery.isLoading && !error && statCards.length > 0 && (
        <Paper sx={{ p: 2.5 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <InsightsIcon fontSize="small" color="action" />
            <Typography variant="h6" fontWeight={600}>
              Request Status Overview
            </Typography>
          </Box>
          <Typography variant="caption" color="textSecondary">
            Counts are calculated by the server and shown exactly as reported. Select a status to
            open the filtered queue.
          </Typography>
          <Grid container spacing={1.5} sx={{ mt: 1 }}>
            {statCards.map((card) => (
              <Grid item xs={6} sm={4} md={2} key={card.label}>
                <Card
                  variant="outlined"
                  sx={{ cursor: card.status ? 'pointer' : 'default' }}
                  onClick={
                    card.status
                      ? () => navigate(`/oms/requests?scope=all&status=${card.status}`)
                      : undefined
                  }
                  aria-label={card.status ? `View ${card.label} requests` : 'Total requests'}
                >
                  <CardContent sx={{ py: 1.5, textAlign: 'center', '&:last-child': { pb: 1.5 } }}>
                    <Typography variant="h6" fontWeight={700}>
                      {card.value}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      {card.label}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        </Paper>
      )}
    </Box>
  );
};

export default RequestsDashboard;
