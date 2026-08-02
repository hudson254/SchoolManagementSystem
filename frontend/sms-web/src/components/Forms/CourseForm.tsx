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
import { courseService } from '../../services/course.service';

const courseSchema = z.object({
  name: z.string().min(1, 'Course name is required').max(100),
  code: z.string()
    .min(1, 'Course code is required')
    .max(20)
    .regex(/^[A-Z0-9]+$/, 'Course code must contain only uppercase letters and numbers'),
  description: z.string().optional(),
  duration: z.number().min(1, 'Duration must be at least 1 month'),
  totalCredits: z.number().min(1, 'Total credits must be at least 1'),
  departmentId: z.string().min(1, 'Department is required'),
  admissionRequirements: z.string().optional(),
  objectives: z.string().optional(),
  isActive: z.boolean().default(true),
});

type CourseFormData = z.infer<typeof courseSchema>;

interface CourseFormProps {
  courseId?: string;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export const CourseForm: React.FC<CourseFormProps> = ({
  courseId,
  onSuccess,
  onCancel,
}) => {
  const isEditMode = !!courseId;

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<CourseFormData>({
    resolver: zodResolver(courseSchema),
    defaultValues: {
      name: '',
      code: '',
      description: '',
      duration: 48,
      totalCredits: 120,
      departmentId: '',
      admissionRequirements: '',
      objectives: '',
      isActive: true,
    },
  });

  // Fetch course data if in edit mode
  const { data: course, isLoading } = useQuery({
    queryKey: ['course', courseId],
    queryFn: () => courseService.getCourse(courseId!),
    enabled: !!courseId,
  });

  // Fetch departments for dropdown
  const { data: departments } = useQuery({
    queryKey: ['departments'],
    queryFn: () => courseService.getDepartments(),
  });

  // Create/Update mutation
  const mutation = useMutation({
    mutationFn: (data: CourseFormData) => {
      if (isEditMode) {
        return courseService.updateCourse(courseId!, data);
      }
      return courseService.createCourse(data);
    },
    onSuccess: () => {
      onSuccess?.();
    },
  });

  // Populate form when course data is loaded
  useEffect(() => {
    if (course) {
      reset({
        name: course.name,
        code: course.code,
        description: course.description || '',
        duration: course.duration,
        totalCredits: course.totalCredits,
        departmentId: course.departmentId,
        admissionRequirements: course.admissionRequirements || '',
        objectives: course.objectives || '',
        isActive: course.isActive,
      });
    }
  }, [course, reset]);

  const onSubmit = (data: CourseFormData) => {
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
        {isEditMode ? 'Edit Course' : 'Create New Course'}
      </Typography>
      <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
        {isEditMode ? 'Update the course information below.' : 'Enter the course details below to create a new course.'}
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
                label="Course Name"
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
                label="Course Code"
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
            name="duration"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Duration (months)"
                type="number"
                required
                onChange={(e) => field.onChange(Number(e.target.value))}
                error={!!errors.duration}
                helperText={errors.duration?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="totalCredits"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Total Credits"
                type="number"
                required
                onChange={(e) => field.onChange(Number(e.target.value))}
                error={!!errors.totalCredits}
                helperText={errors.totalCredits?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="departmentId"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth required error={!!errors.departmentId}>
                <InputLabel>Department</InputLabel>
                <Select {...field} label="Department">
                  {departments?.map((d: any) => (
                    <MenuItem key={d.id} value={d.id}>
                      {d.name} ({d.code})
                    </MenuItem>
                  ))}
                </Select>
                {errors.departmentId && <FormHelperText>{errors.departmentId.message}</FormHelperText>}
              </FormControl>
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="admissionRequirements"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Admission Requirements"
                multiline
                rows={2}
                error={!!errors.admissionRequirements}
                helperText={errors.admissionRequirements?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="objectives"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Course Objectives"
                multiline
                rows={3}
                error={!!errors.objectives}
                helperText={errors.objectives?.message}
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
            'Update Course'
          ) : (
            'Create Course'
          )}
        </Button>
      </Box>
    </Box>
  );
};
