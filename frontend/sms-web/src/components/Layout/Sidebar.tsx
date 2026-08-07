import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  Box,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Divider,
  Typography,
  Avatar,
  Tooltip,
  Collapse,
  Chip,
} from '@mui/material';
import {
  Dashboard,
  People,
  School,
  Book,
  Assignment,
  Event,
  Person,
  Settings,
  Logout,
  Bed,
  Grading,
  Assessment,
  Notifications,
  CalendarMonth,
  ExpandLess,
  ExpandMore,
  MenuBook,
  Class,
  Schedule,
  EventNote,
} from '@mui/icons-material';
import { useAuth } from '../../hooks/useAuth';

interface SidebarProps {
  onClose?: () => void;
}

interface MenuItem {
  text: string;
  icon: React.ReactNode;
  path?: string;
  roles: string[];
  children?: MenuItem[];
}

const menuItems: MenuItem[] = [
  { text: 'Dashboard', icon: <Dashboard />, path: '/dashboard', roles: ['all'] },
  {
    text: 'Students',
    icon: <People />,
    path: '/students',
    roles: ['all'],
  },
  {
    text: 'Lecturers',
    icon: <School />,
    path: '/lecturers',
    roles: ['all'],
  },
  {
    text: 'Academic',
    icon: <MenuBook />,
    roles: ['all'],
    children: [
      { text: 'Courses', icon: <Book />, path: '/courses', roles: ['all'] },
      { text: 'Course Offerings', icon: <EventNote />, path: '/course-offerings', roles: ['all'] },
      { text: 'Units', icon: <Assignment />, path: '/units', roles: ['all'] },
      { text: 'Classes', icon: <Class />, path: '/classes', roles: ['all'] },
    ],
  },
  {
    text: 'Timetable',
    icon: <Schedule />,
    path: '/timetable',
    roles: ['all'],
  },
  {
    text: 'Assignments',
    icon: <Grading />,
    path: '/assignments',
    roles: ['all'],
  },
  {
    text: 'Grades',
    icon: <Assessment />,
    path: '/grades',
    roles: ['all'],
  },
  {
    text: 'Accommodation',
    icon: <Bed />,
    path: '/accommodation',
    roles: ['receptionist', 'administrator'],
  },
  {
    text: 'Calendar',
    icon: <CalendarMonth />,
    path: '/calendar',
    roles: ['all'],
  },
  {
    text: 'Users',
    icon: <Person />,
    path: '/users',
    roles: ['administrator'],
  },
  {
    text: 'Notifications',
    icon: <Notifications />,
    path: '/notifications',
    roles: ['all'],
  },
  {
    text: 'Settings',
    icon: <Settings />,
    path: '/settings',
    roles: ['administrator'],
  },
];

export const Sidebar: React.FC<SidebarProps> = ({ onClose }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuth();
  const [openMenus, setOpenMenus] = useState<Record<string, boolean>>({});

  const handleNavigation = (path: string) => {
    navigate(path);
    if (onClose) onClose();
  };

  const handleLogout = () => {
    logout();
    if (onClose) onClose();
  };

  const handleToggleMenu = (text: string) => {
    setOpenMenus((prev) => ({ ...prev, [text]: !prev[text] }));
  };

  const hasRole = (roles: string[]) => {
    if (roles.includes('all')) return true;
    return roles.some(role => user?.roles?.includes(role));
  };

  const getInitials = () => {
    if (user) {
      return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
    }
    return 'U';
  };

  const renderMenuItem = (item: MenuItem, depth: number = 0) => {
    if (!hasRole(item.roles)) return null;

    const hasChildren = item.children && item.children.length > 0;
    const isActive = item.path ? location.pathname.startsWith(item.path) : false;
    const isOpen = openMenus[item.text] || false;

    return (
      <Box key={item.text}>
        <ListItem disablePadding sx={{ pl: depth * 2 }}>
          <ListItemButton
            onClick={() => {
              if (hasChildren) {
                handleToggleMenu(item.text);
              } else if (item.path) {
                handleNavigation(item.path);
              }
            }}
            sx={{
              mx: 1,
              borderRadius: 2,
              bgcolor: isActive ? 'rgba(87, 100, 38, 0.12)' : 'transparent',
              '&:hover': {
                bgcolor: isActive ? 'rgba(87, 100, 38, 0.18)' : 'rgba(0, 0, 0, 0.04)',
              },
              minHeight: 44,
            }}
            selected={isActive}
          >
            <ListItemIcon sx={{ color: isActive ? '#576426' : 'inherit', minWidth: 36 }}>
              {item.icon}
            </ListItemIcon>
            <ListItemText
              primary={item.text}
              primaryTypographyProps={{
                fontWeight: isActive ? 600 : 400,
                color: isActive ? '#576426' : 'inherit',
                fontSize: '0.9rem',
              }}
            />
            {hasChildren && (
              <Box component="span" sx={{ ml: 1 }}>
                {isOpen ? <ExpandLess /> : <ExpandMore />}
              </Box>
            )}
          </ListItemButton>
        </ListItem>
        {hasChildren && (
          <Collapse in={isOpen} timeout="auto" unmountOnExit>
            <List disablePadding>
              {item.children!.map((child) => renderMenuItem(child, depth + 1))}
            </List>
          </Collapse>
        )}
      </Box>
    );
  };

  const userRoleDisplay = user?.roles?.join(', ') || 'User';

  return (
    <Box sx={{ height: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Box sx={{ p: 3, textAlign: 'center' }}>
        <Typography
          variant="h6"
          sx={{
            color: '#576426',
            fontWeight: 700,
          }}
        >
          School Management
        </Typography>
        <Typography variant="caption" color="textSecondary">
          v1.0.0
        </Typography>
      </Box>

      <Divider />

      <Box sx={{ p: 2, display: 'flex', alignItems: 'center', gap: 2 }}>
        <Avatar sx={{ bgcolor: '#576426', width: 48, height: 48 }}>
          {getInitials()}
        </Avatar>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography variant="body2" fontWeight={600} noWrap>
            {user?.firstName} {user?.lastName}
          </Typography>
          <Chip
            label={userRoleDisplay}
            size="small"
            sx={{
              height: 20,
              fontSize: '0.65rem',
              bgcolor: 'rgba(87, 100, 38, 0.12)',
              color: '#576426',
            }}
          />
        </Box>
      </Box>

      <Divider />

      <List sx={{ flexGrow: 1, overflowY: 'auto', py: 2 }}>
        {menuItems.map((item) => renderMenuItem(item))}
      </List>

      <Divider />

      <List sx={{ p: 1 }}>
        <ListItem disablePadding>
          <ListItemButton
            onClick={handleLogout}
            sx={{
              mx: 1,
              borderRadius: 2,
              '&:hover': {
                bgcolor: 'rgba(211, 47, 47, 0.08)',
              },
            }}
          >
            <ListItemIcon>
              <Logout sx={{ color: '#d32f2f' }} />
            </ListItemIcon>
            <ListItemText
              primary="Logout"
              primaryTypographyProps={{
                color: '#d32f2f',
                fontWeight: 500,
              }}
            />
          </ListItemButton>
        </ListItem>
      </List>
    </Box>
  );
};
