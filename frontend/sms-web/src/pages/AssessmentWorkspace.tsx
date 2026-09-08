import React, { useState } from 'react';
import { Box, Typography, Tabs, Tab, Paper, Grid, TextField, Button, MenuItem, Select, FormControl, InputLabel, Chip, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Alert, Divider } from '@mui/material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { assessmentService } from '../services/assessment.service';
import type { Assessment, StudentResult, AuditLogEntry } from '../types/assessment.types';
import { useAuth } from '../hooks/useAuth';
import { api } from '../services/api';

export const AssessmentWorkspace: React.FC = () => {
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const [tab, setTab] = useState(0);
  const [selectedUnitId, setSelectedUnitId] = useState('');

  const role = user?.roles?.[0] ?? '';
  const canEdit = ['Lecturer', 'Coordinator', 'Administrator', 'SystemAdministrator'].includes(role);
  const canAdmin = ['Coordinator', 'Administrator', 'SystemAdministrator'].includes(role);

  const { data: types } = useQuery({ queryKey: ['assessment-types'], queryFn: () => assessmentService.getTypes() });
  const { data: units } = useQuery({ queryKey: ['units-list'], queryFn: () => api.get('/units'), enabled: canEdit });
  const { data: assessments } = useQuery({ queryKey: ['assessments-by-unit', selectedUnitId], queryFn: () => assessmentService.getAssessmentsByUnit(selectedUnitId), enabled: !!selectedUnitId });
  const { data: weightValidation } = useQuery({ queryKey: ['weight-validation', selectedUnitId], queryFn: () => assessmentService.getWeightValidation(selectedUnitId), enabled: !!selectedUnitId });
  const { data: passFail } = useQuery({ queryKey: ['pass-fail', selectedUnitId], queryFn: () => assessmentService.getPassFailRates(selectedUnitId), enabled: !!selectedUnitId });
  const { data: gradeDistribution } = useQuery({ queryKey: ['grade-distribution', selectedUnitId], queryFn: () => assessmentService.getGradeDistribution(selectedUnitId), enabled: !!selectedUnitId });
  const { data: summary } = useQuery({ queryKey: ['assessment-summary', selectedUnitId], queryFn: () => assessmentService.getAssessmentSummary(selectedUnitId), enabled: !!selectedUnitId });
  const { data: pendingModeration } = useQuery({ queryKey: ['pending-moderation'], queryFn: () => assessmentService.getPendingModeration(), enabled: canAdmin });
  const { data: auditLog } = useQuery({ queryKey: ['assessment-audit-log'], queryFn: () => assessmentService.getAssessmentAuditLog(), enabled: canAdmin });

  const createAssess = useMutation({
    mutationFn: (vars: any) => assessmentService.createAssessment(vars),
    onSuccess: () => { enqueueSnackbar('Assessment created', { variant: 'success' }); queryClient.invalidateQueries({ queryKey: ['assessments-by-unit'] }); queryClient.invalidateQueries({ queryKey: ['weight-validation'] }); },
    onError: (err: any) => enqueueSnackbar(err?.message || 'Failed to create assessment', { variant: 'error' }),
  });
  const submitForReview = useMutation({ mutationFn: () => assessmentService.submitForReview(selectedUnitId, ''), onSuccess: () => enqueueSnackbar('Submitted for review', { variant: 'success' }), onError: (err: any) => enqueueSnackbar(err?.message || 'Submit failed', { variant: 'error' }) });
  const approveResults = useMutation({ mutationFn: () => assessmentService.approveResults(selectedUnitId, ''), onSuccess: () => enqueueSnackbar('Results approved', { variant: 'success' }), onError: (err: any) => enqueueSnackbar(err?.message || 'Approval failed', { variant: 'error' }) });
  const publishResults = useMutation({ mutationFn: () => assessmentService.publishResults(selectedUnitId, ''), onSuccess: () => enqueueSnackbar('Results published', { variant: 'success' }), onError: (err: any) => enqueueSnackbar(err?.message || 'Publish failed', { variant: 'error' }) });

  if (!canEdit) {
    return (<Box><Typography variant="h4" gutterBottom fontWeight={600}>Assessments</Typography><Alert severity="info">You do not have assessment management permissions in this tenant.</Alert></Box>);
  }
  return (
    <Box>
      <Typography variant="h4" gutterBottom fontWeight={600}>Assessments</Typography>
      <Tabs value={tab} onChange={(_e, v) => setTab(v)} sx={{ mb: 3 }}>
        <Tab label="Assessments & Weights" />
        <Tab label="Workflow" />
        <Tab label="Reports" />
        <Tab label="Moderation" />
        <Tab label="Audit Log" />
      </Tabs>

      {tab === 0 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={5}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>Select Unit</Typography>
              <FormControl fullWidth size="small" sx={{ mb: 2 }}>
                <InputLabel>Unit</InputLabel>
                <Select value={selectedUnitId} label="Unit" onChange={(e) => setSelectedUnitId(e.target.value)}>
                  {(units || []).map((u: any) => (<MenuItem key={u.id} value={u.id}>{u.code} - {u.name}</MenuItem>))}
                </Select>
              </FormControl>
              <Divider sx={{ my: 2 }} />
              <Typography variant="h6" gutterBottom>Create Assessment</Typography>
              <TextField label="Name" size="small" fullWidth sx={{ mb: 2 }} id="new-assessment-name" required />
              <FormControl fullWidth size="small" sx={{ mb: 2 }}>
                <InputLabel>Type</InputLabel>
                <Select label="Type" fullWidth defaultValue="">
                  {(types || []).map((t: any) => (<MenuItem key={t.id} value={t.id}>{t.name}</MenuItem>))}
                </Select>
              </FormControl>
              <Grid container spacing={2}>
                <Grid item xs={6}><TextField label="Max Score" type="number" size="small" fullWidth defaultValue={100} /></Grid>
                <Grid item xs={6}><TextField label="Weight %" type="number" size="small" fullWidth required /></Grid>
              </Grid>
              <Button variant="contained" sx={{ mt: 2 }} onClick={() => {
                const name = (document.getElementById('new-assessment-name') as HTMLInputElement)?.value;
                if (!selectedUnitId || !name) { enqueueSnackbar('Unit and assessment name are required', { variant: 'warning' }); return; }
                createAssess.mutate({ name, unitId: selectedUnitId, assessmentTypeId: '', weight: 0, maxMarks: 100 });
              }}>Create</Button>
            </Paper>
          </Grid>
          <Grid item xs={12} md={7}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>Configured Assessments</Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead><TableRow><TableCell>Name</TableCell><TableCell>Max</TableCell><TableCell>Weight</TableCell><TableCell>Lock</TableCell></TableRow></TableHead>
                  <TableBody>
                    {(assessments || []).map((a: Assessment) => (
                      <TableRow key={a.id} hover>
                        <TableCell>{a.name}</TableCell><TableCell>{a.maxScore}</TableCell><TableCell>{a.weight}%</TableCell>
                        <TableCell><Chip label={a.isWeightLocked ? 'Locked' : 'Configurable'} size="small" color={a.isWeightLocked ? 'warning' : 'success'} /></TableCell>
                      </TableRow>
                    ))}
                    {!assessments?.length && (<TableRow><TableCell colSpan={4} align="center">No assessments configured for this unit yet.</TableCell></TableRow>)}
                  </TableBody>
                </Table>
              </TableContainer>
              {weightValidation && (<Alert severity={weightValidation.isValid ? 'success' : 'error'} sx={{ mt: 2 }}>Total weight: {weightValidation.totalWeight}% - {weightValidation.isValid ? 'valid' : 'must total 100%'}</Alert>)}
            </Paper>
          </Grid>
        </Grid>
      )}

      {tab === 1 && (
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>Publication Workflow</Typography>
          <Typography variant="body2" color="textSecondary" gutterBottom>Draft â†’ Pending Review â†’ Approved â†’ Published. Students only see published results.</Typography>
          <Box sx={{ display: 'flex', gap: 2, mt: 2, flexWrap: 'wrap' }}>
            <Button variant="contained" disabled={!selectedUnitId} onClick={() => submitForReview.mutate()}>Submit for Review</Button>
            <Button variant="contained" color="info" disabled={!canAdmin} onClick={() => approveResults.mutate()}>Approve</Button>
            <Button variant="contained" color="success" disabled={!canAdmin} onClick={() => publishResults.mutate()}>Publish</Button>
          </Box>
          <Typography variant="caption" display="block" sx={{ mt: 2 }}>Mark entry and bulk import are performed per assessment through the backend assessment API, which validates ranges, prevents duplicates and calculates weighted contributions on the server.</Typography>
        </Paper>
      )}
      {tab === 2 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={4}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>Pass / Fail</Typography>
              {(passFail as any) && (<>
                <Typography variant="h4">{(passFail as any).passRatePercentage ?? 0}%</Typography>
                <Typography variant="body2" color="textSecondary">Passed: {(passFail as any).passed ?? 0} | Failed: {(passFail as any).failed ?? 0}</Typography>
              </>)}
            </Paper>
          </Grid>
          <Grid item xs={12} md={4}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>Grade Distribution</Typography>
              {(gradeDistribution as any)?.gradeDistribution && Object.entries((gradeDistribution as any).gradeDistribution).map(([k, v]) => (
                <Box key={k} sx={{ display: 'flex', justifyContent: 'space-between', py: 0.5 }}>
                  <Typography variant="body2">{k}</Typography><Chip label={String(v)} size="small" />
                </Box>
              ))}
            </Paper>
          </Grid>
          <Grid item xs={12} md={4}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>Assessment Summary</Typography>
              {(summary as any)?.assessments?.map((a: any, i: number) => (
                <Typography key={i} variant="body2" sx={{ py: 0.5 }}>{a.assessmentName}: avg {a.averageScore}% (graded {a.gradedStudents}/{a.totalStudents})</Typography>
              ))}
              {(summary as any)?.overallAverage !== undefined && (<Chip label={'Overall avg ' + (summary as any).overallAverage + '%'} color="secondary" sx={{ mt: 1 }} />)}
            </Paper>
          </Grid>
        </Grid>
      )}

      {tab === 3 && (
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>Pending Moderation</Typography>
          {!pendingModeration?.length ? (<Typography variant="body2" color="textSecondary">No results awaiting moderation.</Typography>) : (
            <TableContainer><Table size="small">
              <TableHead><TableRow><TableCell>Student</TableCell><TableCell>Unit</TableCell><TableCell>Final %</TableCell><TableCell>Grade</TableCell><TableCell>Status</TableCell></TableRow></TableHead>
              <TableBody>{(pendingModeration || []).map((r: StudentResult) => (
                <TableRow key={r.studentId + '-' + r.unitId} hover>
                  <TableCell>{r.studentName}</TableCell><TableCell>{r.unitName}</TableCell><TableCell>{r.finalScore}</TableCell><TableCell>{r.finalGrade}</TableCell><TableCell>{r.publicationStatus}</TableCell>
                </TableRow>))}
              </TableBody>
            </Table></TableContainer>
          )}
        </Paper>
      )}

      {tab === 4 && (
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>Audit Log</Typography>
          <TableContainer><Table size="small">
            <TableHead><TableRow><TableCell>Timestamp</TableCell><TableCell>Action</TableCell><TableCell>Role / User</TableCell><TableCell>Entity</TableCell><TableCell>Details</TableCell></TableRow></TableHead>
            <TableBody>
              {(auditLog || []).slice(0, 100).map((e: AuditLogEntry) => (
                <TableRow key={e.id} hover>
                  <TableCell>{new Date(e.timestamp).toLocaleString()}</TableCell><TableCell>{e.action}</TableCell>
                  <TableCell>{e.userRole} / {e.userId?.slice(0, 8)}</TableCell><TableCell>{e.entityName}</TableCell><TableCell>{e.newValue || e.reason || ''}</TableCell>
                </TableRow>))}
              {!auditLog?.length && (<TableRow><TableCell colSpan={5} align="center">No audit records yet.</TableCell></TableRow>)}
            </TableBody>
          </Table></TableContainer>
        </Paper>
      )}
    </Box>
  );
};
