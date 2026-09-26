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
} from '@mui/material';
import {
  OmsModuleRequestTypeCodes,
  OmsRequestPriority,
  OMS_REQUEST_PRIORITIES,
  omsRequestsService,
} from '../../services/requests.service';
import { omsRequestPriorityLabel } from '../../utils/omsRequestLifecycle';
import { normalizeError } from '../../utils/errors';

export type OmsModuleRequestModule = 'enrollment' | 'accommodation' | 'assignment';

export interface OmsModuleRequestOption {
  code: string;
  label: string;
}

/**
 * The request types each adapter accepts, with human labels. These codes mirror
 * the backend *RequestTypes constants exactly; the adapter validates them
 * server-side and rejects anything outside the set.
 */
export const OmsModuleRequestTypeOptions: Record<OmsModuleRequestModule, OmsModuleRequestOption[]> =
  {
    enrollment: [
      { code: OmsModuleRequestTypeCodes.enrollment.courseChange, label: 'Request Course Change' },
      { code: OmsModuleRequestTypeCodes.enrollment.exception, label: 'Request Enrollment Exception' },
      { code: OmsModuleRequestTypeCodes.enrollment.unitCorrection, label: 'Request Unit Correction' },
      { code: OmsModuleRequestTypeCodes.enrollment.cancellation, label: 'Request Enrollment Cancellation' },
    ],
    accommodation: [
      { code: OmsModuleRequestTypeCodes.accommodation.transfer, label: 'Request Accommodation Transfer' },
      { code: OmsModuleRequestTypeCodes.accommodation.allocation, label: 'Request Accommodation Allocation' },
      { code: OmsModuleRequestTypeCodes.accommodation.exception, label: 'Request Accommodation Exception' },
      { code: OmsModuleRequestTypeCodes.accommodation.correction, label: 'Request Accommodation Correction' },
    ],
    assignment: [
      { code: OmsModuleRequestTypeCodes.assignment.extension, label: 'Request Assignment Extension' },
      { code: OmsModuleRequestTypeCodes.assignment.reopen, label: 'Request Assignment Reopen' },
      { code: OmsModuleRequestTypeCodes.assignment.correction, label: 'Request Assignment Correction' },
      { code: OmsModuleRequestTypeCodes.assignment.exception, label: 'Request Assignment Exception' },
    ],
  };

const MODULE_OWNERSHIP_NOTICE: Record<OmsModuleRequestModule, string> = {
  enrollment:
    'The Enrollment module remains the owner of this enrollment. Raising this request does not change any enrollment state â€” once approved, a coordinator performs the change through the existing Enrollment commands.',
  accommodation:
    'The Accommodation module remains the owner of this allocation. Raising this request does not move anyone: no house, lane or occupancy data is changed. Once approved, a coordinator performs the change through the existing Accommodation commands.',
  assignment:
    'The Assignment module remains the owner of this assignment. Raising this request does not change any deadline, submission or grading state. Once approved, a coordinator performs the change through the existing Assignment commands.',
};

interface ModuleRequestDialogProps {
  open: boolean;
  module: OmsModuleRequestModule;
  /** The module record id the adapter requires (enrollment/accommodation/assignment id). */
  moduleRecordId: string;
  /** Human description of the record, shown for confirmation. */
  recordLabel: string;
  onClose: () => void;
  /** Called with the created Request so the caller can navigate to its detail page. */
  onCreated: (requestId: string) => void;
}

/**
 * Shared entry point for the three thin module adapters.
 *
 * Flow: collect context â†’ call the module adapter â†’ receive a generic Request â†’
 * navigate to Request Detail, where the shared workflow handles everything else.
 *
 * The dialog deliberately has no approve/reject/assign controls. The adapters
 * expose no lifecycle verbs by design, so there is exactly one workflow engine.
 */
