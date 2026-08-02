import React, { useState, useRef } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  IconButton,
  Chip,
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
  Alert,
  Snackbar,
  Tabs,
  Tab,
} from '@mui/material';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  Event as EventIcon,
  Assignment as AssignmentIcon,
  School as SchoolIcon,
  CalendarToday as CalendarIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { calendarService } from '../services/calendar.service';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

// Import FullCalendar
import FullCalendar from '@fullcalendar/react';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';

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
      id={`calendar-tabpanel-${index}`}
      aria-labelledby={`calendar-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
};

const a11yProps = (index: number) => ({
  id: `calendar-tab-${index}`,
  'aria-controls': `calendar-tabpanel-${index}`,
});

export const Calendar: React.FC = () => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const calendarRef = useRef<any>(null);
  const [tabValue, setTabValue] = useState(0);
  const [eventDialogOpen, setEventDialogOpen] = useState(false);
  const [newEvent, setNewEvent] = useState({
    title: '',
    description: '',
    startDate: '',
    endDate: '',
    location: '',
    eventType: 'other',
  });
  const [success, setSuccess] = useState(false);

  const { data: events, isLoading, refetch } = useQuery({
    queryKey: ['calendar-events'],
    queryFn: () => calendarService.getEvents(),
  });

  const createMutation = useMutation({
    mutationFn: (data: any) => calendarService.createEvent(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['calendar-events'] });
      setEventDialogOpen(false);
      setSuccess(true);
      resetForm();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => calendarService.deleteEvent(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['calendar-events'] });
    },
  });

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleDateSelect = (selectInfo: any) => {
    setNewEvent({
      ...newEvent,
      startDate: selectInfo.startStr,
      endDate: selectInfo.endStr || selectInfo.startStr,
    });
    setEventDialogOpen(true);
  };

  const handleEventClick = (_clickInfo: any) => {
    // Event details could be opened in a dialog here in the future.
  };

  const handleCreateEvent = () => {
    createMutation.mutate(newEvent);
  };

  const handleDeleteEvent = (id: string) => {
    if (window.confirm('Are you sure you want to delete this event?')) {
      deleteMutation.mutate(id);
    }
  };

  const resetForm = () => {
    setNewEvent({
      title: '',
      description: '',
      startDate: '',
      endDate: '',
      location: '',
      eventType: 'other',
    });
  };

  const getEventColor = (type: string) => {
    switch (type) {
      case 'lecture':
        return '#576426';
      case 'assignment':
        return '#f44336';
      case 'exam':
        return '#ff9800';
      case 'holiday':
        return '#2196f3';
      case 'event':
        return '#9c27b0';
      default:
        return '#576426';
    }
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  const calendarEvents = events?.map((event: any) => ({
    id: event.id,
    title: event.title,
    start: event.startDate,
    end: event.endDate,
    extendedProps: {
      description: event.description,
      location: event.location,
      eventType: event.eventType,
    },
    backgroundColor: getEventColor(event.eventType),
    borderColor: getEventColor(event.eventType),
  })) || [];

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>
          Calendar
        </Typography>
        <Box>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setEventDialogOpen(true)}
            sx={{ mr: 1 }}
          >
            Add Event
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

      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="calendar tabs"
          sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}
        >
          <Tab label="Calendar View" {...a11yProps(0)} />
          <Tab label="Upcoming Events" {...a11yProps(1)} />
          <Tab label="Assignments" {...a11yProps(2)} />
        </Tabs>
      </Paper>

      <TabPanel value={tabValue} index={0}>
        <Paper sx={{ p: 2 }}>
          <FullCalendar
            ref={calendarRef}
            plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
            headerToolbar={{
              left: 'prev,next today',
              center: 'title',
              right: 'dayGridMonth,timeGridWeek,timeGridDay',
            }}
            initialView="dayGridMonth"
            editable={true}
            selectable={true}
            selectMirror={true}
            dayMaxEvents={true}
            weekends={true}
            events={calendarEvents}
            select={handleDateSelect}
            eventClick={handleEventClick}
            height="auto"
            eventBackgroundColor="#576426"
            eventTextColor="#ffffff"
          />
        </Paper>
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Upcoming Events
        </Typography>
        <Grid container spacing={2}>
          {events?.filter((e: any) => new Date(e.startDate) >= new Date())
            .slice(0, 10)
            .map((event: any) => (
              <Grid item xs={12} key={event.id}>
                <Paper sx={{ p: 2, display: 'flex', alignItems: 'center', gap: 2 }}>
                  <Box
                    sx={{
                      width: 4,
                      height: 40,
                      bgcolor: getEventColor(event.eventType),
                      borderRadius: 2,
                    }}
                  />
                  <Box sx={{ flex: 1 }}>
                    <Typography variant="body2" fontWeight={500}>
                      {event.title}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      {new Date(event.startDate).toLocaleDateString()} - {event.location || 'No location'}
                    </Typography>
                  </Box>
                  <Chip
                    label={event.eventType}
                    size="small"
                    color="primary"
                  />
                  {(user?.roles?.includes('Administrator') || user?.roles?.includes('Moderator')) && (
                    <IconButton
                      size="small"
                      color="error"
                      onClick={() => handleDeleteEvent(event.id)}
                    >
                      <DeleteIcon />
                    </IconButton>
                  )}
                </Paper>
              </Grid>
            ))}
          {(!events || events.length === 0) && (
            <Grid item xs={12}>
              <Alert severity="info">No upcoming events found.</Alert>
            </Grid>
          )}
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Assignment Deadlines
        </Typography>
        <Grid container spacing={2}>
          {events?.filter((e: any) => e.eventType === 'assignment' && new Date(e.startDate) >= new Date())
            .slice(0, 10)
            .map((event: any) => (
              <Grid item xs={12} key={event.id}>
                <Paper sx={{ p: 2, display: 'flex', alignItems: 'center', gap: 2, borderLeft: 4, borderColor: '#f44336' }}>
                  <Box sx={{ flex: 1 }}>
                    <Typography variant="body2" fontWeight={500}>
                      {event.title}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      Due: {new Date(event.startDate).toLocaleDateString()} - {event.description}
                    </Typography>
                  </Box>
                  <Chip
                    label={new Date(event.startDate) > new Date() ? 'Upcoming' : 'Past Due'}
                    color={new Date(event.startDate) > new Date() ? 'warning' : 'error'}
                    size="small"
                  />
                </Paper>
              </Grid>
            ))}
          {(!events || !events.some((e: any) => e.eventType === 'assignment')) && (
            <Grid item xs={12}>
              <Alert severity="info">No assignments found.</Alert>
            </Grid>
          )}
        </Grid>
      </TabPanel>

      {/* Create Event Dialog */}
      <Dialog open={eventDialogOpen} onClose={() => setEventDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Event</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              label="Title"
              value={newEvent.title}
              onChange={(e) => setNewEvent({ ...newEvent, title: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={newEvent.description}
              onChange={(e) => setNewEvent({ ...newEvent, description: e.target.value })}
              fullWidth
              multiline
              rows={2}
            />
            <Grid container spacing={2}>
              <Grid item xs={6}>
                <TextField
                  label="Start Date"
                  type="datetime-local"
                  value={newEvent.startDate}
                  onChange={(e) => setNewEvent({ ...newEvent, startDate: e.target.value })}
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                  required
                />
              </Grid>
              <Grid item xs={6}>
                <TextField
                  label="End Date"
                  type="datetime-local"
                  value={newEvent.endDate}
                  onChange={(e) => setNewEvent({ ...newEvent, endDate: e.target.value })}
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                  required
                />
              </Grid>
            </Grid>
            <TextField
              label="Location"
              value={newEvent.location}
              onChange={(e) => setNewEvent({ ...newEvent, location: e.target.value })}
              fullWidth
            />
            <FormControl fullWidth>
              <InputLabel>Event Type</InputLabel>
              <Select
                value={newEvent.eventType}
                onChange={(e) => setNewEvent({ ...newEvent, eventType: e.target.value })}
                label="Event Type"
              >
                <MenuItem value="lecture">Lecture</MenuItem>
                <MenuItem value="assignment">Assignment</MenuItem>
                <MenuItem value="exam">Exam</MenuItem>
                <MenuItem value="holiday">Holiday</MenuItem>
                <MenuItem value="event">Event</MenuItem>
                <MenuItem value="other">Other</MenuItem>
              </Select>
            </FormControl>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEventDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleCreateEvent}
            disabled={!newEvent.title || !newEvent.startDate || createMutation.isPending}
          >
            {createMutation.isPending ? 'Creating...' : 'Create'}
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={success}
        autoHideDuration={3000}
        onClose={() => setSuccess(false)}
        message="Event created successfully!"
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      />
    </Box>
  );
};
