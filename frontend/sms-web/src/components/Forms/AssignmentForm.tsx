import React, { useEffect } from 'react';
import {
  Box,
  TextField,
  Button,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormHelperText,
  CircularProgress,
  Typography,
  Divider,
  Alert,
  Switch,
  FormControlLabel,
  Slider,
} from '@mui/material';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { assignmentService } from '../../services/assignment.service';
import { unitService } from '../../services/unit.service';
import { useAuth } from '../../hooks/useAuth';

const assignmentSchema = z.object({
  title: z.string().min(1, 'Assignment title is required').max(200),
  description: z.string().optional(),
  unitId: z.string().min(1, 'Unit is required'),
  maxScore: z.number().min(1, 'Maximum score must be at least 1'),
  weight: z.number().min(1, 'Weight must be at least 1%').max(100, 'Weight cannot exceed 100%'),
  dueDate: z.string().min(1, 'Due date is required'),
  closingDate: z.string().optional(),
  instructions: z.string().optional(),
  allowLateSubmission: z.boolean().default(false),
  latePenaltyPercent: z.number().min(0, 'Penalty cannot be negative').max(100, 'Penalty cannot exceed 100%'),
  status: z.enum(['Draft', 'Published', 'Open', 'Closed', 'Archived']).default('Draft'),
});

type AssignmentFormData = z.infer<typeof assignmentSchema>;

