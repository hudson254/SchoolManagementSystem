import React from 'react';
import { Box, List, ListItem, ListItemIcon, ListItemText, Typography } from '@mui/material';
import { CheckCircle, Cancel } from '@mui/icons-material';
import {
  PasswordRequirements,
  PasswordContext,
  getPasswordStrength,
  checkBreachedPassword,
} from '../../utils/passwordStrength';

export interface PasswordRequirementsChecklistProps {
  /** The current password value. */
  password: string;
  /** User context for blacklist checks (email, username, names, etc.). */
  context?: PasswordContext;
  /** Whether to show the optional 12+ character recommendation. */
  showRecommendation?: boolean;
  /** Whether to perform the breached-password (HIBP) check. */
  enableBreachCheck?: boolean;
}

interface RequirementItem {
  label: string;
  met: boolean;
  recommended?: boolean;
}

/**
 * Live password requirements checklist.
 *
 * Each requirement immediately transitions from red to green when satisfied.
 * Also surfaces blacklist / breached-password warnings.
 */
export const PasswordRequirementsChecklist: React.FC<PasswordRequirementsChecklistProps> = ({
  password,
  context,
  showRecommendation = true,
  enableBreachCheck = true,
}) => {
  const [breached, setBreached] = React.useState(false);

  const strength = getPasswordStrength(password, context);

  const items: RequirementItem[] = [
    { label: 'Minimum 8 characters', met: strength.requirements.minLength },
    { label: 'At least one uppercase letter (A-Z)', met: strength.requirements.hasUpper },
    { label: 'At least one lowercase letter (a-z)', met: strength.requirements.hasLower },
    { label: 'At least one number (0-9)', met: strength.requirements.hasNumber },
    { label: 'At least one special character', met: strength.requirements.hasSpecial },
  ];

  if (showRecommendation) {
    items.push({
      label: '12 or more characters (recommended)',
      met: strength.requirements.min12,
      recommended: true,
    });
  }

  // Debounced breached-password check (HIBP k-Anonymity).
  React.useEffect(() => {
    if (!enableBreachCheck || password.length < 8) {
      setBreached(false);
      return;
    }
    let active = true;
    const timer = setTimeout(async () => {
      const isBreached = await checkBreachedPassword(password);
      if (active) setBreached(isBreached);
    }, 600);
    return () => {
      active = false;
      clearTimeout(timer);
    };
  }, [password, enableBreachCheck]);

  const warnings = strength.blacklistHits;

  return (
    <Box data-testid="password-requirements">
      <List dense disablePadding>
        {items.map((item) => (
          <ListItem key={item.label} disableGutters sx={{ py: 0.25 }}>
            <ListItemIcon sx={{ minWidth: 28 }}>
              {item.met ? (
                <CheckCircle sx={{ color: 'success.main', fontSize: 18 }} />
              ) : (
                <Cancel sx={{ color: 'error.main', fontSize: 18 }} />
              )}
            </ListItemIcon>
            <ListItemText
              primary={
                <Typography
                  variant="body2"
                  sx={{
                    color: item.met ? 'success.main' : item.recommended ? 'text.secondary' : 'error.main',
                    fontWeight: item.met ? 500 : 400,
                  }}
                >
                  {item.label}
                </Typography>
              }
            />
          </ListItem>
        ))}
      </List>

      {warnings.length > 0 && (
        <Box sx={{ mt: 1 }}>
          {warnings.map((warning) => (
            <Typography key={warning} variant="caption" color="error.main" display="block">
              {warning}
            </Typography>
          ))}
        </Box>
      )}

      {breached && (
        <Typography variant="caption" color="error.main" display="block" sx={{ mt: 0.5 }} role="alert">
          This password has appeared in a known data breach. Please choose a different one.
        </Typography>
      )}
    </Box>
  );
};

export default PasswordRequirementsChecklist;
