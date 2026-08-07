import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Chip,
  Grid,
  Divider,
  Alert,
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
  MenuItem,
  FormControl,
  InputLabel,
  Select,
  Tab,
  Tabs,
  Card,
  CardContent,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  ListItemSecondaryAction,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Edit as EditIcon,
  Add as AddIcon,
  Delete as DeleteIcon,
  Person as PersonIcon,
  Group as GroupIcon,
  School as SchoolIcon,
  EventNote as EventNoteIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { courseOfferingService, CourseOfferingStatus } from '../services/course-offering.service';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { CourseOfferingUnitForm } from '../components/Forms/CourseOfferingUnitForm';
import { AssignmentConfirm } from '../components/AssignmentConfirm';

const statusColors: Record<string, 'default' | 'primary' | 'success' | 'warning' | 'error'> = {
  Draft: 'default',
  Scheduled: 'primary',
  Active: 'success',
  Completed: 'default',
  Cancelled: 'error',
};

interface TabPanelProps {
  children?: React.ReactNode;
  value: number;
  index: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => (
  <div role="tabpanel" hidden={value !== index}>
    {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
  </div>
);

export const CourseOfferingDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [activeTab, setActiveTab] = useState(0);
  const [unitDialogOpen, setUnitDialogOpen] = useState(false);
  const [editingUnit, setEditingUnit] = useState<any>(null);
  const [deleteUnitDialogOpen, setDeleteUnitDialogOpen] = useState(false);
  const [unitToDelete, setUnitToDelete] = useState<string | null>(null);
  const [assignStudentDialogOpen, setAssignStudentDialogOpen] = useState(false);
  const [assignLecturerDialogOpen, setAssignLecturerDialogOpen] = useState(false);
  const [studentIds, setStudentIds] = useState<string[]>([]);
  const [lecturerIds, setLecturerIds] = useState<string[]>([]);
  const [studentSearch, setStudentSearch] = useState('');
  const [lecturerSearch, setLecturerSearch] = useState('');

  const isAdmin = user?.roles?.includes('SystemAdministrator') || user?.roles?.includes('Moderator');

  const { data: offering, isLoading, isError, refetch } = useQuery({
    queryKey: ['courseoffering', id],
    queryFn: () => courseOfferingService.getCourseOffering(id!),
    enabled: !!id,
  });

  const deleteUnitMutation = useMutation({
    mutationFn: (unitId: string) => courseOfferingService.deleteUnit(unitId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseoffering', id] });
      setDeleteUnitDialogOpen(false);
      setUnitToDelete(null);
    },
  });

  const assignStudentsMutation = useMutation({
    mutationFn: (data: any) => courseOfferingService.assignStudents(id!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseoffering', id] });
      setAssignStudentDialogOpen(false);
      setStudentIds([]);
      setStudentSearch('');
    },
  });

  const assignLecturersMutation = useMutation({
    mutationFn: (data: any) => courseOfferingService.assignLecturers(id!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseoffering', id] });
      setAssignLecturerDialogOpen(false);
      setLecturerIds([]);
      setLecturerSearch('');
    },
  });

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (isError || !offering) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load course offering details.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const handleDeleteUnit = () => {
    if (unitToDelete) {
      deleteUnitMutation.mutate(unitToDelete);
    }
  };

  const handleAssignStudents = () => {
    assignStudentsMutation.mutate({ studentIds });
  };

  const handleAssignLecturers = () => {
    assignLecturersMutation.mutate({ lecturerIds });
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <IconButton onClick={() => navigate('/course-offerings')} sx={{ mr: 2 }}>
          <ArrowBackIcon />
        </IconButton>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h4" fontWeight={600}>
            {offering.courseName}
          </Typography>
          <Typography variant="subtitle1" color="textSecondary">
            {offering.offeringCode} — Academic Year {offering.academicYearName} · Semester {offering.semesterName}
          </Typography>
        </Box>
        <Box>
          <Chip
            label={offering.status}
            color={(statusColors[offering.status] as any) || 'default'}
            sx={{ mr: 1 }}
          />
          {isAdmin && (
            <Button
              variant="outlined"
              startIcon={<EditIcon />}
              onClick={() => navigate(`/course-offerings/${offering.id}/edit`)}
            >
              Edit
            </Button>
          )}
        </Box>
      </Box>

      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" color="textSecondary">
                Offering Code
              </Typography>
              <Typography variant="h6">{offering.offeringCode}</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" color="textSecondary">
                Intake
              </Typography>
              <Typography variant="h6">{offering.intake || '—'}</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" color="textSecondary">
                Start Date
              </Typography>
              <Typography variant="h6">
                {offering.startDate ? new Date(offering.startDate).toLocaleDateString() : '—'}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" color="textSecondary">
                End Date
              </Typography>
              <Typography variant="h6">
                {offering.endDate ? new Date(offering.endDate).toLocaleDateString() : '—'}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {offering.notes && (
        <Paper sx={{ p: 2, mb: 3 }}>
          <Typography variant="subtitle2" color="textSecondary">
            Notes
          </Typography>
          <Typography variant="body2">{offering.notes}</Typography>
        </Paper>
      )}

      <Paper>
        <Tabs value={activeTab} onChange={(_, v) => setActiveTab(v)}>
          <Tab label={`Units (${offering.units.length})`} />
          <Tab label={`Lecturers (${offering.lecturers.length})`} />
          <Tab label={`Enrollments (${offering.enrollments.length})`} />
        </Tabs>

        <TabPanel value={activeTab} index={0}>
          <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
            {isAdmin && (
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={() => { setEditingUnit(null); setUnitDialogOpen(true); }}
              >
                Add Unit
              </Button>
            )}
          </Box>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Order</TableCell>
                  <TableCell>Code</TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell>Credits</TableCell>
                  <TableCell>Contact Hrs</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {offering.units.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                      <Typography variant="body1" color="textSecondary">
                        No units added yet
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  offering.units.map((unit: any) => (
                    <TableRow key={unit.id} hover>
                      <TableCell>{unit.order}</TableCell>
                      <TableCell>{unit.code}</TableCell>
                      <TableCell>
                        <Typography variant="body2" fontWeight={500}>
                          {unit.name}
                        </Typography>
                        {unit.description && (
                          <Typography variant="caption" color="textSecondary">
                            {unit.description.substring(0, 80)}
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>{unit.credits}</TableCell>
                      <TableCell>{unit.contactHours}</TableCell>
                      <TableCell align="right">
                        {isAdmin && (
                          <>
                            <Tooltip title="Edit">
                              <IconButton
                                size="small"
                                onClick={() => { setEditingUnit(unit); setUnitDialogOpen(true); }}
                              >
                                <EditIcon />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Delete">
                              <IconButton
                                size="small"
                                color="error"
                                onClick={() => { setUnitToDelete(unit.id); setDeleteUnitDialogOpen(true); }}
                              >
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
        </TabPanel>

        <TabPanel value={activeTab} index={1}>
          <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
            {isAdmin && (
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={() => setAssignLecturerDialogOpen(true)}
              >
                Assign Lecturer
              </Button>
            )}
          </Box>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Lecturer</TableCell>
                  <TableCell>Email</TableCell>
                  <TableCell>Role</TableCell>
                  <TableCell>Primary</TableCell>
                  <TableCell>Assigned Date</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {offering.lecturers.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} align="center" sx={{ py: 4 }}>
                      <Typography variant="body1" color="textSecondary">
                        No lecturers assigned
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  offering.lecturers.map((lecturer: any) => (
                    <TableRow key={lecturer.id} hover>
                      <TableCell>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                          <PersonIcon sx={{ color: '#576426' }} />
                          <Typography variant="body2" fontWeight={500}>
                            {lecturer.lecturerName}
                          </Typography>
                        </Box>
                      </TableCell>
                      <TableCell>{lecturer.lecturerEmail || '—'}</TableCell>
                      <TableCell>{lecturer.role || '—'}</TableCell>
                      <TableCell>
                        {lecturer.isPrimary ? (
                          <Chip label="Primary" size="small" color="success" />
                        ) : (
                          '—'
                        )}
                      </TableCell>
                      <TableCell>
                        {lecturer.assignedDate ? new Date(lecturer.assignedDate).toLocaleDateString() : '—'}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </TabPanel>

        <TabPanel value={activeTab} index={2}>
          <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
            {isAdmin && (
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={() => setAssignStudentDialogOpen(true)}
              >
                Assign Student
              </Button>
            )}
          </Box>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Student</TableCell>
                  <TableCell>Student Number</TableCell>
                  <TableCell>Attempt</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Confirmation</TableCell>
                  <TableCell>Enrollment Date</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {offering.enrollments.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                      <Typography variant="body1" color="textSecondary">
                        No students enrolled
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  offering.enrollments.map((enrollment: any) => (
                    <TableRow key={enrollment.id} hover>
                      <TableCell>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                          <SchoolIcon sx={{ color: '#576426' }} />
                          <Typography variant="body2" fontWeight={500}>
                            {enrollment.studentName}
                          </Typography>
                        </Box>
                      </TableCell>
                      <TableCell>{enrollment.studentNumber || '—'}</TableCell>
                      <TableCell>{enrollment.attemptNumber}</TableCell>
                      <TableCell>
                        <Chip label={enrollment.status} size="small" />
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={enrollment.confirmationStatus}
                          size="small"
                          color={
                            enrollment.confirmationStatus === 'Confirmed'
                              ? 'success'
                              : enrollment.confirmationStatus === 'Pending'
                                ? 'warning'
                                : 'default'
                          }
                        />
                      </TableCell>
                      <TableCell>
                        {enrollment.enrollmentDate ? new Date(enrollment.enrollmentDate).toLocaleDateString() : '—'}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </TabPanel>
      </Paper>

      {unitDialogOpen && (
        <CourseOfferingUnitForm
          open={unitDialogOpen}
          onClose={() => setUnitDialogOpen(false)}
          courseOfferingId={offering.id}
          unit={editingUnit}
        />
      )}

      <Dialog open={deleteUnitDialogOpen} onClose={() => setDeleteUnitDialogOpen(false)}>
        <DialogTitle>Delete Unit</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete this unit from the course offering? This will affect the current
            offering only.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteUnitDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            color="error"
            onClick={handleDeleteUnit}
            disabled={deleteUnitMutation.isPending}
          >
            {deleteUnitMutation.isPending ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={assignStudentDialogOpen} onClose={() => setAssignStudentDialogOpen(false)}>
        <DialogTitle>Assign Students</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            size="small"
            placeholder="Search students by name or admission number..."
            value={studentSearch}
            onChange={(e) => setStudentSearch(e.target.value)}
            sx={{ mb: 2, mt: 1 }}
          />
          <Typography variant="subtitle2" color="textSecondary" sx={{ mb: 1 }}>
            Selected Students ({studentIds.length})
          </Typography>
          <TextField
            fullWidth
            size="small"
            placeholder="Enter student IDs (comma-separated)"
            value={studentIds.join(', ')}
            onChange={(e) => setStudentIds(e.target.value.split(',').map(s => s.trim()).filter(Boolean))}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAssignStudentDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleAssignStudents}
            disabled={assignStudentsMutation.isPending || studentIds.length === 0}
          >
            {assignStudentsMutation.isPending ? 'Assigning...' : 'Assign Students'}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={assignLecturerDialogOpen} onClose={() => setAssignLecturerDialogOpen(false)}>
        <DialogTitle>Assign Lecturers</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            size="small"
            placeholder="Search lecturers by name or staff number..."
            value={lecturerSearch}
            onChange={(e) => setLecturerSearch(e.target.value)}
            sx={{ mb: 2, mt: 1 }}
          />
          <Typography variant="subtitle2" color="textSecondary" sx={{ mb: 1 }}>
            Selected Lecturers ({lecturerIds.length})
          </Typography>
          <TextField
            fullWidth
            size="small"
            placeholder="Enter lecturer IDs (comma-separated)"
            value={lecturerIds.join(', ')}
            onChange={(e) => setLecturerIds(e.target.value.split(',').map(s => s.trim()).filter(Boolean))}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAssignLecturerDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleAssignLecturers}
            disabled={assignLecturersMutation.isPending || lecturerIds.length === 0}
          >
            {assignLecturersMutation.isPending ? 'Assigning...' : 'Assign Lecturers'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
