import React from 'react';
import {
  Box,
  Paper,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Button,
  Alert,
  Card,
  CardContent,
} from '@mui/material';
import { History, School, ArrowForward } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { enrollmentService, CourseHistory } from '../services/enrollment.service';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { useAuth } from '../hooks/useAuth';

export const CourseHistoryPage: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const { data: history, isLoading, error } = useQuery<CourseHistory>({
    queryKey: ['course-history'],
    queryFn: () => enrollmentService.getCourseHistory(),
  });

  if (isLoading) return <LoadingSpinner />;

  if (error) {
    return (
      <Box p={3}>
        <Alert severity="error">
          Failed to load course history. Please try again later.
        </Alert>
        <Button sx={{ mt: 2 }} variant="contained" onClick={() => navigate('/dashboard')}>
          Go to Dashboard
        </Button>
      </Box>
    );
  }

  return (
    <Box p={3}>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Box display="flex" alignItems="center" gap={1}>
            <History />
            <Typography variant="h4">Course History</Typography>
          </Box>
          <Button
            variant="contained"
            startIcon={<School />}
            onClick={() => navigate('/enrollment-status')}
          >
            Current Enrollment
          </Button>
        </Box>

        {history?.message && (
          <Alert severity="info" sx={{ mb: 2 }}>
            {history.message}
          </Alert>
        )}

        {history && (
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>Student Information</Typography>
              <Typography><strong>Name:</strong> {history.fullName}</Typography>
              <Typography><strong>Student Number:</strong> {history.studentNumber}</Typography>
              <Typography><strong>Total Enrollments:</strong> {history.totalCount}</Typography>
            </CardContent>
          </Card>
        )}

        {history?.enrollments && history.enrollments.length > 0 ? (
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Course</TableCell>
                  <TableCell>Code</TableCell>
                  <TableCell>Semester</TableCell>
                  <TableCell>Enrolled Date</TableCell>
                  <TableCell>Status</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {history.enrollments.map((enrollment, index) => (
                  <TableRow key={index}>
                    <TableCell>{enrollment.courseName}</TableCell>
                    <TableCell>{enrollment.courseCode}</TableCell>
                    <TableCell>{enrollment.semesterName}</TableCell>
                    <TableCell>{new Date(enrollment.enrolledDate).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <Chip
                        label={enrollment.status}
                        color={enrollment.status === 'Active' ? 'success' : 'default'}
                        size="small"
                      />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        ) : (
          <Box textAlign="center" py={4}>
            <Typography variant="body1" color="text.secondary" gutterBottom>
              No course history found.
            </Typography>
            <Button
              variant="contained"
              endIcon={<ArrowForward />}
              onClick={() => navigate('/course-selection')}
            >
              Enroll in a Course
            </Button>
          </Box>
        )}
      </Paper>
    </Box>
  );
};

export default CourseHistoryPage;
