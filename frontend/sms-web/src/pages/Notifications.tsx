import React, { useState } from 'react';
import {
  Box, Paper, Typography, List, ListItem, ListItemText, ListItemAvatar,
  Avatar, Button, Chip, Divider, Alert, IconButton, Tooltip,
} from '@mui/material';
import {
  Notifications as NotificationsIcon, Info as InfoIcon,
  Warning as WarningIcon, Error as ErrorIcon, CheckCircle as CheckCircleIcon,
  Delete as DeleteIcon, MarkEmailRead as MarkReadIcon, Refresh as RefreshIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationService, Notification } from '../services/notification.service';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

export const Notifications: React.FC = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(0);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['notifications', page],
    queryFn: () => notificationService.getNotifications({ page: page + 1, pageSize: 20 }),
  });

  const markAsReadMutation = useMutation({
    mutationFn: (id: string) => notificationService.markAsRead(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  });

  const markAllAsReadMutation = useMutation({
    mutationFn: () => notificationService.markAllAsRead(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => notificationService.deleteNotification(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  });

  const getIcon = (type: string) => {
    switch (type) {
      case 'info': return <InfoIcon />;
      case 'warning': return <WarningIcon />;
      case 'error': return <ErrorIcon />;
      case 'success': return <CheckCircleIcon />;
      default: return <NotificationsIcon />;
    }
  };

  const getColor = (type: string) => {
    switch (type) {
      case 'info': return 'info.main';
      case 'warning': return 'warning.main';
      case 'error': return 'error.main';
      case 'success': return 'success.main';
      default: return 'primary.main';
    }
  };

  const notifications = data?.items || [];
  const totalPages = data?.totalPages || 0;

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
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>Notifications</Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button variant="outlined" startIcon={<MarkReadIcon />}
            onClick={() => markAllAsReadMutation.mutate()}
            disabled={markAllAsReadMutation.isPending}>Mark All Read</Button>
          <Button variant="outlined" startIcon={<RefreshIcon />} onClick={() => refetch()}>Refresh</Button>
        </Box>
      </Box>

      <Paper sx={{ borderRadius: 2 }}>
        {notifications.length === 0 ? (
          <Box sx={{ p: 4, textAlign: 'center' }}>
            <NotificationsIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
            <Typography color="textSecondary">No notifications yet.</Typography>
          </Box>
        ) : (
          <List sx={{ p: 0 }}>
            {notifications.map((notification: Notification, index: number) => (
              <React.Fragment key={notification.id}>
                <ListItem sx={{ py: 2, bgcolor: notification.isRead ? 'transparent' : 'action.hover' }}
                  secondaryAction={
                    <Box>
                      {!notification.isRead && (
                        <Tooltip title="Mark as read">
                          <IconButton size="small" onClick={() => markAsReadMutation.mutate(notification.id)}>
                            <MarkReadIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      )}
                      <Tooltip title="Delete">
                        <IconButton size="small" onClick={() => deleteMutation.mutate(notification.id)}>
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </Box>
                  }>
                  <ListItemAvatar>
                    <Avatar sx={{ bgcolor: getColor(notification.type) }}>{getIcon(notification.type)}</Avatar>
                  </ListItemAvatar>
                  <ListItemText primary={
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography variant="body1" fontWeight={notification.isRead ? 400 : 600}>{notification.title}</Typography>
                      {!notification.isRead && <Chip label="New" size="small" color="primary" />}
                    </Box>
                  } secondary={
                    <><Typography variant="body2" color="textSecondary">{notification.message}</Typography>
                      <Typography variant="caption" color="textSecondary">{new Date(notification.createdDate).toLocaleString()}</Typography></>
                  } />
                </ListItem>
                {index < notifications.length - 1 && <Divider />}
              </React.Fragment>
            ))}
          </List>
        )}
      </Paper>

      {totalPages > 1 && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
          {Array.from({ length: totalPages }, (_, i) => (
            <Button key={i} variant={page === i ? 'contained' : 'outlined'} size="small" onClick={() => setPage(i)} sx={{ mx: 0.5 }}>{i + 1}</Button>
          ))}
        </Box>
      )}
    </Box>
  );
};
