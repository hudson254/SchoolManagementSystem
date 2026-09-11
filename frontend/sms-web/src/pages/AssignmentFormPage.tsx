import React from 'react';
import { Box, Paper, Typography } from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import { AssignmentForm } from '../components/Forms/AssignmentForm';

export const AssignmentFormPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  const handleSuccess = () => {
    navigate('/assignments');
  };

  const handleCancel = () => {
    navigate('/assignments');
  };

  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 4, maxWidth: 900, mx: 'auto' }}>
        <Typography variant="h5" fontWeight={600} sx={{ mb: 3 }}>
          {id ? 'Edit Assignment' : 'New Assignment'}
        </Typography>
        <AssignmentForm
          assignmentId={id}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </Paper>
    </Box>
  );
};