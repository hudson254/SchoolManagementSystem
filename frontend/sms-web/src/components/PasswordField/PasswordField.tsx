import React, { useState } from 'react';
import {
  TextField,
  InputAdornment,
  IconButton,
  TextFieldProps,
} from '@mui/material';
import { Visibility, VisibilityOff, Lock } from '@mui/icons-material';

export interface PasswordFieldProps extends Omit<TextFieldProps, 'type'> {
  /** Label shown above the field. Defaults to "Password". */
  label?: string;
  /** Independent show/hide toggle (won't affect other password fields). */
  defaultVisible?: boolean;
  /** Optional start adornment icon (defaults to a Lock icon). */
  showStartAdornment?: boolean;
}

/**
 * Reusable password input with an independent show/hide eye toggle.
 *
 * - Rounded field matching the system theme.
 * - Supports light/dark themes, responsive layout, keyboard + screen reader.
 * - Uses `autocomplete="new-password"` for registration (never prefill).
 * - Passwords are never logged or stored.
 */
export const PasswordField: React.FC<PasswordFieldProps> = ({
  label = 'Password',
  defaultVisible = false,
  showStartAdornment = true,
  InputProps,
  inputProps,
  ...rest
}) => {
  const [visible, setVisible] = useState(defaultVisible);

  return (
    <TextField
      {...rest}
      label={label}
      type={visible ? 'text' : 'password'}
      autoComplete="new-password"
      inputProps={{
        ...inputProps,
        'aria-label': rest['aria-label'] || label,
        'aria-autocomplete': 'list',
      }}
      InputProps={{
        ...InputProps,
        startAdornment: showStartAdornment ? (
          <InputAdornment position="start">
            <Lock color="action" />
          </InputAdornment>
        ) : (
          InputProps?.startAdornment
        ),
        endAdornment: (
          <InputAdornment position="end">
            <IconButton
              aria-label={visible ? 'Hide password' : 'Show password'}
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
  );
};

export default PasswordField;
