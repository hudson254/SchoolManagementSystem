import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
  IconButton,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert,
  LinearProgress,
  Tooltip,
  Divider,
  Tabs,
  Tab,
} from '@mui/material';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  Search as SearchIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon,
  CalendarToday as CalendarIcon,
  Schedule as ScheduleIcon,
  Room as RoomIcon,
  Person as PersonIcon,
  School as SchoolIcon,
  Download as DownloadIcon,
  Print as PrintIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { timetableService, TimetableEntry } from '../services/timetable.service';
import { classesService } from '../services/classes.service';
import { semesterService } from '../services/semester.service';
import { useAuth } from '../hooks/useAuth';
import { canManageAcademic, canAdministrate } from '../utils/roles';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

const daysOfWeek = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

const timeSlots = [
  '08:00', '08:30', '09:00', '09:30', '10:00', '10:30',
  '11:00', '11:30', '12:00', '12:30', '13:00', '13:30',
  '14:00', '14:30', '15:00', '15:30', '16:00', '16:30',
  '17:00', '17:30', '18:00', '18:30', '19:00', '19:30'
];

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel = (props: TabPanelProps) => {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`timetable-tabpanel-${index}`}
      aria-labelledby={`timetable-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
};

const a11yProps = (index: number) => ({
  id: `timetable-tab-${index}`,
  'aria-controls': `timetable-tabpanel-${index}`,
});

export const Timetable: React.FC = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [filterSemester, setFilterSemester] = useState<string>('');
  const [filterClass, setFilterClass] = useState<string>('');
  const [filterDay, setFilterDay] = useState<string>('');
  const [selectedView, setSelectedView] = useState<'class' | 'lecturer' | 'student'>('class');
  const [selectedEntity, setSelectedEntity] = useState<string>('');
  const [conflictDialogOpen, setConflictDialogOpen] = useState(false);
  const [conflictData, setConflictData] = useState<any>(null);
  const [entryDialogOpen, setEntryDialogOpen] = useState(false);
  const [editingEntryId, setEditingEntryId] = useState<string | null>(null);
  const [entryForm, setEntryForm] = useState({
    classId: '',
    date: '',
    dayOfWeek: '',
    startTime: '',
    endTime: '',
    venue: '',
  });
  const [entryFormError, setEntryFormError] = useState('');

  const canManage = canManageAcademic(user?.roles);
  const canAdmin = canAdministrate(user?.roles);

  const { data: classOptions } = useQuery({
    queryKey: ['classes', 'select'],
    queryFn: () => classesService.getClasses({ includeInactive: true }),
    enabled: canManage,
  });
  const classesForSelect = classOptions || [];

  const { data: semesterOptions } = useQuery({
    queryKey: ['semesters', 'select'],
    queryFn: () => semesterService.getSemesters(),
    enabled: canManage,
  });
  const semestersForSelect = semesterOptions || [];

  const { data: timetables, isLoading, isError, refetch } = useQuery({
    queryKey: ['timetables', page, rowsPerPage, searchTerm, filterSemester, filterClass, filterDay],
    queryFn: () =>
      timetableService.getTimetables({
        page: page + 1,
        pageSize: rowsPerPage,
        searchTerm: searchTerm || undefined,
        semesterId: filterSemester || undefined,
        classId: filterClass || undefined,
        dayOfWeek: filterDay || undefined,
      }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => timetableService.deleteTimetable(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timetables'] });
    },
  });

  const createEntryMutation = useMutation({
    mutationFn: (payload: any) => timetableService.createTimetable(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timetables'] });
      setEntryDialogOpen(false);
      setEditingEntryId(null);
      setEntryFormError('');
    },
    onError: (error: any) => {
      setEntryFormError(error?.message || 'Failed to create timetable entry. Please try again.');
    },
  });

  const updateEntryMutation = useMutation({
    mutationFn: (payload: any) => timetableService.updateTimetable(payload.id, payload.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timetables'] });
      setEntryDialogOpen(false);
      setEditingEntryId(null);
      setEntryFormError('');
    },
    onError: (error: any) => {
      setEntryFormError(error?.message || 'Failed to update timetable entry. Please try again.');
    },
  });

  const openCreateEntry = () => {
    setEditingEntryId(null);
    setEntryForm({ classId: '', date: '', dayOfWeek: '', startTime: '', endTime: '', venue: '' });
    setEntryFormError('');
    setEntryDialogOpen(true);
  };

  const openEditEntry = (entry: TimetableEntry) => {
    setEditingEntryId(entry.id);
    const datePart = entry.date ? entry.date.slice(0, 10) : '';
    setEntryForm({
      classId: entry.classId,
      date: datePart,
      dayOfWeek: entry.dayOfWeek || '',
      startTime: entry.startTime?.slice(0, 5) || '',
      endTime: entry.endTime?.slice(0, 5) || '',
      venue: entry.venue || '',
    });
    setEntryFormError('');
    setEntryDialogOpen(true);
  };

  const handleEntrySubmit = () => {
    setEntryFormError('');
    if (!entryForm.classId) return setEntryFormError('Please select a class.');
    if (!entryForm.date) return setEntryFormError('Date is required.');
    if (!entryForm.dayOfWeek) return setEntryFormError('Day of week is required.');
    if (!entryForm.startTime) return setEntryFormError('Start time is required.');
    if (!entryForm.endTime) return setEntryFormError('End time is required.');
    if (entryForm.endTime <= entryForm.startTime) {
      return setEntryFormError('End time must be after start time.');
    }

    const klass = classesForSelect.find((c: any) => c.id === entryForm.classId);
    const payload = {
      classId: entryForm.classId,
      unitId: klass?.unitId,
      lecturerId: klass?.lecturerId || null,
      date: new Date(entryForm.date).toISOString(),
      dayOfWeek: entryForm.dayOfWeek,
      startTime: `${entryForm.startTime}:00`,
      endTime: `${entryForm.endTime}:00`,
      venue: entryForm.venue || undefined,
    };

    if (editingEntryId) {
      updateEntryMutation.mutate({ id: editingEntryId, data: payload });
    } else {
      createEntryMutation.mutate(payload);
    }
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleSearch = () => {
    setSearchTerm(searchInput);
    setPage(0);
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleDelete = (id: string) => {
    if (window.confirm('Are you sure you want to delete this timetable entry?')) {
      deleteMutation.mutate(id);
    }
  };

  const handleCheckConflicts = async () => {
    try {
      const result = await timetableService.checkConflicts({
        classId: selectedEntity,
        semesterId: filterSemester,
      });
      setConflictData(result);
      setConflictDialogOpen(true);
    } catch (error) {
      console.error('Failed to check conflicts:', error);
    }
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load timetable data. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const entries = timetables?.items || [];
  const totalCount = timetables?.totalCount || 0;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>
          Timetable Management
        </Typography>
        <Box>
          {canManage && (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={openCreateEntry}
              sx={{ mr: 1 }}
            >
              Add Entry
            </Button>
          )}
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={() => refetch()}
          >
            Refresh
          </Button>
        </Box>
      </Box>

      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="timetable tabs"
          sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}
        >
          <Tab label="List View" {...a11yProps(0)} />
          <Tab label="Calendar View" {...a11yProps(1)} />
          <Tab label="My Timetable" {...a11yProps(2)} />
        </Tabs>
      </Paper>

      <TabPanel value={tabValue} index={0}>
        <Paper sx={{ p: 2, mb: 3 }}>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} sm={3}>
              <TextField
                fullWidth
                size="small"
                placeholder="Search by class or venue..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                onKeyPress={handleKeyPress}
                InputProps={{
                  startAdornment: <SearchIcon sx={{ mr: 1, color: 'text.secondary' }} />,
                  endAdornment: (
                    <Button size="small" onClick={handleSearch}>
                      Search
                    </Button>
                  ),
                }}
              />
            </Grid>
            <Grid item xs={12} sm={2}>
              <FormControl fullWidth size="small">
                <InputLabel>Semester</InputLabel>
                <Select
                  value={filterSemester}
                  onChange={(e) => setFilterSemester(e.target.value)}
                  label="Semester"
                >
                  <MenuItem value="">All</MenuItem>
                  {semestersForSelect.map((s) => (
                    <MenuItem key={s.id} value={s.id}>{s.name}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={2}>
              <FormControl fullWidth size="small">
                <InputLabel>Day</InputLabel>
                <Select
                  value={filterDay}
                  onChange={(e) => setFilterDay(e.target.value)}
                  label="Day"
                >
                  <MenuItem value="">All</MenuItem>
                  {daysOfWeek.map((day) => (
                    <MenuItem key={day} value={day}>{day}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={3}>
              <FormControl fullWidth size="small">
                <InputLabel>Class</InputLabel>
                <Select
                  value={filterClass}
                  onChange={(e) => setFilterClass(e.target.value)}
                  label="Class"
                >
                  <MenuItem value="">All</MenuItem>
                  {classesForSelect.map((c: any) => (
                    <MenuItem key={c.id} value={c.id}>{c.name} ({c.code})</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={2}>
              <Button
                fullWidth
                variant="outlined"
                onClick={() => {
                  setFilterSemester('');
                  setFilterClass('');
                  setFilterDay('');
                  setSearchInput('');
                  setSearchTerm('');
                }}
              >
                Clear Filters
              </Button>
            </Grid>
          </Grid>
        </Paper>

        <Paper>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Class</TableCell>
                  <TableCell>Unit</TableCell>
                  <TableCell>Lecturer</TableCell>
                  <TableCell>Day</TableCell>
                  <TableCell>Time</TableCell>
                  <TableCell>Venue</TableCell>
                  <TableCell>Semester</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {entries.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={8} align="center" sx={{ py: 4 }}>
                      <Typography variant="body1" color="textSecondary">
                        No timetable entries found
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  entries.map((entry: any) => (
                    <TableRow key={entry.id} hover>
                      <TableCell>{entry.className}</TableCell>
                      <TableCell>{entry.unitName} ({entry.unitCode})</TableCell>
                      <TableCell>{entry.lecturerName}</TableCell>
                      <TableCell>{entry.dayOfWeek}</TableCell>
                      <TableCell>
                        {entry.startTime} - {entry.endTime}
                      </TableCell>
                      <TableCell>{entry.venue || 'TBD'}</TableCell>
                      <TableCell>{entry.semesterName}</TableCell>
                      <TableCell align="right">
                        <Tooltip title="View">
                          <IconButton size="small" onClick={() => openEditEntry(entry as TimetableEntry)}>
                            <ViewIcon />
                          </IconButton>
                        </Tooltip>
                        {canManage && (
                          <>
                            <Tooltip title="Edit">
                              <IconButton size="small" onClick={() => openEditEntry(entry as TimetableEntry)}>
                                <EditIcon />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Delete">
                              <IconButton size="small" onClick={() => handleDelete(entry.id)}>
                                <DeleteIcon />
                              </IconButton>
                            </Tooltip>
                          </>
                        )}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination
            rowsPerPageOptions={[5, 10, 25, 50]}
            component="div"
            count={totalCount}
            rowsPerPage={rowsPerPage}
            page={page}
            onPageChange={handleChangePage}
            onRowsPerPageChange={handleChangeRowsPerPage}
          />
        </Paper>
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" fontWeight={600} gutterBottom>
            Calendar View
          </Typography>
          <Box sx={{ overflowX: 'auto' }}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ minWidth: 80 }}>Time</TableCell>
                  {daysOfWeek.map((day) => (
                    <TableCell key={day} sx={{ minWidth: 120, fontWeight: 600 }}>
                      {day}
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {timeSlots.map((time) => (
                  <TableRow key={time}>
                    <TableCell sx={{ fontWeight: 500, fontSize: '0.75rem' }}>
                      {time}
                    </TableCell>
                    {daysOfWeek.map((day) => {
                      const entry = entries.find(
                        (e: any) => e.dayOfWeek === day && e.startTime <= time && e.endTime > time
                      );
                      return (
                        <TableCell key={`${day}-${time}`} sx={{ p: 0.5 }}>
                          {entry && (
                            <Box
                              sx={{
                                bgcolor: '#576426',
                                color: 'white',
                                p: 1,
                                borderRadius: 1,
                                fontSize: '0.7rem',
                                minHeight: 40,
                                display: 'flex',
                                flexDirection: 'column',
                                justifyContent: 'center',
                              }}
                            >
                              <Typography variant="caption" fontWeight={600}>
                                {entry.unitCode}
                              </Typography>
                              <Typography variant="caption" sx={{ fontSize: '0.6rem', opacity: 0.9 }}>
                                {entry.venue || 'TBD'}
                              </Typography>
                            </Box>
                          )}
                        </TableCell>
                      );
                    })}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        </Paper>
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" fontWeight={600} gutterBottom>
            My Timetable
          </Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={4}>
              <FormControl fullWidth>
                <InputLabel>View Type</InputLabel>
                <Select
                  value={selectedView}
                  onChange={(e) => setSelectedView(e.target.value as any)}
                  label="View Type"
                >
                  <MenuItem value="class">Class View</MenuItem>
                  <MenuItem value="lecturer">Lecturer View</MenuItem>
                  <MenuItem value="student">Student View</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={4}>
              <FormControl fullWidth>
                <InputLabel>Select Entity</InputLabel>
                <Select
                  value={selectedEntity}
                  onChange={(e) => setSelectedEntity(e.target.value)}
                  label="Select Entity"
                >
                  <MenuItem value="">Select...</MenuItem>
                  {selectedView === 'class' && (
                    <>
                      <MenuItem value="class1">CSC101 - Class A</MenuItem>
                      <MenuItem value="class2">CSC201 - Class B</MenuItem>
                    </>
                  )}
                  {selectedView === 'lecturer' && (
                    <>
                      <MenuItem value="lecturer1">Dr. Smith</MenuItem>
                      <MenuItem value="lecturer2">Prof. Johnson</MenuItem>
                    </>
                  )}
                  {selectedView === 'student' && (
                    <>
                      <MenuItem value="student1">John Doe</MenuItem>
                      <MenuItem value="student2">Jane Smith</MenuItem>
                    </>
                  )}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={4}>
              <Button
                fullWidth
                variant="contained"
                startIcon={<CalendarIcon />}
                onClick={handleCheckConflicts}
                disabled={!selectedEntity}
              >
                Check Conflicts
              </Button>
            </Grid>
          </Grid>

          <Box sx={{ mt: 3, overflowX: 'auto' }}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Time</TableCell>
                  {daysOfWeek.map((day) => (
                    <TableCell key={day} sx={{ fontWeight: 600 }}>
                      {day}
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {timeSlots.map((time) => (
                  <TableRow key={time}>
                    <TableCell sx={{ fontWeight: 500, fontSize: '0.75rem' }}>
                      {time}
                    </TableCell>
                    {daysOfWeek.map((day) => (
                      <TableCell key={`${day}-${time}`} sx={{ p: 0.5 }}>
                        {/* Entry would be displayed here based on selection */}
                        <Box sx={{ height: 40, border: '1px dashed #e0e0e0', borderRadius: 1 }} />
                      </TableCell>
                    ))}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        </Paper>
      </TabPanel>

      {/* Conflict Dialog */}
      <Dialog open={conflictDialogOpen} onClose={() => setConflictDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Timetable Conflicts</DialogTitle>
        <DialogContent>
          {conflictData ? (
            <Box>
              {conflictData.conflicts && conflictData.conflicts.length > 0 ? (
                <>
                  <Alert severity="warning" sx={{ mb: 2 }}>
                    Found {conflictData.conflicts.length} conflict(s)
                  </Alert>
                  {conflictData.conflicts.map((conflict: any, index: number) => (
                    <Paper key={index} sx={{ p: 2, mb: 2, bgcolor: '#fff3e0' }}>
                      <Typography variant="body2" fontWeight={500}>
                        {conflict.description}
                      </Typography>
                      <Typography variant="caption" color="textSecondary">
                        {conflict.details}
                      </Typography>
                    </Paper>
                  ))}
                </>
              ) : (
                <Alert severity="success">
                  No conflicts found. The timetable is clean.
                </Alert>
              )}
            </Box>
          ) : (
            <LinearProgress />
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConflictDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
<Dialog open={entryDialogOpen} onClose={() => setEntryDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{editingEntryId ? 'Edit Timetable Entry' : 'Add Timetable Entry'}</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
            {entryFormError && (
              <Alert severity="error" onClose={() => setEntryFormError('')}>
                {entryFormError}
              </Alert>
            )}
            <FormControl fullWidth required>
              <InputLabel>Class</InputLabel>
              <Select
                value={entryForm.classId}
                onChange={(e) => setEntryForm({ ...entryForm, classId: e.target.value })}
                label="Class"
              >
                {classesForSelect.map((c: any) => (
                  <MenuItem key={c.id} value={c.id}>
                    {c.name} ({c.code}) — {c.unitName} / {c.lecturerName}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Date"
              type="date"
              value={entryForm.date}
              onChange={(e) => {
                const d = new Date(e.target.value);
                setEntryForm({
                  ...entryForm,
                  date: e.target.value,
                  dayOfWeek: isNaN(d.getTime())
                    ? entryForm.dayOfWeek
                    : d.toLocaleDateString('en-US', { weekday: 'long' }),
                });
              }}
              fullWidth
              InputLabelProps={{ shrink: true }}
              required
            />
            <TextField
              label="Day of Week"
              value={entryForm.dayOfWeek}
              onChange={(e) => setEntryForm({ ...entryForm, dayOfWeek: e.target.value })}
              fullWidth
              InputLabelProps={{ shrink: true }}
              placeholder="Auto-filled from date"
            />
            <Grid container spacing={2}>
              <Grid item xs={6}>
                <TextField
                  label="Start Time"
                  type="time"
                  value={entryForm.startTime}
                  onChange={(e) => setEntryForm({ ...entryForm, startTime: e.target.value })}
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                  required
                />
              </Grid>
              <Grid item xs={6}>
                <TextField
                  label="End Time"
                  type="time"
                  value={entryForm.endTime}
                  onChange={(e) => setEntryForm({ ...entryForm, endTime: e.target.value })}
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                  required
                />
              </Grid>
            </Grid>
            <TextField
              label="Venue / Room"
              value={entryForm.venue}
              onChange={(e) => setEntryForm({ ...entryForm, venue: e.target.value })}
              fullWidth
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEntryDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleEntrySubmit}
            disabled={createEntryMutation.isPending || updateEntryMutation.isPending}
          >
            {createEntryMutation.isPending || updateEntryMutation.isPending
              ? 'Saving...'
              : editingEntryId
                ? 'Save Changes'
                : 'Create Entry'}
          </Button>
        </DialogActions>
      </Dialog>
};