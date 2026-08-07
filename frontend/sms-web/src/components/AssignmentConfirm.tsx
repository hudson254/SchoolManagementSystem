import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Box,
  Chip,
  Divider,
  Alert,
  TextField,
} from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { confirmationService, PendingEnrollment } from '../services/confirmation.service';
import { useAuth } from '../hooks/useAuth';

interface AssignmentConfirmProps {
  open: boolean;
  onClose: () => void;
  pending: PendingEnrollment;
  type: 'enrollment' | 'teaching';
}

export const AssignmentConfirm: React.FC<AssignmentConfirmProps> = ({
  open,
  onClose,
  pending,
  type,
}) => {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const [issueDescription, setIssueDescription] = useState('');
  const [showIssueForm, setShowIssueForm] = useState(false);

  const isEnrollment = type === 'enrollment';

  const confirmMutation = useMutation({
    mutationFn: () =>
      isEnrollment
        ? confirmationService.confirmEnrollment(pending.id, { confirm: true })
        : confirmationService.confirmTeaching(pending.id, { confirm: true }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-confirmations'] });
      onClose();
    },
  });

  const reportIssueMutation = useMutation({
    mutationFn: () =>
      confirmationService.reportIssue({
        reporterUserId: user?.id || '',
        assignmentType: isEnrollment ? 'Enrollment' : 'Teaching',
        courseOfferingId: pending.courseOfferingId || pending.id,
        courseOfferingEnrollmentId: isEnrollment ? pending.id : undefined,
        courseOfferingLecturerId: !isEnrollment ? pending.id : undefined,
        reason: issueDescription,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-confirmations'] });
      onClose();
    },
  });

  const handleConfirm = () => {
    confirmMutation.mutate();
  };

  const handleReportIssue = () => {
    reportIssueMutation.mutate();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        {isEnrollment ? 'Enrollment Confirmation' : 'Teaching Assignment Confirmation'}
      </DialogTitle>
      <DialogContent>
        <Alert severity="info" sx={{ mb: 2 }}>
          {isEnrollment
            ? 'You have been enrolled in the following course. Please confirm that this enrollment is correct.'
            : 'You have been assigned to teach the following course. Please confirm this teaching assignment.'}
        </Alert>

        <Box sx={{ p: 2, bgcolor: 'background.default', borderRadius: 1 }}>
          <Typography variant="h6">{pending.courseName}</Typography>
          <Box sx={{ display: 'flex', gap: 1, mt: 1, flexWrap: 'wrap' }}>
            <Chip label={`Year: ${pending.academicYearName}`} size="small" />
            <Chip label={`Semester: ${pending.semesterName}`} size="small" />
            <Chip label={pending.offeringCode} size="small" variant="outlined" />
          </Box>
        </Box>

        <Divider sx={{ my: 2 }} />

        {showIssueForm ? (
          <Box>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Describe the issue
            </Typography>
            <TextField
              fullWidth
              multiline
              rows={3}
              placeholder="Please describe the issue with this assignment..."
              value={issueDescription}
              onChange={(e) => setIssueDescription(e.target.value)}
            />
          </Box>
        ) : (
          <Typography variant="body2" color="textSecondary">
            If you believe this assignment is incorrect, you may report an issue. A moderator or
            administrator will review your report.
          </Typography>
        )}
      </DialogContent>
      <DialogActions>
        {showIssueForm ? (
          <>
            <Button onClick={() => setShowIssueForm(false)}>Back</Button>
            <Button
              variant="contained"
              color="warning"
              onClick={handleReportIssue}
              disabled={reportIssueMutation.isPending || !issueDescription.trim()}
            >
              {reportIssueMutation.isPending ? 'Submitting...' : 'Submit Issue Report'}
            </Button>
          </>
        ) : (
          <>
            <Button
              color="warning"
              onClick={() => setShowIssueForm(true)}
              disabled={confirmMutation.isPending}
            >
              Report an Issue
            </Button>
            <Button
              variant="contained"
              color="primary"
              onClick={handleConfirm}
              disabled={confirmMutation.isPending}
            >
              {confirmMutation.isPending
                ? 'Confirming...'
                : isEnrollment
                  ? 'Confirm Enrollment'
                  : 'Accept Teaching Assignment'}
            </Button>
          </>
        )}
      </DialogActions>
    </Dialog>
  );
};
