import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  IconButton,
  Collapse,
  Alert,
  Stack,
  Divider,
} from '@mui/material';
import {
  InstallMobile as InstallMobileIcon,
  Close as CloseIcon,
  IosShare as IosShareIcon,
  MenuBook as MenuBookIcon,
  AddToHomeScreen as AddToHomeScreenIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { usePwaInstall } from '../../hooks/usePwaInstall';

/**
 * Install prompt for the School Management System PWA.
 *
 * Behaviour, matching the requirement to be clear, unobtrusive, responsive and
 * respectful of the user's choice:
 *  - mounted only from the Layout, which sits behind ProtectedRoute, so it never
 *    interrupts the login screen;
 *  - hides permanently once the app is installed, and its dismissal is persisted so
 *    the user is not nagged on every visit;
 *  - on Chromium it calls the real native install dialog. On iOS/iPadOS, where
 *    `beforeinstallprompt` does not exist, it shows accurate manual instructions
 *    rather than a button that cannot work.
 *
 * Nothing private is displayed: the copy is static and contains no user data.
 */
export const InstallPrompt: React.FC = () => {
  const {
    isInstalled,
    canInstall,
    isDismissed,
    platform,
    status,
    install,
    dismiss,
    resetDismissal,
  } = usePwaInstall();

  // Expanded instructions, so the banner itself stays one compact row.
  const [showHelp, setShowHelp] = useState(false);

  // Already installed, or the user said no. Never render in either case.
  if (isInstalled || isDismissed) return null;

  // Nothing to offer: no install API on this browser and it is not iOS. Showing a
  // banner that cannot be actioned is worse than showing nothing.
  if (status === 'unavailable') return null;

  const isIos = platform === 'ios';

  const handleInstall = async () => {
    if (isIos) {
      setShowHelp((v) => !v);
      return;
    }
    await install();
  };

  return (
    <Box sx={{ px: { xs: 2, sm: 3 }, pt: { xs: 1, sm: 2 } }}>
      <Paper
        elevation={0}
        variant="outlined"
        role="status"
        sx={{
          p: { xs: 1.5, sm: 2 },
          borderRadius: 2,
          borderColor: 'primary.light',
          bgcolor: 'action.hover',
        }}
      >
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1.5}
          alignItems={{ xs: 'stretch', sm: 'center' }}
        >
          <InstallMobileIcon color="primary" sx={{ alignSelf: { xs: 'flex-start', sm: 'center' } }} />

          <Box sx={{ flexGrow: 1, minWidth: 0 }}>
            <Typography variant="subtitle1" fontWeight={600}>
              Install School Management System
            </Typography>
            <Typography variant="body2" color="textSecondary">
              Install the School Management System on your device for faster access and important
              system notifications.
            </Typography>
          </Box>

          <Stack direction="row" spacing={1} sx={{ flexShrink: 0 }}>
            <Button
              variant="contained"
              onClick={handleInstall}
              disabled={!canInstall && !isIos}
              startIcon={isIos ? <IosShareIcon /> : <InstallMobileIcon />}
              sx={{ whiteSpace: 'nowrap' }}
            >
              Install
            </Button>

            {/* Instructions stay reachable even after a dismissal, so a user who
                declined once is not locked out of finding out how to install. */}
            <IconButton
              aria-label="Installation instructions"
              onClick={() => {
                setShowHelp((v) => !v);
                resetDismissal();
              }}
            >
              <MenuBookIcon />
            </IconButton>

            <IconButton aria-label="Dismiss install prompt" onClick={dismiss}>
              <CloseIcon />
            </IconButton>
          </Stack>
        </Stack>

        <Collapse in={showHelp || isIos} unmountOnExit>
          <Divider sx={{ my: 1.5 }} />
          <InstallInstructions isIos={isIos} />
        </Collapse>

        {!canInstall && !isIos && (
          <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mt: 1 }}>
            This browser has not offered an install prompt yet. Use the instructions above, or open
            the site in Chrome or Edge.
          </Typography>
        )}
      </Paper>
    </Box>
  );
};

/**
 * Manual installation instructions.
 *
 * On iOS these are the ONLY route: the platform has no programmatic install API, so
 * showing an "Install" button that silently did nothing would be dishonest.
 */
const InstallInstructions: React.FC<{ isIos: boolean }> = ({ isIos }) => (
  <Alert severity="info" icon={false} sx={{ bgcolor: 'transparent', p: 0 }}>
    <Typography variant="body2" fontWeight={600} gutterBottom>
      {isIos ? 'Install on iPhone / iPad' : 'How to install'}
    </Typography>
    <Box component="ol" sx={{ pl: 2.5, m: 0 }}>
      {isIos ? (
        <>
          <Typography component="li" variant="body2">
            Tap the <strong>Share</strong> button in Safari.
          </Typography>
          <Typography component="li" variant="body2">
            Scroll down and choose <strong>Add to Home Screen</strong>.
          </Typography>
          <Typography component="li" variant="body2">
            Tap <strong>Add</strong>. The app then opens from your home screen like any other
            application.
          </Typography>
        </>
      ) : (
        <>
          <Typography component="li" variant="body2">
            Select <strong>Install</strong> above and accept the browser confirmation.
          </Typography>
          <Typography component="li" variant="body2">
            Alternatively open the browser menu and choose{' '}
            <strong>Install School Management System</strong> or <strong>Add to Home screen</strong>.
          </Typography>
          <Typography component="li" variant="body2">
            Already done? Open the installed application from your desktop or home screen.
          </Typography>
        </>
      )}
    </Box>
    <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mt: 1 }}>
      {isIos
        ? 'iOS does not offer a programmatic install prompt, so this manual step is required.'
        : 'Installation requires a secure (HTTPS) connection.'}
    </Typography>
  </Alert>
);

/**
 * Update banner for an installed PWA.
 *
 * Without this a user with the app installed would keep running the previous
 * frontend build indefinitely: the new worker downloaded but nobody told them, and
 * the old worker would not step aside. This is what makes a deployment actually
 * reach installed users.
 */
export const PwaUpdateBanner: React.FC<{
  updateAvailable: boolean;
  onApplyUpdate: () => Promise<void>;
}> = ({ updateAvailable, onApplyUpdate }) => {
  if (!updateAvailable) return null;

  return (
    <Box sx={{ px: { xs: 2, sm: 3 }, pt: { xs: 1, sm: 2 } }}>
      <Alert
        severity="info"
        icon={<RefreshIcon />}
        action={
          <Button color="inherit" size="small" onClick={onApplyUpdate}>
            Reload
          </Button>
        }
        sx={{ borderRadius: 2 }}
      >
        A new version of the School Management System is available. Reload to update.
      </Alert>
    </Box>
  );
};

/** Small badge shown in the header once the app is running standalone. */
export const InstalledAppBadge: React.FC = () => {
  const { isInstalled } = usePwaInstall();
  if (!isInstalled) return null;

  return (
    <Typography
      variant="caption"
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 0.5,
        color: 'rgba(255,255,255,0.85)',
      }}
    >
      <AddToHomeScreenIcon fontSize="inherit" />
      Installed
    </Typography>
  );
};