import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box, Paper, Typography, List, ListItem, ListItemText, ListItemAvatar,
  Avatar, Button, Chip, Divider, Alert, IconButton, Tooltip, Stack,
  ToggleButton, ToggleButtonGroup, Pagination,
} from '@mui/material';
import {
  Notifications as NotificationsIcon, Info as InfoIcon,
  Warning as WarningIcon,
  Delete as DeleteIcon, MarkEmailRead as MarkReadIcon, Refresh as RefreshIcon,
  ReportProblem as ReportProblemIcon, OpenInNew as OpenInNewIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  notificationService,
  Notification,
  NOTIFICATIONS_PAGE_SIZE,
  getNotificationTimestamp,
} from '../services/notification.service';
import {
  formatNotificationDate,
  getNotificationActionLabel,
  getNotificationActionUrl,
  getNotificationVisuals,
} from '../utils/notifications';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

type ReadFilter = 'all' | 'unread' | 'read';

export const Notifications: React.FC = () => {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  // 1-based page, matching the API contract. This used to be 0-based, so the
  // component requested page 1 (i.e. API page 2) and the first page was skipped.
  const [page, setPage] = useState(1);
  const [readFilter, setReadFilter] = useState<ReadFilter>('all');

  // isRead is tri-state on the API: undefined = all, false = unread, true = read.
  const isReadParam = readFilter === 'all' ? undefined : readFilter === 'unread' ? false : true;

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['notifications', page, readFilter],
    queryFn: () =>
      notificationService.getNotifications({
        page,
        pageSize: NOTIFICATIONS_PAGE_SIZE,
        isRead: isReadParam,
      }),
    // Refresh when the user returns to the tab, so a notification raised in
    // another tab or while the window was in the background appears without a
    // manual reload. This is the documented LAN-only substitute for push.
    refetchOnWindowFocus: true,
  });

  // The header bell and this page read the same data, so a read-state change has
  // to invalidate both or the badge and the list disagree.
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['notifications'] });
    queryClient.invalidateQueries({ queryKey: ['header-notifications'] });
    queryClient.invalidateQueries({ queryKey: ['header-unread-count'] });
  };

  const markAsReadMutation = useMutation({
    mutationFn: (id: string) => notificationService.markAsRead(id),
    onSuccess: invalidate,
  });

  const markAllAsReadMutation = useMutation({
    mutationFn: () => notificationService.markAllAsRead(),
    onSuccess: invalidate,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => notificationService.deleteNotification(id),
    onSuccess: invalidate,
  });

  /**
   * Opening a notification marks it read and follows its action target.
   * The target is sanitised server-side and re-checked here; it is a navigation
   * hint only, and the destination page's API calls remain the authorization and
   * tenant-isolation boundary.
   */
  const handleOpen = (notification: Notification) => {
    if (!notification.isRead) {
      markAsReadMutation.mutate(notification.id);
    }
    const target = getNotificationActionUrl(notification);
    if (target) navigate(target);
  };

  const notifications = data?.items || [];
  const totalPages = data?.totalPages || 0;
  const totalCount = data?.totalCount || 0;

if (isLoading) return <LoadingSpinner />;

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">Failed to load notifications. Please try again.</Alert>
      </Box>
    );
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, flexWrap: 'wrap', gap: 1 }}>
        <Box>
          <Typography variant="h4" fontWeight={600}>Notifications</Typography>
          <Typography variant="body2" color="textSecondary">
            {totalCount} notification{totalCount === 1 ? '' : 's'} in your history
          </Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" startIcon={<MarkReadIcon />}
            onClick={() => markAllAsReadMutation.mutate()}
            disabled={markAllAsReadMutation.isPending}>Mark All Read</Button>
          <Button variant="outlined" startIcon={<RefreshIcon />} onClick={() => refetch()}>Refresh</Button>
        </Stack>
      </Box>

      <ToggleButtonGroup
        value={readFilter}
        exclusive
        onChange={(_e, value: ReadFilter | null) => {
          // MUI also fires on "deselect" with a null value; ignoring it keeps the
          // filter from ever landing in an undefined state.
          if (value) {
            setReadFilter(value);
            setPage(1);
          }
        }}
        size="small"
        sx={{ mb: 2 }}
      >
        <ToggleButton value="all">All</ToggleButton>
        <ToggleButton value="unread">Unread</ToggleButton>
        <ToggleButton value="read">Read</ToggleButton>
      </ToggleButtonGroup>

      <Paper sx={{ borderRadius: 2 }}>
        {notifications.length === 0 ? (
          <Box sx={{ p: 4, textAlign: 'center' }}>
            <NotificationsIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
            <Typography color="textSecondary">
              {readFilter === 'all' ? 'No notifications yet.' : `No ${readFilter} notifications.`}
            </Typography>
          </Box>
        ) : (
          <List sx={{ p: 0 }}>
            {notifications.map((notification: Notification, index: number) => (
              <NotificationRow
                key={notification.id}
                notification={notification}
                showDivider={index < notifications.length - 1}
                onOpen={() => handleOpen(notification)}
                onMarkAsRead={() => markAsReadMutation.mutate(notification.id)}
                onDelete={() => deleteMutation.mutate(notification.id)}
              />
            ))}
          </List>
        )}
      </Paper>

      {totalPages > 1 && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
          <Pagination
            count={totalPages}
            page={page}
            onChange={(_e, value) => setPage(value)}
            color="primary"
          />
        </Box>
      )}
    </Box>
  );
};

