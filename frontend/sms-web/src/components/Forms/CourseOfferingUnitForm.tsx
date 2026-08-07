import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Grid,
  Box,
  Typography,
} from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { courseOfferingService } from '../../services/course-offering.service';

interface CourseOfferingUnitFormProps {
  open: boolean;
  onClose: () => void;
  courseOfferingId: string;
  unit?: any;
}

export const CourseOfferingUnitForm: React.FC<CourseOfferingUnitFormProps> = ({
  open,
  onClose,
  courseOfferingId,
  unit,
}) => {
  const queryClient = useQueryClient();
  const isEdit = !!unit;

  const [formData, setFormData] = useState({
    name: '',
    code: '',
    description: '',
    credits: 0,
    contactHours: 0,
    order: 0,
    learningOutcomes: '',
    assessmentMethods: '',
    assessmentWeighting: '',
    isActive: true,
  });

  useEffect(() => {
    if (unit) {
      setFormData({
        name: unit.name || '',
        code: unit.code || '',
        description: unit.description || '',
        credits: unit.credits || 0,
        contactHours: unit.contactHours || 0,
        order: unit.order || 0,
        learningOutcomes: unit.learningOutcomes || '',
        assessmentMethods: unit.assessmentMethods || '',
        assessmentWeighting: unit.assessmentWeighting || '',
        isActive: unit.isActive ?? true,
      });
    } else {
      setFormData({
        name: '',
        code: '',
        description: '',
        credits: 0,
        contactHours: 0,
        order: 0,
        learningOutcomes: '',
        assessmentMethods: '',
        assessmentWeighting: '',
        isActive: true,
      });
    }
  }, [unit, open]);

  const createMutation = useMutation({
    mutationFn: (data: any) => courseOfferingService.createUnit(courseOfferingId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseoffering', courseOfferingId] });
      onClose();
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: any) => courseOfferingService.updateUnit(unit.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseoffering', courseOfferingId] });
      onClose();
    },
  });

  const handleChange = (field: string, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = () => {
    if (isEdit) {
      updateMutation.mutate(formData);
    } else {
      createMutation.mutate(formData);
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        {isEdit ? 'Edit Course Offering Unit' : 'Add Course Offering Unit'}
      </DialogTitle>
      <DialogContent>
        <Box sx={{ mt: 2 }}>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                size="small"
                label="Unit Name *"
                value={formData.name}
                onChange={(e) => handleChange('name', e.target.value)}
                required
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                size="small"
                label="Unit Code *"
                value={formData.code}
                onChange={(e) => handleChange('code', e.target.value)}
                required
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                size="small"
                label="Description"
                multiline
                rows={2}
                value={formData.description}
                onChange={(e) => handleChange('description', e.target.value)}
              />
            </Grid>
            <Grid item xs={12} sm={4}>
              <TextField
                fullWidth
                size="small"
                label="Credits"
                type="number"
                value={formData.credits}
                onChange={(e) => handleChange('credits', parseInt(e.target.value) || 0)}
              />
            </Grid>
            <Grid item xs={12} sm={4}>
              <TextField
                fullWidth
                size="small"
                label="Contact Hours"
                type="number"
                value={formData.contactHours}
                onChange={(e) => handleChange('contactHours', parseInt(e.target.value) || 0)}
              />
            </Grid>
            <Grid item xs={12} sm={4}>
              <TextField
                fullWidth
                size="small"
                label="Order"
                type="number"
                value={formData.order}
                onChange={(e) => handleChange('order', parseInt(e.target.value) || 0)}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                size="small"
                label="Learning Outcomes"
                multiline
                rows={2}
                value={formData.learningOutcomes}
                onChange={(e) => handleChange('learningOutcomes', e.target.value)}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                size="small"
                label="Assessment Methods"
                multiline
                rows={2}
                value={formData.assessmentMethods}
                onChange={(e) => handleChange('assessmentMethods', e.target.value)}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                size="small"
                label="Assessment Weighting"
                value={formData.assessmentWeighting}
                onChange={(e) => handleChange('assessmentWeighting', e.target.value)}
              />
            </Grid>
          </Grid>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={handleSubmit}
          disabled={isPending || !formData.name || !formData.code}
        >
          {isPending ? 'Saving...' : isEdit ? 'Update' : 'Add'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
