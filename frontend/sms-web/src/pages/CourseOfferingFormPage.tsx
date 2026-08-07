import React from 'react';
import { Box, Paper, Typography } from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import { CourseOfferingForm } from '../components/Forms/CourseOfferingForm';

export const CourseOfferingFormPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  const handleSuccess = (offeringId?: string) => {
    if (offeringId) {
      navigate(`/course-offerings/${offeringId}`);
    } else {
      navigate('/course-offerings');
    }
  };

  const handleCancel = () => {
    navigate('/course-offerings');
  };

  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 4, maxWidth: 900, mx: 'auto' }}>
        <Typography variant="h5" fontWeight={600} sx={{ mb: 3 }}>
          {id ? 'Edit Course Offering' : 'New Course Offering'}
        </Typography>
        <CourseOfferingForm
          offeringId={id}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </Paper>
    </Box>
  );
};
