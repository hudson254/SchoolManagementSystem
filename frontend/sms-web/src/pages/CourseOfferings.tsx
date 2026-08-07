import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
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
  Tooltip,
  Menu,
  MenuItem,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControl,
  InputLabel,
  Select,
  Grid,
  Alert,
} from '@mui/material';
import {
  Add as AddIcon,
  Search as SearchIcon,
  MoreVert as MoreVertIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon,
  Refresh as RefreshIcon,
  EventNote as EventNoteIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { courseOfferingService, CourseOffering, CourseOfferingStatus } from '../services/course-offering.service';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

const statusColors: Record<string, 'default' | 'primary' | 'success' | 'warning' | 'error'> = {
  Draft: 'default',
  Scheduled: 'primary',
  Active: 'success',
  Completed: 'default',
  Cancelled: 'error',
};

export const CourseOfferings: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [filterStatus, setFilterStatus] = useState<string>('');
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [offeringToDelete, setOfferingToDelete] = useState<string | null>(null);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['courseofferings', page, rowsPerPage, searchTerm, filterStatus],
    queryFn: () =>
      courseOfferingService.getCourseOfferings({
        searchTerm: searchTerm || undefined,
        includeInactive: filterStatus ? filterStatus === 'includeInactive' : undefined,
      }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => courseOfferingService.deleteCourseOffering(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseofferings'] });
      setDeleteDialogOpen(false);
      setOfferingToDelete(null);
    },
  });

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

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, id: string) => {
    setAnchorEl(event.currentTarget);
    setSelectedId(id);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedId(null);
  };

  const handleDeleteClick = (id: string) => {
    setOfferingToDelete(id);
    setDeleteDialogOpen(true);
    handleMenuClose();
  };

  const handleDeleteConfirm = () => {
    if (offeringToDelete) {
      deleteMutation.mutate(offeringToDelete);
    }
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load course offerings. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const offerings = data?.items || [];

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>
          Course Offerings
        </Typography>
        <Box>
          {(user?.roles?.includes('SystemAdministrator') || user?.roles?.includes('Moderator')) && (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => navigate('/course-offerings/new')}
              sx={{ mr: 1 }}
            >
              New Offering
            </Button>
          )}
          <Button variant="outlined" startIcon={<RefreshIcon />} onClick={() => refetch()}>
            Refresh
          </Button>
        </Box>
      </Box>

      <Paper sx={{ p: 2, mb: 3 }}>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} sm={4}>
            <TextField
              fullWidth
              size="small"
              placeholder="Search by course name, code, or offering code..."
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
          <Grid item xs={12} sm={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Status</InputLabel>
              <Select
                value={filterStatus}
                onChange={(e) => setFilterStatus(e.target.value)}
                label="Status"
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="Draft">Draft</MenuItem>
                <MenuItem value="Scheduled">Scheduled</MenuItem>
                <MenuItem value="Active">Active</MenuItem>
                <MenuItem value="Completed">Completed</MenuItem>
                <MenuItem value="Cancelled">Cancelled</MenuItem>
                <MenuItem value="includeInactive">Include Inactive</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={3}>
            <Button
              fullWidth
              variant="outlined"
              onClick={() => {
                setFilterStatus('');
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
                <TableCell>Offering</TableCell>
                <TableCell>Course</TableCell>
                <TableCell>Academic Year</TableCell>
                <TableCell>Semester</TableCell>
                <TableCell>Intake</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {offerings.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 4 }}>
                    <Typography variant="body1" color="textSecondary">
                      No course offerings found
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                offerings.map((offering: CourseOffering) => (
                  <TableRow key={offering.id} hover>
                    <TableCell>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                        <EventNoteIcon sx={{ color: '#576426' }} />
                        <Box>
                          <Typography variant="body2" fontWeight={500}>
                            {offering.offeringCode}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            {offering.startDate ? new Date(offering.startDate).toLocaleDateString() : '—'} —{' '}
                            {offering.endDate ? new Date(offering.endDate).toLocaleDateString() : '—'}
                          </Typography>
                        </Box>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" fontWeight={500}>
                        {offering.courseName}
                      </Typography>
                      <Typography variant="caption" color="textSecondary">
                        {offering.courseCode}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{offering.academicYearName}</Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{offering.semesterName}</Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{offering.intake || '—'}</Typography>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={offering.status}
                        color={(statusColors[offering.status] as any) || 'default'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title="View">
                        <IconButton size="small" onClick={() => navigate(`/course-offerings/${offering.id}`)}>
                          <ViewIcon />
                        </IconButton>
                      </Tooltip>
                      {(user?.roles?.includes('SystemAdministrator') || user?.roles?.includes('Moderator')) && (
                        <>
                          <Tooltip title="Edit">
                            <IconButton
                              size="small"
                              onClick={() => navigate(`/course-offerings/${offering.id}/edit`)}
                            >
                              <EditIcon />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="More">
                            <IconButton size="small" onClick={(e) => handleMenuOpen(e, offering.id)}>
                              <MoreVertIcon />
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
          count={data?.totalCount || 0}
          rowsPerPage={rowsPerPage}
          page={page}
          onPageChange={handleChangePage}
          onRowsPerPageChange={handleChangeRowsPerPage}
        />
      </Paper>

      <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
        <MenuItem onClick={() => { handleMenuClose(); navigate(`/course-offerings/${selectedId}`); }}>
          <ViewIcon fontSize="small" sx={{ mr: 1 }} /> View
        </MenuItem>
        <MenuItem onClick={() => { handleMenuClose(); navigate(`/course-offerings/${selectedId}/edit`); }}>
          <EditIcon fontSize="small" sx={{ mr: 1 }} /> Edit
        </MenuItem>
        <MenuItem onClick={() => { if (selectedId) handleDeleteClick(selectedId); }} sx={{ color: 'error.main' }}>
          <DeleteIcon fontSize="small" sx={{ mr: 1 }} /> Delete
        </MenuItem>
      </Menu>

      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Course Offering</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete this course offering? This action cannot be undone and will affect
            historical records.
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
