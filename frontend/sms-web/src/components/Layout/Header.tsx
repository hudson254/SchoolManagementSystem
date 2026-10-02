import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  IconButton,
  Badge,
  Menu,
  MenuItem,
  Avatar,
  Tooltip,
  TextField,
  InputAdornment,
  Chip,
  Divider,
  List,
  ListItem,
  ListItemText,
  ListItemAvatar,
  ListItemIcon,
} from '@mui/material';
import {
  Notifications,
  Search,
  Person,
  Settings,
  Logout,
  DarkMode,
  LightMode,
  Dashboard as DashboardIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
  Warning as WarningIcon,
  ReportProblem as ReportProblemIcon,
} from '@mui/icons-material';
import { useAuth } from '../../hooks/useAuth';
import { useTheme } from '../../contexts/ThemeContext';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationService, getNotificationTimestamp } from '../../services/notification.service';
import {
  formatNotificationDate,
  getNotificationActionLabel,
  getNotificationActionUrl,
  getNotificationVisuals,
  priorityRank,
} from '../../utils/notifications';

interface NotificationItem {
  id: string;
  title: string;
  message: string;
  type: string;
  priority: string;
  isRead: boolean;
  timestamp: string;
  actionUrl: string | null;
  actionLabel: string;
  /** Severity rank, used to sort the most important unread item to the top. */
  rank: number;
}

const typeOf = (raw: string): NotificationItem['type'] => {
  switch ((raw || 'info').toLowerCase()) {
    case 'success': return 'success';
    case 'warning': return 'warning';
    case 'error': return 'error';
    default: return 'info';
  }
};

