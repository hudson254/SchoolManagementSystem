import React from 'react';
import { Box, Alert, Button, LinearProgress, Typography } from '@mui/material';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { StudyMaterialsSection } from '../components/StudyMaterials/StudyMaterialsSection';

/**
 * Study Materials page for a single unit. Reached from the course-offering
 * Units tab and from dashboard course cards. Unit display info is carried in
 * query params so the page works for every role (students cannot fetch unit
 * details through the moderator-only unit endpoints).
 */
export const UnitStudyMaterialsPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const unitName = searchParams.get('name') || undefined;
  const unitCode = searchParams.get('code') || undefined;

  if (!id) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">Unit ID is missing.</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3, maxWidth: 900, mx: 'auto' }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5" fontWeight={600}>
          {unitName ? `${unitCode ? `${unitCode} — ` : ''}${unitName}` : 'Unit Study Materials'}
        </Typography>
        <Button variant="outlined" onClick={() => navigate(-1)}>
          Back
        </Button>
      </Box>
      <React.Suspense fallback={<LinearProgress />}>
        <StudyMaterialsSection unitId={id} unitName={unitName} unitCode={unitCode} />
      </React.Suspense>
    </Box>
  );
};

export default UnitStudyMaterialsPage;
