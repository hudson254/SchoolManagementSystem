import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Button,
  CircularProgress,
  TextField,
  Alert,
  Chip,
  Divider,
  Paper,
  Tabs,
  Tab,
} from '@mui/material';
import { VerifiedUser, QrCode2, Search } from '@mui/icons-material';
import { useMutation } from '@tanstack/react-query';
import { certificateService, VerificationResult } from '../services/certificate.service';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`verify-tabpanel-${index}`}
      aria-labelledby={`verify-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

export const CertificateVerification: React.FC = () => {
  const [tab, setTab] = useState(0);
  const [certificateNumber, setCertificateNumber] = useState('');
  const [token, setToken] = useState('');
  const [qrCodeData, setQrCodeData] = useState('');

  const verifyByNumber = useMutation({
    mutationFn: (num: string) => certificateService.verifyByCertificateNumber(num),
  });

  const verifyByToken = useMutation({
    mutationFn: (tok: string) => certificateService.verifyByToken(tok),
  });

  const verifyByQr = useMutation({
    mutationFn: (data: string) => certificateService.verifyByQrCode(data),
  });

  const activeMutation = tab === 0 ? verifyByNumber : tab === 1 ? verifyByToken : verifyByQr;
  const result: VerificationResult | undefined = activeMutation.data;

  const handleVerify = () => {
    if (tab === 0 && certificateNumber.trim()) {
      verifyByNumber.mutate(certificateNumber.trim());
    } else if (tab === 1 && token.trim()) {
      verifyByToken.mutate(token.trim());
    } else if (tab === 2 && qrCodeData.trim()) {
      verifyByQr.mutate(qrCodeData.trim());
    }
  };

  return (
    <Box sx={{ maxWidth: 800, mx: 'auto', py: 4, px: 2 }}>
      <Box sx={{ textAlign: 'center', mb: 4 }}>
        <VerifiedUser color="primary" sx={{ fontSize: 56, mb: 1 }} />
        <Typography variant="h4" gutterBottom fontWeight={600}>
          Certificate Verification
        </Typography>
        <Typography variant="body1" color="textSecondary">
          Verify the authenticity of a certificate issued by the institution.
        </Typography>
      </Box>

      <Card>
        <CardContent>
          <Tabs value={tab} onChange={(_, v) => setTab(v)} variant="fullWidth">
            <Tab label="Certificate Number" icon={<Search />} iconPosition="start" />
            <Tab label="Verification Token" icon={<VerifiedUser />} iconPosition="start" />
            <Tab label="QR Code" icon={<QrCode2 />} iconPosition="start" />
          </Tabs>

          <TabPanel value={tab} index={0}>
            <TextField
              fullWidth
              label="Certificate Number (e.g. SMS-2026-DIT-000001)"
              value={certificateNumber}
              onChange={(e) => setCertificateNumber(e.target.value)}
              placeholder="SMS-2026-DIT-000001"
              size="small"
            />
          </TabPanel>
          <TabPanel value={tab} index={1}>
            <TextField
              fullWidth
              label="Verification Token"
              value={token}
              onChange={(e) => setToken(e.target.value)}
              placeholder="Paste verification token from certificate"
              size="small"
            />
          </TabPanel>
          <TabPanel value={tab} index={2}>
            <TextField
              fullWidth
              label="QR Code Data"
              value={qrCodeData}
              onChange={(e) => setQrCodeData(e.target.value)}
              placeholder="Paste QR code URL or token data"
              size="small"
            />
          </TabPanel>

          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
            <Button
              variant="contained"
              color="primary"
              size="large"
              startIcon={activeMutation.isPending ? <CircularProgress size={20} /> : <VerifiedUser />}
              disabled={activeMutation.isPending}
              onClick={handleVerify}
            >
              {activeMutation.isPending ? 'Verifying...' : 'Verify Certificate'}
            </Button>
          </Box>

          {activeMutation.error && (
            <Alert severity="error" sx={{ mt: 3 }}>
              Failed to verify certificate. Please check the input and try again.
            </Alert>
          )}

          {result && (
            <Paper variant="outlined" sx={{ mt: 3, p: 2 }}>
              {result.isValid ? (
                <>
                  <Alert severity="success" sx={{ mb: 2 }}>
                    <Typography fontWeight={600}>✓ Certificate is VALID</Typography>
                    This certificate has been verified as authentic and issued by the institution.
                  </Alert>
                  <Divider sx={{ my: 2 }} />
                  <Grid container spacing={2}>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="caption" color="textSecondary">Certificate Number</Typography>
                      <Typography fontWeight={600}>{result.certificateNumber}</Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="caption" color="textSecondary">Status</Typography>
                      <Chip label={result.status || 'Issued'} color="success" size="small" />
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="caption" color="textSecondary">Student</Typography>
                      <Typography fontWeight={600}>{result.studentName}</Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="caption" color="textSecondary">Course</Typography>
                      <Typography fontWeight={600}>{result.courseName}</Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="caption" color="textSecondary">Offering</Typography>
                      <Typography fontWeight={600}>{result.offeringCode}</Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="caption" color="textSecondary">Issue Date</Typography>
                      <Typography fontWeight={600}>
                        {result.issueDate ? new Date(result.issueDate).toLocaleDateString() : '-'}
                      </Typography>
                    </Grid>
                  </Grid>
                </>
              ) : (
                <Alert severity="error">
                  <Typography fontWeight={600}>✗ Certificate is NOT VALID</Typography>
                  {result.errorMessage || 'This certificate could not be verified.'}
                </Alert>
              )}
            </Paper>
          )}
        </CardContent>
      </Card>
    </Box>
  );
};
