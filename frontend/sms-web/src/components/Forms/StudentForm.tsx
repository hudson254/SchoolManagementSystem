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
} from '@mui/material';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { studentService } from '../../services/student.service';
import { courseService } from '../../services/course.service';

const studentSchema = z.object({
  firstName: z.string().min(1, 'First name is required').max(100),
  lastName: z.string().min(1, 'Last name is required').max(100),
  email: z.string().email('Invalid email address').min(1, 'Email is required'),
  phoneNumber: z.string().min(1, 'Phone number is required').max(20),
  dateOfBirth: z.string().min(1, 'Date of birth is required'),
  gender: z.string().optional(),
  address: z.string().optional(),
  programmeId: z.string().optional(),
  emergencyContactName: z.string().optional(),
  emergencyContactPhone: z.string().optional(),
  emergencyContactRelation: z.string().optional(),
});

type StudentFormData = z.infer<typeof studentSchema>;

interface StudentFormProps {
  studentId?: string;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export const StudentForm: React.FC<StudentFormProps> = ({
  studentId,
  onSuccess,
  onCancel,
}) => {
  const isEditMode = !!studentId;

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<StudentFormData>({
    resolver: zodResolver(studentSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      dateOfBirth: '',
      gender: '',
      address: '',
      programmeId: '',
      emergencyContactName: '',
      emergencyContactPhone: '',
      emergencyContactRelation: '',
    },
  });

  // Fetch student data if in edit mode
  const { data: student, isLoading } = useQuery({
    queryKey: ['student', studentId],
    queryFn: () => studentService.getStudent(studentId!),
    enabled: !!studentId,
  });

  // Fetch programmes for dropdown
  const { data: programmes } = useQuery({
    queryKey: ['programmes'],
    queryFn: () => courseService.getProgrammes(),
  });

  // Create/Update mutation
  const mutation = useMutation({
    mutationFn: (data: StudentFormData) => {
      if (isEditMode) {
        return studentService.updateStudent(studentId!, data);
      }
      return studentService.createStudent(data);
    },
    onSuccess: () => {
      onSuccess?.();
    },
  });

  // Populate form when student data is loaded
  useEffect(() => {
    if (student) {
      reset({
        firstName: student.firstName,
        lastName: student.lastName,
        email: student.email,
        phoneNumber: student.phoneNumber || '',
        dateOfBirth: student.dateOfBirth ? new Date(student.dateOfBirth).toISOString().split('T')[0] : '',
        gender: student.gender || '',
        address: student.address || '',
        programmeId: student.programmeId || '',
        emergencyContactName: student.emergencyContactName || '',
        emergencyContactPhone: student.emergencyContactPhone || '',
        emergencyContactRelation: student.emergencyContactRelation || '',
      });
    }
  }, [student, reset]);

  const onSubmit = (data: StudentFormData) => {
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
        {isEditMode ? 'Edit Student' : 'Create New Student'}
      </Typography>
      <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
        {isEditMode ? 'Update the student information below.' : 'Enter the student details below to create a new student record.'}
      </Typography>

      <Divider sx={{ mb: 3 }} />

      <Typography variant="subtitle2" fontWeight={600} sx={{ mb: 2 }}>
        Personal Information
      </Typography>

      <Grid container spacing={3}>
        <Grid item xs={12} sm={6}>
          <Controller
            name="firstName"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="First Name"
                required
                error={!!errors.firstName}
                helperText={errors.firstName?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="lastName"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Last Name"
                required
                error={!!errors.lastName}
                helperText={errors.lastName?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="email"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Email Address"
                type="email"
                required
                error={!!errors.email}
                helperText={errors.email?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="phoneNumber"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Phone Number"
                required
                error={!!errors.phoneNumber}
                helperText={errors.phoneNumber?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="dateOfBirth"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Date of Birth"
                type="date"
                required
                InputLabelProps={{ shrink: true }}
                error={!!errors.dateOfBirth}
                helperText={errors.dateOfBirth?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="gender"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth error={!!errors.gender}>
                <InputLabel>Gender</InputLabel>
                <Select {...field} label="Gender">
                  <MenuItem value="">Not Specified</MenuItem>
                  <MenuItem value="Male">Male</MenuItem>
                  <MenuItem value="Female">Female</MenuItem>
                  <MenuItem value="Other">Other</MenuItem>
                </Select>
                {errors.gender && <FormHelperText>{errors.gender.message}</FormHelperText>}
              </FormControl>
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="address"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Address"
                multiline
                rows={2}
                error={!!errors.address}
                helperText={errors.address?.message}
              />
            )}
          />
        </Grid>
      </Grid>

      <Divider sx={{ my: 3 }} />

      <Typography variant="subtitle2" fontWeight={600} sx={{ mb: 2 }}>
        Academic Information
      </Typography>

      <Grid container spacing={3}>
        <Grid item xs={12}>
          <Controller
            name="programmeId"
            control={control}
            render={({ field }) => (
              <FormControl fullWidth error={!!errors.programmeId}>
                <InputLabel>Programme</InputLabel>
                <Select {...field} label="Programme">
                  <MenuItem value="">Not Assigned</MenuItem>
                  {programmes?.map((p: any) => (
                    <MenuItem key={p.id} value={p.id}>
                      {p.name} ({p.code})
                    </MenuItem>
                  ))}
                </Select>
                {errors.programmeId && <FormHelperText>{errors.programmeId.message}</FormHelperText>}
              </FormControl>
            )}
          />
        </Grid>
      </Grid>

      <Divider sx={{ my: 3 }} />

      <Typography variant="subtitle2" fontWeight={600} sx={{ mb: 2 }}>
        Emergency Contact
      </Typography>

      <Grid container spacing={3}>
        <Grid item xs={12} sm={6}>
          <Controller
            name="emergencyContactName"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Contact Name"
                error={!!errors.emergencyContactName}
                helperText={errors.emergencyContactName?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12} sm={6}>
          <Controller
            name="emergencyContactPhone"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Contact Phone"
                error={!!errors.emergencyContactPhone}
                helperText={errors.emergencyContactPhone?.message}
              />
            )}
          />
        </Grid>
        <Grid item xs={12}>
          <Controller
            name="emergencyContactRelation"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                fullWidth
                label="Relationship"
                error={!!errors.emergencyContactRelation}
                helperText={errors.emergencyContactRelation?.message}
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
            'Update Student'
          ) : (
            'Create Student'
          )}
        </Button>
      </Box>
    </Box>
  );
};