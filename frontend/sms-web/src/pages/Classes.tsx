import React, { useState } from "react";
import { Box, Paper, Typography, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TablePagination, Chip, Alert, LinearProgress, TextField, Button } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { timetableService } from "../services/timetable.service";
import { LoadingSpinner } from "../components/Common/LoadingSpinner";

export const Classes: React.FC = () => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);

  const { data, isLoading, isError } = useQuery({
    queryKey: ["classes-timetable", page, rowsPerPage],
    queryFn: () => timetableService.getTimetables({ page: page + 1, pageSize: rowsPerPage }),
  });

  if (isLoading) return <LoadingSpinner />;

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">Failed to load classes. Please try again.</Alert>
      </Box>
    );
  }

  const items = data?.items || [];
  const totalCount = data?.totalCount || 0;

  const getDayColor = (day: string) => {
    const colors: Record<string, string> = { Monday: "#1976d2", Tuesday: "#388e3c", Wednesday: "#f57c00", Thursday: "#7b1fa2", Friday: "#d32f2f", Saturday: "#00796b", Sunday: "#616161" };
    return colors[day] || "#616161";
  };

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>Classes</Typography>
      </Box>
      <Paper>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Class Name</TableCell>
                <TableCell>Unit</TableCell>
                <TableCell>Lecturer</TableCell>
                <TableCell>Day</TableCell>
                <TableCell>Time</TableCell>
                <TableCell>Venue</TableCell>
                <TableCell>Semester</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {items.map((entry: any) => (
                <TableRow key={entry.id} hover>
                  <TableCell>{entry.className || entry.unitName}</TableCell>
                  <TableCell>{entry.unitName} ({entry.unitCode})</TableCell>
                  <TableCell>{entry.lecturerName}</TableCell>
                  <TableCell><Chip label={entry.dayOfWeek} size="small" sx={{ bgcolor: getDayColor(entry.dayOfWeek), color: "white" }} /></TableCell>
                  <TableCell>{entry.startTime} - {entry.endTime}</TableCell>
                  <TableCell>{entry.venue || "N/A"}</TableCell>
                  <TableCell>{entry.semesterName}</TableCell>
                </TableRow>
              ))}
              {items.length === 0 && (
                <TableRow><TableCell colSpan={7} align="center">No classes found</TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination component="div" count={totalCount} page={page} onPageChange={(e, p) => setPage(p)} rowsPerPage={rowsPerPage} onRowsPerPageChange={(e) => { setRowsPerPage(parseInt(e.target.value, 10)); setPage(0); }} rowsPerPageOptions={[5, 10, 25]} />
      </Paper>
    </Box>
  );
};
