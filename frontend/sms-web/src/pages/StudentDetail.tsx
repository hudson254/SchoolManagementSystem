import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Grid,
  Avatar,
  Chip,
  Button,
  Divider,
  Tab,
  Tabs,
  Card,
  CardContent,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  List,
  ListItem,
  ListItemText,
  ListItemAvatar,
  IconButton,
  Tooltip,
  Alert,
  LinearProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Email as EmailIcon,
  Phone as PhoneIcon,
  LocationOn as LocationOnIcon,
  CalendarToday as CalendarIcon,
  School as SchoolIcon,
  Book as BookIcon,
  Grade as GradeIcon,
  AttachMoney as MoneyIcon,
  Bed as BedIcon,
  Download as DownloadIcon,
  Print as PrintIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useNavigate } from 'react-router-dom';
import { studentService } from '../services/student.service';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

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
      id={`student-tabpanel-${index}`}
      aria-labelledby={`student-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
};

const a11yProps = (index: number) => ({
  id: `student-tab-${index}`,
  'aria-controls': `student-tabpanel-${index}`,
});

export const StudentDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  const { data: student, isLoading, isError, refetch } = useQuery({
    queryKey: ['student', id],
    queryFn: () => studentService.getStudent(id!),
    enabled: !!id,
  });

  const deleteMutation = useMutation({
    mutationFn: () => studentService.deleteStudent(id!),
    onSuccess: () => {
      navigate('/students');
    },
  });

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleDelete = () => {
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = () => {
    deleteMutation.mutate();
  };

  const handlePrint = () => {
    window.print();
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (isError || !student) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load student details. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Active':
        return 'success';
      case 'Suspended':
        return 'warning';
      case 'Graduated':
        return 'info';
      case 'Withdrawn':
        return 'error';
      case 'Probation':
        return 'warning';
      default:
        return 'default';
    }
  };

  const getInitials = () => {
    return `${student.firstName[0]}${student.lastName[0]}`.toUpperCase();
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <IconButton onClick={() => navigate('/students')}>
            <ArrowBackIcon />
          </IconButton>
          <Typography variant="h4" fontWeight={600}>
            Student Details
          </Typography>
        </Box>
        <Box>
          <Tooltip title="Print">
            <IconButton onClick={handlePrint}>
              <PrintIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Download Transcript">
            <IconButton>
              <DownloadIcon />
            </IconButton>
          </Tooltip>
          {(user?.roles?.includes('SystemAdministrator') || user?.roles?.includes('Moderator')) && (
            <>
              <Button
                variant="outlined"
                startIcon={<EditIcon />}
                onClick={() => navigate(`/students/${id}/edit`)}
                sx={{ mr: 1 }}
              >
                Edit
              </Button>
              <Button
                variant="contained"
                color="error"
                startIcon={<DeleteIcon />}
                onClick={handleDelete}
              >
                Delete
              </Button>
            </>
          )}
        </Box>
      </Box>

      {/* Header Card */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Grid container spacing={3} alignItems="center">
          <Grid item>
            <Avatar
              sx={{
                width: 100,
                height: 100,
                bgcolor: '#576426',
                fontSize: 40,
              }}
            >
              {getInitials()}
            </Avatar>
          </Grid>
          <Grid item xs>
            <Box>
              <Typography variant="h5" fontWeight={600}>
                {student.firstName} {student.lastName}
              </Typography>
              <Typography variant="body1" color="textSecondary">
                {student.studentNumber}
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, mt: 1, flexWrap: 'wrap' }}>
                <Chip
                  label={student.academicStatus || 'Active'}
                  color={getStatusColor(student.academicStatus || 'Active')}
                  size="small"
                />
                <Chip
                  label={student.isEnrolled ? 'Enrolled' : 'Not Enrolled'}
                  color={student.isEnrolled ? 'success' : 'default'}
                  size="small"
                />
                {student.programmeName && (
                  <Chip
                    label={student.programmeName}
                    icon={<SchoolIcon />}
                    size="small"
                    variant="outlined"
                  />
                )}
              </Box>
            </Box>
          </Grid>
          <Grid item>
            <Box sx={{ textAlign: 'right' }}>
              <Typography variant="body2" color="textSecondary">
                GPA
              </Typography>
              <Typography variant="h4" fontWeight={600} color="primary">
                {student.cumulativeGPA?.toFixed(2) || 'N/A'}
              </Typography>
              <Typography variant="caption" color="textSecondary">
                {student.totalCreditsEarned} Credits Earned
              </Typography>
            </Box>
          </Grid>
        </Grid>
      </Paper>

      {/* Tabs */}
      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="student tabs"
          sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}
        >
          <Tab label="Profile" {...a11yProps(0)} />
          <Tab label="Enrollments" {...a11yProps(1)} />
          <Tab label="Grades" {...a11yProps(2)} />
          <Tab label="Attendance" {...a11yProps(3)} />
          <Tab label="Timetable" {...a11yProps(4)} />
          <Tab label="Accommodation" {...a11yProps(5)} />
        </Tabs>
      </Paper>

      {/* Tab Content */}
      <TabPanel value={tabValue} index={0}>
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" fontWeight={600} gutterBottom>
                  Personal Information
                </Typography>
                <Divider sx={{ mb: 2 }} />
                <List>
                  <ListItem>
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: 'transparent' }}>
                        <EmailIcon color="action" />
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText primary="Email" secondary={student.email} />
                  </ListItem>
                  <ListItem>
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: 'transparent' }}>
                        <PhoneIcon color="action" />
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText primary="Phone" secondary={student.phoneNumber || 'Not provided'} />
                  </ListItem>
                  <ListItem>
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: 'transparent' }}>
                        <CalendarIcon color="action" />
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText
                      primary="Date of Birth"
                      secondary={new Date(student.dateOfBirth).toLocaleDateString()}
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: 'transparent' }}>
                        <LocationOnIcon color="action" />
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText primary="Address" secondary={student.address || 'Not provided'} />
                  </ListItem>
                  <ListItem>
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: 'transparent' }}>
                        <SchoolIcon color="action" />
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText
                      primary="Enrollment Date"
                      secondary={new Date(student.enrollmentDate).toLocaleDateString()}
                    />
                  </ListItem>
                </List>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" fontWeight={600} gutterBottom>
                  Academic Information
                </Typography>
                <Divider sx={{ mb: 2 }} />
                <List>
                  <ListItem>
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: 'transparent' }}>
                        <BookIcon color="action" />
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText primary="Programme" secondary={student.programmeName || 'Not Assigned'} />
                  </ListItem>
                  <ListItem>
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: 'transparent' }}>
                        <GradeIcon color="action" />
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText
                      primary="Academic Status"
                      secondary={
                        <Chip
                          label={student.academicStatus || 'Active'}
                          color={getStatusColor(student.academicStatus || 'Active')}
                          size="small"
                        />
                      }
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: 'transparent' }}>
                        <GradeIcon color="action" />
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText
                      primary="Cumulative GPA"
                      secondary={student.cumulativeGPA?.toFixed(2) || 'N/A'}
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: 'transparent' }}>
                        <SchoolIcon color="action" />
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText
                      primary="Credits Earned"
                      secondary={`${student.totalCreditsEarned} credits`}
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: 'transparent' }}>
                        <BedIcon color="action" />
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText
                      primary="Accommodation"
                      secondary={
                        student.accommodation ? `${student.accommodation.houseNumber} - ${student.accommodation.laneName}` : 'Not Assigned'
                      }
                    />
                  </ListItem>
                </List>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Enrollments
        </Typography>
        {student.enrollments && student.enrollments.length > 0 ? (
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Unit</TableCell>
                  <TableCell>Code</TableCell>
                  <TableCell>Credits</TableCell>
                  <TableCell>Semester</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Date</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {student.enrollments.map((enrollment: any) => (
                  <TableRow key={enrollment.id}>
                    <TableCell>{enrollment.unitName}</TableCell>
                    <TableCell>{enrollment.unitCode}</TableCell>
                    <TableCell>{enrollment.credits}</TableCell>
                    <TableCell>{enrollment.semesterName}</TableCell>
                    <TableCell>
                      <Chip
                        label={enrollment.status}
                        color={enrollment.status === 'Completed' ? 'success' : 'warning'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      {new Date(enrollment.enrollmentDate).toLocaleDateString()}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        ) : (
          <Alert severity="info">No enrollments found for this student.</Alert>
        )}
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Grades
        </Typography>
        {student.grades && student.grades.length > 0 ? (
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Unit</TableCell>
                  <TableCell>Code</TableCell>
                  <TableCell>Credits</TableCell>
                  <TableCell>Grade</TableCell>
                  <TableCell>Score</TableCell>
                  <TableCell>Semester</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {student.grades.map((grade: any) => (
                  <TableRow key={grade.id}>
                    <TableCell>{grade.unitName}</TableCell>
                    <TableCell>{grade.unitCode}</TableCell>
                    <TableCell>{grade.credits}</TableCell>
                    <TableCell>
                      <Chip
                        label={grade.grade || 'N/A'}
                        color={grade.grade && grade.grade !== 'F' ? 'success' : 'error'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>{grade.score || 'N/A'}</TableCell>
                    <TableCell>{grade.semesterName}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        ) : (
          <Alert severity="info">No grades found for this student.</Alert>
        )}
      </TabPanel>

      <TabPanel value={tabValue} index={3}>
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Attendance
        </Typography>
        <Alert severity="info">Attendance records will be displayed here.</Alert>
      </TabPanel>

      <TabPanel value={tabValue} index={4}>
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Timetable
        </Typography>
        <Alert severity="info">Timetable will be displayed here.</Alert>
      </TabPanel>

      <TabPanel value={tabValue} index={5}>
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Accommodation
        </Typography>
        {student.accommodation ? (
          <Card>
            <CardContent>
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Typography variant="body2" color="textSecondary">
                    House Number
                  </Typography>
                  <Typography variant="h6">{student.accommodation.houseNumber}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="body2" color="textSecondary">
                    Lane
                  </Typography>
                  <Typography variant="h6">{student.accommodation.laneName}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="body2" color="textSecondary">
                    Assigned Date
                  </Typography>
                  <Typography variant="h6">
                    {new Date(student.accommodation.assignedDate).toLocaleDateString()}
                  </Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="body2" color="textSecondary">
                    Status
                  </Typography>
                  <Chip
                    label={student.accommodation.status}
                    color={student.accommodation.status === 'Active' ? 'success' : 'default'}
                    size="small"
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        ) : (
          <Alert severity="info">No accommodation assigned for this student.</Alert>
        )}
      </TabPanel>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Student</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete this student? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            color="error"
            onClick={handleDeleteConfirm}
            disabled={deleteMutation.isPending}
          >
            {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
