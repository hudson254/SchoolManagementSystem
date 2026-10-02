import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Box, Drawer, AppBar, Toolbar, IconButton, useMediaQuery, useTheme } from '@mui/material';
import { Menu as MenuIcon } from '@mui/icons-material';
import { Header } from './Header';
import { Sidebar } from './Sidebar';
import { Footer } from './Footer';
import { InstallPrompt, PwaUpdateBanner } from '../Pwa/InstallPrompt';
import { useServiceWorker } from '../../hooks/useServiceWorker';
import { useNotificationRealtime } from '../../hooks/useNotificationRealtime';
import { useAuth } from '../../hooks/useAuth';

const drawerWidth = 280;

export const Layout: React.FC = () => {
  const [mobileOpen, setMobileOpen] = useState(false);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
  const { user } = useAuth();

  // Service worker lifecycle for the installed PWA. Surfacing an available update
  // is what stops an installed user being pinned to a stale frontend build.
  const { updateAvailable, applyUpdate } = useServiceWorker();

  // Live notification delivery. Mounted here - once, inside the authenticated
  // shell - so there is exactly ONE SignalR connection per signed-in session
  // rather than one per page. The hook is gated on `user`, so it starts on login
  // and its cleanup closes the socket on logout/unmount.
  useNotificationRealtime(!!user);

  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', flexDirection: 'column' }}>
      <AppBar
        position="fixed"
        sx={{
          width: { sm: `calc(100% - ${drawerWidth}px)` },
          ml: { sm: `${drawerWidth}px` },
          bgcolor: 'primary.main',
          zIndex: theme.zIndex.drawer + 1,
        }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            aria-label="open drawer"
            edge="start"
            onClick={handleDrawerToggle}
            sx={{ mr: 2, display: { sm: 'none' } }}
          >
            <MenuIcon />
          </IconButton>
          <Header />
        </Toolbar>
      </AppBar>

      <Box
        component="nav"
        sx={{ width: { sm: drawerWidth }, flexShrink: { sm: 0 } }}
        aria-label="mailbox folders"
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', sm: 'none' },
            '& .MuiDrawer-paper': {
              boxSizing: 'border-box',
              width: drawerWidth,
            },
          }}
        >
          <Sidebar onClose={handleDrawerToggle} />
        </Drawer>

        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', sm: 'block' },
            '& .MuiDrawer-paper': {
              boxSizing: 'border-box',
              width: drawerWidth,
            },
          }}
          open
        >
          <Sidebar />
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 0,
          width: { sm: `calc(100% - ${drawerWidth}px)` },
          ml: { sm: `${drawerWidth}px` },
          mt: '64px',
          display: 'flex',
          flexDirection: 'column',
          minHeight: 'calc(100vh - 64px)',
        }}
      >
        <Box sx={{ flexGrow: 1, p: { xs: 2, sm: 3 } }}>
          {/* Install and update prompts live inside the authenticated layout, so
              they never appear on the login screen and require a signed-in user. */}
          <InstallPrompt />
          <PwaUpdateBanner updateAvailable={updateAvailable} onApplyUpdate={applyUpdate} />
          <Outlet />
        </Box>
        <Footer />
      </Box>
    </Box>
  );
};