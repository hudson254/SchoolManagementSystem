import React, { useState, useMemo } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Switch,
  FormControlLabel,
  Alert,
  CircularProgress,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Refresh as RefreshIcon,
  Class as ClassIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { classesService, ClassItem, CreateClassRequest } from '../services/classes.service';
import { unitService } from '../services/unit.service';
import { lecturerService } from '../services/lecturer.service';
import { semesterService } from '../services/semester.service';
import { useAuth } from '../hooks/useAuth';
import { canManageAcademic, canAdministrate } from '../utils/roles';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

interface ClassFormState {
  name: string;
  code: string;
  description: string;
  unitId: string;
  lecturerId: string;
  semesterId: string;
  maxCapacity: number;
  startDate: string;
  endDate: string;
  scheduleDay: string;
  startTime: string;
  endTime: string;
  isActive: boolean;
}

const emptyForm: ClassFormState = {
  name: '',
  code: '',
  description: '',
  unitId: '',
  lecturerId: '',
  semesterId: '',
  maxCapacity: 50,
  startDate: '',
  endDate: '',
  scheduleDay: '',
  startTime: '',
  endTime: '',
  isActive: true,
};

interface UpdatePayload {
  id: string;
  data: CreateClassRequest;
}

export const Classes: React.FC = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<ClassFormState>(emptyForm);
  const [formError, setFormError] = useState('');

  const canManage = canManageAcademic(user?.roles);
  const canAdmin = canAdministrate(user?.roles);

  const { data: classes, isLoading, isError, refetch } = useQuery({
    queryKey: ['classes'],
    queryFn: () => classesService.getClasses({ includeInactive: true }),
  });

  const { data: units } = useQuery({
    queryKey: ['units', 'select'],
    queryFn: () => unitService.getUnits({ pageSize: 100, isActive: true }),
    enabled: canManage,
  });

  const { data: lecturers } = useQuery({
    queryKey: ['lecturers', 'select'],
    queryFn: () => lecturerService.getLecturers({ pageSize: 100, isActive: true }),
    enabled: canManage,
  });

  const { data: semesters } = useQuery({
    queryKey: ['semesters', 'select'],
    queryFn: () => semesterService.getSemesters(),
    enabled: canManage,
  });

  const unitOptions = units?.items || [];
  const lecturerOptions = lecturers?.items || [];
  const semesterOptions = semesters || [];

  const createMutation = useMutation({
    mutationFn: (payload: CreateClassRequest) => classesService.createClass(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      handleCloseDialog();
    },
    onError: (error: any) => {
      setFormError(error?.message || 'Failed to create class. Please try again.');
    },
  });

  const updateMutation = useMutation({
    mutationFn: (payload: UpdatePayload) => classesService.updateClass(payload.id, payload.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      handleCloseDialog();
    },
    onError: (error: any) => {
      setFormError(error?.message || 'Failed to update class. Please try again.');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => classesService.deleteClass(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
    },
  });

const handleOpenCreate = () => {
    setEditingId(null);
    setForm(emptyForm);
    setFormError('');
    setDialogOpen(true);
  };

  const handleOpenEdit = (klass: ClassItem) => {
    setEditingId(klass.id);
    setForm({
      name: klass.name,
      code: klass.code,
      description: klass.description || '',
      unitId: klass.unitId,
      lecturerId: klass.lecturerId,
      semesterId: klass.semesterId,
      maxCapacity: klass.maxCapacity,
      startDate: klass.startDate ? klass.startDate.slice(0, 10) : '',
      endDate: klass.endDate ? klass.endDate.slice(0, 10) : '',
      scheduleDay: klass.scheduleDay || '',
      startTime: klass.startTime || '',
      endTime: klass.endTime || '',
      isActive: klass.isActive,
    });
    setFormError('');
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
    setEditingId(null);
    setForm(emptyForm);
    setFormError('');
  };

  const handleSubmit = () => {
    setFormError('');
    if (!form.name.trim()) return setFormError('Class name is required.');
    if (!form.code.trim()) return setFormError('Class code is required.');
    if (!form.unitId) return setFormError('Please select a unit.');
    if (!form.lecturerId) return setFormError('Please select a lecturer.');
    if (!form.semesterId) return setFormError('Please select a semester.');
    if (!form.startDate) return setFormError('Start date is required.');
    if (!form.endDate) return setFormError('End date is required.');
    if (form.endDate <= form.startDate) return setFormError('End date must be after start date.');
    if (form.startTime && form.endTime && form.endTime <= form.startTime) {
      return setFormError('End time must be after start time.');
    }

    const payload: CreateClassRequest = {
      name: form.name.trim(),
      code: form.code.trim(),
      description: form.description.trim() || undefined,
      unitId: form.unitId,
      lecturerId: form.lecturerId,
      semesterId: form.semesterId,
      maxCapacity: form.maxCapacity,
      startDate: new Date(form.startDate).toISOString(),
      endDate: new Date(form.endDate).toISOString(),
      scheduleDay: form.scheduleDay || undefined,
      startTime: form.startTime
        ? `${form.startTime.length === 5 ? form.startTime + ':00' : form.startTime}`
        : undefined,
      endTime: form.endTime
        ? `${form.endTime.length === 5 ? form.endTime + ':00' : form.endTime}`
        : undefined,
      isActive: form.isActive,
    };

    if (editingId) {
      updateMutation.mutate({ id: editingId, data: payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  const handleDelete = (klass: ClassItem) => {
    if (window.confirm(`Delete class "${klass.name}"? This cannot be undone.`)) {
      deleteMutation.mutate(klass.id);
    }
  };

  const isSubmitting = createMutation.isPending || updateMutation.isPending;
  const rows = useMemo(() => classes || [], [classes]);

  if (isLoading) return <LoadingSpinner />;

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load classes. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>
          Classes
        </Typography>
        <Box>
          {canManage && (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={handleOpenCreate}
              sx={{ mr: 1 }}
            >
              New Class
            </Button>
          )}
          <Button variant="outlined" startIcon={<RefreshIcon />} onClick={() => refetch()}>
            Refresh
          </Button>
        </Box>
      </Box>

<Paper>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Class</TableCell>
                <TableCell>Unit</TableCell>
                <TableCell>Lecturer</TableCell>
                <TableCell>Day / Time</TableCell>
                <TableCell>Semester</TableCell>
                <TableCell>Capacity</TableCell>
                <TableCell>Status</TableCell>
                {canManage && <TableCell align="right">Actions</TableCell>}
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((klass: ClassItem) => (
                <TableRow key={klass.id} hover>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <ClassIcon fontSize="small" color="primary" />
                      <Box>
                        <Typography variant="body2" fontWeight={600}>
                          {klass.name}
                        </Typography>
                        <Typography variant="caption" color="textSecondary">
                          {klass.code}
                        </Typography>
                      </Box>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">{klass.unitName}</Typography>
                    <Typography variant="caption" color="textSecondary">
                      {klass.unitCode}
                    </Typography>
                  </TableCell>
                  <TableCell>{klass.lecturerName || '—'}</TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {klass.scheduleDay || '—'}
                      {klass.startTime ? ` ${klass.startTime}–${klass.endTime || ''}` : ''}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      {klass.startDate ? new Date(klass.startDate).toLocaleDateString() : ''}
                      {klass.endDate ? ` – ${new Date(klass.endDate).toLocaleDateString()}` : ''}
                    </Typography>
                  </TableCell>
                  <TableCell>{klass.semesterName || '—'}</TableCell>
                  <TableCell>
                    {klass.currentEnrollment}/{klass.maxCapacity}
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={klass.isActive ? 'Active' : 'Inactive'}
                      color={klass.isActive ? 'success' : 'default'}
                      size="small"
                    />
                  </TableCell>
                  {canManage && (
                    <TableCell align="right">
                      <Tooltip title="Edit">
                        <IconButton size="small" onClick={() => handleOpenEdit(klass)}>
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                      {canAdmin && (
                        <Tooltip title="Delete">
                          <IconButton size="small" color="error" onClick={() => handleDelete(klass)}>
                            <DeleteIcon />
                          </IconButton>
                        </Tooltip>
                      )}
                    </TableCell>
                  )}
                </TableRow>
              ))}
              {rows.length === 0 && (
                <TableRow>
                  <TableCell colSpan={canManage ? 8 : 7} align="center">
                    No classes found.{' '}
                    {canManage ? 'Click "New Class" to create the first class.' : ''}
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

<Dialog open={dialogOpen} onClose={handleCloseDialog} maxWidth="md" fullWidth>
        <DialogTitle>{editingId ? 'Edit Class' : 'New Class'}</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            {formError && (
              <Alert severity="error" sx={{ mb: 2 }} onClose={() => setFormError('')}>
                {formError}
              </Alert>
            )}
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <TextField
                  label="Class Name"
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                  fullWidth
                  required
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label="Class Code"
                  value={form.code}
                  onChange={(e) => setForm({ ...form, code: e.target.value.toUpperCase() })}
                  fullWidth
                  required
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Description"
                  value={form.description}
                  onChange={(e) => setForm({ ...form, description: e.target.value })}
                  fullWidth
                  multiline
                  rows={2}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <FormControl fullWidth required>
                  <InputLabel>Unit</InputLabel>
                  <Select
                    value={form.unitId}
                    onChange={(e) => setForm({ ...form, unitId: e.target.value })}
                    label="Unit"
                  >
                    {unitOptions.map((u: any) => (
                      <MenuItem key={u.id} value={u.id}>
                        {u.name} ({u.code})
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} sm={6}>
                <FormControl fullWidth required>
                  <InputLabel>Lecturer</InputLabel>
                  <Select
                    value={form.lecturerId}
                    onChange={(e) => setForm({ ...form, lecturerId: e.target.value })}
                    label="Lecturer"
                  >
                    {lecturerOptions.map((l: any) => (
                      <MenuItem key={l.id} value={l.id}>
                        {l.firstName} {l.lastName}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>

<Grid item xs={12} sm={6}>
                <FormControl fullWidth required>
                  <InputLabel>Semester</InputLabel>
                  <Select
                    value={form.semesterId}
                    onChange={(e) => setForm({ ...form, semesterId: e.target.value })}
                    label="Semester"
                  >
                    {semesterOptions.map((s) => (
                      <MenuItem key={s.id} value={s.id}>
                        {s.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label="Max Capacity"
                  type="number"
                  value={form.maxCapacity}
                  onChange={(e) => setForm({ ...form, maxCapacity: parseInt(e.target.value, 10) || 0 })}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label="Start Date"
                  type="date"
                  value={form.startDate}
                  onChange={(e) => setForm({ ...form, startDate: e.target.value })}
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                  required
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  label="End Date"
                  type="date"
                  value={form.endDate}
                  onChange={(e) => setForm({ ...form, endDate: e.target.value })}
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                  required
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <FormControl fullWidth>
                  <InputLabel>Schedule Day</InputLabel>
                  <Select
                    value={form.scheduleDay}
                    onChange={(e) => setForm({ ...form, scheduleDay: e.target.value })}
                    label="Schedule Day"
                  >
                    <MenuItem value="">—</MenuItem>
                    {DAYS.map((day) => (
                      <MenuItem key={day} value={day}>
                        {day}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  label="Start Time"
                  type="time"
                  value={form.startTime}
                  onChange={(e) => setForm({ ...form, startTime: e.target.value })}
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  label="End Time"
                  type="time"
                  value={form.endTime}
                  onChange={(e) => setForm({ ...form, endTime: e.target.value })}
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                />
              </Grid>
              <Grid item xs={12}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={form.isActive}
                      onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                    />
                  }
                  label={form.isActive ? 'Active' : 'Inactive'}
                />
              </Grid>
            </Grid>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>Cancel</Button>
          <Button variant="contained" onClick={handleSubmit} disabled={isSubmitting}>
            {isSubmitting ? <CircularProgress size={20} /> : editingId ? 'Save Changes' : 'Create Class'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
