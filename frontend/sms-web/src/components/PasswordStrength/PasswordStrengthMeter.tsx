import React from 'react';
import { Box, LinearProgress, Typography } from '@mui/material';
import { PasswordLevel } from '../../utils/passwordStrength';

export interface PasswordStrengthMeterProps {
  /** 0-100 strength score. */
  score: number;
  /** Current strength level. */
  level: PasswordLevel;
  /** Optional label shown next to the bar. */
  label?: string;
}

const LEVEL_COLORS: Record<PasswordLevel, string> = {
  Weak: '#f44336',
  Medium: '#ff9800',
  Strong: '#2196f3',
  'Very Strong': '#4caf50',
};

const LEVEL_LABELS: Record<PasswordLevel, string> = {
  Weak: 'Weak',
  Medium: 'Medium',
  Strong: 'Strong',
  'Very Strong': 'Very Strong',
};

/**
 * Animated password strength meter.
 *
 * - Smoothly animates the bar width as the password improves.
 * - Color-coded by strength level with a clear label for screen readers.
 * - Supports light/dark themes and responsive layout.
 */
export const PasswordStrengthMeter: React.FC<PasswordStrengthMeterProps> = ({
  score,
  level,
  label,
}) => {
  const normalizedScore = Math.max(0, Math.min(100, score));
  const color = LEVEL_COLORS[level] || LEVEL_COLORS.Weak;

  return (
    <Box sx={{ mt: 1, mb: 1 }} role="group" aria-label="Password strength">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
        <Typography variant="caption" color="text.secondary">
          {label || 'Password strength'}
        </Typography>
        <Typography
          variant="caption"
          sx={{ color, fontWeight: 600 }}
          aria-live="polite"
        >
          {LEVEL_LABELS[level]}
        </Typography>
      </Box>
      <LinearProgress
        variant="determinate"
        value={normalizedScore}
        aria-label={`Password strength: ${LEVEL_LABELS[level]}`}
        sx={{
          height: 8,
          borderRadius: 4,
          backgroundColor: (theme) =>
            theme.palette.mode === 'dark' ? '#333333' : '#e0e0e0',
          '& .MuiLinearProgress-bar': {
            backgroundColor: color,
            transition: 'width 0.4s ease-in-out',
          },
        }}
      />
    </Box>
  );
};

export default PasswordStrengthMeter;
