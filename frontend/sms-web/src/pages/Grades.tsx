import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
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
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControl,
  InputLabel,
  Select,
  Grid,
  Alert,
  IconButton,
  LinearProgress,
} from '@mui/material';
import {
  Search as SearchIcon,
  Refresh as RefreshIcon,
  Download as DownloadIcon,
  Print as PrintIcon,
  Visibility as ViewIcon,
  Edit as EditIcon,
  CheckCircle as CheckCircleIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { gradeService } from '../services/grade.service';
import { useAuth } from '../hooks/useAuth';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

export const Grades: React.FC = () => {
  const { user } = useAuth();

  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [orderBy, setOrderBy] = useState('createdDate');
  const [orderDirection, setOrderDirection] = useState<'asc' | 'desc'>('desc');
  const [filterUnit, setFilterUnit] = useState<string>('');
  const [filterSemester, setFilterSemester] = useState<string>('');
  const [filterStudent, setFilterStudent] = useState<string>('');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['grades', page, rowsPerPage, searchTerm, orderBy, orderDirection, filterUnit, filterSemester, filterStudent],
    queryFn: () =>
      gradeService.getGrades({
        page: page + 1,
        pageSize: rowsPerPage,
        searchTerm: searchTerm || undefined,
        sortBy: orderBy,
        sortDescending: orderDirection === 'desc',
        unitId: filterUnit || undefined,
        semesterId: filterSemester || undefined,
        studentId: filterStudent || undefined,
      }),
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

  const getGradeColor = (grade: string) => {
    if (!grade) return 'default';
    const gradeMap: Record<string, 'success' | 'warning' | 'error' | 'info'> = {
      'A': 'success',
      'A-': 'success',
      'B+': 'success',
      'B': 'success',
      'B-': 'info',
      'C+': 'info',
      'C': 'info',
      'C-': 'warning',
      'D': 'warning',
      'F': 'error',
    };
    return gradeMap[grade] || 'default';
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load grades. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const grades = data?.items || [];
  const totalCount = data?.totalCount || 0;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>
          Grades
        </Typography>
        <Box>
          <Tooltip title="Export to Excel">
            <Button
              variant="outlined"
              startIcon={<DownloadIcon />}
              sx={{ mr: 1 }}
            >
              Export
            </Button>
          </Tooltip>
          <Tooltip title="Print">
            <IconButton>
              <PrintIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Refresh">
            <IconButton onClick={() => refetch()}>
              <RefreshIcon />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      <Paper sx={{ p: 2, mb: 3 }}>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} sm={3}>
            <TextField
              fullWidth
              size="small"
              placeholder="Search by student or unit..."
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
              <InputLabel>Unit</InputLabel>
              <Select
                value={filterUnit}
                onChange={(e) => setFilterUnit(e.target.value)}
                label="Unit"
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="csc101">CSC101</MenuItem>
                <MenuItem value="csc201">CSC201</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Semester</InputLabel>
              <Select
                value={filterSemester}
                onChange={(e) => setFilterSemester(e.target.value)}
                label="Semester"
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="fall2024">Fall 2024</MenuItem>
                <MenuItem value="spring2025">Spring 2025</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={3}>
            <Button
              fullWidth
              variant="outlined"
              onClick={() => {
                setFilterUnit('');
                setFilterSemester('');
                setFilterStudent('');
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
                <TableCell>Student</TableCell>
                <TableCell>Student Number</TableCell>
                <TableCell>Unit</TableCell>
                <TableCell>Credits</TableCell>
                <TableCell>Semester</TableCell>
                <TableCell>Score</TableCell>
                <TableCell>Grade</TableCell>
                <TableCell>Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {grades.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} align="center" sx={{ py: 4 }}>
                    <Typography variant="body1" color="textSecondary">
                      No grades found
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                grades.map((grade: any) => (
                  <TableRow key={grade.id} hover>
                    <TableCell>{grade.studentName}</TableCell>
                    <TableCell>{grade.studentNumber}</TableCell>
                    <TableCell>{grade.unitName}</TableCell>
                    <TableCell>{grade.credits}</TableCell>
                    <TableCell>{grade.semesterName}</TableCell>
                    <TableCell>{grade.score !== null && grade.score !== undefined ? grade.score : 'N/A'}</TableCell>
                    <TableCell>
                      {grade.gradeValue ? (
                        <Chip
                          label={grade.gradeValue}
                          color={getGradeColor(grade.gradeValue)}
                          size="small"
                        />
                      ) : (
                        <Typography variant="caption" color="textSecondary">
                          Not graded
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={grade.isPublished ? 'Published' : 'Draft'}
                        color={grade.isPublished ? 'success' : 'warning'}
                        size="small"
                      />
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
    </Box>
  );
};