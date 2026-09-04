import React, { useState } from "react";
import { Box, Paper, Typography, TextField, Button, Grid, CircularProgress, Alert, Snackbar } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { lecturerService } from "../services/lecturer.service";

export const AddLecturerPage: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [formData, setFormData] = useState({ firstName: "", lastName: "", email: "", employeeNumber: "", phoneNumber: "", password: "", specialization: "", qualifications: "" });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => { setFormData({ ...formData, [e.target.name]: e.target.value }); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setLoading(true); setError(null);
    try { await lecturerService.createLecturer(formData); setSuccess(true); setTimeout(() => navigate("/lecturers"), 1500); }
    catch (err: any) { setError(err?.message || "Failed to create lecturer"); }
    finally { setLoading(false); }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 4, maxWidth: 700, mx: "auto" }}>
        <Typography variant="h5" fontWeight={600} sx={{ mb: 3 }}>Add New Lecturer</Typography>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        <Box component="form" onSubmit={handleSubmit}>
          <Grid container spacing={3}>
            <Grid item xs={12} sm={6}><TextField fullWidth label="First Name" name="firstName" value={formData.firstName} onChange={handleChange} required /></Grid>
            <Grid item xs={12} sm={6}><TextField fullWidth label="Last Name" name="lastName" value={formData.lastName} onChange={handleChange} required /></Grid>
            <Grid item xs={12} sm={6}><TextField fullWidth label="Email" name="email" type="email" value={formData.email} onChange={handleChange} required /></Grid>
            <Grid item xs={12} sm={6}><TextField fullWidth label="Employee Number" name="employeeNumber" value={formData.employeeNumber} onChange={handleChange} required /></Grid>
            <Grid item xs={12} sm={6}><TextField fullWidth label="Phone Number" name="phoneNumber" value={formData.phoneNumber} onChange={handleChange} /></Grid>
            <Grid item xs={12} sm={6}><TextField fullWidth label="Password" name="password" type="password" value={formData.password} onChange={handleChange} required /></Grid>
            <Grid item xs={12}><TextField fullWidth label="Specialization" name="specialization" value={formData.specialization} onChange={handleChange} /></Grid>
            <Grid item xs={12}><TextField fullWidth label="Qualifications" name="qualifications" multiline rows={3} value={formData.qualifications} onChange={handleChange} /></Grid>
          </Grid>
          <Box sx={{ mt: 4, display: "flex", gap: 2, justifyContent: "flex-end" }}>
            <Button variant="outlined" onClick={() => navigate("/lecturers")}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={loading}>{loading ? <CircularProgress size={24} /> : "Create Lecturer"}</Button>
          </Box>
        </Box>
      </Paper>
      <Snackbar open={success} message="Lecturer created successfully" autoHideDuration={3000} />
    </Box>
  );
};
