import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Grid,
  MenuItem,
  Paper,
  TextField,
  Typography,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  omsRequestsService,
  OmsRequestPriority,
  OmsRequestType,
  OMS_REQUEST_PRIORITIES,
} from '../../services/requests.service';
import { omsRequestPriorityLabel } from '../../utils/omsRequestLifecycle';
import { normalizeError } from '../../utils/errors';
import { EmptyState } from '../../components/Common/EmptyState';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';

/** Maximums mirrored from CreateRequestCommandValidator. */
const TITLE_MAX = 200;
const DESCRIPTION_MAX = 2000;

interface FieldErrors {
  requestType?: string;
  title?: string;
  description?: string;
  dueDate?: string;
  RelatedEntityId?: string;
  [key: string]: string | undefined;
}

/**
 * Create a generic OMS request.
 *
 * The form is driven entirely by the RequestTypes the server returns â€” the
 * type list, display names, default priorities and descriptions all come from
 * `GET /oms/requests/types`. Nothing about a request type is hard-coded here, so
 * a type an administrator configures appears automatically.
 *
 * Attachments cannot travel with the create call (CreateRequestCommand has no
 * such field), so after creation the user is taken to the Request Detail page
 * where the attachment panel is available.
 */
export const CreateRequestPage: React.FC = () => {
  const navigate = useNavigate();

  const [requestTypeCode, setRequestTypeCode] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<OmsRequestPriority | ''>('');
  const [dueDate, setDueDate] = useState('');
  const [relatedEntityId, setRelatedEntityId] = useState('');
  const [relatedEntityType, setRelatedEntityType] = useState('');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [serverError, setServerError] = useState<string | null>(null);

  const typesQuery = useQuery({
    queryKey: ['oms-request-types'],
    queryFn: () => omsRequestsService.getRequestTypes(),
    retry: false,
  });

  const activeTypes: OmsRequestType[] = useMemo(
    () => (typesQuery.data ?? []).filter((type) => type.isActive),
    [typesQuery.data],
  );

  const selectedType = activeTypes.find((type) => type.code === requestTypeCode);

  // Seed the priority from the selected type's server-provided default, but
  // never overwrite a value the user has already chosen.
  useEffect(() => {
    if (selectedType && priority === '') {
      setPriority(selectedType.defaultPriority);
    }
  }, [selectedType, priority]);

  const createMutation = useMutation({
    mutationFn: () =>
      omsRequestsService.createRequest({
        requestType: requestTypeCode,
        title: title.trim(),
        description: description.trim() || undefined,
        priority: priority === '' ? undefined : priority,
        dueDate: dueDate ? new Date(dueDate).toISOString() : undefined,
        relatedEntityId: relatedEntityId.trim() || undefined,
        relatedEntityType: relatedEntityType.trim() || undefined,
      }),
    onSuccess: (created) => {
      navigate(`/oms/requests/${created.id}`);
    },
    onError: (error) => {
      const normalized = normalizeError(error);
      setServerError(normalized.serverMessage || normalized.message);
      // Surface field-level validation errors returned by the server.
      if (normalized.errors) {
        const mapped: FieldErrors = {};
        for (const [key, messages] of Object.entries(normalized.errors)) {
          if (messages && messages.length > 0) {
            mapped[key] = messages[0];
          }
        }
        setFieldErrors(mapped);
      }
    },
  });

  const validate = (): boolean => {
    const errors: FieldErrors = {};

    if (!requestTypeCode) {
      errors.requestType = 'Request type is required.';
    }
    if (!title.trim()) {
      errors.title = 'Request title is required.';
    } else if (title.trim().length > TITLE_MAX) {
      errors.title = `Title must be ${TITLE_MAX} characters or fewer.`;
    }
    if (description.trim().length > DESCRIPTION_MAX) {
      errors.description = `Description must be ${DESCRIPTION_MAX} characters or fewer.`;
    }
    if (dueDate && Number.isNaN(new Date(dueDate).getTime())) {
      errors.dueDate = 'Due date is not a valid date.';
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    setServerError(null);
    if (validate()) {
      createMutation.mutate();
    }
  };

  const handleCancel = () => {
    navigate('/oms/requests');
  };

  const typesError = typesQuery.isError ? normalizeError(typesQuery.error) : null;

  if (typesQuery.isLoading) {
    return <LoadingSpinner message="Loading request types..." />;
  }

  if (typesError) {
    return (
      <Box>
        <Button startIcon={<ArrowBackIcon />} onClick={handleCancel} sx={{ mb: 2 }}>
          Back to Requests
        </Button>
        <Alert severity={typesError.statusCode === 403 ? 'warning' : 'error'}>
          {typesError.statusCode === 403
            ? 'You do not have permission to create requests.'
            : `${typesError.serverMessage || typesError.message} (${typesError.code})`}
        </Alert>
      </Box>
    );
  }

  if (activeTypes.length === 0) {
    return (
      <Box>
        <Button startIcon={<ArrowBackIcon />} onClick={handleCancel} sx={{ mb: 2 }}>
          Back to Requests
        </Button>
        <EmptyState
          title="No request types are available"
          description="There are no active request types configured for this school, so there is nothing to raise yet. An administrator can activate a request type to enable this."
        />
      </Box>
    );
  }

  return (
    <Box>
      <Button startIcon={<ArrowBackIcon />} onClick={handleCancel} sx={{ mb: 2 }}>
        Back to Requests
      </Button>
      <Typography variant="h5" fontWeight={600} gutterBottom>
        Create Request
      </Typography>
      <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
        Raise a request for review. The request number is assigned by the server once it is
        created.
      </Typography>

      {serverError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setServerError(null)}>
          {serverError}
        </Alert>
      )}

      <Paper sx={{ p: 3 }}>
        <Box component="form" onSubmit={handleSubmit} noValidate>
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <TextField
                select
                fullWidth
                required
                label="Request Type"
                value={requestTypeCode}
                onChange={(event) => {
                  setRequestTypeCode(event.target.value);
                  setPriority('');
                }}
                error={!!fieldErrors.requestType}
                helperText={
                  fieldErrors.requestType ||
                  (selectedType?.description ?? 'Choose the kind of request you need.')
                }
                inputProps={{ 'aria-label': 'Request Type' }}
              >
                {activeTypes.map((type) => (
                  <MenuItem key={type.id} value={type.code}>
                    {type.displayName}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12}>
              <TextField
                fullWidth
                required
                label="Title"
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                error={!!fieldErrors.title}
                helperText={fieldErrors.title || `${title.length}/${TITLE_MAX} characters`}
                inputProps={{ maxLength: TITLE_MAX, 'aria-label': 'Title' }}
              />
            </Grid>

            <Grid item xs={12}>
              <TextField
                fullWidth
                multiline
                minRows={5}
                label="Description"
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                error={!!fieldErrors.description}
                helperText={
                  fieldErrors.description ||
                  'Explain the situation and what outcome you are asking for.'
                }
                inputProps={{ 'aria-label': 'Description' }}
              />
            </Grid>

            <Grid item xs={12} md={6}>
              <TextField
                select
                fullWidth
                label="Priority"
                value={priority}
                onChange={(event) =>
                  setPriority(
                    event.target.value === ''
                      ? ''
                      : (Number(event.target.value) as OmsRequestPriority),
                  )
                }
                helperText="Defaulted from the selected request type."
                inputProps={{ 'aria-label': 'Priority' }}
              >
                {OMS_REQUEST_PRIORITIES.map((value) => (
                  <MenuItem key={value} value={value}>
                    {omsRequestPriorityLabel(value)}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} md={6}>
              <TextField
                fullWidth
                type="date"
                label="Due Date"
                value={dueDate}
                onChange={(event) => setDueDate(event.target.value)}
                error={!!fieldErrors.dueDate}
                helperText={fieldErrors.dueDate || 'Optional target date for resolution.'}
                InputLabelProps={{ shrink: true }}
                inputProps={{ 'aria-label': 'Due Date' }}
              />
            </Grid>

            <Grid item xs={12} md={6}>
              <TextField
                fullWidth
                label="Related Entity Type"
                value={relatedEntityType}
                onChange={(event) => setRelatedEntityType(event.target.value)}
                helperText="Optional. e.g. Enrollment, Accommodation, Assignment."
                inputProps={{ maxLength: 100, 'aria-label': 'Related Entity Type' }}
              />
            </Grid>

            <Grid item xs={12} md={6}>
              <TextField
                fullWidth
                label="Related Entity ID"
                value={relatedEntityId}
                onChange={(event) => setRelatedEntityId(event.target.value)}
                error={!!fieldErrors.RelatedEntityId}
                helperText={
                  fieldErrors.RelatedEntityId ||
                  'Optional identifier of the existing SMS record this concerns.'
                }
                inputProps={{ maxLength: 100, 'aria-label': 'Related Entity ID' }}
              />
            </Grid>

            <Grid item xs={12}>
              <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1, mt: 1 }}>
                <Button onClick={handleCancel} disabled={createMutation.isPending}>
                  Cancel
                </Button>
                <Button
                  type="submit"
                  variant="contained"
                  disabled={createMutation.isPending}
                  startIcon={
                    createMutation.isPending ? (
                      <CircularProgress size={16} color="inherit" />
                    ) : undefined
                  }
                >
                  {createMutation.isPending ? 'Creatingâ€¦' : 'Create Request'}
                </Button>
              </Box>
            </Grid>
          </Grid>
        </Box>
      </Paper>
    </Box>
  );
};

export default CreateRequestPage;