/**
 * One row of the notification centre.
 *
 * Extracted so the presentation rules (severity colour, left bar, text chips,
 * action button) live in one place and can be tested directly.
 */
const NotificationRow: React.FC<{
  notification: Notification;
  showDivider: boolean;
  onOpen: () => void;
  onMarkAsRead: () => void;
  onDelete: () => void;
}> = ({ notification, showDivider, onOpen, onMarkAsRead, onDelete }) => {
  const visuals = getNotificationVisuals(notification);
  const actionUrl = getNotificationActionUrl(notification);

  const isSevere = visuals.severityLabel === 'Critical'
    || visuals.severityLabel === 'Security'
    || visuals.severityLabel === 'Important';

  return (
    <>
      <ListItem
        sx={{
          py: 2,
          bgcolor: notification.isRead ? 'transparent' : 'action.hover',
          // The left bar encodes read state and severity WITHOUT relying on colour
          // alone; the severity chip below repeats it as text.
          borderLeft: 4,
          borderLeftColor: notification.isRead
            ? 'transparent'
            : visuals.color === 'error' ? 'error.main'
            : visuals.color === 'warning' ? 'warning.main'
            : 'primary.main',
        }}
        secondaryAction={
          <Stack direction="row" spacing={0.5}>
            {!notification.isRead && (
              <Tooltip title="Mark as read">
                <IconButton
                  size="small"
                  aria-label={`Mark "${notification.title}" as read`}
                  onClick={onMarkAsRead}
                >
                  <MarkReadIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
            <Tooltip title="Delete">
              <IconButton
                size="small"
                aria-label={`Delete "${notification.title}"`}
                onClick={onDelete}
              >
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        }
      >
        <ListItemAvatar>
          <Avatar sx={{ bgcolor: `${visuals.color}.main` }}>
            {visuals.color === 'error' ? <ReportProblemIcon />
              : visuals.color === 'warning' ? <WarningIcon />
              : <InfoIcon />}
          </Avatar>
        </ListItemAvatar>
        <ListItemText
          sx={{ pr: 12 }}
          primary={
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
              <Typography variant="body1" fontWeight={notification.isRead ? 400 : 600}>
                {notification.title}
              </Typography>
              {!notification.isRead && <Chip label="New" size="small" color="primary" />}
              {/* Severity as TEXT, so the distinction never depends on colour alone. */}
              {isSevere && (
                <Chip
                  label={visuals.severityLabel}
                  size="small"
                  variant="outlined"
                  color={visuals.color === 'error' ? 'error' : 'warning'}
                />
              )}
              {notification.isExpired && (
                <Chip label="Expired" size="small" variant="outlined" />
              )}
            </Box>
          }
          secondary={
            <>
              <Typography variant="body2" color="textSecondary">
                {notification.message}
              </Typography>
              <Typography variant="caption" color="textSecondary">
                {notification.type} &middot;{' '}
                {formatNotificationDate(getNotificationTimestamp(notification))}
              </Typography>
            </>
          }
        />
        {actionUrl && (
          <Button
            size="small"
            startIcon={<OpenInNewIcon />}
            onClick={onOpen}
            sx={{ mt: 1, alignSelf: 'flex-start', ml: 7 }}
          >
            {getNotificationActionLabel(notification)}
          </Button>
        )}
      </ListItem>
      {showDivider && <Divider />}
    </>
  );
};
