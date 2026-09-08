import React from 'react';
import {
  Box, Paper, Typography, Grid, Avatar, Chip, Button, Divider,
  Card, CardContent, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Alert,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon, Email as EmailIcon, Phone as PhoneIcon,
  Verified as VerifiedIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useParams, useNavigate } from 'react-router-dom';
import { lecturerService } from '../services/lecturer.service';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';


export const LecturerDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: lecturer, isLoading, isError, refetch } = useQuery({
    queryKey: ['lecturer', id],
    queryFn: () => lecturerService.getLecturer(id!),
    enabled: !!id,
  });
  if (isLoading) return <LoadingSpinner />;
  if (isError || !lecturer) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load lecturer details. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>Retry</Button>
        </Alert>
      </Box>
    );
  }
  return (
    <Box sx={{ p: 3 }}>
      <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/lecturers')} sx={{ mb: 2 }}>
        Back to Lecturers
      </Button>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Grid container spacing={3} alignItems="center">
          <Grid item>
            <Avatar sx={{ width: 80, height: 80, bgcolor: 'primary.main', fontSize: '2rem' }}>
              {lecturer.firstName?.[0]}{lecturer.lastName?.[0]}
            </Avatar>
          </Grid>
          <Grid item xs>
            <Typography variant="h5" fontWeight={600}>{lecturer.firstName} {lecturer.lastName}</Typography>
            <Typography variant="body2" color="textSecondary">{lecturer.employeeNumber}</Typography>
            <Box sx={{ mt: 1, display: 'flex', gap: 1 }}>
              {lecturer.isVerified && <Chip icon={<VerifiedIcon />} label="Verified" color="success" size="small" />}
              <Chip label={lecturer.isActive ? 'Active' : 'Inactive'} color={lecturer.isActive ? 'success' : 'default'} size="small" />
            </Box>
          </Grid>
        </Grid>
      </Paper>
<Paper sx={{ p: 3, mb: 3 }}><Typography variant="h6" fontWeight={600} gutterBottom>Contact Information</Typography><Divider sx={{ mb: 2 }} />
        <Grid container spacing={2}>
          <Grid item xs={12} sm={6}><Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}><EmailIcon color="action" /><Typography>{lecturer.email}</Typography></Box></Grid>
          <Grid item xs={12} sm={6}><Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}><PhoneIcon color="action" /><Typography>{lecturer.phoneNumber || 'N/A'}</Typography></Box></Grid>
        </Grid>
      </Paper>
      <Paper sx={{ p: 3, mb: 3 }}><Typography variant="h6" fontWeight={600} gutterBottom>Professional Information</Typography><Divider sx={{ mb: 2 }} />
        <Grid container spacing={2}>
          <Grid item xs={12} sm={6}><Typography variant="body2" color="textSecondary">Specialization</Typography><Typography>{lecturer.specialization || 'N/A'}</Typography></Grid>
          <Grid item xs={12} sm={6}><Typography variant="body2" color="textSecondary">Qualifications</Typography><Typography>{lecturer.qualifications || 'N/A'}</Typography></Grid>
          <Grid item xs={12} sm={6}><Typography variant="body2" color="textSecondary">Hire Date</Typography><Typography>{lecturer.hireDate ? new Date(lecturer.hireDate).toLocaleDateString() : 'N/A'}</Typography></Grid>
        </Grid>
      </Paper>
      <Paper sx={{ p: 3, mb: 3 }}><Typography variant="h6" fontWeight={600} gutterBottom>Units Allocated</Typography><Divider sx={{ mb: 2 }} />
        {lecturer.units && lecturer.units.length > 0 ? (
          <TableContainer><Table><TableHead><TableRow><TableCell>Code</TableCell><TableCell>Name</TableCell><TableCell>Credits</TableCell><TableCell>Semester</TableCell></TableRow></TableHead>
            <TableBody>{lecturer.units.map((unit: any) => (<TableRow key={unit.id}><TableCell>{unit.code}</TableCell><TableCell>{unit.name}</TableCell><TableCell>{unit.credits}</TableCell><TableCell>{unit.semesterName}</TableCell></TableRow>))}</TableBody></Table></TableContainer>
        ) : <Alert severity="info">No units allocated.</Alert>}
      </Paper>
      {lecturer.totalUnitsAllocated > 0 && (
        <Paper sx={{ p: 3 }}><Typography variant="h6" fontWeight={600} gutterBottom>Summary</Typography><Divider sx={{ mb: 2 }} />
          <Grid container spacing={2}>
            <Grid item xs={12} sm={4}><Card variant="outlined"><CardContent><Typography variant="h4" color="primary" fontWeight={600}>{lecturer.currentUnitsCount || 0}</Typography><Typography variant="body2" color="textSecondary">Current Units</Typography></CardContent></Card></Grid>
            <Grid item xs={12} sm={4}><Card variant="outlined"><CardContent><Typography variant="h4" color="primary" fontWeight={600}>{lecturer.totalUnitsAllocated}</Typography><Typography variant="body2" color="textSecondary">Total Allocations</Typography></CardContent></Card></Grid>
          </Grid>
        </Paper>
      )}
    </Box>
  );
};