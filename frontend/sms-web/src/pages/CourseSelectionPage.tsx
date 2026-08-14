import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Grid,
  Card,
  CardContent,
  CardActions,
  Button,
  CircularProgress,
  Alert,
  Stepper,
  Step,
  StepLabel,
  Chip,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  List,
  ListItem,
  ListItemText,
  Checkbox,
  FormControlLabel,
} from '@mui/material';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { enrollmentService, StudentEnrollmentStatus, CourseOption } from '../services/enrollment.service';
import { courseService } from '../services/course.service';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { useAuth } from '../hooks/useAuth';
import { useSnackbar } from 'notistack';

const STEPS = ['Select Course', 'Confirm Units', 'Submit'];

interface Course {
  id: string;
  name: string;
  code: string;
  description?: string;
}

interface Unit {
  id: string;
  code: string;
  name: string;
  credits: number;
}

export const CourseSelectionPage: React.FC = () => {
  const navigate = useNavigate();
  const { enqueueSnackbar } = useSnackbar();
  const { user } = useAuth();
  const [activeStep, setActiveStep] = useState(0);
  const [selectedCourse, setSelectedCourse] = useState<string>('');
  const [selectedUnits, setSelectedUnits] = useState<string[]>([]);

  // Fetch student's enrollment status
  const { data: status, isLoading: statusLoading } = useQuery<StudentEnrollmentStatus>({
    queryKey: ['my-enrollment-status'],
    queryFn: () => enrollmentService.getMyStatus(),
  });

  // Fetch available courses
  const { data: coursesResponse, isLoading: coursesLoading } = useQuery({
    queryKey: ['active-courses'],
    queryFn: () => courseService.getCourses({ isActive: true, pageSize: 100 }),
    enabled: activeStep === 0,
  });
  const courses = coursesResponse?.items;

  // Fetch course units
  const { data: units, isLoading: unitsLoading } = useQuery<Unit[]>({
    queryKey: ['course-units', selectedCourse],
    queryFn: () => courseService.getUnits(selectedCourse) as Promise<any>,
    enabled: activeStep === 1 && !!selectedCourse,
  });

  const submitMutation = useMutation({
    mutationFn: (courseId: string) => enrollmentService.submitEnrollment(courseId),
    onSuccess: (data) => {
      enqueueSnackbar(data.message || 'Enrollment submitted successfully!', { variant: 'success' });
      navigate('/enrollment-status');
    },
    onError: (error: any) => {
      enqueueSnackbar(error?.message || 'Failed to submit enrollment', { variant: 'error' });
    },
  });

  const handleNext = () => {
    if (activeStep === 0 && !selectedCourse) {
      enqueueSnackbar('Please select a course', { variant: 'warning' });
      return;
    }
    if (activeStep === 1 && selectedUnits.length === 0) {
      enqueueSnackbar('Please select at least one unit', { variant: 'warning' });
      return;
    }
    if (activeStep === STEPS.length - 1) {
      submitMutation.mutate(selectedCourse);
      return;
    }
    setActiveStep((prev) => prev + 1);
  };

  const handleBack = () => {
    setActiveStep((prev) => prev - 1);
  };

  const toggleUnit = (unitId: string) => {
    setSelectedUnits((prev) =>
      prev.includes(unitId) ? prev.filter((id) => id !== unitId) : [...prev, unitId]
    );
  };

  if (statusLoading) return <LoadingSpinner />;

  // If already approved, redirect
  if (status?.isApproved) {
    return (
      <Box p={3}>
        <Alert severity="info">
          You are already enrolled and approved. You can view your enrollment details in your profile.
        </Alert>
        <Button sx={{ mt: 2 }} variant="contained" onClick={() => navigate('/dashboard')}>
          Go to Dashboard
        </Button>
      </Box>
    );
  }

  // If pending approval, show status
  if (status?.isPendingApproval) {
    return (
      <Box p={3}>
        <Alert severity="warning">
          Your enrollment is pending approval. Please wait for an administrator to review your application.
        </Alert>
        <Button sx={{ mt: 2 }} variant="contained" onClick={() => navigate('/enrollment-status')}>
          View Status
        </Button>
      </Box>
    );
  }

  return (
    <Box p={3}>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h4" gutterBottom>
          Course Selection
        </Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Select your course and units for the upcoming semester
        </Typography>

        <Stepper activeStep={activeStep} sx={{ my: 4 }}>
          {STEPS.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        {activeStep === 0 && (
          <Box>
            <Typography variant="h6" gutterBottom>
              Available Courses
            </Typography>
            {coursesLoading ? (
              <CircularProgress />
            ) : (
              <Grid container spacing={2}>
                {courses?.map((course) => (
                  <Grid item xs={12} sm={6} md={4} key={course.id}>
                    <Card
                      variant={selectedCourse === course.id ? 'elevation' : 'outlined'}
                      sx={{
                        cursor: 'pointer',
                        borderColor: selectedCourse === course.id ? 'primary.main' : undefined,
                        borderWidth: selectedCourse === course.id ? 2 : 1,
                      }}
                      onClick={() => setSelectedCourse(course.id)}
                    >
                      <CardContent>
                        <Typography variant="h6">{course.name}</Typography>
                        <Typography color="text.secondary">{course.code}</Typography>
                        {course.description && (
                          <Typography variant="body2" sx={{ mt: 1 }}>
                            {course.description}
                          </Typography>
                        )}
                      </CardContent>
                      <CardActions>
                        <Chip
                          label={selectedCourse === course.id ? 'Selected' : 'Select'}
                          color={selectedCourse === course.id ? 'primary' : 'default'}
                          size="small"
                        />
                      </CardActions>
                    </Card>
                  </Grid>
                ))}
              </Grid>
            )}
          </Box>
        )}

        {activeStep === 1 && (
          <Box>
            <Typography variant="h6" gutterBottom>
              Select Units for {courses?.find((c) => c.id === selectedCourse)?.name}
            </Typography>
            {unitsLoading ? (
              <CircularProgress />
            ) : (
              <List>
                {units?.map((unit) => (
                  <ListItem key={unit.id} dense>
                    <ListItemText
                      primary={unit.name}
                      secondary={`${unit.code} - ${unit.credits} Credits`}
                    />
                    <Checkbox
                      checked={selectedUnits.includes(unit.id)}
                      onChange={() => toggleUnit(unit.id)}
                    />
                  </ListItem>
                ))}
              </List>
            )}
            {selectedUnits.length > 0 && (
              <Typography variant="body2" sx={{ mt: 2 }}>
                Selected {selectedUnits.length} unit(s)
              </Typography>
            )}
          </Box>
        )}

        {activeStep === 2 && (
          <Box>
            <Alert severity="info" sx={{ mb: 2 }}>
              Please review your selections before submitting
            </Alert>
            <Typography variant="subtitle1">
              Course: {courses?.find((c) => c.id === selectedCourse)?.name}
            </Typography>
            <Typography variant="subtitle2" sx={{ mt: 1 }}>
              Selected Units ({selectedUnits.length}):
            </Typography>
            <List dense>
              {units
                ?.filter((u) => selectedUnits.includes(u.id))
                .map((unit) => (
                  <ListItem key={unit.id}>
                    <ListItemText primary={unit.name} secondary={`${unit.code} - ${unit.credits} Credits`} />
                  </ListItem>
                ))}
            </List>
          </Box>
        )}

        <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 3 }}>
          {activeStep > 0 && (
            <Button onClick={handleBack} sx={{ mr: 1 }}>
              Back
            </Button>
          )}
          <Button
            variant="contained"
            onClick={handleNext}
            disabled={submitMutation.isPending}
          >
            {submitMutation.isPending
              ? 'Submitting...'
              : activeStep === STEPS.length - 1
              ? 'Submit'
              : 'Next'}
          </Button>
        </Box>
      </Paper>
    </Box>
  );
};

export default CourseSelectionPage;
