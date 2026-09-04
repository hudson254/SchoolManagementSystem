import React from 'react';
import { Box, Paper, Typography } from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import { StudentForm } from '../components/Forms/StudentForm';

export const AddStudentPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  const handleSuccess = () => {
    navigate('/students');
  };

  const handleCancel = () => {
    navigate('/students');
  };

  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 4, maxWidth: 900, mx: 'auto' }}>
        <Typography variant='h5' fontWeight={600} sx={{ mb: 3 }}>
          {id ? 'Edit Student' : 'Add New Student'}
        </Typography>
        <StudentForm
          studentId={id}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </Paper>
    </Box>
  );
};
