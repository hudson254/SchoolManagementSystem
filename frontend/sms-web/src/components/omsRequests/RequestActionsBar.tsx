import React, { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  MenuItem,
  TextField,
  Typography,
} from '@mui/material';
import type {
  OmsRequestActionContext,
  OmsRequestActionDescriptor,
} from '../../utils/omsRequestLifecycle';
import { getAvailableOmsRequestActions } from '../../utils/omsRequestLifecycle';

/** Maximums copied from the FluentValidation validators of each command. */
const REASON_MAX = 1000;

export interface RequestActionSubmitPayload {
  reason?: string;
  notes?: string;
  assigneeId?: string;
}

interface RequestActionsBarProps {
  context: OmsRequestActionContext;
  submitting: boolean;
  onAction: (
    descriptor: OmsRequestActionDescriptor,
    payload: RequestActionSubmitPayload,
  ) => Promise<unknown>;
  /** Candidate assignees, already authorized by the caller. Empty = free text. */
  assigneeOptions?: { id: string; label: string }[];
}

const isAssigneeFieldFor = (descriptor?: OmsRequestActionDescriptor | null): boolean =>
  descriptor?.requiredField === 'assignedUserId' ||
  descriptor?.requiredField === 'newAssignedUserId';

/**
 * Renders only the lifecycle actions that are actually valid right now.
 *
 * The action set is derived by `getAvailableOmsRequestActions`, which mirrors
 * the server's status guards, role gates and ownership rules. That derivation is
 * a presentation concern: the API re-validates every one of these transitions,
 * and the parent surface surfaces any 403/409 verbatim rather than trusting this
 * component's judgement.
 */
export const RequestActionsBar: React.FC<RequestActionsBarProps> = ({
  context,
  submitting,
  onAction,
  assigneeOptions = [],
}) => {
  const [openAction, setOpenAction] = useState<OmsRequestActionDescriptor | null>(null);
  const [reason, setReason] = useState('');
  const [assigneeId, setAssigneeId] = useState('');
  const [dialogError, setDialogError] = useState<string | null>(null);

  const actions = getAvailableOmsRequestActions(context);

  if (actions.length === 0) {
    return (
      <Box data-testid="request-actions">
        <Alert severity="info">
          No actions are available to you on this request in its current status.
        </Alert>
      </Box>
    );
  }

  const resetDialog = () => {
    setOpenAction(null);
    setReason('');
    setAssigneeId('');
    setDialogError(null);
  };

  const handleButtonClick = async (descriptor: OmsRequestActionDescriptor) => {
    if (!descriptor.requiresInput) {
      // No input required: fire immediately. The server decides whether it is
      // still valid, and any rejection is surfaced by the parent.
      await onAction(descriptor, {});
      return;
    }
    setDialogError(null);
    setReason('');
    setAssigneeId('');
    setOpenAction(descriptor);
  };

  const handleDialogSubmit = async () => {
    if (!openAction) return;
    setDialogError(null);

    const trimmedReason = reason.trim();
    const isAssigneeField = isAssigneeFieldFor(openAction);

    // Client-side guard mirrors the server validator. The server remains the
    // authority â€” this only avoids a guaranteed round-trip rejection.
    if (isAssigneeField && !assigneeId.trim()) {
      setDialogError(
        openAction.requiredField === 'assignedUserId'
          ? 'An assignee is required.'
          : 'A new assignee is required.',
      );
      return;
    }
    if (!isAssigneeField && openAction.requiredField && !trimmedReason) {
      setDialogError('A reason is required.');
      return;
    }
    if (trimmedReason.length > REASON_MAX) {
      setDialogError(`Must be ${REASON_MAX} characters or fewer.`);
      return;
    }

    const payload: RequestActionSubmitPayload = {};
    if (isAssigneeField) payload.assigneeId = assigneeId.trim();
    if (trimmedReason) {
      if (openAction.requiredField) payload.reason = trimmedReason;
      else if (openAction.optionalField) payload.notes = trimmedReason;
    }

    try {
      await onAction(openAction, payload);
      resetDialog();
    } catch (error) {
      // Keep the dialog open so the user can correct their input.
      const message =
        error && typeof error === 'object' && 'serverMessage' in error
          ? String((error as { serverMessage?: string }).serverMessage ?? '')
          : '';
      setDialogError(message || 'The action could not be completed.');
    }
  };

  const isAssigneeField = isAssigneeFieldFor(openAction);

  return (
    <Box data-testid="request-actions">
      <Typography variant="h6" fontWeight={600} gutterBottom>
        Available Actions
      </Typography>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
        {actions.map((descriptor) => (
          <Button
            key={descriptor.key}
            variant={descriptor.variant}
            color={descriptor.color}
            onClick={() => handleButtonClick(descriptor)}
            disabled={submitting}
            startIcon={
              submitting && openAction?.key === descriptor.key ? (
                <CircularProgress size={14} color="inherit" />
              ) : undefined
            }
          >
            {descriptor.label}
          </Button>
        ))}
      </Box>

      <Dialog open={!!openAction} onClose={resetDialog} fullWidth maxWidth="sm">
        <DialogTitle>{openAction?.label}</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 2 }}>
            {isAssigneeField
              ? 'Select who this request should be routed to. The server verifies that you are allowed to make this assignment.'
              : 'Provide the reason for this action. It is recorded in the request audit trail and shown to the requester.'}
          </DialogContentText>

          {dialogError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {dialogError}
            </Alert>
          )}

          {isAssigneeField ? (
            assigneeOptions.length > 0 ? (
              <TextField
                select
                fullWidth
                size="small"
                label="Assignee"
                value={assigneeId}
                onChange={(event) => setAssigneeId(event.target.value)}
                inputProps={{ 'aria-label': 'Assignee' }}
              >
                {assigneeOptions.map((option) => (
                  <MenuItem key={option.id} value={option.id}>
                    {option.label}
                  </MenuItem>
                ))}
              </TextField>
            ) : (
              <TextField
                fullWidth
                size="small"
                label="Assignee user id"
                value={assigneeId}
                onChange={(event) => setAssigneeId(event.target.value)}
                inputProps={{ 'aria-label': 'Assignee user id' }}
              />
            )
          ) : (
            <TextField
              fullWidth
              multiline
              minRows={3}
              size="small"
              label={openAction?.requiredField ? 'Reason (required)' : 'Notes (optional)'}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              inputProps={{ 'aria-label': 'Reason' }}
            />
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={resetDialog} disabled={submitting}>
            Cancel
          </Button>
          <Button
            onClick={handleDialogSubmit}
            variant="contained"
            color={openAction?.color === 'inherit' ? 'primary' : (openAction?.color ?? 'primary')}
            disabled={submitting}
          >
            {submitting ? 'Workingâ€¦' : `Confirm ${openAction?.label ?? ''}`.trim()}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
