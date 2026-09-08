import React from 'react';
import {
  Box, Paper, Typography, Grid, Chip, Button, Divider, Card, CardContent,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Alert,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon, Book as BookIcon, School as SchoolIcon,
  People as PeopleIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useParams, useNavigate } from 'react-router-dom';
import { courseService } from '../services/course.service';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

export const CourseDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: course, isLoading, isError, refetch } = useQuery({
    queryKey: ['course', id],
    queryFn: () => courseService.getCourse(id!),
    enabled: !!id,
  });
  if (isLoading) return <LoadingSpinner />;
  if (isError || !course) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">Failed to load course details. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>Retry</Button>
        </Alert>
      </Box>
    );
  }
  return (
    <Box sx={{ p: 3 }}>
      <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/courses')} sx={{ mb: 2 }}>Back to Courses</Button>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Grid container spacing={2} alignItems="center">
          <Grid item><SchoolIcon sx={{ fontSize: 60, color: 'primary.main' }} /></Grid>
          <Grid item xs>
            <Typography variant="h5" fontWeight={600}>{course.name}</Typography>
            <Typography variant="body2" color="textSecondary">{course.code}</Typography>
            <Box sx={{ mt: 1, display: 'flex', gap: 1 }}>
              <Chip label={course.isActive ? 'Active' : 'Inactive'} color={course.isActive ? 'success' : 'default'} size="small" />
              {course.departmentName && <Chip label={course.departmentName} size="small" variant="outlined" />}
            </Box>
          </Grid>
        </Grid>
      </Paper>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" fontWeight={600} gutterBottom>Course Information</Typography>
        <Divider sx={{ mb: 2 }} />
        <Grid container spacing={2}>
          <Grid item xs={12} sm={6}>
            <Typography variant="body2" color="textSecondary">Description</Typography>
            <Typography>{course.description || 'No description available.'}</Typography>
          </Grid>
          <Grid item xs={12} sm={3}>
            <Typography variant="body2" color="textSecondary">Duration</Typography>
            <Typography>{course.duration ? `${course.duration} years` : 'N/A'}</Typography>
          </Grid>
          <Grid item xs={12} sm={3}>
            <Typography variant="body2" color="textSecondary">Credits</Typography>
            <Typography>{course.totalCredits ?? 'N/A'}</Typography>
          </Grid>
        </Grid>
      </Paper>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" fontWeight={600} gutterBottom>Units</Typography>
        <Divider sx={{ mb: 2 }} />
        {course.units && course.units.length > 0 ? (
          <TableContainer><Table><TableHead><TableRow><TableCell>Code</TableCell><TableCell>Name</TableCell><TableCell>Credits</TableCell></TableRow></TableHead>
            <TableBody>{course.units.map((unit: any) => (<TableRow key={unit.id}><TableCell>{unit.code}</TableCell><TableCell>{unit.name}</TableCell><TableCell>{unit.credits}</TableCell></TableRow>))}</TableBody></Table></TableContainer>
        ) : <Alert severity="info">No units associated with this course.</Alert>}
      </Paper>
      <Grid container spacing={2}>
        {course.totalUnits > 0 && (
          <Grid item xs={12} sm={4}>
            <Card><CardContent><Typography variant="h4" color="primary" fontWeight={600}>{course.totalUnits}</Typography><Typography variant="body2" color="textSecondary">Total Units</Typography></CardContent></Card>
          </Grid>
        )}
      </Grid>
    </Box>
  );
};