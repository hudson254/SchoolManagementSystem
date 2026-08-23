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
  Switch,
  FormControlLabel,
} from '@mui/material';
import {
  Add as AddIcon,
  Search as SearchIcon,
  MoreVert as MoreVertIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon,
  PersonAdd as PersonAddIcon,
  Refresh as RefreshIcon,
  Lock as LockIcon,
  LockOpen as LockOpenIcon,
  Verified as VerifiedIcon,
  Security as SecurityIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { userService } from '../services/user.service';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

export const Users: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [orderBy, setOrderBy] = useState('createdDate');
  const [orderDirection, setOrderDirection] = useState<'asc' | 'desc'>('desc');
  const [filterRole, setFilterRole] = useState<string>('');
  const [filterActive, setFilterActive] = useState<string>('');
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [userToDelete, setUserToDelete] = useState<string | null>(null);
  const [roleDialogOpen, setRoleDialogOpen] = useState(false);
  const [selectedUserRoles, setSelectedUserRoles] = useState<string[]>([]);
  const [roleUser, setRoleUser] = useState<string | null>(null);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['users', page, rowsPerPage, searchTerm, orderBy, orderDirection, filterRole, filterActive],
    queryFn: () =>
      userService.getUsers({
        page: page + 1,
        pageSize: rowsPerPage,
        searchTerm: searchTerm || undefined,
        sortBy: orderBy,
        sortDescending: orderDirection === 'desc',
        role: filterRole || undefined,
        isActive: filterActive ? filterActive === 'true' : undefined,
      }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => userService.deleteUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setDeleteDialogOpen(false);
      setUserToDelete(null);
    },
  });

  const activateMutation = useMutation({
    mutationFn: (id: string) => userService.activateUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => userService.deactivateUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });

  const roleMutation = useMutation({
    mutationFn: (data: { userId: string; roles: string[] }) => userService.assignRoles(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setRoleDialogOpen(false);
      setRoleUser(null);
      setSelectedUserRoles([]);
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

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, userId: string) => {
    setAnchorEl(event.currentTarget);
    setSelectedUserId(userId);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedUserId(null);
  };

  const handleDeleteClick = (id: string) => {
    setUserToDelete(id);
    setDeleteDialogOpen(true);
    handleMenuClose();
  };

  const handleDeleteConfirm = () => {
    if (userToDelete) {
      deleteMutation.mutate(userToDelete);
    }
  };

  const handleToggleActive = (id: string, currentStatus: boolean) => {
    if (currentStatus) {
      deactivateMutation.mutate(id);
    } else {
      activateMutation.mutate(id);
    }
    handleMenuClose();
  };

  const handleManageRoles = (userId: string, currentRoles: string[]) => {
    setRoleUser(userId);
    setSelectedUserRoles(currentRoles);
    setRoleDialogOpen(true);
    handleMenuClose();
  };

  const handleRoleToggle = (role: string) => {
    setSelectedUserRoles((prev) =>
      prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role]
    );
  };

  const handleRolesSave = () => {
    if (roleUser) {
      roleMutation.mutate({ userId: roleUser, roles: selectedUserRoles });
    }
  };

  const getInitials = (firstName: string, lastName: string) => {
    return `${firstName[0]}${lastName[0]}`.toUpperCase();
  };

  const availableRoles = ['SystemAdministrator', 'Moderator', 'Lecturer', 'Student', 'Receptionist'];

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load users. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const users = data?.items || [];
  const totalCount = data?.totalCount || 0;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>
          User Management
        </Typography>
        <Box>
          <Button
            variant="contained"
            startIcon={<PersonAddIcon />}
            onClick={() => navigate('/users/new')}
            sx={{ mr: 1 }}
          >
            Add User
          </Button>
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
              placeholder="Search by name, email, or username..."
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
              <InputLabel>Role</InputLabel>
              <Select
                value={filterRole}
                onChange={(e) => setFilterRole(e.target.value)}
                label="Role"
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="SystemAdministrator">System Administrator</MenuItem>
                <MenuItem value="Administrator">Administrator</MenuItem>
                <MenuItem value="Coordinator">Coordinator</MenuItem>
                <MenuItem value="Lecturer">Lecturer</MenuItem>
                <MenuItem value="Student">Student</MenuItem>
                <MenuItem value="Receptionist">Receptionist</MenuItem>
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
                setFilterRole('');
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
                <TableCell>User</TableCell>
                <TableCell>
                  <TableSortLabel
                    active={orderBy === 'email'}
                    direction={orderDirection}
                    onClick={() => handleSort('email')}
                  >
                    Email
                  </TableSortLabel>
                </TableCell>
                <TableCell>Roles</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Verified</TableCell>
                <TableCell>Last Login</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {users.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 4 }}>
                    <Typography variant="body1" color="textSecondary">
                      No users found
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                users.map((userItem: any) => (
                  <TableRow key={userItem.id} hover>
                    <TableCell>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                        <Avatar sx={{ bgcolor: '#576426' }}>
                          {getInitials(userItem.firstName, userItem.lastName)}
                        </Avatar>
                        <Box>
                          <Typography variant="body2" fontWeight={500}>
                            {userItem.firstName} {userItem.lastName}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            {userItem.userName}
                          </Typography>
                        </Box>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{userItem.email}</Typography>
                    </TableCell>
                    <TableCell>
                      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                        {userItem.roles?.map((role: string) => (
                          <Chip
                            key={role}
                            label={role}
                            size="small"
                            color={role === 'SystemAdministrator' ? 'error' : 'primary'}
                            variant="outlined"
                          />
                        ))}
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={userItem.isActive ? 'Active' : 'Inactive'}
                        color={userItem.isActive ? 'success' : 'default'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={userItem.isEmailVerified ? 'Verified' : 'Pending'}
                        color={userItem.isEmailVerified ? 'success' : 'warning'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">
                        {userItem.lastLoginDate ? new Date(userItem.lastLoginDate).toLocaleDateString() : 'Never'}
                      </Typography>
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title="View">
                        <IconButton
                          size="small"
                          onClick={() => navigate(`/users/${userItem.id}`)}
                        >
                          <ViewIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Edit">
                        <IconButton
                          size="small"
                          onClick={() => navigate(`/users/${userItem.id}/edit`)}
                        >
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="More">
                        <IconButton
                          size="small"
                          onClick={(e) => handleMenuOpen(e, userItem.id)}
                        >
                          <MoreVertIcon />
                        </IconButton>
                      </Tooltip>
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
        <MenuItem onClick={() => { handleMenuClose(); navigate(`/users/${selectedUserId}`); }}>
          <ViewIcon fontSize="small" sx={{ mr: 1 }} /> View
        </MenuItem>
        <MenuItem onClick={() => { handleMenuClose(); navigate(`/users/${selectedUserId}/edit`); }}>
          <EditIcon fontSize="small" sx={{ mr: 1 }} /> Edit
        </MenuItem>
        <MenuItem onClick={() => { if (selectedUserId) handleManageRoles(selectedUserId, []); }}>
          <SecurityIcon fontSize="small" sx={{ mr: 1 }} /> Manage Roles
        </MenuItem>
        {users.find((u: any) => u.id === selectedUserId)?.isActive ? (
          <MenuItem onClick={() => { if (selectedUserId) handleToggleActive(selectedUserId, true); }}>
            <LockIcon fontSize="small" sx={{ mr: 1 }} /> Deactivate
          </MenuItem>
        ) : (
          <MenuItem onClick={() => { if (selectedUserId) handleToggleActive(selectedUserId, false); }}>
            <LockOpenIcon fontSize="small" sx={{ mr: 1 }} /> Activate
          </MenuItem>
        )}
        <MenuItem onClick={() => { if (selectedUserId) handleDeleteClick(selectedUserId); }} sx={{ color: 'error.main' }}>
          <DeleteIcon fontSize="small" sx={{ mr: 1 }} /> Delete
        </MenuItem>
      </Menu>

      {/* Delete Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Delete User</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete this user? This action cannot be undone.
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

      {/* Manage Roles Dialog */}
      <Dialog open={roleDialogOpen} onClose={() => setRoleDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Manage Roles</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
            Select the roles to assign to this user.
          </Typography>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
            {availableRoles.map((role) => (
              <FormControlLabel
                key={role}
                control={
                  <Switch
                    checked={selectedUserRoles.includes(role)}
                    onChange={() => handleRoleToggle(role)}
                  />
                }
                label={role}
              />
            ))}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRoleDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleRolesSave}
            disabled={roleMutation.isPending}
          >
            {roleMutation.isPending ? 'Saving...' : 'Save Roles'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};