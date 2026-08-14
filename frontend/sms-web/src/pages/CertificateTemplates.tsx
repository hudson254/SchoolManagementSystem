import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  CircularProgress,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Tooltip,
  Switch,
  FormControlLabel,
  MenuItem,
} from '@mui/material';
import { Add, Edit, Delete, Description } from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { certificateService } from '../services/certificate.service';
import {
  CertificateTemplate,
  CertificateTemplateRequest,
} from '../types/certificate.types';

const emptyForm: CertificateTemplateRequest = {
  name: '',
  description: '',
  version: '1.0',
  type: 'Certificate',
  status: 'Active',
  filePath: '',
  logoPath: '',
  watermarkPath: '',
  fieldMappings: '{}',
  isDefault: false,
  courseId: undefined,
};

export const CertificateTemplates: React.FC = () => {
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<CertificateTemplate | null>(null);
  const [form, setForm] = useState<CertificateTemplateRequest>(emptyForm);
  const [deleteDialog, setDeleteDialog] = useState<CertificateTemplate | null>(null);

  const { data: templates, isLoading } = useQuery({
    queryKey: ['certificate-templates'],
    queryFn: () => certificateService.getTemplates(),
  });

  const createMutation = useMutation({
    mutationFn: (data: CertificateTemplateRequest) => certificateService.createTemplate(data),
    onSuccess: () => {
      enqueueSnackbar('Template created successfully', { variant: 'success' });
      queryClient.invalidateQueries({ queryKey: ['certificate-templates'] });
      setDialogOpen(false);
      setForm(emptyForm);
    },
    onError: (err: any) => enqueueSnackbar(err?.message || 'Failed to create template', { variant: 'error' }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: CertificateTemplateRequest }) =>
      certificateService.updateTemplate(id, data),
    onSuccess: () => {
      enqueueSnackbar('Template updated successfully', { variant: 'success' });
      queryClient.invalidateQueries({ queryKey: ['certificate-templates'] });
      setDialogOpen(false);
      setForm(emptyForm);
      setEditing(null);
    },
    onError: (err: any) => enqueueSnackbar(err?.message || 'Failed to update template', { variant: 'error' }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => certificateService.deleteTemplate(id),
    onSuccess: () => {
      enqueueSnackbar('Template deleted', { variant: 'success' });
      queryClient.invalidateQueries({ queryKey: ['certificate-templates'] });
      setDeleteDialog(null);
    },
    onError: (err: any) => enqueueSnackbar(err?.message || 'Failed to delete template', { variant: 'error' }),
  });

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm);
    setDialogOpen(true);
  };

  const openEdit = (template: CertificateTemplate) => {
    setEditing(template);
    setForm({
      name: template.name,
      description: template.description,
      version: template.version,
      type: template.type,
      status: template.status,
      filePath: template.filePath,
      logoPath: template.logoPath,
      watermarkPath: template.watermarkPath,
      fieldMappings: template.fieldMappings,
      isDefault: template.isDefault,
      courseId: template.courseId,
    });
    setDialogOpen(true);
  };

  const handleSave = () => {
    if (!form.name.trim() || !form.type.trim() || !form.filePath.trim()) {
      enqueueSnackbar('Please fill in Name, Type, and File Path', { variant: 'warning' });
      return;
    }
    if (editing) {
      updateMutation.mutate({ id: editing.id, data: form });
    } else {
      createMutation.mutate(form);
    }
  };

  const handleChange = (field: keyof CertificateTemplateRequest) => (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const value = e.target.type === 'checkbox' ? e.target.checked : e.target.value;
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h4" fontWeight={600}>
          Certificate Templates
        </Typography>
        <Button variant="contained" startIcon={<Add />} onClick={openCreate}>
          New Template
        </Button>
      </Box>
      <Typography variant="body2" color="textSecondary" gutterBottom>
        Manage certificate templates used for generating certificates.
      </Typography>

      <Card>
        <CardContent>
          {isLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress />
            </Box>
          ) : (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Name</TableCell>
                    <TableCell>Type</TableCell>
                    <TableCell>Version</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Default</TableCell>
                    <TableCell>Updated</TableCell>
                    <TableCell align="right">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {templates && templates.length > 0 ? (
                    templates.map((tpl) => (
                      <TableRow key={tpl.id} hover>
                        <TableCell>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Description fontSize="small" color="primary" />
                            <Box>
                              <Typography variant="body2" fontWeight={600}>{tpl.name}</Typography>
                              <Typography variant="caption" color="textSecondary">{tpl.description}</Typography>
                            </Box>
                          </Box>
                        </TableCell>
                        <TableCell>{tpl.type}</TableCell>
                        <TableCell>{tpl.version}</TableCell>
                        <TableCell>
                          <Chip label={tpl.status} color={tpl.status === 'Active' ? 'success' : 'default'} size="small" />
                        </TableCell>
                        <TableCell>{tpl.isDefault ? 'Yes' : 'No'}</TableCell>
                        <TableCell>{tpl.updatedAt ? new Date(tpl.updatedAt).toLocaleDateString() : '-'}</TableCell>
                        <TableCell align="right">
                          <Tooltip title="Edit">
                            <IconButton size="small" onClick={() => openEdit(tpl)}>
                              <Edit fontSize="small" />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="Delete">
                            <IconButton size="small" onClick={() => setDeleteDialog(tpl)}>
                              <Delete fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={7} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                        <Description sx={{ fontSize: 40, mb: 1, opacity: 0.4 }} />
                        <Typography variant="body2">No templates found. Create one to get started.</Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      {/* Create/Edit Dialog */}
      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>{editing ? 'Edit Template' : 'New Template'}</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Name *"
                value={form.name}
                onChange={handleChange('name')}
                size="small"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Type *"
                value={form.type}
                onChange={handleChange('type')}
                size="small"
                select
              >
                <MenuItem value="Certificate">Certificate</MenuItem>
                <MenuItem value="Completion">Completion</MenuItem>
                <MenuItem value="Transcript">Transcript</MenuItem>
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Version"
                value={form.version}
                onChange={handleChange('version')}
                size="small"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Status"
                value={form.status}
                onChange={handleChange('status')}
                size="small"
                select
              >
                <MenuItem value="Active">Active</MenuItem>
                <MenuItem value="Inactive">Inactive</MenuItem>
              </TextField>
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={form.description}
                onChange={handleChange('description')}
                size="small"
                multiline
                rows={2}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Template File Path *"
                value={form.filePath}
                onChange={handleChange('filePath')}
                size="small"
                placeholder="/templates/certificate-v1.pdf"
                helperText="Path to the PDF template file"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Logo Path"
                value={form.logoPath}
                onChange={handleChange('logoPath')}
                size="small"
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Watermark Path"
                value={form.watermarkPath}
                onChange={handleChange('watermarkPath')}
                size="small"
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Field Mappings (JSON)"
                value={form.fieldMappings}
                onChange={handleChange('fieldMappings')}
                size="small"
                multiline
                rows={3}
                placeholder='{"studentName": {"x": 100, "y": 200}}'
              />
            </Grid>
            <Grid item xs={12}>
              <FormControlLabel
                control={
                  <Switch
                    checked={form.isDefault}
                    onChange={handleChange('isDefault')}
                  />
                }
                label="Set as default template"
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            disabled={createMutation.isPending || updateMutation.isPending}
            onClick={handleSave}
          >
            {createMutation.isPending || updateMutation.isPending ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Dialog */}
      <Dialog open={!!deleteDialog} onClose={() => setDeleteDialog(null)}>
        <DialogTitle>Delete Template</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="textSecondary">
            Are you sure you want to delete template "{deleteDialog?.name}"? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialog(null)}>Cancel</Button>
          <Button
            color="error"
            variant="contained"
            disabled={deleteMutation.isPending}
            onClick={() => deleteDialog && deleteMutation.mutate(deleteDialog.id)}
          >
            {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
