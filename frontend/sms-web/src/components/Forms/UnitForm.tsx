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
} from '@mui/material';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { unitService } from '../../services/unit.service';
import { courseService } from '../../services/course.service';

const unitSchema = z.object({
  name: z.string().min(1, 'Unit name is required').max(100),
  code: z.string()
    .min(1, 'Unit code is required')
    .max(20)
    .regex(/^[A-Z0-9]+$/, 'Unit code must contain only uppercase letters and numbers'),
  description: z.string().optional(),
  credits: z.number().min(1, 'Credits must be at least 1').max(6, 'Credits cannot exceed 6'),
  contactHours: z.number().min(1, 'Contact hours must be at least 1'),
  courseId: z.string().min(1, 'Course is required'),
  prerequisiteUnitId: z.string().optional(),
  learningOutcomes: z.string().optional(),
  assessmentMethods: z.string().optional(),
  recommendedTextbooks: z.string().optional(),
  isActive: z.boolean().default(true),
});

type UnitFormData = z.infer<typeof unitSchema>;

interface UnitFormProps {
  unitId?: string;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export const UnitForm: React.FC<UnitFormProps> = ({
  unitId,
  onSuccess,
  onCancel,
}) => {
  const isEditMode = !!unitId;

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<UnitFormData>({
    resolver: zodResolver(unitSchema),
    defaultValues: {
      name: '',
      code: '',
      description: '',
      credits: 3,
      contactHours: 3,
      courseId: '',
      prerequisiteUnitId: '',
      learningOutcomes: '',
      assessmentMethods: '',
      recommendedTextbooks: '',
      isActive: true,
    },
  });

  const courseId = watch('courseId');

  // Fetch unit data if in edit mode
  const { data: unit, isLoading } = useQuery({
    queryKey: ['unit', unitId],
    queryFn: () => unitService.getUnit(unitId!),
    enabled: !!unitId,
  });

  // Fetch courses for dropdown
  const { data: courses } = useQuery({
    queryKey: ['courses'],
    queryFn: () => courseService.getCourses({ page: 1, pageSize: 100 }),
  });

  // Fetch units for prerequisite dropdown (filtered by selected course)
  const { data: units } = useQuery({
    queryKey: ['units', courseId],
    queryFn: () => unitService.getUnits({ courseId, page: 1, pageSize: 100 }),
    enabled: !!courseId,
  });

  // Create/Update mutation
  const mutation = useMutation({
    mutationFn: (data: UnitFormData) => {
      if (isEditMode) {
        return unitService.updateUnit(unitId!, data);
      }
      return unitService.createUnit(data);
    },
    onSuccess: () => {
      onSuccess?.();
    },
  });

  // Populate form when unit data is loaded
  useEffect(() => {
    if (unit) {
      reset({
        name: unit.name,
        code: unit.code,
        description: unit.description || '',
        credits: unit.credits,
        contactHours: unit.contactHours,
        courseId: unit.courseId,
        prerequisiteUnitId: unit.prerequisiteUnitId || '',
        learningOutcomes: unit.learningOutcomes || '',
        assessmentMethods: unit.assessmentMethods || '',
        recommendedTextbooks: unit.recommendedTextbooks || '',
        isActive: unit.isActive,
      });
    }
  }, [unit, reset]);

  const onSubmit = (data: UnitFormData) => {
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
        {isEditMode ? 'Edit Unit' : 'Create New Unit'}
      </Typography>
      <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
        {isEditMode ? 'Update the unit information below.' : 'Enter the unit details below to create a new unit.'}
      </Typography>

      <Divider sx={{ mb: 3 }} />

      <Grid container spacing={3}>
        <Grid item xs={12} sm={6}>
          <Controller
            name="name"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Unit Name"
                required
                error={!!errors.name}
                helperText={errors.name?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="code"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Unit Code"
                required
                error={!!errors.code}
                helperText={errors.code?.message || 'Uppercase letters and numbers only'}
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
            name="credits"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Credits"
                type="number"
                required
                onChange={(e) => field.onChange(Number(e.target.value))}
                error={!!errors.credits}
                helperText={errors.credits?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="contactHours"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Contact Hours per Week"
                type="number"
                required
                onChange={(e) => field.onChange(Number(e.target.value))}
                error={!!errors.contactHours}
                helperText={errors.contactHours?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="courseId"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth required error={!!errors.courseId}>
                <InputLabel>Course</InputLabel>
                <Select {...field} label="Course">
                  {courses?.items?.map((c: any) => (
                    <MenuItem key={c.id} value={c.id}>
                      {c.name} ({c.code})
                    </MenuItem>
                  ))}
                </Select>
                {errors.courseId && <FormHelperText>{errors.courseId.message}</FormHelperText>}
              </FormControl>
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="prerequisiteUnitId"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth error={!!errors.prerequisiteUnitId}>
                <InputLabel>Prerequisite Unit</InputLabel>
                <Select {...field} label="Prerequisite Unit">
                  <MenuItem value="">None</MenuItem>
                  {units?.items
                    ?.filter((u: any) => u.id !== unitId)
                    .map((u: any) => (
                      <MenuItem key={u.id} value={u.id}>
                        {u.name} ({u.code})
                      </MenuItem>
                    ))}
                </Select>
                {errors.prerequisiteUnitId && <FormHelperText>{errors.prerequisiteUnitId.message}</FormHelperText>}
              </FormControl>
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="learningOutcomes"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Learning Outcomes"
                multiline
                rows={2}
                error={!!errors.learningOutcomes}
                helperText={errors.learningOutcomes?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="assessmentMethods"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Assessment Methods"
                multiline
                rows={2}
                error={!!errors.assessmentMethods}
                helperText={errors.assessmentMethods?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="recommendedTextbooks"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Recommended Textbooks"
                multiline
                rows={2}
                error={!!errors.recommendedTextbooks}
                helperText={errors.recommendedTextbooks?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="isActive"
            control={control}
            render={({ field }) => (
              <FormControlLabel
                control={
                  <Switch
                    checked={field.value}
                    onChange={(e) => field.onChange(e.target.checked)}
                  />
                }
                label={field.value ? 'Active' : 'Inactive'}
              />
            )}
          />
        </Grid>
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
            'Update Unit'
          ) : (
            'Create Unit'
          )}
        </Button>
      </Box>
    </Box>
  );
};
