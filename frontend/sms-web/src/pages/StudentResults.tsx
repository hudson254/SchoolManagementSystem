import React from 'react';
import { Box, Typography, Paper, Grid, Chip, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Alert, CircularProgress } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { assessmentService } from '../services/assessment.service';
import { userService } from '../services/user.service';
import type { StudentResult } from '../types/assessment.types';
import { useAuth } from '../hooks/useAuth';

export const StudentResults: React.FC = () => {
  const { user } = useAuth();

  const { data: profile } = useQuery({
    queryKey: ['profile', user?.id],
    queryFn: () => userService.getProfile(),
    enabled: !!user,
  });
  const studentId = (profile as any)?.studentId || user?.id || '';

  const { data: results, isLoading, error } = useQuery({
    queryKey: ['student-results', studentId],
    queryFn: () => assessmentService.getStudentResults(studentId),
    enabled: !!studentId,
  });

  if (isLoading) return <CircularProgress sx={{ m: 4 }} />;
  if (error) return (<Box sx={{ p: 3 }}><Alert severity="error">Could not load results. Please try again.</Alert></Box>);
  if (!results?.length) return (<Box sx={{ p: 3 }}><Alert severity="info">No published results yet.</Alert></Box>);

  return (
    <Box sx={{ p: 0 }}>
      <Typography variant="h4" gutterBottom fontWeight={600}>My Results</Typography>
      <Typography variant="body2" color="textSecondary" gutterBottom>
        Only published results are displayed. Draft, pending-review and unapproved results are never exposed to students.
      </Typography>
      {results.map((r: StudentResult) => (
        <Paper key={r.unitId} sx={{ p: 3, mb: 3 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
            <Typography variant="h6">{r.unitName}</Typography>
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
              <Chip label={r.publicationStatus} size="small" color={r.isPublished ? 'success' : 'default'} />
              {r.isEligibleForCertificate && <Chip label="Certificate Eligible" size="small" color="primary" />}
            </Box>
          </Box>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={6} sm={2}><Typography variant="body2" color="textSecondary">Final Score</Typography><Typography variant="h6">{r.finalScore}%</Typography></Grid>
            <Grid item xs={6} sm={2}><Typography variant="body2" color="textSecondary">Grade</Typography><Typography variant="h6" sx={{ color: r.gradeColor || 'inherit' }}>{r.finalGrade}</Typography></Grid>
            <Grid item xs={12} sm={4}><Typography variant="body2" color="textSecondary">Description</Typography><Typography variant="h6">{r.gradeDescription}</Typography></Grid>
            <Grid item xs={12} sm={4}><Typography variant="body2" color="textSecondary">Weighted Average</Typography><Typography variant="h6">{r.totalWeight}% total weight</Typography></Grid>
          </Grid>
          <TableContainer sx={{ mt: 2 }}>
            <Table size="small">
              <TableHead>
                <TableRow><TableCell>Assessment</TableCell><TableCell>Type</TableCell><TableCell>Mark</TableCell><TableCell>Max</TableCell><TableCell>Weight</TableCell><TableCell>Weighted Contribution</TableCell></TableRow>
              </TableHead>
              <TableBody>
                {r.assessmentMarks.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell>{m.assessmentName || m.assessmentId}</TableCell>
                    <TableCell>{m.entrySource}</TableCell>
                    <TableCell>{m.score}</TableCell>
                    <TableCell>{m.maxScore}</TableCell>
                    <TableCell>{m.weight}%</TableCell>
                    <TableCell>{m.weightedScore}</TableCell>
                  </TableRow>
                ))}
                {!r.assessmentMarks?.length && (<TableRow><TableCell colSpan={6} align="center">No assessment-level marks available.</TableCell></TableRow>)}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      ))}
    </Box>
  );
};