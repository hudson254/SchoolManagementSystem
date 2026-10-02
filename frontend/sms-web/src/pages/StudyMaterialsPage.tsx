import React, { useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  InputAdornment,
  LinearProgress,
  TextField,
  Typography,
} from '@mui/material';
import {
  MenuBook as MenuBookIcon,
  Search as SearchIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { studyMaterialService, StudyMaterialUnit } from '../services/studyMaterial.service';
import { useAuth } from '../hooks/useAuth';
import { hasAnyRole, LECTURER } from '../utils/roles';
import { normalizeError } from '../utils/errors';

/**
 * Study Materials entry point (Academics → Study Materials).
 *
 * Lists ONLY the units the signed-in user is entitled to — for a lecturer the
 * units they are appointed to teach, for a student the units they are enrolled
 * to study. The list is produced by the server from the same persisted
 * relationships that authorize the upload/list/download endpoints, so this
 * selector can never surface a unit the API would reject, and it cannot be used
 * to probe for units outside the caller's academic scope: the unit id in the URL
 * is re-authorized on every request.
 *
 * Choosing a unit navigates to the existing per-unit materials page, which hosts
 * StudyMaterialsSection (list, download, upload).
 */
export const StudyMaterialsPage: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [search, setSearch] = useState('');

  const canUpload = hasAnyRole(user?.roles, LECTURER);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['study-materials', 'my-units'],
    queryFn: () => studyMaterialService.getMyUnits(),
    retry: false,
  });

  const units: StudyMaterialUnit[] = Array.isArray(data) ? data : [];

  const visibleUnits = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return units;
    return units.filter(
      (u) =>
        u.code?.toLowerCase().includes(term) ||
        u.name?.toLowerCase().includes(term) ||
        u.courseName?.toLowerCase().includes(term)
    );
  }, [units, search]);

  const openUnit = (unit: StudyMaterialUnit) => {
    const params = new URLSearchParams({ name: unit.name ?? '', code: unit.code ?? '' });
    navigate(`/units/${unit.unitId}/materials?${params.toString()}`);
  };

  return (
    <Box sx={{ p: 3, maxWidth: 1100, mx: 'auto' }}>
      <Box sx={{ mb: 3 }}>
        <Typography variant="h5" fontWeight={600}>
          Study Materials
        </Typography>
        <Typography variant="body2" color="textSecondary" sx={{ mt: 0.5 }}>
          {canUpload
            ? 'Units you are appointed to teach. Open a unit to upload or manage its lecture notes and handouts.'
            : 'Units you are enrolled to study. Open a unit to view and download its lecture notes and handouts.'}
        </Typography>
      </Box>

      {!isLoading && units.length > 0 && (
        <TextField
          fullWidth
          size="small"
          placeholder="Search by unit code, name or course"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ mb: 2 }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon fontSize="small" />
              </InputAdornment>
            ),
          }}
        />
      )}

      {isLoading ? (
        <LinearProgress />
      ) : isError ? (
        <Alert severity="error">
          {normalizeError(error).message ||
            'Study materials are not available for your account.'}
        </Alert>
      ) : units.length === 0 ? (
        <Alert severity="info">
          {canUpload
            ? 'You are not currently appointed to teach any unit, so there are no study materials to manage yet.'
            : 'You are not currently enrolled in any unit, so there are no study materials available yet.'}
        </Alert>
      ) : visibleUnits.length === 0 ? (
        <Alert severity="info">No units match “{search}”.</Alert>
      ) : (
        <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' } }}>
          {visibleUnits.map((unit) => (
            <Card key={unit.unitId} variant="outlined" sx={{ borderRadius: 2 }}>
              <CardActionArea onClick={() => openUnit(unit)} sx={{ height: '100%' }}>
                <CardContent>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 1 }}>
                    <Box sx={{ minWidth: 0 }}>
                      <Typography variant="subtitle1" fontWeight={600} noWrap>
                        {unit.code ? `${unit.code} — ` : ''}
                        {unit.name}
                      </Typography>
                      {unit.courseName && (
                        <Typography variant="body2" color="textSecondary" noWrap>
                          {unit.courseName}
                        </Typography>
                      )}
                    </Box>
                    <MenuBookIcon color="primary" />
                  </Box>
                  <Box sx={{ display: 'flex', gap: 1, mt: 1.5, flexWrap: 'wrap' }}>
                    <Chip
                      size="small"
                      variant="outlined"
                      label={unit.accessRole === 'Lecturer' ? 'You teach this unit' : 'Enrolled'}
                    />
                    {unit.credits > 0 && (
                      <Chip size="small" variant="outlined" label={`${unit.credits} credits`} />
                    )}
                  </Box>
                </CardContent>
              </CardActionArea>
            </Card>
          ))}
        </Box>
      )}
    </Box>
  );
};

export default StudyMaterialsPage;