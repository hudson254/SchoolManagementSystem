import React from 'react';
import { Box, Paper, Typography } from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import { CourseForm } from '../components/Forms/CourseForm';

export const AddCoursePage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  const handleSuccess = () => {
    navigate('/courses');
  };

  const handleCancel = () => {
    navigate('/courses');
  };

  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 4, maxWidth: 900, mx: 'auto' }}>
        <Typography variant='h5' fontWeight={600} sx={{ mb: 3 }}>
          {id ? 'Edit Course' : 'Add New Course'}
        </Typography>
        <CourseForm
          courseId={id}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </Paper>
    </Box>
  );
};
