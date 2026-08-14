import React, { useState } from 'react';
import {
  TextField,
  InputAdornment,
  IconButton,
  TextFieldProps,
  Box,
  Typography,
} from '@mui/material';
import { Visibility, VisibilityOff, Lock, CheckCircle, Cancel } from '@mui/icons-material';

export interface ConfirmPasswordFieldProps extends Omit<TextFieldProps, 'type'> {
  /** The original password value to compare against. */
  password: string;
  /** Label shown above the field. Defaults to "Confirm Password". */
  label?: string;
  /** Whether to show the live match indicator. Defaults to true. */
  showMatchIndicator?: boolean;
}

/**
 * Confirm-password field with live match validation.
 *
 * - Independent show/hide visibility toggle.
 * - Displays ✓ Passwords match / ✗ Passwords do not match in real time.
 * - Keyboard and screen-reader accessible.
 * - Uses `autocomplete="new-password"` for registration.
 */
export const ConfirmPasswordField: React.FC<ConfirmPasswordFieldProps> = ({
  password,
  label = 'Confirm Password',
  showMatchIndicator = true,
  InputProps,
  inputProps,
  value,
  ...rest
}) => {
  const [visible, setVisible] = useState(false);
  const currentValue = (value as string) || '';
  const hasValue = currentValue.length > 0;
  const matches = hasValue && currentValue === password;

  return (
    <Box>
      <TextField
        {...rest}
        value={value}
        label={label}
        type={visible ? 'text' : 'password'}
        autoComplete="new-password"
        error={rest.error || (hasValue && !matches)}
        helperText={
          rest.helperText ||
          (showMatchIndicator && hasValue
            ? matches
              ? 'Passwords match'
              : 'Passwords do not match'
            : '')
        }
        FormHelperTextProps={{
          sx: {
            color: hasValue ? (matches ? 'success.main' : 'error.main') : undefined,
          },
        }}
        inputProps={{
          ...inputProps,
          'aria-label': rest['aria-label'] || label,
          'aria-invalid': hasValue && !matches,
        }}
        InputProps={{
          ...InputProps,
          startAdornment: (
            <InputAdornment position="start">
              <Lock color="action" />
            </InputAdornment>
          ),
          endAdornment: (
            <InputAdornment position="end">
              {showMatchIndicator && hasValue && (
                <Box component="span" sx={{ mr: 0.5, display: 'inline-flex', alignItems: 'center' }} aria-hidden="true">
                  {matches ? (
                    <CheckCircle sx={{ color: 'success.main', fontSize: 20 }} />
                  ) : (
                    <Cancel sx={{ color: 'error.main', fontSize: 20 }} />
                  )}
                </Box>
              )}
              <IconButton
                aria-label={visible ? 'Hide confirm password' : 'Show confirm password'}
                aria-pressed={visible}
                onClick={() => setVisible((v) => !v)}
                onMouseDown={(e) => e.preventDefault()}
                edge="end"
                size="small"
              >
                {visible ? <VisibilityOff /> : <Visibility />}
              </IconButton>
            </InputAdornment>
          ),
        }}
      />
      {showMatchIndicator && hasValue && (
        <Typography
          variant="caption"
          sx={{ color: matches ? 'success.main' : 'error.main', display: 'block', mt: 0.5 }}
          role="status"
          aria-live="polite"
        >
          {matches ? '✓ Passwords match' : '✗ Passwords do not match'}
        </Typography>
      )}
    </Box>
  );
};

export default ConfirmPasswordField;
