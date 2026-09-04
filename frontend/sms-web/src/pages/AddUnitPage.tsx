import React from 'react';
import { Box, Paper, Typography } from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import { UnitForm } from '../components/Forms/UnitForm';

export const AddUnitPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  const handleSuccess = () => {
    navigate('/units');
  };

  const handleCancel = () => {
    navigate('/units');
  };

  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 4, maxWidth: 900, mx: 'auto' }}>
        <Typography variant='h5' fontWeight={600} sx={{ mb: 3 }}>
          {id ? 'Edit Unit' : 'Add New Unit'}
        </Typography>
        <UnitForm
          unitId={id}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </Paper>
    </Box>
  );
};
