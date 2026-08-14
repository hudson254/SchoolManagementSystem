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
} from '@mui/material';
import { Download, WorkspacePremium, VerifiedUser } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { certificateService } from '../services/certificate.service';
import { useAuth } from '../hooks/useAuth';
import { userService } from '../services/user.service';
import { Certificate, CertificateStatus } from '../types/certificate.types';

export const MyCertificates: React.FC = () => {
  const { user } = useAuth();
  const { enqueueSnackbar } = useSnackbar();
  const [studentId, setStudentId] = useState<string | null>(null);

  // Resolve the student's ID - the user profile may not directly expose studentId.
  // We fall back to the auth user id, and attempt to look up the student profile.
  const { data: profile } = useQuery({
    queryKey: ['profile', user?.id],
    queryFn: () => userService.getProfile(),
    enabled: !!user,
  });

  const { data: certificates, isLoading, error } = useQuery({
    queryKey: ['my-certificates', studentId],
    queryFn: () => {
      // The studentId is derived from the profile if available, else the user id.
      const id = studentId || (profile as any)?.studentId || user?.id;
      return certificateService.getByStudent(id);
    },
    enabled: !!user && !!(studentId || (profile as any)?.studentId || user?.id),
  });

  // If profile provides a studentId, use it automatically.
  React.useEffect(() => {
    if (!studentId && (profile as any)?.studentId) {
      setStudentId((profile as any).studentId);
    }
  }, [profile, studentId]);

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
        My Certificates
      </Typography>
      <Typography variant="body2" color="textSecondary" gutterBottom>
        View and download certificates issued for your completed course offerings.
      </Typography>

      {!studentId && (
        <Alert severity="info" sx={{ mb: 3 }}>
          No associated student record found. Please contact the administrator if you believe you should have certificates.
        </Alert>
      )}

      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Issued Certificates
          </Typography>
          {isLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress />
            </Box>
          ) : error ? (
            <Alert severity="error">Failed to load certificates.</Alert>
          ) : (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Certificate #</TableCell>
                    <TableCell>Offering</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Issue Date</TableCell>
                    <TableCell>Classification</TableCell>
                    <TableCell align="right">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {certificates && certificates.length > 0 ? (
                    certificates.map((cert) => (
                      <TableRow key={cert.id} hover>
                        <TableCell>{cert.certificateNumber}</TableCell>
                        <TableCell>{cert.offeringCode || cert.courseOfferingId}</TableCell>
                        <TableCell>
                          <Chip label={cert.status} color={statusColor(cert.status) as any} size="small" />
                        </TableCell>
                        <TableCell>{cert.issueDate ? new Date(cert.issueDate).toLocaleDateString() : '-'}</TableCell>
                        <TableCell>{cert.classification || '-'}</TableCell>
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
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={6} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                        <WorkspacePremium sx={{ fontSize: 40, mb: 1, opacity: 0.4 }} />
                        <Typography variant="body2">No certificates issued yet.</Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>
    </Box>
  );
};
