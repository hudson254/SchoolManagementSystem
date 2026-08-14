import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  CircularProgress,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Tooltip,
  MenuItem,
  Autocomplete,
} from '@mui/material';
import {
  Download,
  Refresh,
  Add,
  Block,
  FilePresent,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { certificateService } from '../services/certificate.service';
import { courseOfferingService } from '../services/course-offering.service';
import {
  Certificate,
  CertificateStatus,
  BulkGenerationResult,
} from '../types/certificate.types';

export const Certificates: React.FC = () => {
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const [selectedOffering, setSelectedOffering] = useState<string>('');
  const [revokeDialog, setRevokeDialog] = useState<{ cert: Certificate; open: boolean }>({ cert: null as any, open: false });
  const [revokeReason, setRevokeReason] = useState('');
  const [bulkResult, setBulkResult] = useState<BulkGenerationResult | null>(null);

  // Load course offerings for the bulk generation dropdown
  const { data: offeringsData } = useQuery({
    queryKey: ['course-offerings', 'completed'],
    queryFn: () => courseOfferingService.getCourseOfferings({ includeInactive: true }),
  });

  // List certificates via the paginated search endpoint
  const { data: certificates, isLoading } = useQuery({
    queryKey: ['certificates'],
    queryFn: async () => {
      const result = await certificateService.search({ pageNumber: 1, pageSize: 50 });
      return result.items as Certificate[];
    },
  });

  const generateMutation = useMutation({
    mutationFn: (data: { studentId: string; courseOfferingId: string }) =>
      certificateService.generate(data),
    onSuccess: () => {
      enqueueSnackbar('Certificate generated successfully', { variant: 'success' });
      queryClient.invalidateQueries({ queryKey: ['certificates'] });
    },
    onError: (err: any) => {
      enqueueSnackbar(err?.message || 'Failed to generate certificate', { variant: 'error' });
    },
  });

  const bulkGenerateMutation = useMutation({
    mutationFn: () => certificateService.bulkGenerate({ courseOfferingId: selectedOffering }),
    onSuccess: (data) => {
      setBulkResult(data);
      enqueueSnackbar(`Generated ${data.generated} certificates`, { variant: 'success' });
      queryClient.invalidateQueries({ queryKey: ['certificates'] });
    },
    onError: (err: any) => {
      enqueueSnackbar(err?.message || 'Bulk generation failed', { variant: 'error' });
    },
  });

  const bulkGenerateAllMutation = useMutation({
    mutationFn: () => certificateService.bulkGenerateAll(),
    onSuccess: (data) => {
      setBulkResult(data);
      enqueueSnackbar(`Generated ${data.generated} certificates across all offerings`, { variant: 'success' });
      queryClient.invalidateQueries({ queryKey: ['certificates'] });
    },
    onError: (err: any) => {
      enqueueSnackbar(err?.message || 'Bulk generation (all) failed', { variant: 'error' });
    },
  });

  const revokeMutation = useMutation({
    mutationFn: (data: { id: string; reason: string }) =>
      certificateService.revoke(data.id, { reason: data.reason }),
    onSuccess: () => {
      enqueueSnackbar('Certificate revoked', { variant: 'success' });
      setRevokeDialog({ cert: null as any, open: false });
      setRevokeReason('');
      queryClient.invalidateQueries({ queryKey: ['certificates'] });
    },
    onError: (err: any) => {
      enqueueSnackbar(err?.message || 'Failed to revoke certificate', { variant: 'error' });
    },
  });

  const handleDownload = async (id: string, number: string) => {
    try {
      const blob = await certificateService.download(id);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${number}.pdf`;
      a.click();
      window.URL.revokeObjectURL(url);
    } catch (err: any) {
      enqueueSnackbar(err?.message || 'Download failed', { variant: 'error' });
    }
  };

  const handleRevoke = () => {
    if (!revokeDialog.cert || !revokeReason.trim()) return;
    revokeMutation.mutate({ id: revokeDialog.cert.id, reason: revokeReason });
  };

  const statusColor = (status: CertificateStatus) => {
    switch (status) {
      case CertificateStatus.Issued: return 'success';
      case CertificateStatus.Pending: return 'warning';
      case CertificateStatus.Revoked: return 'error';
      case CertificateStatus.Superseded: return 'default';
      default: return 'default';
    }
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom fontWeight={600}>
        Certificates
      </Typography>
      <Typography variant="body2" color="textSecondary" gutterBottom>
        Manage certificate generation, revocation, and bulk issuance for completed course offerings.
      </Typography>

      {/* Bulk generation panel */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Bulk Certificate Generation
          </Typography>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} md={5}>
              <Autocomplete
                options={offeringsData?.items || []}
                getOptionLabel={(opt) => `${opt.offeringCode} - ${opt.courseName || ''}`}
                onChange={(_, value) => setSelectedOffering(value?.id || '')}
                renderInput={(params) => (
                  <TextField {...params} label="Course Offering" size="small" />
                )}
              />
            </Grid>
            <Grid item>
              <Button
                variant="contained"
                startIcon={<Add />}
                disabled={!selectedOffering || bulkGenerateMutation.isPending}
                onClick={() => bulkGenerateMutation.mutate()}
              >
                {bulkGenerateMutation.isPending ? 'Generating...' : 'Generate for Offering'}
              </Button>
            </Grid>
            <Grid item>
              <Button
                variant="outlined"
                startIcon={<Refresh />}
                disabled={bulkGenerateAllMutation.isPending}
                onClick={() => bulkGenerateAllMutation.mutate()}
              >
                {bulkGenerateAllMutation.isPending ? 'Generating...' : 'Generate All Completed'}
              </Button>
            </Grid>
          </Grid>

          {bulkResult && (
            <Alert severity="info" sx={{ mt: 2 }}>
              Processed {bulkResult.totalProcessed}: {bulkResult.generated} generated, {bulkResult.skipped} skipped, {bulkResult.errors} errors, {bulkResult.warnings} warnings.
            </Alert>
          )}
        </CardContent>
      </Card>

      {/* Certificates table */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Issued Certificates
          </Typography>
          {isLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress />
            </Box>
          ) : (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Certificate #</TableCell>
                    <TableCell>Student</TableCell>
                    <TableCell>Offering</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Issue Date</TableCell>
                    <TableCell align="right">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {certificates && certificates.length > 0 ? (
                    certificates.map((cert) => (
                      <TableRow key={cert.id} hover>
                        <TableCell>{cert.certificateNumber}</TableCell>
                        <TableCell>{cert.studentName || cert.studentId}</TableCell>
                        <TableCell>{cert.offeringCode || cert.courseOfferingId}</TableCell>
                        <TableCell>
                          <Chip label={cert.status} color={statusColor(cert.status) as any} size="small" />
                        </TableCell>
                        <TableCell>{cert.issueDate ? new Date(cert.issueDate).toLocaleDateString() : '-'}</TableCell>
                        <TableCell align="right">
                          <Tooltip title="Download PDF">
                            <span>
                              <IconButton
                                size="small"
                                disabled={cert.status !== CertificateStatus.Issued}
                                onClick={() => handleDownload(cert.id, cert.certificateNumber)}
                              >
                                <Download fontSize="small" />
                              </IconButton>
                            </span>
                          </Tooltip>
                          <Tooltip title="Revoke">
                            <span>
                              <IconButton
                                size="small"
                                disabled={cert.status !== CertificateStatus.Issued}
                                onClick={() => setRevokeDialog({ cert, open: true })}
                              >
                                <Block fontSize="small" />
                              </IconButton>
                            </span>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={6} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                        <FilePresent sx={{ fontSize: 40, mb: 1, opacity: 0.4 }} />
                        <Typography variant="body2">No certificates found. Use bulk generation above to issue certificates.</Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      {/* Revoke dialog */}
      <Dialog open={revokeDialog.open} onClose={() => setRevokeDialog({ cert: null as any, open: false })}>
        <DialogTitle>Revoke Certificate</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="textSecondary" gutterBottom>
            Revoking certificate {revokeDialog.cert?.certificateNumber} will invalidate it permanently.
          </Typography>
          <TextField
            fullWidth
            multiline
            rows={3}
            label="Reason"
            value={revokeReason}
            onChange={(e) => setRevokeReason(e.target.value)}
            margin="normal"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRevokeDialog({ cert: null as any, open: false })}>Cancel</Button>
          <Button
            color="error"
            variant="contained"
            disabled={!revokeReason.trim() || revokeMutation.isPending}
            onClick={handleRevoke}
          >
            {revokeMutation.isPending ? 'Revoking...' : 'Revoke'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
