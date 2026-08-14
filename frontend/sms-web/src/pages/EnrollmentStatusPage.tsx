import React from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Alert,
  Chip,
  CircularProgress,
  Card,
  CardContent,
  Stepper,
  Step,
  StepLabel,
  StepIcon,
} from '@mui/material';
import {
  CheckCircle,
  HourglassEmpty,
  Cancel,
  School,
  ArrowForward,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { enrollmentService, StudentEnrollmentStatus } from '../services/enrollment.service';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

const STATUS_STEPS = ['Register', 'Select Course', 'Pending Approval', 'Approved'];

export const EnrollmentStatusPage: React.FC = () => {
  const navigate = useNavigate();

  const { data: status, isLoading, error } = useQuery<StudentEnrollmentStatus>({
    queryKey: ['my-enrollment-status'],
    queryFn: () => enrollmentService.getMyStatus(),
    refetchInterval: 30000, // Poll every 30s
  });

  if (isLoading) return <LoadingSpinner />;

  if (error) {
    return (
      <Box p={3}>
        <Alert severity="error">
          Failed to load enrollment status. Please try again later.
        </Alert>
        <Button sx={{ mt: 2 }} variant="contained" onClick={() => navigate('/dashboard')}>
          Go to Dashboard
        </Button>
      </Box>
    );
  }

  const getActiveStep = (): number => {
    if (!status) return 0;
    if (status.isApproved) return 3;
    if (status.isPendingApproval) return 2;
    if (status.hasSelectedCourse) return 1;
    return 0;
  };

  const getStatusChip = () => {
    if (!status) return null;
    if (status.isApproved) return <Chip icon={<CheckCircle />} label="Approved" color="success" />;
    if (status.isPendingApproval) return <Chip icon={<HourglassEmpty />} label="Pending Approval" color="warning" />;
    if (status.needsCourseSelection) return <Chip icon={<School />} label="Course Selection Required" color="info" />;
    return <Chip label={status.registrationStatus} color="default" />;
  };

  return (
    <Box p={3}>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Typography variant="h4">Enrollment Status</Typography>
          {getStatusChip()}
        </Box>

        {status?.message && (
          <Alert severity="info" sx={{ mb: 3 }}>
            {status.message}
          </Alert>
        )}

        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>Student Information</Typography>
            <Typography><strong>Name:</strong> {status?.fullName}</Typography>
            <Typography><strong>Student Number:</strong> {status?.studentNumber}</Typography>
            <Typography><strong>Email:</strong> {status?.email}</Typography>
            {status?.selectedCourseName && (
              <Typography><strong>Selected Course:</strong> {status.selectedCourseName}</Typography>
            )}
            <Typography><strong>Units Enrolled:</strong> {status?.unitsCount || 0}</Typography>
          </CardContent>
        </Card>

        <Stepper activeStep={getActiveStep()} alternativeLabel sx={{ mb: 4 }}>
          {STATUS_STEPS.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        {status?.needsCourseSelection && (
          <Box textAlign="center" mt={3}>
            <Typography variant="body1" gutterBottom>
              You need to select a course and units to continue.
            </Typography>
            <Button
              variant="contained"
              size="large"
              endIcon={<ArrowForward />}
              onClick={() => navigate('/course-selection')}
            >
              Select Course Now
            </Button>
          </Box>
        )}

        {status?.isPendingApproval && (
          <Alert severity="warning" sx={{ mt: 2 }}>
            Your enrollment is pending approval from an administrator. You will be notified once
            your registration has been reviewed. This page refreshes automatically.
          </Alert>
        )}

        {status?.isApproved && (
          <Box textAlign="center" mt={3}>
            <Alert severity="success" sx={{ mb: 2 }}>
              Your registration has been approved! You can now access all system features.
            </Alert>
            <Button
              variant="contained"
              onClick={() => navigate('/dashboard')}
            >
              Go to Dashboard
            </Button>
          </Box>
        )}
      </Paper>
    </Box>
  );
};

export default EnrollmentStatusPage;
