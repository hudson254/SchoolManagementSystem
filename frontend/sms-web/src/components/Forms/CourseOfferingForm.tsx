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
  academicYearId: z.string().min(1, 'Academic Year is required'),
  semesterId: z.string().min(1, 'Semester is required'),
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
    watch,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<CourseOfferingFormData>({
    resolver: zodResolver(courseOfferingSchema),
    defaultValues: {
      courseId: '',
      academicYearId: '',
      semesterId: '',
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

  const watchedAcademicYearId = watch('academicYearId');

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

  // Fetch academic years
  const { data: academicYears } = useQuery({
    queryKey: ['academicyears'],
    queryFn: () => api.get<AcademicYear[]>('/academicyears'),
  });

  // Fetch semesters filtered by academic year
  const { data: semesters } = useQuery({
    queryKey: ['semesters', watchedAcademicYearId],
    queryFn: () => api.get<Semester[]>('/semesters', { params: { academicYearId: watchedAcademicYearId || undefined } }),
    enabled: !!watchedAcademicYearId,
  });

  // Create/Update mutation
  const mutation = useMutation({
    mutationFn: (data: CourseOfferingFormData) => {
      if (isEditMode) {
        return courseOfferingService.updateCourseOffering(offeringId!, data);
      }
      return courseOfferingService.createCourseOffering(data);
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
        academicYearId: offering.academicYearId,
        semesterId: offering.semesterId,
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
            name="academicYearId"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth required error={!!errors.academicYearId}>
                <InputLabel>Academic Year</InputLabel>
                <Select {...field} label="Academic Year">
                  {(academicYears || []).map((ay: AcademicYear) => (
                    <MenuItem key={ay.id} value={ay.id}>
                      {ay.name}
                    </MenuItem>
                  ))}
                </Select>
                {errors.academicYearId && <FormHelperText>{errors.academicYearId.message}</FormHelperText>}
              </FormControl>
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="semesterId"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth required error={!!errors.semesterId}>
                <InputLabel>Semester</InputLabel>
                <Select {...field} label="Semester">
                  {(semesters || []).map((s: Semester) => (
                    <MenuItem key={s.id} value={s.id}>
                      {s.name}
                    </MenuItem>
                  ))}
                </Select>
                {errors.semesterId && <FormHelperText>{errors.semesterId.message}</FormHelperText>}
              </FormControl>
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
