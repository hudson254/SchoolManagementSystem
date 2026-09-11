import React, { useState } from 'react';
import {
  Box, Paper, Typography, TextField, Button, List, ListItem,
  ListItemText, ListItemAvatar, Avatar, Divider, Alert, CircularProgress,
} from '@mui/material';
import { Search as SearchIcon, People, School, Book, Assignment } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useLocation, useNavigate } from 'react-router-dom';
import { studentService } from '../services/student.service';
import { lecturerService } from '../services/lecturer.service';
import { courseService } from '../services/course.service';
import { unitService } from '../services/unit.service';
import { useAuth } from '../hooks/useAuth';
import { canManageAcademic, hasAnyRole, LECTURER, RECEPTIONIST } from '../utils/roles';

export const Search: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuth();
  const params = new URLSearchParams(location.search ?? '');
  const [query, setQuery] = useState(params.get('q') || '');

  const canReadStudents = canManageAcademic(user?.roles) || hasAnyRole(user?.roles, RECEPTIONIST);
  const canReadLecturers = canManageAcademic(user?.roles) || hasAnyRole(user?.roles, RECEPTIONIST);
  const canReadCourses = canManageAcademic(user?.roles) || hasAnyRole(user?.roles, LECTURER);
  const canReadUnits = canManageAcademic(user?.roles) || hasAnyRole(user?.roles, LECTURER);

  const handleSearch = () => {
    if (query.trim()) navigate(`/search?q=${encodeURIComponent(query.trim())}`);
  };

  const studentsQuery = useQuery({
    queryKey: ['search-students', query],
    queryFn: () => studentService.getStudents({ searchTerm: query, pageSize: 8 }),
    enabled: canReadStudents && query.trim().length > 0,
  });
  const lecturersQuery = useQuery({
    queryKey: ['search-lecturers', query],
    queryFn: () => lecturerService.getLecturers({ searchTerm: query, pageSize: 8 }),
    enabled: canReadLecturers && query.trim().length > 0,
  });
  const coursesQuery = useQuery({
    queryKey: ['search-courses', query],
    queryFn: () => courseService.getCourses({ searchTerm: query, pageSize: 8 }),
    enabled: canReadCourses && query.trim().length > 0,
  });
  const unitsQuery = useQuery({
    queryKey: ['search-units', query],
    queryFn: () => unitService.getUnits({ searchTerm: query, pageSize: 8 }),
    enabled: canReadUnits && query.trim().length > 0,
  });

  return (
    <Box>
      <Paper sx={{ p: 3, mb: 3, borderRadius: 2 }}>
        <Typography variant="h5" fontWeight={600} sx={{ mb: 2 }}>Search</Typography>
        <TextField
          fullWidth
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onKeyPress={(e: React.KeyboardEvent) => { if (e.key === 'Enter') handleSearch(); }}
          placeholder="Search students, lecturers, courses or units..."
          InputProps={{
            startAdornment: <SearchIcon sx={{ mr: 1, color: 'text.secondary' }} />,
            endAdornment: (
              <Button size="small" onClick={handleSearch} disabled={!query.trim()}>Search</Button>
            ),
          }}
        />
      </Paper>

      {query.trim().length === 0 && <Alert severity="info">Type a search term above and press Enter.</Alert>}
      {query.trim().length > 0 && (studentsQuery.isLoading || lecturersQuery.isLoading || coursesQuery.isLoading || unitsQuery.isLoading) && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}><CircularProgress size={28} /></Box>
      )}
{query.trim().length > 0
        && !(studentsQuery.isLoading || lecturersQuery.isLoading || coursesQuery.isLoading || unitsQuery.isLoading)
        && (
          <Paper sx={{ borderRadius: 2 }}>
            {canReadStudents && (
              <>
                <Typography variant="h6" sx={{ px: 2, py: 1 }}>Students ({(studentsQuery.data?.items || []).length})</Typography>
                <List dense>
                  {(studentsQuery.data?.items || []).length === 0 && <ListItem><ListItemText primary="No students matched." /></ListItem>}
                  {(studentsQuery.data?.items || []).map((s: any) => (
                    <ListItem key={s.id} component="button" onClick={() => navigate(`/students/${s.id}`)}>
                      <ListItemAvatar><Avatar><People /></Avatar></ListItemAvatar>
                      <ListItemText primary={`${s.firstName} ${s.lastName}`.trim()} secondary={`${s.studentNumber} • ${s.email}`} />
                    </ListItem>
                  ))}
                </List>
                <Divider sx={{ my: 1 }} />
              </>
            )}

            {canReadLecturers && (
              <>
                <Typography variant="h6" sx={{ px: 2, py: 1 }}>Lecturers ({(lecturersQuery.data?.items || []).length})</Typography>
                <List dense>
                  {(lecturersQuery.data?.items || []).length === 0 && <ListItem><ListItemText primary="No lecturers matched." /></ListItem>}
                  {(lecturersQuery.data?.items || []).map((l: any) => (
                    <ListItem key={l.id} component="button" onClick={() => navigate(`/lecturers/${l.id}`)}>
                      <ListItemAvatar><Avatar><School /></Avatar></ListItemAvatar>
                      <ListItemText primary={`${l.firstName} ${l.lastName}`.trim()} secondary={`${l.employeeNumber} • ${l.email}`} />
                    </ListItem>
                  ))}
                </List>
                <Divider sx={{ my: 1 }} />
              </>
            )}

            {canReadCourses && (
              <>
                <Typography variant="h6" sx={{ px: 2, py: 1 }}>Courses ({(coursesQuery.data?.items || []).length})</Typography>
                <List dense>
                  {(coursesQuery.data?.items || []).length === 0 && <ListItem><ListItemText primary="No courses matched." /></ListItem>}
                  {(coursesQuery.data?.items || []).map((c: any) => (
                    <ListItem key={c.id} component="button" onClick={() => navigate(`/courses/${c.id}`)}>
                      <ListItemAvatar><Avatar><Book /></Avatar></ListItemAvatar>
                      <ListItemText primary={c.name} secondary={`${c.code} • ${c.duration} year(s)`} />
                    </ListItem>
                  ))}
                </List>
                <Divider sx={{ my: 1 }} />
              </>
            )}

            {canReadUnits && (
              <>
                <Typography variant="h6" sx={{ px: 2, py: 1 }}>Units ({(unitsQuery.data?.items || []).length})</Typography>
                <List dense>
                  {(unitsQuery.data?.items || []).length === 0 && <ListItem><ListItemText primary="No units matched." /></ListItem>}
                  {(unitsQuery.data?.items || []).map((u: any) => (
                    <ListItem key={u.id} component="button" onClick={() => navigate(`/units/${u.id}`)}>
                      <ListItemAvatar><Avatar><Assignment /></Avatar></ListItemAvatar>
                      <ListItemText primary={u.name} secondary={`${u.code} • ${u.credits} credits`} />
                    </ListItem>
                  ))}
                </List>
              </>
            )}
          </Paper>
        )}
    </Box>
  );
};

export default Search;