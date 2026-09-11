import React from 'react';
import {
  Box,
  Paper,
  Typography,
  Chip,
  Divider,
  Button,
  Grid,
  LinearProgress,
  Alert,
} from '@mui/material';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { assignmentService } from '../services/assignment.service';
import { useAuth } from '../hooks/useAuth';
import { hasAnyRole, LECTURER, COORDINATOR } from '../utils/roles';

export const AssignmentDetailPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const canManageAssignments = hasAnyRole(user?.roles, LECTURER, COORDINATOR);

  const { data: assignment, isLoading, isError, refetch } = useQuery({
    queryKey: ['assignment', id],
    queryFn: () => assignmentService.getAssignment(id!),
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <Box sx={{ p: 3 }}>
        <LinearProgress />
      </Box>
    );
  }

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load assignment details. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const a = assignment;

  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 4, maxWidth: 900, mx: 'auto' }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h5" fontWeight={600}>
            Assignment Details
          </Typography>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button variant="outlined" onClick={() => navigate('/assignments')}>
              Back to Assignments
            </Button>
            {canManageAssignments && (
              <Button
                variant="contained"
                onClick={() => navigate(`/assignments/${id}/edit`)}
              >
                Edit Assignment
              </Button>
            )}
          </Box>
        </Box>
        <Divider sx={{ mb: 2 }} />

        {!a ? (
          <Alert severity="warning">Assignment not found.</Alert>
        ) : (
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <Typography variant="h6" fontWeight={700}>
                {a.title}
              </Typography>
              <Chip
                label={a.status}
                color={a.status === 'Published' ? 'info' : a.status === 'Open' ? 'success' : 'default'}
                size="small"
                sx={{ ml: 1 }}
              />
            </Grid>
            <Grid item xs={12}>
              <Typography variant="body2" color="textSecondary">
                {a.description}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Unit
              </Typography>
              <Typography variant="body1">
                {a.unitName} ({a.unitCode})
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Lecturer
              </Typography>
              <Typography variant="body1">{a.lecturerName}</Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Semester
              </Typography>
              <Typography variant="body1">{a.semesterName}</Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Due Date
              </Typography>
              <Typography variant="body1">
                {a.dueDate ? new Date(a.dueDate).toLocaleString() : '—'}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Closing Date
              </Typography>
              <Typography variant="body1">
                {a.closingDate ? new Date(a.closingDate).toLocaleString() : '—'}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Weight / Max Score
              </Typography>
              <Typography variant="body1">
                {a.weight}% / {a.maxScore}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Submissions
              </Typography>
              <Typography variant="body1">
                {a.submissionCount || 0} submitted
                {a.gradedCount !== undefined && ` • ${a.gradedCount} graded`}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Late Submission
              </Typography>
              <Typography variant="body1">
                {a.allowLateSubmission
                  ? `Allowed (${a.latePenaltyPercent}% penalty)`
                  : 'Not allowed'}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Graded Automatically
              </Typography>
              <Typography variant="body1">{a.isGraded ? 'Yes' : 'No'}</Typography>
            </Grid>
            {a.instructions && (
              <Grid item xs={12}>
                <Typography variant="caption" color="textSecondary" display="block">
                  Instructions
                </Typography>
                <Typography variant="body2" whiteSpace="preWrap">
                  {a.instructions}
                </Typography>
              </Grid>
            )}
          </Grid>
        )}
      </Paper>
    </Box>
  );
};