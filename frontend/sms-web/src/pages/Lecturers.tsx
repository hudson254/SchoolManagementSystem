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
  TableSortLabel,
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
  Avatar,
  LinearProgress,
  Alert,
} from '@mui/material';
import {
  Add as AddIcon,
  Search as SearchIcon,
  MoreVert as MoreVertIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon,
  Verified as VerifiedIcon,
  PersonAdd as PersonAddIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { lecturerService } from '../services/lecturer.service';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

export const Lecturers: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [orderBy, setOrderBy] = useState('createdDate');
  const [orderDirection, setOrderDirection] = useState<'asc' | 'desc'>('desc');
  const [filterVerified, setFilterVerified] = useState<string>('');
  const [filterActive, setFilterActive] = useState<string>('');
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [selectedLecturerId, setSelectedLecturerId] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [lecturerToDelete, setLecturerToDelete] = useState<string | null>(null);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['lecturers', page, rowsPerPage, searchTerm, orderBy, orderDirection, filterVerified, filterActive],
    queryFn: () =>
      lecturerService.getLecturers({
        page: page + 1,
        pageSize: rowsPerPage,
        searchTerm: searchTerm || undefined,
        sortBy: orderBy,
        sortDescending: orderDirection === 'desc',
        isVerified: filterVerified ? filterVerified === 'true' : undefined,
        isActive: filterActive ? filterActive === 'true' : undefined,
      }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => lecturerService.deleteLecturer(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lecturers'] });
      setDeleteDialogOpen(false);
      setLecturerToDelete(null);
    },
  });

  const verifyMutation = useMutation({
    mutationFn: (id: string) => lecturerService.verifyLecturer(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lecturers'] });
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

  const handleSort = (property: string) => {
    const isAsc = orderBy === property && orderDirection === 'asc';
    setOrderDirection(isAsc ? 'desc' : 'asc');
    setOrderBy(property);
  };

  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, lecturerId: string) => {
    setAnchorEl(event.currentTarget);
    setSelectedLecturerId(lecturerId);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedLecturerId(null);
  };

  const handleDeleteClick = (id: string) => {
    setLecturerToDelete(id);
    setDeleteDialogOpen(true);
    handleMenuClose();
  };

  const handleDeleteConfirm = () => {
    if (lecturerToDelete) {
      deleteMutation.mutate(lecturerToDelete);
    }
  };

  const handleVerify = (id: string) => {
    verifyMutation.mutate(id);
    handleMenuClose();
  };

  const getInitials = (firstName: string, lastName: string) => {
    return `${firstName[0]}${lastName[0]}`.toUpperCase();
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load lecturers. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const lecturers = data?.items || [];
  const totalCount = data?.totalCount || 0;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>
          Lecturers
        </Typography>
        <Box>
          {(user?.roles?.includes('SystemAdministrator') || user?.roles?.includes('Moderator')) && (
            <Button
              variant="contained"
              startIcon={<PersonAddIcon />}
              onClick={() => navigate('/lecturers/new')}
              sx={{ mr: 1 }}
            >
              Add Lecturer
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

      <Paper sx={{ p: 2, mb: 3 }}>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} sm={4}>
            <TextField
              fullWidth
              size="small"
              placeholder="Search by name, email, or employee number..."
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
              <InputLabel>Verified</InputLabel>
              <Select
                value={filterVerified}
                onChange={(e) => setFilterVerified(e.target.value)}
                label="Verified"
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="true">Verified</MenuItem>
                <MenuItem value="false">Unverified</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Status</InputLabel>
              <Select
                value={filterActive}
                onChange={(e) => setFilterActive(e.target.value)}
                label="Status"
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="true">Active</MenuItem>
                <MenuItem value="false">Inactive</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={2}>
            <Button
              fullWidth
              variant="outlined"
              onClick={() => {
                setFilterVerified('');
                setFilterActive('');
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
                <TableCell>Lecturer</TableCell>
                <TableCell>
                  <TableSortLabel
                    active={orderBy === 'employeeNumber'}
                    direction={orderDirection}
                    onClick={() => handleSort('employeeNumber')}
                  >
                    Employee Number
                  </TableSortLabel>
                </TableCell>
                <TableCell>Specialization</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Verified</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {lecturers.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                    <Typography variant="body1" color="textSecondary">
                      No lecturers found
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                lecturers.map((lecturer: any) => (
                  <TableRow key={lecturer.id} hover>
                    <TableCell>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                        <Avatar sx={{ bgcolor: '#1976d2' }}>
                          {getInitials(lecturer.firstName, lecturer.lastName)}
                        </Avatar>
                        <Box>
                          <Typography variant="body2" fontWeight={500}>
                            {lecturer.firstName} {lecturer.lastName}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            {lecturer.email}
                          </Typography>
                        </Box>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{lecturer.employeeNumber}</Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{lecturer.specialization || 'Not Specified'}</Typography>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={lecturer.isActive ? 'Active' : 'Inactive'}
                        color={lecturer.isActive ? 'success' : 'default'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      {lecturer.isVerified ? (
                        <Chip
                          icon={<VerifiedIcon />}
                          label="Verified"
                          color="success"
                          size="small"
                        />
                      ) : (
                        <Chip
                          label="Pending"
                          color="warning"
                          size="small"
                        />
                      )}
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title="View">
                        <IconButton
                          size="small"
                          onClick={() => navigate(`/lecturers/${lecturer.id}`)}
                        >
                          <ViewIcon />
                        </IconButton>
                      </Tooltip>
                      {(user?.roles?.includes('SystemAdministrator') || user?.roles?.includes('Moderator')) && (
                        <>
                          <Tooltip title="Edit">
                            <IconButton
                              size="small"
                              onClick={() => navigate(`/lecturers/${lecturer.id}/edit`)}
                            >
                              <EditIcon />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="More">
                            <IconButton
                              size="small"
                              onClick={(e) => handleMenuOpen(e, lecturer.id)}
                            >
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
          count={totalCount}
          rowsPerPage={rowsPerPage}
          page={page}
          onPageChange={handleChangePage}
          onRowsPerPageChange={handleChangeRowsPerPage}
        />
      </Paper>

      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        <MenuItem onClick={() => { handleMenuClose(); navigate(`/lecturers/${selectedLecturerId}`); }}>
          <ViewIcon fontSize="small" sx={{ mr: 1 }} /> View
        </MenuItem>
        <MenuItem onClick={() => { handleMenuClose(); navigate(`/lecturers/${selectedLecturerId}/edit`); }}>
          <EditIcon fontSize="small" sx={{ mr: 1 }} /> Edit
        </MenuItem>
        {!lecturers.find(l => l.id === selectedLecturerId)?.isVerified && (
          <MenuItem onClick={() => { if (selectedLecturerId) handleVerify(selectedLecturerId); }}>
            <VerifiedIcon fontSize="small" sx={{ mr: 1 }} /> Verify
          </MenuItem>
        )}
        <MenuItem onClick={() => { if (selectedLecturerId) handleDeleteClick(selectedLecturerId); }} sx={{ color: 'error.main' }}>
          <DeleteIcon fontSize="small" sx={{ mr: 1 }} /> Delete
        </MenuItem>
      </Menu>

      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete Lecturer</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete this lecturer? This action cannot be undone.
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