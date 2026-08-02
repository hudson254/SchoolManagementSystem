import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Grid,
  Card,
  CardContent,
  CardActions,
  Button,
  IconButton,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  Divider,
  Alert,
  LinearProgress,
  Chip,
} from '@mui/material';
import {
  PictureAsPdf as PdfIcon,
  Download as DownloadIcon,
  Print as PrintIcon,
  Assessment as AssessmentIcon,
  People as PeopleIcon,
  School as SchoolIcon,
  Bed as BedIcon,
  CalendarToday as CalendarIcon,
  Assignment as AssignmentIcon,
  Grade as GradeIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { reportService } from '../services/report.service';

interface ReportType {
  id: string;
  name: string;
  description: string;
  icon: React.ReactNode;
  parameters: ReportParameter[];
}

interface ReportParameter {
  key: string;
  label: string;
  type: 'text' | 'date' | 'select' | 'number';
  options?: { value: string; label: string }[];
  required?: boolean;
}

const reportTypes: ReportType[] = [
  {
    id: 'student-enrollment',
    name: 'Student Enrollment Report',
    description: 'View student enrollment statistics and trends',
    icon: <PeopleIcon />,
    parameters: [
      { key: 'semesterId', label: 'Semester', type: 'select', options: [], required: false },
      { key: 'programmeId', label: 'Programme', type: 'select', options: [], required: false },
    ],
  },
  {
    id: 'lecturer-workload',
    name: 'Lecturer Workload Report',
    description: 'View lecturer workload and allocation statistics',
    icon: <SchoolIcon />,
    parameters: [
      { key: 'semesterId', label: 'Semester', type: 'select', options: [], required: true },
    ],
  },
  {
    id: 'course-statistics',
    name: 'Course Statistics Report',
    description: 'View course enrollment and performance statistics',
    icon: <AssessmentIcon />,
    parameters: [
      { key: 'semesterId', label: 'Semester', type: 'select', options: [], required: false },
    ],
  },
  {
    id: 'assignment-completion',
    name: 'Assignment Completion Report',
    description: 'View assignment submission and completion rates',
    icon: <AssignmentIcon />,
    parameters: [
      { key: 'assignmentId', label: 'Assignment', type: 'select', options: [], required: true },
    ],
  },
  {
    id: 'grade-distribution',
    name: 'Grade Distribution Report',
    description: 'View grade distribution across units and semesters',
    icon: <GradeIcon />,
    parameters: [
      { key: 'semesterId', label: 'Semester', type: 'select', options: [], required: false },
      { key: 'unitId', label: 'Unit', type: 'select', options: [], required: false },
    ],
  },
  {
    id: 'occupancy',
    name: 'Accommodation Occupancy Report',
    description: 'View room occupancy and availability statistics',
    icon: <BedIcon />,
    parameters: [
      { key: 'buildingId', label: 'Building', type: 'select', options: [], required: false },
    ],
  },
  {
    id: 'timetable-utilization',
    name: 'Timetable Utilization Report',
    description: 'View timetable usage and room utilization',
    icon: <CalendarIcon />,
    parameters: [
      { key: 'semesterId', label: 'Semester', type: 'select', options: [], required: true },
    ],
  },
];

export const Reports: React.FC = () => {
  const [selectedReport, setSelectedReport] = useState<string>('');
  const [parameters, setParameters] = useState<Record<string, any>>({});
  const [isGenerating, setIsGenerating] = useState(false);
  const [generatedReport, setGeneratedReport] = useState<{ url: string; name: string } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleReportSelect = (reportId: string) => {
    setSelectedReport(reportId);
    setGeneratedReport(null);
    setError(null);
    // Reset parameters for the selected report
    const report = reportTypes.find((r) => r.id === reportId);
    if (report) {
      const params: Record<string, any> = {};
      report.parameters.forEach((p) => {
        params[p.key] = '';
      });
      setParameters(params);
    }
  };

  const handleParameterChange = (key: string, value: any) => {
    setParameters((prev) => ({ ...prev, [key]: value }));
  };

  const handleGenerateReport = async () => {
    try {
      setIsGenerating(true);
      setError(null);

      const report = reportTypes.find((r) => r.id === selectedReport);
      if (!report) return;

      // Validate required parameters
      for (const param of report.parameters) {
        if (param.required && !parameters[param.key]) {
          setError(`Please fill in the required field: ${param.label}`);
          setIsGenerating(false);
          return;
        }
      }

      // Generate report
      const result = await reportService.generateReport({
        reportType: selectedReport,
        ...parameters,
      });

      setGeneratedReport({
        url: result.url,
        name: result.fileName,
      });
    } catch (err) {
      setError('Failed to generate report. Please try again.');
    } finally {
      setIsGenerating(false);
    }
  };

  const handleDownload = async (format: 'pdf' | 'excel' | 'csv') => {
    // Download logic
    window.open(`${generatedReport?.url}?format=${format}`, '_blank');
  };

  const selectedReportData = reportTypes.find((r) => r.id === selectedReport);

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" fontWeight={600}>
          Reports
        </Typography>
      </Box>

      <Grid container spacing={3}>
        {/* Report Selection */}
        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight={600} gutterBottom>
              Available Reports
            </Typography>
            <Divider sx={{ mb: 2 }} />
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              {reportTypes.map((report) => (
                <Paper
                  key={report.id}
                  sx={{
                    p: 2,
                    cursor: 'pointer',
                    border: selectedReport === report.id ? '2px solid #576426' : '1px solid #e0e0e0',
                    '&:hover': {
                      borderColor: '#576426',
                      bgcolor: 'rgba(87, 100, 38, 0.04)',
                    },
                  }}
                  onClick={() => handleReportSelect(report.id)}
                >
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                    <Box sx={{ color: '#576426' }}>{report.icon}</Box>
                    <Box>
                      <Typography variant="body2" fontWeight={500}>
                        {report.name}
                      </Typography>
                      <Typography variant="caption" color="textSecondary">
                        {report.description}
                      </Typography>
                    </Box>
                  </Box>
                </Paper>
              ))}
            </Box>
          </Paper>
        </Grid>

        {/* Report Configuration */}
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight={600} gutterBottom>
              {selectedReportData ? selectedReportData.name : 'Select a Report'}
            </Typography>
            <Divider sx={{ mb: 3 }} />

            {selectedReportData ? (
              <>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
                  {selectedReportData.description}
                </Typography>

                <Grid container spacing={2}>
                  {selectedReportData.parameters.map((param) => (
                    <Grid item xs={12} sm={6} key={param.key}>
                      <FormControl fullWidth size="small">
                        {param.type === 'select' ? (
                          <>
                            <InputLabel>{param.label}</InputLabel>
                            <Select
                              value={parameters[param.key] || ''}
                              onChange={(e) => handleParameterChange(param.key, e.target.value)}
                              label={param.label}
                            >
                              <MenuItem value="">
                                <em>{param.required ? 'Select...' : 'Optional'}</em>
                              </MenuItem>
                              {param.options?.map((option) => (
                                <MenuItem key={option.value} value={option.value}>
                                  {option.label}
                                </MenuItem>
                              ))}
                            </Select>
                          </>
                        ) : param.type === 'date' ? (
                          <TextField
                            type="date"
                            label={param.label}
                            value={parameters[param.key] || ''}
                            onChange={(e) => handleParameterChange(param.key, e.target.value)}
                            InputLabelProps={{ shrink: true }}
                          />
                        ) : param.type === 'number' ? (
                          <TextField
                            type="number"
                            label={param.label}
                            value={parameters[param.key] || ''}
                            onChange={(e) => handleParameterChange(param.key, e.target.value)}
                          />
                        ) : (
                          <TextField
                            label={param.label}
                            value={parameters[param.key] || ''}
                            onChange={(e) => handleParameterChange(param.key, e.target.value)}
                          />
                        )}
                      </FormControl>
                    </Grid>
                  ))}
                </Grid>

                {error && (
                  <Alert severity="error" sx={{ mt: 2 }}>
                    {error}
                  </Alert>
                )}

                {isGenerating && (
                  <Box sx={{ mt: 2 }}>
                    <LinearProgress />
                    <Typography variant="caption" color="textSecondary">
                      Generating report...
                    </Typography>
                  </Box>
                )}

                <Box sx={{ mt: 3, display: 'flex', gap: 2 }}>
                  <Button
                    variant="contained"
                    startIcon={<AssessmentIcon />}
                    onClick={handleGenerateReport}
                    disabled={isGenerating}
                  >
                    Generate Report
                  </Button>
                  {generatedReport && (
                    <>
                      <Button
                        variant="outlined"
                        startIcon={<PdfIcon />}
                        onClick={() => handleDownload('pdf')}
                      >
                        PDF
                      </Button>
                      <Button
                        variant="outlined"
                        startIcon={<DownloadIcon />}
                        onClick={() => handleDownload('excel')}
                      >
                        Excel
                      </Button>
                      <Button
                        variant="outlined"
                        onClick={() => handleDownload('csv')}
                      >
                        CSV
                      </Button>
                    </>
                  )}
                </Box>

                {generatedReport && (
                  <Box sx={{ mt: 3, p: 2, bgcolor: '#f5f7f0', borderRadius: 1 }}>
                    <Typography variant="body2" fontWeight={500}>
                      Report Ready
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      {generatedReport.name}
                    </Typography>
                    <Box sx={{ mt: 1 }}>
                      <Chip label="Ready to download" color="success" size="small" />
                    </Box>
                  </Box>
                )}
              </>
            ) : (
              <Box
                sx={{
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  justifyContent: 'center',
                  py: 8,
                }}
              >
                <AssessmentIcon sx={{ fontSize: 64, color: '#e0e0e0', mb: 2 }} />
                <Typography variant="body1" color="textSecondary">
                  Select a report from the left panel to configure and generate
                </Typography>
              </Box>
            )}
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};