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
  Tab,
  Tabs,
  Alert,
  LinearProgress,
  Tooltip,
  Divider,
  Avatar,
  List,
  ListItem,
  ListItemText,
  ListItemAvatar,
} from '@mui/material';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  Search as SearchIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon,
  Bed as BedIcon,
  Apartment as BuildingIcon,
  Home as HomeIcon,
  Person as PersonIcon,
  SwapHoriz as TransferIcon,
  CheckCircle as CheckCircleIcon,
  Cancel as CancelIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { accommodationService } from '../services/accommodation.service';
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
      id={`accommodation-tabpanel-${index}`}
      aria-labelledby={`accommodation-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
};

const a11yProps = (index: number) => ({
  id: `accommodation-tab-${index}`,
  'aria-controls': `accommodation-tabpanel-${index}`,
});

export const Accommodation: React.FC = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [filterBuilding, setFilterBuilding] = useState<string>('');
  const [filterBlock, setFilterBlock] = useState<string>('');
  const [filterStatus, setFilterStatus] = useState<string>('');
  const [assignDialogOpen, setAssignDialogOpen] = useState(false);
  const [selectedRoom, setSelectedRoom] = useState<string | null>(null);
  const [selectedStudent, setSelectedStudent] = useState<string>('');
  const [selectedSemester, setSelectedSemester] = useState<string>('');

  const { data: rooms, isLoading, isError, refetch } = useQuery({
    queryKey: ['rooms', page, rowsPerPage, searchTerm, filterBuilding, filterBlock, filterStatus],
    queryFn: () =>
      accommodationService.getRooms({
        page: page + 1,
        pageSize: rowsPerPage,
        searchTerm: searchTerm || undefined,
        buildingId: filterBuilding || undefined,
        blockId: filterBlock || undefined,
        isAvailable: filterStatus === 'available' ? true : filterStatus === 'occupied' ? false : undefined,
      }),
  });

  const { data: buildings } = useQuery({
    queryKey: ['buildings'],
    queryFn: () => accommodationService.getBuildings(),
  });

  const assignMutation = useMutation({
    mutationFn: (data: { roomId: string; studentId: string; semesterId: string }) =>
      accommodationService.assignRoom(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] });
      setAssignDialogOpen(false);
      setSelectedRoom(null);
      setSelectedStudent('');
      setSelectedSemester('');
    },
  });

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

  const handleAssignRoom = (roomId: string) => {
    setSelectedRoom(roomId);
    setAssignDialogOpen(true);
  };

  const handleAssignConfirm = () => {
    if (selectedRoom && selectedStudent && selectedSemester) {
      assignMutation.mutate({
        roomId: selectedRoom,
        studentId: selectedStudent,
        semesterId: selectedSemester,
      });
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Available':
        return 'success';
      case 'Occupied':
        return 'warning';
      case 'Maintenance':
        return 'error';
      case 'Reserved':
        return 'info';
      default:
        return 'default';
    }
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load accommodation data. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const roomList = rooms?.items || [];
  const totalCount = rooms?.totalCount || 0;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>
          Accommodation Management
        </Typography>
        <Box>
          {(user?.roles?.includes('Receptionist') || user?.roles?.includes('Administrator')) && (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              sx={{ mr: 1 }}
            >
              Add Building
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
          aria-label="accommodation tabs"
          sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}
        >
          <Tab label="Rooms" {...a11yProps(0)} />
          <Tab label="Buildings" {...a11yProps(1)} />
          <Tab label="Assignments" {...a11yProps(2)} />
          <Tab label="Reports" {...a11yProps(3)} />
        </Tabs>
      </Paper>

      <TabPanel value={tabValue} index={0}>
        <Paper sx={{ p: 2, mb: 3 }}>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} sm={3}>
              <TextField
                fullWidth
                size="small"
                placeholder="Search by room number..."
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
                <InputLabel>Building</InputLabel>
                <Select
                  value={filterBuilding}
                  onChange={(e) => setFilterBuilding(e.target.value)}
                  label="Building"
                >
                  <MenuItem value="">All</MenuItem>
                  {buildings?.map((b: any) => (
                    <MenuItem key={b.id} value={b.id}>{b.name}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={2}>
              <FormControl fullWidth size="small">
                <InputLabel>Status</InputLabel>
                <Select
                  value={filterStatus}
                  onChange={(e) => setFilterStatus(e.target.value)}
                  label="Status"
                >
                  <MenuItem value="">All</MenuItem>
                  <MenuItem value="available">Available</MenuItem>
                  <MenuItem value="occupied">Occupied</MenuItem>
                  <MenuItem value="maintenance">Maintenance</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={2}>
              <Button
                fullWidth
                variant="outlined"
                onClick={() => {
                  setFilterBuilding('');
                  setFilterBlock('');
                  setFilterStatus('');
                  setSearchInput('');
                  setSearchTerm('');
                }}
              >
                Clear Filters
              </Button>
            </Grid>
            <Grid item xs={12} sm={2}>
              <Button
                fullWidth
                variant="contained"
                startIcon={<AddIcon />}
                onClick={() => {}}
              >
                Add Room
              </Button>
            </Grid>
          </Grid>
        </Paper>

        <Paper>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Room</TableCell>
                  <TableCell>Block</TableCell>
                  <TableCell>Building</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Capacity</TableCell>
                  <TableCell>Price</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Occupant</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {roomList.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={9} align="center" sx={{ py: 4 }}>
                      <Typography variant="body1" color="textSecondary">
                        No rooms found
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  roomList.map((room: any) => (
                    <TableRow key={room.id} hover>
                      <TableCell>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <BedIcon sx={{ color: '#576426' }} />
                          <Typography variant="body2" fontWeight={500}>
                            {room.roomNumber}
                          </Typography>
                        </Box>
                      </TableCell>
                      <TableCell>{room.blockName}</TableCell>
                      <TableCell>{room.buildingName}</TableCell>
                      <TableCell>{room.roomType || 'Standard'}</TableCell>
                      <TableCell>{room.capacity}</TableCell>
                      <TableCell>
                        {new Intl.NumberFormat('en-KE', { style: 'currency', currency: 'KES' }).format(room.pricePerSemester)}
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={room.status}
                          color={getStatusColor(room.status)}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        {room.currentOccupant || 'Vacant'}
                      </TableCell>
                      <TableCell align="right">
                        <Tooltip title="View">
                          <IconButton size="small">
                            <ViewIcon />
                          </IconButton>
                        </Tooltip>
                        {(user?.roles?.includes('Receptionist') || user?.roles?.includes('Administrator')) && (
                          <>
                            {room.status === 'Available' && (
                              <Tooltip title="Assign">
                                <IconButton
                                  size="small"
                                  onClick={() => handleAssignRoom(room.id)}
                                >
                                  <PersonIcon />
                                </IconButton>
                              </Tooltip>
                            )}
                            <Tooltip title="Edit">
                              <IconButton size="small">
                                <EditIcon />
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
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Buildings Overview
        </Typography>
        <Grid container spacing={3}>
          {buildings?.map((building: any) => (
            <Grid item xs={12} md={6} lg={4} key={building.id}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                    <BuildingIcon sx={{ fontSize: 40, color: '#576426' }} />
                    <Box>
                      <Typography variant="h6">{building.name}</Typography>
                      <Typography variant="caption" color="textSecondary">
                        {building.address || 'No address'}
                      </Typography>
                    </Box>
                  </Box>
                  <Divider sx={{ mb: 2 }} />
                  <Grid container spacing={1}>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="textSecondary">
                        Floors
                      </Typography>
                      <Typography variant="body1" fontWeight={500}>
                        {building.totalFloors}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="textSecondary">
                        Blocks
                      </Typography>
                      <Typography variant="body1" fontWeight={500}>
                        {building.blocks?.length || 0}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="textSecondary">
                        Total Rooms
                      </Typography>
                      <Typography variant="body1" fontWeight={500}>
                        {building.totalRooms || 0}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="textSecondary">
                        Occupancy
                      </Typography>
                      <Typography variant="body1" fontWeight={500}>
                        {building.occupancyRate?.toFixed(1) || 0}%
                      </Typography>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Current Assignments
        </Typography>
        <Alert severity="info" sx={{ mb: 2 }}>
          View and manage room assignments for students.
        </Alert>
        {/* Assignment list would go here */}
      </TabPanel>

      <TabPanel value={tabValue} index={3}>
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Occupancy Reports
        </Typography>
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="subtitle1" fontWeight={500}>
                  Overall Occupancy
                </Typography>
                <Typography variant="h3" color="primary">
                  {rooms?.totalCount ? ((rooms.totalCount - (roomList.filter((r: any) => r.status === 'Available').length)) / rooms.totalCount * 100).toFixed(1) : 0}%
                </Typography>
                <Typography variant="caption" color="textSecondary">
                  {roomList.filter((r: any) => r.status === 'Occupied').length} occupied of {rooms?.totalCount || 0} rooms
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="subtitle1" fontWeight={500}>
                  Available Rooms
                </Typography>
                <Typography variant="h3" color="success">
                  {roomList.filter((r: any) => r.status === 'Available').length}
                </Typography>
                <Typography variant="caption" color="textSecondary">
                  Ready for assignment
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>

      {/* Assign Room Dialog */}
      <Dialog open={assignDialogOpen} onClose={() => setAssignDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Assign Room</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Typography variant="body2" color="textSecondary">
              Room: {selectedRoom}
            </Typography>
            <FormControl fullWidth>
              <InputLabel>Student</InputLabel>
              <Select
                value={selectedStudent}
                onChange={(e) => setSelectedStudent(e.target.value)}
                label="Student"
              >
                <MenuItem value="">Select Student</MenuItem>
                {/* Student list would be loaded from API */}
                <MenuItem value="student1">John Doe (STU-2024-0001)</MenuItem>
                <MenuItem value="student2">Jane Smith (STU-2024-0002)</MenuItem>
              </Select>
            </FormControl>
            <FormControl fullWidth>
              <InputLabel>Semester</InputLabel>
              <Select
                value={selectedSemester}
                onChange={(e) => setSelectedSemester(e.target.value)}
                label="Semester"
              >
                <MenuItem value="">Select Semester</MenuItem>
                <MenuItem value="sem1">Fall 2024</MenuItem>
                <MenuItem value="sem2">Spring 2025</MenuItem>
              </Select>
            </FormControl>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAssignDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleAssignConfirm}
            disabled={!selectedStudent || !selectedSemester || assignMutation.isPending}
          >
            {assignMutation.isPending ? 'Assigning...' : 'Assign Room'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};