import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  IconButton,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert,
  CircularProgress,
  Card,
  CardContent,
  Grid,
  Divider,
  FormControlLabel,
  Switch,
  MenuItem,
  Table,
  TableHead,
  TableBody,
  TableRow,
  TableCell,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Home as HomeIcon,
  Refresh as RefreshIcon,
  Hotel as HotelIcon,
  Login as CheckInIcon,
  Logout as CheckOutIcon,
  SwapHoriz as TransferIcon,
  Bed as BedIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { accommodationService } from '../services/accommodation.service';
import { studentService } from '../services/student.service';
import { lecturerService } from '../services/lecturer.service';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

const STAFF_ROLES = ['Receptionist', 'Coordinator', 'Administrator', 'SystemAdministrator'];

export const Accommodation: React.FC = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const isAdmin =
    user?.roles?.includes('SystemAdministrator') || user?.roles?.includes('Administrator');
  const isStaff = (user?.roles || []).some((r) => STAFF_ROLES.includes(r));

  // Lane state
  const [laneDialogOpen, setLaneDialogOpen] = useState(false);
  const [editingLaneId, setEditingLaneId] = useState<string | null>(null);
  const [laneName, setLaneName] = useState('');
  const [laneDescription, setLaneDescription] = useState('');
  const [laneHouseCount, setLaneHouseCount] = useState(10);
  const [laneCapacity, setLaneCapacity] = useState(1);
  const [laneError, setLaneError] = useState('');
  const [deleteLaneConfirmOpen, setDeleteLaneConfirmOpen] = useState(false);
  const [laneToDelete, setLaneToDelete] = useState<string | null>(null);

  // House/Room state
  const [roomDialogOpen, setRoomDialogOpen] = useState(false);
  const [selectedLaneId, setSelectedLaneId] = useState<string | null>(null);
  const [editingRoomId, setEditingRoomId] = useState<string | null>(null);
  const [roomNumber, setRoomNumber] = useState('');
  const [roomName, setRoomName] = useState('');
  const [roomCapacity, setRoomCapacity] = useState(1);
  const [roomAvailable, setRoomAvailable] = useState(true);
  const [roomError, setRoomError] = useState('');
  const [deleteRoomConfirmOpen, setDeleteRoomConfirmOpen] = useState(false);
  const [roomToDelete, setRoomToDelete] = useState<string | null>(null);

  // Assignment state
  const [assignDialogOpen, setAssignDialogOpen] = useState(false);
  const [assignOccupantType, setAssignOccupantType] = useState('Student');
  const [assignSearch, setAssignSearch] = useState('');
  const [assignOccupantId, setAssignOccupantId] = useState<string | null>(null);
  const [assignOccupantLabel, setAssignOccupantLabel] = useState('');
  const [assignLaneId, setAssignLaneId] = useState('');
  const [assignHouseId, setAssignHouseId] = useState('');
  const [assignRemarks, setAssignRemarks] = useState('');
  const [assignError, setAssignError] = useState('');
  const [reassignDialogOpen, setReassignDialogOpen] = useState(false);
  const [reassignOccupantId, setReassignOccupantId] = useState<string | null>(null);
  const [reassignOccupantType, setReassignOccupantType] = useState('Student');
  const [reassignLaneId, setReassignLaneId] = useState('');
  const [reassignNewHouseId, setReassignNewHouseId] = useState('');
  const [reassignError, setReassignError] = useState('');
  const [vacateConfirmOpen, setVacateConfirmOpen] = useState(false);
  const [houseToVacate, setHouseToVacate] = useState<string | null>(null);
  const [assignmentFilter, setAssignmentFilter] = useState('');

  // Success/Error messages
  const [successMessage, setSuccessMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');

  // Fetch lanes
  const { data: lanes, isLoading: lanesLoading, isError: lanesError, refetch: refetchLanes } = useQuery({
    queryKey: ['lanes'],
    queryFn: () => accommodationService.getLanes(),
  });

  // Fetch all houses
  const { data: houses, isLoading: housesLoading, refetch: refetchHouses } = useQuery({
    queryKey: ['houses'],
    queryFn: () => accommodationService.getHouses(),
  });

  // Fetch active assignments (occupancy)
  const { data: assignments, refetch: refetchAssignments } = useQuery({
    queryKey: ['assignments', assignmentFilter],
    queryFn: () => accommodationService.getAssignments({ searchTerm: assignmentFilter || undefined }),
  });

  const showSuccess = (msg: string) => {
    setSuccessMessage(msg);
    setTimeout(() => setSuccessMessage(''), 5000);
  };
  const showError = (msg: string) => {
    setErrorMessage(msg);
    setTimeout(() => setErrorMessage(''), 6000);
  };

  const refreshAll = () => {
    refetchLanes();
    refetchHouses();
    refetchAssignments();
  };
// ===== Lane mutations =====
  const createLaneMutation = useMutation({
    mutationFn: (data: { laneName: string; description?: string; numberOfHouses: number; defaultCapacity: number }) =>
      accommodationService.createLane({ ...data, startingHouseNumber: 1 }),
    onSuccess: () => {
      refreshAll();
      showSuccess('Lane created successfully');
      handleCloseLaneDialog();
    },
    onError: (err: any) => {
      setLaneError(err?.response?.data?.message || 'Failed to create lane');
    },
  });

  const updateLaneMutation = useMutation({
    mutationFn: (data: { id: string; laneName: string; description?: string }) =>
      accommodationService.updateLane(data.id, { laneName: data.laneName, description: data.description, isActive: true }),
    onSuccess: () => {
      refreshAll();
      showSuccess('Lane updated successfully');
      handleCloseLaneDialog();
    },
    onError: (err: any) => {
      setLaneError(err?.response?.data?.message || 'Failed to update lane');
    },
  });

  const deleteLaneMutation = useMutation({
    mutationFn: (id: string) => accommodationService.deleteLane(id),
    onSuccess: () => {
      refreshAll();
      showSuccess('Lane deleted successfully');
      setDeleteLaneConfirmOpen(false);
      setLaneToDelete(null);
    },
    onError: (err: any) => {
      showError(err?.response?.data?.message || 'Failed to delete lane');
      setDeleteLaneConfirmOpen(false);
      setLaneToDelete(null);
    },
  });

  // ===== House mutations =====
  const createHouseMutation = useMutation({
    mutationFn: (data: { laneId: string; houseNumber: string; defaultCapacity: number }) =>
      accommodationService.createHouses({
        laneId: data.laneId,
        numberOfHouses: 1,
        startingHouseNumber: parseInt(data.houseNumber, 10) || 1,
        defaultCapacity: data.defaultCapacity,
      }),
    onSuccess: () => {
      refreshAll();
      showSuccess('Room added successfully');
      handleCloseRoomDialog();
    },
    onError: (err: any) => {
      setRoomError(err?.response?.data?.message || 'Failed to add room');
    },
  });

  const updateHouseMutation = useMutation({
    mutationFn: (data: { id: string; houseNumber: string; houseName?: string; capacity?: number; isAvailable?: boolean; status?: string }) =>
      accommodationService.updateHouse(data.id, data),
    onSuccess: () => {
      refreshAll();
      showSuccess('Room updated successfully');
      handleCloseRoomDialog();
    },
    onError: (err: any) => {
      setRoomError(err?.response?.data?.message || 'Failed to update room');
    },
  });

  const deleteHouseMutation = useMutation({
    mutationFn: (id: string) => accommodationService.deleteHouse(id),
    onSuccess: () => {
      refreshAll();
      showSuccess('Room deleted successfully');
      setDeleteRoomConfirmOpen(false);
      setRoomToDelete(null);
    },
    onError: (err: any) => {
      showError(err?.response?.data?.message || 'Failed to delete room');
      setDeleteRoomConfirmOpen(false);
      setRoomToDelete(null);
    },
  });

  const toggleHouseAvailabilityMutation = useMutation({
    mutationFn: (data: { houseId: string; isUnavailable: boolean; notes?: string }) =>
      accommodationService.setHouseUnavailable(data.houseId, data.isUnavailable, data.notes),
    onSuccess: () => {
      refreshAll();
      showSuccess('House availability updated');
    },
    onError: (err: any) => {
      showError(err?.response?.data?.message || 'Failed to update house availability');
    },
  });
// ===== Assignment mutations =====
  const assignHouseMutation = useMutation({
    mutationFn: (data: {
      occupantType: 'Student' | 'Lecturer';
      occupantId: string;
      houseId: string;
      remarks?: string;
    }) =>
      accommodationService.assignHouse(data.houseId, {
        studentId: data.occupantType === 'Student' ? data.occupantId : undefined,
        lecturerId: data.occupantType === 'Lecturer' ? data.occupantId : undefined,
        occupantType: data.occupantType,
        houseId: data.houseId,
        remarks: data.remarks,
      }),
    onSuccess: () => {
      refreshAll();
      setAssignDialogOpen(false);
      showSuccess('Occupant assigned to house successfully');
      resetAssignDialog();
    },
    onError: (err: any) => {
      setAssignError(err?.response?.data?.message || 'Failed to assign occupant');
    },
  });

  const reassignMutation = useMutation({
    mutationFn: (data: { occupantId: string; occupantType: 'Student' | 'Lecturer'; newHouseId: string; remarks?: string }) =>
      accommodationService.reassignHouse(data.occupantId, {
        occupantType: data.occupantType,
        newHouseId: data.newHouseId,
        remarks: data.remarks,
      }),
    onSuccess: () => {
      refreshAll();
      setReassignDialogOpen(false);
      showSuccess('Occupant transferred successfully');
      setReassignOccupantId(null);
      setReassignNewHouseId('');
    },
    onError: (err: any) => {
      setReassignError(err?.response?.data?.message || 'Failed to transfer occupant');
    },
  });

  const vacateHouseMutation = useMutation({
    mutationFn: (houseId: string) => accommodationService.vacateHouse(houseId, {}),
    onSuccess: () => {
      refreshAll();
      setVacateConfirmOpen(false);
      setHouseToVacate(null);
      showSuccess('House vacated successfully');
    },
    onError: (err: any) => {
      showError(err?.response?.data?.message || 'Failed to vacate house');
      setVacateConfirmOpen(false);
      setHouseToVacate(null);
    },
  });

  const checkInMutation = useMutation({
    mutationFn: (assignmentId: string) => accommodationService.checkInAssignment(assignmentId, {}),
    onSuccess: () => {
      refreshAll();
      showSuccess('Occupant checked in');
    },
    onError: (err: any) => {
      showError(err?.response?.data?.message || 'Check-in failed');
    },
  });

  const checkOutMutation = useMutation({
    mutationFn: (assignmentId: string) => accommodationService.checkOutAssignment(assignmentId, {}),
    onSuccess: () => {
      refreshAll();
      showSuccess('Occupant checked out');
    },
    onError: (err: any) => {
      showError(err?.response?.data?.message || 'Check-out failed');
    },
  });

  // Occupant search
  const { data: searchStudents, refetch: refetchStudents } = useQuery({
    queryKey: ['accommodation-search-students', assignSearch],
    queryFn: () => studentService.getStudents({ searchTerm: assignSearch, pageSize: 20 }),
  });

  const { data: searchLecturers, refetch: refetchLecturers } = useQuery({
    queryKey: ['accommodation-search-lecturers', assignSearch],
    queryFn: () => lecturerService.getLecturers({ searchTerm: assignSearch, pageSize: 20 }),
  });
// ===== Lane dialog handlers =====
  const handleOpenAddLane = () => {
    setEditingLaneId(null);
    setLaneName('');
    setLaneDescription('');
    setLaneHouseCount(10);
    setLaneCapacity(1);
    setLaneError('');
    setLaneDialogOpen(true);
  };

  const handleOpenEditLane = (lane: any) => {
    setEditingLaneId(lane.id);
    setLaneName(lane.laneName);
    setLaneDescription(lane.description || '');
    setLaneHouseCount(lane.totalHouses || 0);
    setLaneCapacity(1);
    setLaneError('');
    setLaneDialogOpen(true);
  };

  function handleCloseLaneDialog() {
    setLaneDialogOpen(false);
    setEditingLaneId(null);
    setLaneName('');
    setLaneDescription('');
    setLaneError('');
  };

  const handleSaveLane = () => {
    if (!laneName.trim()) {
      setLaneError('Lane name is required');
      return;
    }
    setLaneError('');
    if (editingLaneId) {
      updateLaneMutation.mutate({ id: editingLaneId, laneName: laneName.trim(), description: laneDescription.trim() || undefined });
    } else {
      const count = parseInt(String(laneHouseCount), 10) || 0;
      if (count <= 0) {
        setLaneError('Number of houses must be greater than 0');
        return;
      }
      createLaneMutation.mutate({
        laneName: laneName.trim(),
        description: laneDescription.trim() || undefined,
        numberOfHouses: count,
        defaultCapacity: parseInt(String(laneCapacity), 10) || 1,
      });
    }
  };

  const handleDeleteLaneClick = (laneId: string) => {
    setLaneToDelete(laneId);
    setDeleteLaneConfirmOpen(true);
  };

  const handleDeleteLaneConfirm = () => {
    if (laneToDelete) {
      deleteLaneMutation.mutate(laneToDelete);
    }
  };

  // ===== Room dialog handlers =====
  const handleOpenAddRoom = (laneId: string) => {
    setSelectedLaneId(laneId);
    setEditingRoomId(null);
    setRoomNumber('');
    setRoomName('');
    setRoomCapacity(1);
    setRoomAvailable(true);
    setRoomError('');
    setRoomDialogOpen(true);
  };

  const handleOpenEditRoom = (house: any) => {
    setSelectedLaneId(house.laneId);
    setEditingRoomId(house.id);
    setRoomNumber(house.houseNumber);
    setRoomName(house.houseName || '');
    setRoomCapacity(house.capacity || 1);
    setRoomAvailable(!!house.isAvailable);
    setRoomError('');
    setRoomDialogOpen(true);
  };

  function handleCloseRoomDialog() {
    setRoomDialogOpen(false);
    setSelectedLaneId(null);
    setEditingRoomId(null);
    setRoomNumber('');
    setRoomName('');
    setRoomError('');
  };

  const handleSaveRoom = () => {
    if (!roomNumber.trim()) {
      setRoomError('Room number is required');
      return;
    }
    setRoomError('');
    if (editingRoomId) {
      updateHouseMutation.mutate({
        id: editingRoomId,
        houseNumber: roomNumber.trim(),
        houseName: roomName.trim() || undefined,
        capacity: parseInt(String(roomCapacity), 10) || 1,
        isAvailable: roomAvailable,
      });
    } else if (selectedLaneId) {
      createHouseMutation.mutate({
        laneId: selectedLaneId,
        houseNumber: roomNumber.trim(),
        defaultCapacity: parseInt(String(roomCapacity), 10) || 1,
      });
    }
  };

  const handleDeleteRoomClick = (houseId: string) => {
    setRoomToDelete(houseId);
    setDeleteRoomConfirmOpen(true);
  };

  const handleDeleteRoomConfirm = () => {
    if (roomToDelete) {
      deleteHouseMutation.mutate(roomToDelete);
    }
  };

  const handleToggleHouseAvailability = (house: any) => {
    const isUnavailable = house.isAvailable === true;
    toggleHouseAvailabilityMutation.mutate({
      houseId: house.id,
      isUnavailable,
      notes: `Availability changed from UI (${house.isAvailable ? 'available' : 'unavailable'} -> ${isUnavailable ? 'unavailable' : 'available'})`,
    });
  };
// ===== Assignment dialog handlers =====
  function resetAssignDialog() {
    setAssignOccupantType('Student');
    setAssignSearch('');
    setAssignOccupantId(null);
    setAssignOccupantLabel('');
    setAssignLaneId('');
    setAssignHouseId('');
    setAssignRemarks('');
    setAssignError('');
  };

  const handleOpenAssign = () => {
    resetAssignDialog();
    setAssignDialogOpen(true);
  };

  const assignableHouses = (houses || []).filter(
    (h: any) => h.isAvailable && (h.remainingCapacity ?? h.capacity - h.occupiedCount) > 0
  );

  const reassignmentTargetHouses = (houses || []).filter(
    (h: any) => h.isAvailable && (h.remainingCapacity ?? h.capacity - h.occupiedCount) > 0
  );

  const handleOpenReassign = (assignment: any) => {
    setReassignOccupantId(assignment.occupantType === 'Student' ? assignment.studentId : assignment.lecturerId);
    setReassignOccupantType(assignment.occupantType);
    setReassignLaneId(assignment.laneId || '');
    setReassignNewHouseId('');
    setReassignError('');
    setReassignDialogOpen(true);
  };

  const handleAssignSubmit = () => {
    if (!assignOccupantId || !assignHouseId) {
      setAssignError('Select an occupant and a house');
      return;
    }
    setAssignError('');
    assignHouseMutation.mutate({
      occupantType: assignOccupantType as 'Student' | 'Lecturer',
      occupantId: assignOccupantId,
      houseId: assignHouseId,
      remarks: assignRemarks.trim() || undefined,
    });
  };

  const handleReassignSubmit = () => {
    if (!reassignOccupantId || !reassignNewHouseId) {
      setReassignError('Select an occupant and a destination house');
      return;
    }
    setReassignError('');
    reassignMutation.mutate({
      occupantId: reassignOccupantId,
      occupantType: reassignOccupantType as 'Student' | 'Lecturer',
      newHouseId: reassignNewHouseId,
    });
  };

  const handleVacateHouseClick = (houseId: string) => {
    setHouseToVacate(houseId);
    setVacateConfirmOpen(true);
  };

  const handleCheckIn = (assignmentId: string) => checkInMutation.mutate(assignmentId);
  const handleCheckOut = (assignmentId: string) => checkOutMutation.mutate(assignmentId);

  // Clear messages after timeout
  useEffect(() => {
    if (successMessage || errorMessage) {
      const timer = setTimeout(() => {
        setSuccessMessage('');
        setErrorMessage('');
      }, 5000);
      return () => clearTimeout(timer);
    }
  }, [successMessage, errorMessage]);

  if (lanesLoading || housesLoading) return <LoadingSpinner />;

  if (lanesError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load accommodation data. Please try again.
          <Button size="small" onClick={() => refetchLanes()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const laneList = lanes || [];
  const houseList = houses || [];
  const assignmentList = assignments || [];
return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>
          Accommodation
        </Typography>
        <Box>
          {isStaff && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={handleOpenAddLane} sx={{ mr: 1 }}>
              Add Lane
            </Button>
          )}
          {isStaff && (
            <Button variant="outlined" startIcon={<BedIcon />} onClick={handleOpenAssign} sx={{ mr: 1 }}>
              Assign Occupant
            </Button>
          )}
          <Button variant="outlined" startIcon={<RefreshIcon />} onClick={refreshAll}>
            Refresh
          </Button>
        </Box>
      </Box>

      {/* Success/Error Messages */}
      {successMessage && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccessMessage('')}>
          {successMessage}
        </Alert>
      )}
      {errorMessage && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setErrorMessage('')}>
          {errorMessage}
        </Alert>
      )}

      {/* Lanes List */}
      {laneList.length === 0 ? (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <HotelIcon sx={{ fontSize: 64, color: 'text.disabled', mb: 2 }} />
          <Typography variant="h6" color="textSecondary" gutterBottom>
            No Lanes Found
          </Typography>
          <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
            Create your first lane to start managing accommodation.
          </Typography>
          {isStaff && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={handleOpenAddLane}>
              Add Lane
            </Button>
          )}
        </Paper>
      ) : (
        <Grid container spacing={3}>
          {laneList.map((lane: any) => {
            const laneRooms = houseList.filter((h: any) => h.laneId === lane.id);
            return (
              <Grid item xs={12} key={lane.id}>
                <Card variant="outlined">
                  <CardContent>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                        <HomeIcon sx={{ color: '#576426' }} />
                        <Typography variant="h6" fontWeight={600}>
                          {lane.laneName}
                        </Typography>
                        <Chip label={lane.isActive ? 'Active' : 'Inactive'} color={lane.isActive ? 'success' : 'default'} size="small" />
                        <Chip label={`${laneRooms.length} houses`} variant="outlined" size="small" />
                      </Box>
                      {isStaff && (
                        <Box>
                          {isAdmin && (
                            <IconButton size="small" onClick={() => handleDeleteLaneClick(lane.id)} title="Delete Lane" color="error">
                              <DeleteIcon fontSize="small" />
                            </IconButton>
                          )}
                          <IconButton size="small" onClick={() => handleOpenEditLane(lane)} title="Edit Lane">
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Box>
                      )}
                    </Box>
                    {lane.description && (
                      <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                        {lane.description}
                      </Typography>
                    )}

                    <Divider sx={{ mb: 2 }} />

                    <Typography variant="subtitle2" fontWeight={600} gutterBottom>
                      Houses
                    </Typography>
                    {laneRooms.length === 0 ? (
                      <Typography variant="body2" color="textSecondary" sx={{ ml: 2, mb: 1 }}>
                        No houses added yet.
                      </Typography>
                    ) : (
                      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 1 }}>
                        {laneRooms.map((house: any) => {
                          const free = (house.remainingCapacity ?? house.capacity - house.occupiedCount) ?? 0;
                          const full = free <= 0;
                          return (
                            <Chip
                              key={house.id}
                              label={`${house.houseNumber}${house.houseName ? ` · ${house.houseName}` : ''} (${house.occupiedCount ?? 0}/${house.capacity ?? 1}${house.isAvailable ? '' : ' · Unavailable'})`}
                              variant="outlined"
                              color={full || !house.isAvailable ? 'error' : house.isOccupied ? 'warning' : 'success'}
                              onClick={isStaff ? () => handleOpenEditRoom(house) : undefined}
                              onDelete={isAdmin ? () => handleDeleteRoomClick(house.id) : undefined}
                              title={`House ${house.houseNumber} - ${house.status || 'Vacant'} · ${house.occupiedCount ?? 0}/${house.capacity ?? 1} · ${house.isAvailable ? 'Available' : 'Unavailable'}`}
                            />
                          );
                        })}
                      </Box>
                    )}

                    {isStaff && (
                      <Button size="small" startIcon={<AddIcon />} onClick={() => handleOpenAddRoom(lane.id)}>
                        Add House
                      </Button>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            );
          })}
        </Grid>
      )}
{/* Occupancy / Assignments */}
      <Paper sx={{ p: 3, mt: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, flexWrap: 'wrap', gap: 1 }}>
          <Typography variant="h6" fontWeight={600}>
            Current Occupancy ({assignmentList.length})
          </Typography>
          <TextField
            size="small"
            variant="outlined"
            placeholder="Search occupant, house or lane..."
            value={assignmentFilter}
            onChange={(e) => setAssignmentFilter(e.target.value)}
            sx={{ minWidth: 280 }}
          />
        </Box>
        {assignmentList.length === 0 ? (
          <Typography variant="body2" color="textSecondary">
            No active accommodation assignments. Assign an occupant using the Assign Occupant button.
          </Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Occupant</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>House</TableCell>
                <TableCell>Lane</TableCell>
                <TableCell>Check-in</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {assignmentList.map((a: any) => (
                <TableRow key={a.id} hover>
                  <TableCell>
                    <Typography variant="body2" fontWeight={600}>
                      {a.occupantType === 'Student' ? a.studentName : a.lecturerName}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      {a.occupantType === 'Student' ? a.studentNumber : a.employeeNumber}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip size="small" variant="outlined" label={a.occupantType} />
                  </TableCell>
                  <TableCell>
                    {a.houseNumber}
                    {a.houseName ? ` · ${a.houseName}` : ''}
                    <Typography variant="caption" display="block" color="textSecondary">
                      {a.houseOccupiedCount}/{a.houseCapacity} occupied
                    </Typography>
                  </TableCell>
                  <TableCell>{a.laneName}</TableCell>
                  <TableCell>
                    {a.isCheckedIn ? (
                      <Chip size="small" color="success" label={`Checked in ${a.checkInDate ? new Date(a.checkInDate).toLocaleDateString() : ''}`} />
                    ) : (
                      <Chip size="small" variant="outlined" label="Not checked in" />
                    )}
                  </TableCell>
                  <TableCell align="right">
                    <Box sx={{ display: 'flex', gap: 0.5, justifyContent: 'flex-end' }}>
                      {!a.isCheckedIn && !a.isCheckedOut && (
                        <IconButton size="small" title="Check in" color="success" disabled={checkInMutation.isPending} onClick={() => handleCheckIn(a.id)}>
                          <CheckInIcon fontSize="small" />
                        </IconButton>
                      )}
                      {a.isCheckedIn && !a.isCheckedOut && (
                        <>
                          <IconButton size="small" title="Check out" color="error" disabled={checkOutMutation.isPending} onClick={() => handleCheckOut(a.id)}>
                            <CheckOutIcon fontSize="small" />
                          </IconButton>
                          <IconButton size="small" title="Transfer to another house" onClick={() => handleOpenReassign(a)}>
                            <TransferIcon fontSize="small" />
                          </IconButton>
                        </>
                      )}
                    </Box>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Paper>
{/* Lane Add/Edit Dialog */}
      <Dialog open={laneDialogOpen} onClose={handleCloseLaneDialog} maxWidth="sm" fullWidth>
        <DialogTitle>{editingLaneId ? 'Edit Lane' : 'Add Lane'}</DialogTitle>
        <DialogContent>
          {laneError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {laneError}
            </Alert>
          )}
          <Box sx={{ pt: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              fullWidth
              label="Lane Name"
              value={laneName}
              onChange={(e) => setLaneName(e.target.value)}
              error={!!laneError && laneError.includes('name')}
              helperText={laneError && laneError.includes('name') ? laneError : 'e.g. Lane A, North Wing, Male Lane'}
              autoFocus
            />
            {!editingLaneId && (
              <TextField
                fullWidth
                label="Number of Houses"
                type="number"
                value={String(laneHouseCount)}
                onChange={(e) => setLaneHouseCount(parseInt(e.target.value, 10) || 0)}
                helperText="Houses created automatically with this lane"
              />
            )}
            {!editingLaneId && (
              <TextField
                fullWidth
                label="Default Capacity per House"
                type="number"
                value={String(laneCapacity)}
                onChange={(e) => setLaneCapacity(parseInt(e.target.value, 10) || 1)}
                helperText="Maximum occupants per house (students + lecturers)"
              />
            )}
            <TextField
              fullWidth
              label="Description (optional)"
              value={laneDescription}
              onChange={(e) => setLaneDescription(e.target.value)}
              multiline
              rows={2}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseLaneDialog}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleSaveLane}
            disabled={!laneName.trim() || createLaneMutation.isPending || updateLaneMutation.isPending}
          >
            {createLaneMutation.isPending || updateLaneMutation.isPending ? (
              <CircularProgress size={24} />
            ) : editingLaneId ? (
              'Update Lane'
            ) : (
              'Create Lane'
            )}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Lane Confirmation */}
      <Dialog open={deleteLaneConfirmOpen} onClose={() => setDeleteLaneConfirmOpen(false)}>
        <DialogTitle>Delete Lane</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete this lane? This action cannot be undone.
            {laneToDelete && houseList.filter((h: any) => h.laneId === laneToDelete).length > 0 && (
              <Box sx={{ mt: 1 }}>
                <Alert severity="warning">
                  This lane contains {houseList.filter((h: any) => h.laneId === laneToDelete).length} house(s).
                </Alert>
              </Box>
            )}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteLaneConfirmOpen(false)}>Cancel</Button>
          <Button variant="contained" color="error" onClick={handleDeleteLaneConfirm} disabled={deleteLaneMutation.isPending}>
            {deleteLaneMutation.isPending ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
{/* Room Add/Edit Dialog */}
      <Dialog open={roomDialogOpen} onClose={handleCloseRoomDialog} maxWidth="sm" fullWidth>
        <DialogTitle>{editingRoomId ? 'Edit House' : 'Add House'}</DialogTitle>
        <DialogContent>
          {roomError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {roomError}
            </Alert>
          )}
          <Box sx={{ pt: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              fullWidth
              label="House Number"
              value={roomNumber}
              onChange={(e) => setRoomNumber(e.target.value)}
              error={!!roomError && roomError.includes('number')}
              helperText={roomError && roomError.includes('number') ? roomError : 'e.g. 001, 101, A1'}
              autoFocus
            />
            <TextField
              fullWidth
              label="House Name (optional)"
              value={roomName}
              onChange={(e) => setRoomName(e.target.value)}
              helperText="e.g. Sunflower Hall, Block A - 3"
            />
            <TextField
              fullWidth
              label="Capacity"
              type="number"
              value={String(roomCapacity)}
              onChange={(e) => setRoomCapacity(parseInt(e.target.value, 10) || 1)}
              helperText="Maximum occupants (students + lecturers)"
            />
            {editingRoomId && (
              <FormControlLabel
                control={
                  <Switch
                    checked={roomAvailable}
                    onChange={(e) => setRoomAvailable(e.target.checked)}
                    color="success"
                  />
                }
                label={roomAvailable ? 'Available for assignment' : 'Unavailable (no new assignments)'}
              />
            )}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseRoomDialog}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleSaveRoom}
            disabled={!roomNumber.trim() || createHouseMutation.isPending || updateHouseMutation.isPending}
          >
            {createHouseMutation.isPending || updateHouseMutation.isPending ? (
              <CircularProgress size={24} />
            ) : editingRoomId ? (
              'Update House'
            ) : (
              'Add House'
            )}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Room Confirmation */}
      <Dialog open={deleteRoomConfirmOpen} onClose={() => setDeleteRoomConfirmOpen(false)}>
        <DialogTitle>Delete House</DialogTitle>
        <DialogContent>
          <Typography>Are you sure you want to delete this house? This action cannot be undone.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteRoomConfirmOpen(false)}>Cancel</Button>
          <Button variant="contained" color="error" onClick={handleDeleteRoomConfirm} disabled={deleteHouseMutation.isPending}>
            {deleteHouseMutation.isPending ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
{/* Assign Occupant Dialog */}
      <Dialog open={assignDialogOpen} onClose={() => setAssignDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Assign Occupant to House</DialogTitle>
        <DialogContent>
          {assignError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {assignError}
            </Alert>
          )}
          <Box sx={{ pt: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              select
              fullWidth
              label="Occupant Type"
              value={assignOccupantType}
              onChange={(e) => {
                setAssignOccupantType(e.target.value);
                setAssignOccupantId(null);
                setAssignOccupantLabel('');
              }}
            >
              <MenuItem value="Student">Student</MenuItem>
              <MenuItem value="Lecturer">Lecturer</MenuItem>
            </TextField>
            <TextField
              fullWidth
              label={`Search ${assignOccupantType.toLowerCase() === 'lecturer' ? 'lecturer' : 'student'} by name or number`}
              value={assignSearch}
              onChange={(e) => {
                setAssignSearch(e.target.value);
                setAssignOccupantId(null);
                setAssignOccupantLabel('');
                if (assignOccupantType === 'Student') refetchStudents(); else refetchLecturers();
              }}
            />
            <Box sx={{ maxHeight: 200, overflowY: 'auto', border: '1px solid rgba(0,0,0,0.12)', borderRadius: 1 }}>
              {(assignOccupantType === 'Student' ? (searchStudents?.items || []) : (searchLecturers?.items || [])).map((oc: any) => {
                const label = assignOccupantType === 'Student'
                  ? `${oc.firstName} ${oc.middleName ? oc.middleName + ' ' : ''}${oc.lastName} (${oc.studentNumber})`
                  : `${oc.firstName} ${oc.lastName} (${oc.employeeNumber})`;
                const isSelected = assignOccupantId === oc.id;
                return (
                  <Chip
                    key={oc.id}
                    label={label}
                    variant={isSelected ? 'outlined' : undefined}
                    color={isSelected ? 'success' : 'default'}
                    onClick={() => {
                      setAssignOccupantId(oc.id);
                      setAssignOccupantLabel(label);
                    }}
                    sx={{ m: 0.5 }}
                  />
                );
              })}
              {assignSearch.trim().length > 0 && (assignOccupantType === 'Student' ? (searchStudents?.items || []) : (searchLecturers?.items || [])).length === 0 && (
                <Typography variant="caption" color="textSecondary">
                  No matching {assignOccupantType.toLowerCase()} found.
                </Typography>
              )}
            </Box>
            {assignOccupantLabel && (
              <Typography variant="body2">Selected: {assignOccupantLabel}</Typography>
            )}
<TextField
              select
              fullWidth
              label="Lane"
              value={assignLaneId}
              onChange={(e) => {
                setAssignLaneId(e.target.value);
                setAssignHouseId('');
              }}
            >
              <MenuItem value="">-- Select lane --</MenuItem>
              {laneList.map((lane: any) => (
                <MenuItem key={lane.id} value={lane.id}>{lane.laneName}</MenuItem>
              ))}
            </TextField>
            <TextField
              select
              fullWidth
              label="House"
              value={assignHouseId}
              onChange={(e) => setAssignHouseId(e.target.value)}
            >
              <MenuItem value="">-- Select house --</MenuItem>
              {assignableHouses
                .filter((h: any) => !assignLaneId || h.laneId === assignLaneId)
                .map((h: any) => (
                  <MenuItem key={h.id} value={h.id}>
                    {h.houseNumber}{h.houseName ? ` · ${h.houseName}` : ''} ({h.occupiedCount ?? 0}/{h.capacity ?? 1} occupied)
                  </MenuItem>
                ))}
            </TextField>
            <TextField
              fullWidth
              label="Remarks (optional)"
              value={assignRemarks}
              onChange={(e) => setAssignRemarks(e.target.value)}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAssignDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleAssignSubmit}
            disabled={!assignOccupantId || !assignHouseId || assignHouseMutation.isPending}
          >
            {assignHouseMutation.isPending ? <CircularProgress size={24} /> : 'Assign'}
          </Button>
        </DialogActions>
      </Dialog>
{/* Reassign (Transfer) Dialog */}
      <Dialog open={reassignDialogOpen} onClose={() => setReassignDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Transfer Occupant to Another House</DialogTitle>
        <DialogContent>
          {reassignError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {reassignError}
            </Alert>
          )}
          <Box sx={{ pt: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              select
              fullWidth
              label="Lane"
              value={reassignLaneId}
              onChange={(e) => {
                setReassignLaneId(e.target.value);
                setReassignNewHouseId('');
              }}
            >
              <MenuItem value="">-- Select lane --</MenuItem>
              {laneList.map((lane: any) => (
                <MenuItem key={lane.id} value={lane.id}>{lane.laneName}</MenuItem>
              ))}
            </TextField>
            <TextField
              select
              fullWidth
              label="Destination House"
              value={reassignNewHouseId}
              onChange={(e) => setReassignNewHouseId(e.target.value)}
            >
              <MenuItem value="">-- Select house --</MenuItem>
              {reassignmentTargetHouses
                .filter((h: any) => !reassignLaneId || h.laneId === reassignLaneId)
                .map((h: any) => (
                  <MenuItem key={h.id} value={h.id}>
                    {h.houseNumber}{h.houseName ? ` · ${h.houseName}` : ''} ({h.occupiedCount ?? 0}/{h.capacity ?? 1} occupied)
                  </MenuItem>
                ))}
            </TextField>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setReassignDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleReassignSubmit}
            disabled={!reassignNewHouseId || reassignMutation.isPending}
          >
            {reassignMutation.isPending ? <CircularProgress size={24} /> : 'Transfer'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Vacate House Confirmation */}
      <Dialog open={vacateConfirmOpen} onClose={() => setVacateConfirmOpen(false)}>
        <DialogTitle>Vacate House</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to vacate this house? All active occupants will be checked out and the house will be marked vacant.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setVacateConfirmOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            color="error"
            onClick={() => houseToVacate && vacateHouseMutation.mutate(houseToVacate)}
            disabled={vacateHouseMutation.isPending}
          >
            {vacateHouseMutation.isPending ? 'Vacating...' : 'Vacate'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};