export const Header: React.FC = () => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [notificationAnchor, setNotificationAnchor] = useState<null | HTMLElement>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const { mode, toggleTheme } = useTheme();
  const queryClient = useQueryClient();

  const { data: notificationsData } = useQuery({
    queryKey: ['header-notifications'],
    queryFn: () => notificationService.getNotifications({ page: 1, pageSize: 10 }),
    // Never poll the bell for a user who is not signed in.
    enabled: !!user,
    // The panel is a live view; refetching on window focus is what makes a
    // notification raised in another tab (or while the window was in the
    // background) appear without a manual reload. This is the documented
    // substitute for real background push on a LAN-only deployment.
    refetchOnWindowFocus: true,
    staleTime: 30_000,
  });
  const { data: unreadData } = useQuery({
    queryKey: ['header-unread-count'],
    queryFn: () => notificationService.getUnreadCount(),
    enabled: !!user,
    refetchOnWindowFocus: true,
    staleTime: 30_000,
  });

  const markAllReadMutation = useMutation({
    mutationFn: () => notificationService.markAllAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['header-notifications'] });
      queryClient.invalidateQueries({ queryKey: ['header-unread-count'] });
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  // Marking a single notification read must update the bell immediately, otherwise
  // the badge stays stale until the next refetch.
  const markAsReadMutation = useMutation({
    mutationFn: (id: string) => notificationService.markAsRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['header-notifications'] });
      queryClient.invalidateQueries({ queryKey: ['header-unread-count'] });
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  const rawNotifications = notificationsData?.items || [];
  const notifications: NotificationItem[] = rawNotifications
    .map((n) => {
      // Keep the raw record so the shared presentation helpers can read the real
      // fields (priority, type) instead of the lossy 'type' bucket.
      const visuals = getNotificationVisuals(n);
      return {
        id: n.id,
        title: n.title || 'Notification',
        message: n.message || '',
        type: n.type,
        priority: visuals.severityLabel,
        isRead: !!n.isRead,
        timestamp: getNotificationTimestamp(n),
        actionUrl: getNotificationActionUrl(n),
        actionLabel: getNotificationActionLabel(n),
        rank: priorityRank(n.priority),
      };
    })
    // Most severe first so an unresolved Important/Critical item is never buried
    // under routine informational noise.
    .sort((a, b) => b.rank - a.rank);

  const unreadCount = unreadData?.count || 0;
  const hasCriticalUnread = !!unreadData?.hasCritical;

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleNotificationOpen = (event: React.MouseEvent<HTMLElement>) => {
    setNotificationAnchor(event.currentTarget);
  };

  const handleNotificationClose = () => {
    setNotificationAnchor(null);
  };

  /**
   * Opening a notification marks it read and, when it carries an action target,
   * navigates there. The target was sanitised server-side AND re-checked by
   * getNotificationActionUrl, and navigating is only a hint: the destination page
   * and its API calls still enforce authorization and tenant isolation.
   */
  const handleNotificationClick = (notification: NotificationItem) => {
    if (!notification.isRead) {
      markAsReadMutation.mutate(notification.id);
    }
    if (notification.actionUrl) {
      navigate(notification.actionUrl);
    }
    handleNotificationClose();
  };

  const handleLogout = () => {
    handleMenuClose();
    logout();
    navigate('/login');
  };

  const handleSearch = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && searchQuery) {
      navigate(`/search?q=${searchQuery}`);
    }
  };

  const getNotificationIcon = (notification: NotificationItem) => {
    // Severity drives the icon so Important/Critical items are distinguishable
    // WITHOUT relying on colour alone (the text label is rendered beside it).
    switch (notification.priority) {
      case 'Critical':
      case 'Security':
        return <ReportProblemIcon color="error" />;
      case 'Important':
        return <WarningIcon color="warning" />;
      case 'Informational':
        return <InfoIcon color="info" />;
      default:
        return <InfoIcon color="primary" />;
    }
  };

  const getInitials = () => {
    if (user) {
      return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
    }
    return 'U';
  };

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        width: '100%',
        gap: 2,
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <Box
          component="img"
          src="/logo.png"
          alt="School Management System logo"
          sx={{ height: 32, width: 'auto', objectFit: 'contain' }}
        />
        <Typography variant="h6" fontWeight={600} noWrap>
          School Management System
        </Typography>
      </Box>

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flex: 1, maxWidth: 400 }}>
        <TextField
          size="small"
          placeholder="Search..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          onKeyDown={handleSearch}
          sx={{
            width: '100%',
            '& .MuiOutlinedInput-root': {
              bgcolor: 'rgba(255,255,255,0.15)',
              borderRadius: 2,
              '& input': { color: 'white' },
              '& .MuiOutlinedInput-notchedOutline': { borderColor: 'transparent' },
              '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: 'rgba(255,255,255,0.3)' },
            },
          }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Search sx={{ color: 'rgba(255,255,255,0.7)' }} />
              </InputAdornment>
            ),
          }}
        />
      </Box>

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <Tooltip title="Toggle theme">
          <IconButton onClick={toggleTheme} sx={{ color: 'white' }}>
            {mode === 'dark' ? <LightMode /> : <DarkMode />}
          </IconButton>
        </Tooltip>

        <Tooltip title={hasCriticalUnread ? 'Notifications (action required)' : 'Notifications'}>
          <IconButton
            onClick={handleNotificationOpen}
            sx={{ color: 'white' }}
            aria-label={
              unreadCount > 0
                ? `Notifications, ${unreadCount} unread`
                : 'Notifications'
            }
          >
            <Badge
              badgeContent={unreadCount}
              color="error"
              // A non-zero unread badge must not disappear when the count is a
              // multiple of 10, and must stay visible at 0.
              max={99}
              showZero
            >
              <Notifications />
            </Badge>
          </IconButton>
        </Tooltip>

        <Tooltip title="Profile">
          <IconButton onClick={handleMenuOpen} sx={{ color: 'white' }}>
            <Avatar sx={{ width: 32, height: 32, bgcolor: 'rgba(255,255,255,0.2)' }}>
              {getInitials()}
            </Avatar>
          </IconButton>
        </Tooltip>

        <Chip
          label={user?.roles?.[0] || 'User'}
          size="small"
          sx={{
            bgcolor: 'rgba(255,255,255,0.15)',
            color: 'white',
            borderRadius: 1,
            '& .MuiChip-label': { px: 1 },
          }}
        />
      </Box>

      {/* Profile Menu */}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
        transformOrigin={{ horizontal: 'right', vertical: 'top' }}
        anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
        sx={{ mt: 1 }}
      >
        <Box sx={{ px: 2, py: 1.5, minWidth: 200 }}>
          <Typography variant="subtitle2" fontWeight={600}>
            {user?.firstName} {user?.lastName}
          </Typography>
          <Typography variant="caption" color="textSecondary">
            {user?.email}
          </Typography>
        </Box>
        <Divider />
        <MenuItem onClick={() => { handleMenuClose(); navigate('/dashboard'); }}>
          <ListItemIcon>
            <DashboardIcon fontSize="small" />
          </ListItemIcon>
          Dashboard
        </MenuItem>
        <MenuItem onClick={() => { handleMenuClose(); navigate('/profile'); }}>
          <ListItemIcon>
            <Person fontSize="small" />
          </ListItemIcon>
          Profile
        </MenuItem>
        <MenuItem onClick={() => { handleMenuClose(); navigate('/settings'); }}>
          <ListItemIcon>
            <Settings fontSize="small" />
          </ListItemIcon>
          Settings
        </MenuItem>
        <Divider />
        <MenuItem onClick={handleLogout} sx={{ color: 'error.main' }}>
          <ListItemIcon>
            <Logout fontSize="small" color="error" />
          </ListItemIcon>
          Logout
        </MenuItem>
      </Menu>

      {/* Notifications Menu */}
      <Menu
        anchorEl={notificationAnchor}
        open={Boolean(notificationAnchor)}
        onClose={handleNotificationClose}
        transformOrigin={{ horizontal: 'right', vertical: 'top' }}
        anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
        sx={{ mt: 1, maxHeight: 400, width: 380 }}
      >
        <Box sx={{ p: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="subtitle1" fontWeight={600}>
            Notifications
          </Typography>
          <Typography
            variant="caption"
            color="primary"
            sx={{ cursor: 'pointer' }}
            onClick={() => markAllReadMutation.mutate()}
          >
            Mark all as read
          </Typography>
        </Box>
        <Divider />
        <List sx={{ p: 0 }}>
          {notifications.length === 0 && (
            <ListItem sx={{ py: 2 }}>
              <ListItemText
                primary={<Typography variant="body2">No notifications</Typography>}
                secondary="You're all caught up."
              />
            </ListItem>
          )}
          {notifications.map((notification) => (
            <ListItem
              key={notification.id}
              sx={{
                bgcolor: notification.isRead ? 'transparent' : 'rgba(87, 100, 38, 0.08)',
                '&:hover': { bgcolor: 'rgba(0,0,0,0.04)' },
                cursor: 'pointer',
              }}
              onClick={() => handleNotificationClick(notification)}
            >
              <ListItemAvatar>
                <Avatar sx={{ bgcolor: 'transparent' }}>
                  {getNotificationIcon(notification)}
                </Avatar>
              </ListItemAvatar>
              <ListItemText
                primary={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography variant="body2" fontWeight={notification.isRead ? 400 : 600}>
                      {notification.title}
                    </Typography>
                    {/* Severity as TEXT, so it is not conveyed by colour alone. */}
                    {!notification.isRead && notification.priority !== 'Normal' && (
                      <Typography
                        variant="caption"
                        sx={{
                          fontWeight: 700,
                          textTransform: 'uppercase',
                          color:
                            notification.priority === 'Critical' || notification.priority === 'Security'
                              ? 'error.main'
                              : 'warning.main',
                        }}
                      >
                        {notification.priority}
                      </Typography>
                    )}
                  </Box>
                }
                secondary={
                  <>
                    <Typography variant="caption" color="textSecondary" display="block">
                      {notification.message}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      {formatNotificationDate(notification.timestamp)}
                    </Typography>
                    {notification.actionUrl && (
                      <Typography
                        variant="caption"
                        sx={{ display: 'block', color: 'primary.main', fontWeight: 600 }}
                      >
                        {notification.actionLabel}
                      </Typography>
                    )}
                  </>
                }
              />
            </ListItem>
          ))}
        </List>
        <Divider />
        <Box sx={{ p: 1, textAlign: 'center' }}>
          <Typography
            variant="caption"
            color="primary"
            sx={{ cursor: 'pointer' }}
            onClick={() => {
              handleNotificationClose();
              navigate('/notifications');
            }}
          >
            View all notifications
          </Typography>
        </Box>
      </Menu>
    </Box>
  );
};