export const ModuleRequestDialog: React.FC<ModuleRequestDialogProps> = ({
  open,
  module,
  moduleRecordId,
  recordLabel,
  onClose,
  onCreated,
}) => {
  const options = OmsModuleRequestTypeOptions[module];
  const [requestType, setRequestType] = useState(options[0]?.code ?? '');
  const [reason, setReason] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<OmsRequestPriority>(OmsRequestPriority.Normal);
  const [dueDate, setDueDate] = useState('');
  const [title, setTitle] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const reset = () => {
    setRequestType(options[0]?.code ?? '');
    setReason('');
    setDescription('');
    setPriority(OmsRequestPriority.Normal);
    setDueDate('');
    setTitle('');
    setValidationError(null);
    setApiError(null);
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setApiError(null);

    if (!moduleRecordId) {
      setValidationError('A valid record reference is required.');
      return;
    }
    if (!requestType) {
      setValidationError('Please choose the type of request you need.');
      return;
    }
    setValidationError(null);

    const payload = {
      requestType,
      title: title.trim() || undefined,
      description: description.trim() || undefined,
      reason: reason.trim() || undefined,
      priority,
      dueDate: dueDate ? new Date(dueDate).toISOString() : undefined,
    };

    setSubmitting(true);
    try {
      const created =
        module === 'enrollment'
          ? await omsRequestsService.createEnrollmentRequest({
              enrollmentId: moduleRecordId,
              ...payload,
            })
          : module === 'accommodation'
            ? await omsRequestsService.createAccommodationRequest({
                accommodationId: moduleRecordId,
                ...payload,
              })
            : await omsRequestsService.createAssignmentRequest({
                assignmentId: moduleRecordId,
                ...payload,
              });

      reset();
      onCreated(created.id);
    } catch (error) {
      const normalized = normalizeError(error);
      setApiError(normalized.serverMessage || normalized.message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm">
      <DialogTitle>Raise a Request</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>{recordLabel}</DialogContentText>

        <Alert severity="info" sx={{ mb: 2 }}>
          {MODULE_OWNERSHIP_NOTICE[module]}
        </Alert>

        {apiError && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setApiError(null)}>
            {apiError}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit} id="module-request-form">
          <TextField
            select
            fullWidth
            size="small"
            required
            label="Request Type"
            value={requestType}
            onChange={(event) => setRequestType(event.target.value)}
            error={!!validationError}
            helperText={validationError}
            inputProps={{ 'aria-label': 'Request Type' }}
            sx={{ mb: 2 }}
          >
            {options.map((option) => (
              <MenuItem key={option.code} value={option.code}>
                {option.label}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            fullWidth
            size="small"
            label="Title (optional)"
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            helperText="Leave blank to let the server compose a title from the module context."
            inputProps={{ maxLength: 200, 'aria-label': 'Title' }}
            sx={{ mb: 2 }}
          />

          <TextField
            fullWidth
            size="small"
            multiline
            minRows={2}
            label="Description (optional)"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            inputProps={{ 'aria-label': 'Description' }}
            sx={{ mb: 2 }}
          />

          <TextField
            fullWidth
            size="small"
            multiline
            minRows={2}
            label="Reason"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            helperText="Why is this needed? This is recorded on the request for the approver."
            inputProps={{ maxLength: 1000, 'aria-label': 'Reason' }}
            sx={{ mb: 2 }}
          />

          <TextField
            select
            fullWidth
            size="small"
            label="Priority"
            value={priority}
            onChange={(event) => setPriority(Number(event.target.value) as OmsRequestPriority)}
            sx={{ mb: 2 }}
            inputProps={{ 'aria-label': 'Priority' }}
          >
            {OMS_REQUEST_PRIORITIES.map((value) => (
              <MenuItem key={value} value={value}>
                {omsRequestPriorityLabel(value)}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            fullWidth
            size="small"
            type="date"
            label="Needed By (optional)"
            value={dueDate}
            onChange={(event) => setDueDate(event.target.value)}
            InputLabelProps={{ shrink: true }}
            inputProps={{ 'aria-label': 'Needed By' }}
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={submitting}>
          Cancel
        </Button>
        <Button
          onClick={handleSubmit}
          variant="contained"
          disabled={submitting}
          startIcon={submitting ? <CircularProgress size={16} color="inherit" /> : undefined}
        >
          {submitting ? 'Submittingâ€¦' : 'Submit Request'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
