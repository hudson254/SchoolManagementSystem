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
import { courseOfferingService, CourseOfferingStatus } from '../../services/course-offering.service';
import { courseService } from '../../services/course.service';
import { api } from '../../services/api';

const courseOfferingSchema = z.object({
  courseId: z.string().min(1, 'Course is required'),
  academicYearName: z.string().min(1, 'Academic Year is required'),
  semesterName: z.string().min(1, 'Semester is required'),
  intake: z.string().optional(),
  startDate: z.string().min(1, 'Start date is required'),
  endDate: z.string().min(1, 'End date is required'),
  registrationStartDate: z.string().optional(),
  registrationEndDate: z.string().optional(),
  status: z.nativeEnum(CourseOfferingStatus),
  notes: z.string().optional(),
  isActive: z.boolean().default(true),
}).refine((data) => {
  if (data.endDate && data.startDate) {
    return new Date(data.endDate) > new Date(data.startDate);
  }
  return true;
}, {
  message: 'End date must be after start date',
  path: ['endDate'],
});

type CourseOfferingFormData = z.infer<typeof courseOfferingSchema>;

interface AcademicYear {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  isCurrent: boolean;
}

interface Semester {
  id: string;
  name: string;
  semesterNumber: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
  isCurrent: boolean;
  academicYearId: string;
}

interface CourseOfferingFormProps {
  offeringId?: string;
  onSuccess?: (id?: string) => void;
  onCancel?: () => void;
}

export const CourseOfferingForm: React.FC<CourseOfferingFormProps> = ({
  offeringId,
  onSuccess,
  onCancel,
}) => {
  const isEditMode = !!offeringId;

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<CourseOfferingFormData>({
    resolver: zodResolver(courseOfferingSchema),
    defaultValues: {
      courseId: '',
      academicYearName: '',
      semesterName: '',
      intake: '',
      startDate: '',
      endDate: '',
      registrationStartDate: '',
      registrationEndDate: '',
      status: CourseOfferingStatus.Draft,
      notes: '',
      isActive: true,
    },
  });

  // Fetch offering data if in edit mode
  const { data: offering, isLoading: offeringLoading } = useQuery({
    queryKey: ['courseoffering', offeringId],
    queryFn: () => courseOfferingService.getCourseOffering(offeringId!),
    enabled: !!offeringId,
  });

  // Fetch courses for dropdown
  const { data: courses } = useQuery({
    queryKey: ['courses', 'active'],
    queryFn: () => courseService.getCourses({ isActive: true, pageSize: 100 }),
  });

  // Create/Update mutation
  const mutation = useMutation({
    mutationFn: (data: CourseOfferingFormData) => {
      // The backend binds DateTime fields to PostgreSQL 'timestamp with time
      // zone' columns, which require ISO-8601 UTC strings. HTML date inputs
      // yield 'yyyy-MM-dd' (and an empty string when left blank), which the
      // API cannot deserialize into its nullable DateTime fields.
      const toUtcDateTime = (value?: string): string | null =>
        value ? `${value}T00:00:00Z` : null;

      const payload = {
        ...data,
        startDate: toUtcDateTime(data.startDate),
        endDate: toUtcDateTime(data.endDate),
        registrationStartDate: toUtcDateTime(data.registrationStartDate),
        registrationEndDate: toUtcDateTime(data.registrationEndDate),
      };
      if (isEditMode) {
        return courseOfferingService.updateCourseOffering(offeringId!, payload);
      }
      return courseOfferingService.createCourseOffering(payload);
    },
    onSuccess: (result) => {
      onSuccess?.(result?.id);
    },
  });

  // Populate form when offering data is loaded
  useEffect(() => {
    if (offering) {
      reset({
        courseId: offering.courseId,
        academicYearName: offering.academicYearName || '',
        semesterName: offering.semesterName || '',
        intake: offering.intake || '',
        startDate: offering.startDate ? offering.startDate.slice(0, 10) : '',
        endDate: offering.endDate ? offering.endDate.slice(0, 10) : '',
        registrationStartDate: offering.registrationStartDate ? offering.registrationStartDate.slice(0, 10) : '',
        registrationEndDate: offering.registrationEndDate ? offering.registrationEndDate.slice(0, 10) : '',
        status: offering.status,
        notes: offering.notes || '',
        isActive: offering.isActive,
      });
    }
  }, [offering, reset]);

  const onSubmit = (data: CourseOfferingFormData) => {
    mutation.mutate(data);
  };

  if (offeringLoading) {
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
        {isEditMode ? 'Edit Course Offering' : 'Create New Course Offering'}
      </Typography>
      <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
        {isEditMode
          ? 'Update the course offering information below.'
          : 'Enter the course offering details below to create a new offering.'}
      </Typography>

      <Divider sx={{ mb: 3 }} />

      <Grid container spacing={3}>
        <Grid item xs={12} sm={6}>
          <Controller
            name="courseId"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth required error={!!errors.courseId}>
                <InputLabel>Course</InputLabel>
                <Select {...field} label="Course">
                  {(courses?.items || []).map((c: any) => (
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
        <Grid item xs={12} sm={6}>
          <Controller
            name="intake"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Intake"
                error={!!errors.intake}
                helperText={errors.intake?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="academicYearName"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                required
                label="Academic Year"
                placeholder="e.g. 2026/2027"
                error={!!errors.academicYearName}
                helperText={errors.academicYearName?.message || 'Enter academic year (e.g. 2026/2027)'}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="semesterName"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                required
                label="Semester"
                placeholder="e.g. Semester 1"
                error={!!errors.semesterName}
                helperText={errors.semesterName?.message || 'Enter semester (e.g. Semester 1)'}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="startDate"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Start Date"
                type="date"
                InputLabelProps={{ shrink: true }}
                required
                error={!!errors.startDate}
                helperText={errors.startDate?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="endDate"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="End Date"
                type="date"
                InputLabelProps={{ shrink: true }}
                required
                error={!!errors.endDate}
                helperText={errors.endDate?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="registrationStartDate"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Registration Open Date"
                type="date"
                InputLabelProps={{ shrink: true }}
                error={!!errors.registrationStartDate}
                helperText={errors.registrationStartDate?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="registrationEndDate"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Registration Close Date"
                type="date"
                InputLabelProps={{ shrink: true }}
                error={!!errors.registrationEndDate}
                helperText={errors.registrationEndDate?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="status"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth>
                <InputLabel>Status</InputLabel>
                <Select {...field} label="Status">
                  <MenuItem value={CourseOfferingStatus.Draft}>Draft</MenuItem>
                  <MenuItem value={CourseOfferingStatus.Scheduled}>Scheduled</MenuItem>
                  <MenuItem value={CourseOfferingStatus.Active}>Active</MenuItem>
                  <MenuItem value={CourseOfferingStatus.Completed}>Completed</MenuItem>
                  <MenuItem value={CourseOfferingStatus.Cancelled}>Cancelled</MenuItem>
                </Select>
              </FormControl>
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="notes"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Notes"
                multiline
                rows={3}
                error={!!errors.notes}
                helperText={errors.notes?.message}
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
            'Update Offering'
          ) : (
            'Create Offering'
          )}
        </Button>
      </Box>
    </Box>
  );
};
