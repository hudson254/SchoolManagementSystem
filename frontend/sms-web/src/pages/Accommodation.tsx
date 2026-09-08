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
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Home as HomeIcon,
  Refresh as RefreshIcon,
  Hotel as HotelIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { accommodationService } from '../services/accommodation.service';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

export const Accommodation: React.FC = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  // Lane state
  const [laneDialogOpen, setLaneDialogOpen] = useState(false);
  const [editingLaneId, setEditingLaneId] = useState<string | null>(null);
  const [laneName, setLaneName] = useState('');
  const [laneDescription, setLaneDescription] = useState('');
  const [laneError, setLaneError] = useState('');
  const [deleteLaneConfirmOpen, setDeleteLaneConfirmOpen] = useState(false);
  const [laneToDelete, setLaneToDelete] = useState<string | null>(null);

  // House/Room state
  const [roomDialogOpen, setRoomDialogOpen] = useState(false);
  const [selectedLaneId, setSelectedLaneId] = useState<string | null>(null);
  const [editingRoomId, setEditingRoomId] = useState<string | null>(null);
  const [roomNumber, setRoomNumber] = useState('');
  const [roomError, setRoomError] = useState('');
  const [deleteRoomConfirmOpen, setDeleteRoomConfirmOpen] = useState(false);
  const [roomToDelete, setRoomToDelete] = useState<string | null>(null);

  // Success/Error messages
  const [successMessage, setSuccessMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');

  // Fetch lanes
  const { data: lanes, isLoading: lanesLoading, isError: lanesError, refetch: refetchLanes } = useQuery({
    queryKey: ['lanes'],
    queryFn: () => accommodationService.getLanes(),
  });

  // Fetch all houses/rooms
  const { data: houses, isLoading: housesLoading } = useQuery({
    queryKey: ['houses'],
    queryFn: () => accommodationService.getHouses(),
  });

  // Create Lane mutation
  const createLaneMutation = useMutation({
    mutationFn: (data: { laneName: string; description?: string }) =>
      accommodationService.createLane({ ...data, numberOfHouses: 0, startingHouseNumber: 1 }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lanes'] });
      setSuccessMessage('Lane created successfully');
      handleCloseLaneDialog();
    },
    onError: (err: any) => {
      setLaneError(err?.response?.data?.message || 'Failed to create lane');
    },
  });

  // Update Lane mutation
  const updateLaneMutation = useMutation({
    mutationFn: (data: { id: string; laneName: string; description?: string }) =>
      accommodationService.updateLane(data.id, { laneName: data.laneName, description: data.description, isActive: true }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lanes'] });
      setSuccessMessage('Lane updated successfully');
      handleCloseLaneDialog();
    },
    onError: (err: any) => {
      setLaneError(err?.response?.data?.message || 'Failed to update lane');
    },
  });

  // Delete Lane mutation
  const deleteLaneMutation = useMutation({
    mutationFn: (id: string) => accommodationService.deleteLane(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lanes'] });
      queryClient.invalidateQueries({ queryKey: ['houses'] });
      setSuccessMessage('Lane deleted successfully');
      setDeleteLaneConfirmOpen(false);
      setLaneToDelete(null);
    },
    onError: (err: any) => {
      setErrorMessage(err?.response?.data?.message || 'Failed to delete lane');
      setDeleteLaneConfirmOpen(false);
      setLaneToDelete(null);
    },
  });

  // Create House/Room mutation
  const createHouseMutation = useMutation({
    mutationFn: (data: { laneId: string; houseNumber: string }) =>
      accommodationService.createHouses({ laneId: data.laneId, numberOfHouses: 1, startingHouseNumber: parseInt(data.houseNumber) || 1 }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['houses'] });
      setSuccessMessage('Room added successfully');
      handleCloseRoomDialog();
    },
    onError: (err: any) => {
      setRoomError(err?.response?.data?.message || 'Failed to add room');
    },
  });

  // Update House/Room mutation
  const updateHouseMutation = useMutation({
    mutationFn: (data: { id: string; houseNumber: string }) =>
      accommodationService.updateHouse(data.id, { houseNumber: data.houseNumber }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['houses'] });
      setSuccessMessage('Room updated successfully');
      handleCloseRoomDialog();
    },
    onError: (err: any) => {
      setRoomError(err?.response?.data?.message || 'Failed to update room');
    },
  });

  // Delete House/Room mutation
  const deleteHouseMutation = useMutation({
    mutationFn: (id: string) => accommodationService.deleteHouse(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['houses'] });
      setSuccessMessage('Room deleted successfully');
      setDeleteRoomConfirmOpen(false);
      setRoomToDelete(null);
    },
    onError: (err: any) => {
      setErrorMessage(err?.response?.data?.message || 'Failed to delete room');
      setDeleteRoomConfirmOpen(false);
      setRoomToDelete(null);
    },
  });

  // Lane dialog handlers
  const handleOpenAddLane = () => {
    setEditingLaneId(null);
    setLaneName('');
    setLaneDescription('');
    setLaneError('');
    setLaneDialogOpen(true);
  };

  const handleOpenEditLane = (lane: any) => {
    setEditingLaneId(lane.id);
    setLaneName(lane.laneName);
    setLaneDescription(lane.description || '');
    setLaneError('');
    setLaneDialogOpen(true);
  };

  const handleCloseLaneDialog = () => {
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
      createLaneMutation.mutate({ laneName: laneName.trim(), description: laneDescription.trim() || undefined });
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

  // Room dialog handlers
  const handleOpenAddRoom = (laneId: string) => {
    setSelectedLaneId(laneId);
    setEditingRoomId(null);
    setRoomNumber('');
    setRoomError('');
    setRoomDialogOpen(true);
  };

  const handleOpenEditRoom = (house: any) => {
    setSelectedLaneId(house.laneId);
    setEditingRoomId(house.id);
    setRoomNumber(house.houseNumber);
    setRoomError('');
    setRoomDialogOpen(true);
  };

  const handleCloseRoomDialog = () => {
    setRoomDialogOpen(false);
    setSelectedLaneId(null);
    setEditingRoomId(null);
    setRoomNumber('');
    setRoomError('');
  };

  const handleSaveRoom = () => {
    if (!roomNumber.trim()) {
      setRoomError('Room number is required');
      return;
    }
    setRoomError('');
    if (editingRoomId) {
      updateHouseMutation.mutate({ id: editingRoomId, houseNumber: roomNumber.trim() });
    } else if (selectedLaneId) {
      createHouseMutation.mutate({ laneId: selectedLaneId, houseNumber: roomNumber.trim() });
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

  const isAdmin = user?.roles?.includes('SystemAdministrator') || user?.roles?.includes('Administrator');

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

  return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>
          Accommodation
        </Typography>
        <Box>
          {isAdmin && (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={handleOpenAddLane}
              sx={{ mr: 1 }}
            >
              Add Lane
            </Button>
          )}
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={() => {
              refetchLanes();
              queryClient.invalidateQueries({ queryKey: ['houses'] });
            }}
          >
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
          {isAdmin && (
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
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <HomeIcon sx={{ color: '#576426' }} />
                        <Typography variant="h6" fontWeight={600}>
                          {lane.laneName}
                        </Typography>
                        <Chip
                          label={lane.isActive ? 'Active' : 'Inactive'}
                          color={lane.isActive ? 'success' : 'default'}
                          size="small"
                        />
                        <Chip
                          label={`${laneRooms.length} room`}
                          variant="outlined"
                          size="small"
                        />
                      </Box>
                      {isAdmin && (
                        <Box>
                          <IconButton size="small" onClick={() => handleOpenEditLane(lane)} title="Edit Lane">
                            <EditIcon fontSize="small" />
                          </IconButton>
                          <IconButton size="small" onClick={() => handleDeleteLaneClick(lane.id)} title="Delete Lane" color="error">
                            <DeleteIcon fontSize="small" />
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

                    {/* Rooms in this lane */}
                    <Typography variant="subtitle2" fontWeight={600} gutterBottom>
                      Rooms
                    </Typography>
                    {laneRooms.length === 0 ? (
                      <Typography variant="body2" color="textSecondary" sx={{ ml: 2, mb: 1 }}>
                        No rooms added yet.
                      </Typography>
                    ) : (
                      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 1 }}>
                        {laneRooms.map((house: any) => (
                          <Chip
                            key={house.id}
                            label={house.houseNumber}
                            variant="outlined"
                            color={house.isOccupied ? 'error' : 'success'}
                            onDelete={isAdmin ? () => handleDeleteRoomClick(house.id) : undefined}
                            onClick={isAdmin ? () => handleOpenEditRoom(house) : undefined}
                            title={`Room ${house.houseNumber} - ${house.isOccupied ? 'Occupied' : house.status || 'Vacant'}`}
                          />
                        ))}
                      </Box>
                    )}

                    {isAdmin && (
                      <Button
                        size="small"
                        startIcon={<AddIcon />}
                        onClick={() => handleOpenAddRoom(lane.id)}
                      >
                        Add Room
                      </Button>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            );
          })}
        </Grid>
      )}

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
                  This lane contains {houseList.filter((h: any) => h.laneId === laneToDelete).length} room(s).
                  Deleting the lane will also delete all associated rooms.
                </Alert>
              </Box>
            )}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteLaneConfirmOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            color="error"
            onClick={handleDeleteLaneConfirm}
            disabled={deleteLaneMutation.isPending}
          >
            {deleteLaneMutation.isPending ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Room Add/Edit Dialog */}
      <Dialog open={roomDialogOpen} onClose={handleCloseRoomDialog} maxWidth="sm" fullWidth>
        <DialogTitle>{editingRoomId ? 'Edit Room' : 'Add Room'}</DialogTitle>
        <DialogContent>
          {roomError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {roomError}
            </Alert>
          )}
          <Box sx={{ pt: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              fullWidth
              label="Room Number"
              value={roomNumber}
              onChange={(e) => setRoomNumber(e.target.value)}
              error={!!roomError && roomError.includes('number')}
              helperText={roomError && roomError.includes('number') ? roomError : 'e.g. 001, 101, A1, Room 1'}
              autoFocus
            />
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
              'Update Room'
            ) : (
              'Add Room'
            )}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Room Confirmation */}
      <Dialog open={deleteRoomConfirmOpen} onClose={() => setDeleteRoomConfirmOpen(false)}>
        <DialogTitle>Delete Room</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete this room? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteRoomConfirmOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            color="error"
            onClick={handleDeleteRoomConfirm}
            disabled={deleteHouseMutation.isPending}
          >
            {deleteHouseMutation.isPending ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