interface AssignmentFormProps {
  assignmentId?: string;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export const AssignmentForm: React.FC<AssignmentFormProps> = ({
  assignmentId,
  onSuccess,
  onCancel,
}) => {
  const isEditMode = !!assignmentId;
  const { user } = useAuth();

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<AssignmentFormData>({
    resolver: zodResolver(assignmentSchema),
    defaultValues: {
      title: '',
      description: '',
      unitId: '',
      maxScore: 100,
      weight: 20,
      dueDate: '',
      closingDate: '',
      instructions: '',
      allowLateSubmission: false,
      latePenaltyPercent: 10,
      status: 'Draft',
    },
  });

  const allowLateSubmission = watch('allowLateSubmission');
  const dueDate = watch('dueDate');

  // Fetch assignment data if in edit mode
  const { data: assignment, isLoading } = useQuery({
    queryKey: ['assignment', assignmentId],
    queryFn: () => assignmentService.getAssignment(assignmentId!),
    enabled: !!assignmentId,
  });

  // Fetch units for dropdown
  const { data: units } = useQuery({
    queryKey: ['units'],
    queryFn: () => unitService.getUnits({ page: 1, pageSize: 100 }),
  });

  // Create/Update mutation
  const mutation = useMutation({
    mutationFn: (data: AssignmentFormData) => {
      const payload = {
        ...data,
        lecturerId: user?.id,
      };
      if (isEditMode) {
        return assignmentService.updateAssignment(assignmentId!, payload);
      }
      return assignmentService.createAssignment(payload);
    },
    onSuccess: () => {
      onSuccess?.();
    },
  });

  // Populate form when assignment data is loaded
  useEffect(() => {
    if (assignment) {
      reset({
        title: assignment.title,
        description: assignment.description || '',
        unitId: assignment.unitId,
        maxScore: assignment.maxScore,
        weight: assignment.weight,
        dueDate: assignment.dueDate ? new Date(assignment.dueDate).toISOString().slice(0, 16) : '',
        closingDate: assignment.closingDate ? new Date(assignment.closingDate).toISOString().slice(0, 16) : '',
        instructions: assignment.instructions || '',
        allowLateSubmission: assignment.allowLateSubmission,
        latePenaltyPercent: assignment.latePenaltyPercent,
        status: assignment.status as any,
      });
    }
  }, [assignment, reset]);

  const onSubmit = (data: AssignmentFormData) => {
    mutation.mutate(data);
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box component="form" onSubmit={handleSubmit(onSubmit)}>
      {mutation.error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {(mutation.error as any).message || 'An error occurred. Please try again.'}
        </Alert>
      )}

      <Typography variant="h6" fontWeight={600} gutterBottom>
        {isEditMode ? 'Edit Assignment' : 'Create New Assignment'}
      </Typography>
      <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
        {isEditMode ? 'Update the assignment information below.' : 'Enter the assignment details below to create a new assignment.'}
      </Typography>

      <Divider sx={{ mb: 3 }} />

      <Grid container spacing={3}>
        <Grid item xs={12}>
          <Controller
            name="title"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Assignment Title"
                required
                error={!!errors.title}
                helperText={errors.title?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="description"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Description"
                multiline
                rows={3}
                error={!!errors.description}
                helperText={errors.description?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="unitId"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth required error={!!errors.unitId}>
                <InputLabel>Unit</InputLabel>
                <Select {...field} label="Unit">
                  {units?.items?.map((u: any) => (
                    <MenuItem key={u.id} value={u.id}>
                      {u.name} ({u.code})
                    </MenuItem>
                  ))}
                </Select>
                {errors.unitId && <FormHelperText>{errors.unitId.message}</FormHelperText>}
              </FormControl>
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="status"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth error={!!errors.status}>
                <InputLabel>Status</InputLabel>
                <Select {...field} label="Status">
                  <MenuItem value="Draft">Draft</MenuItem>
                  <MenuItem value="Published">Published</MenuItem>
                  <MenuItem value="Open">Open</MenuItem>
                  <MenuItem value="Closed">Closed</MenuItem>
                  <MenuItem value="Archived">Archived</MenuItem>
                </Select>
                {errors.status && <FormHelperText>{errors.status.message}</FormHelperText>}
              </FormControl>
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="maxScore"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Maximum Score"
                type="number"
                required
                onChange={(e) => field.onChange(Number(e.target.value))}
                error={!!errors.maxScore}
                helperText={errors.maxScore?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="weight"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Weight (%)"
                type="number"
                required
                onChange={(e) => field.onChange(Number(e.target.value))}
                error={!!errors.weight}
                helperText={errors.weight?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="dueDate"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Due Date"
                type="datetime-local"
                required
                InputLabelProps={{ shrink: true }}
                error={!!errors.dueDate}
                helperText={errors.dueDate?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="closingDate"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Closing Date (Optional)"
                type="datetime-local"
                InputLabelProps={{ shrink: true }}
                error={!!errors.closingDate}
                helperText={errors.closingDate?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="instructions"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Instructions"
                multiline
                rows={3}
                error={!!errors.instructions}
                helperText={errors.instructions?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="allowLateSubmission"
            control={control}
            render={({ field }) => (
              <FormControlLabel
                control={
                  <Switch
                    checked={field.value}
                    onChange={(e) => field.onChange(e.target.checked)}
                  />
                }
                label="Allow Late Submissions"
              />
            )}
          />
        </Grid>
        {allowLateSubmission && (
          <Grid item xs={12}>
            <Controller
              name="latePenaltyPercent"
              control={control}
              render={({ field }) => (
                <Box>
                  <Typography variant="body2" gutterBottom>
                    Late Penalty: {field.value}%
                  </Typography>
                  <Slider
                    value={field.value}
                    onChange={(e, value) => field.onChange(value)}
                    min={0}
                    max={100}
                    step={5}
                    valueLabelDisplay="auto"
                    marks={[
                      { value: 0, label: '0%' },
                      { value: 50, label: '50%' },
                      { value: 100, label: '100%' },
                    ]}
                  />
                </Box>
              )}
            />
          </Grid>
        )}
      </Grid>

      <Box sx={{ mt: 4, display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
        <Button variant="outlined" onClick={onCancel}>
          Cancel
        </Button>
        <Button
          type="submit"
          variant="contained"
          disabled={!isDirty || isSubmitting || mutation.isPending}
        >
          {isSubmitting || mutation.isPending ? (
            <CircularProgress size={24} />
          ) : isEditMode ? (
            'Update Assignment'
          ) : (
            'Create Assignment'
          )}
        </Button>
      </Box>
    </Box>
  );
};