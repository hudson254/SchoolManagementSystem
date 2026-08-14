import React, { useState, useRef, useEffect } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormControlLabel,
  Switch,
  Typography,
  Paper,
  Grid,
  IconButton,
  Tooltip,
  Divider,
  Chip,
  useTheme,
} from '@mui/material';
import {
  Save as SaveIcon,
  Delete as DeleteIcon,
  DragIndicator as DragIcon,
  Add as AddIcon,
  Preview as PreviewIcon,
} from '@mui/icons-material';
import { certificateService } from '../services/certificate.service';
import { CertificateTemplate, CertificateTemplateRequest } from '../types/certificate.types';
import { useSnackbar } from 'notistack';

export type TemplateFieldType =
  | 'studentName'
  | 'courseName'
  | 'courseOffering'
  | 'startDate'
  | 'completionDate'
  | 'courseDuration'
  | 'finalGrade'
  | 'classification'
  | 'certificateNumber'
  | 'qrCode'
  | 'dateIssued'
  | 'digitalSignature'
  | 'institutionLogo'
  | 'watermark';

export interface FieldMapping {
  id: string;
  type: TemplateFieldType;
  label: string;
  x: number;
  y: number;
  width: number;
  height: number;
  font: string;
  fontSize: number;
  fontColor: string;
  alignment: 'left' | 'center' | 'right';
  bold: boolean;
  italic: boolean;
  rotation: number;
  charSpacing: number;
  lineSpacing: number;
}

const FIELD_LABELS: Record<TemplateFieldType, string> = {
  studentName: 'Student Full Name',
  courseName: 'Course Name',
  courseOffering: 'Course Offering',
  startDate: 'Start Date',
  completionDate: 'Completion Date',
  courseDuration: 'Course Duration',
  finalGrade: 'Final Grade',
  classification: 'Award Classification',
  certificateNumber: 'Certificate Number',
  qrCode: 'QR Code',
  dateIssued: 'Date Issued',
  digitalSignature: 'Digital Signature',
  institutionLogo: 'Institution Logo',
  watermark: 'Watermark',
};

const FONT_OPTIONS = ['Helvetica', 'Times-Roman', 'Courier', 'Arial', 'Georgia', 'Verdana'];

interface TemplateMapperProps {
  template: CertificateTemplate | null;
  onSave: (fieldMappings: string) => void;
  onCancel: () => void;
}

export const TemplateMapper: React.FC<TemplateMapperProps> = ({ template, onSave, onCancel }) => {
  const { enqueueSnackbar } = useSnackbar();
  const theme = useTheme();
  const canvasRef = useRef<HTMLDivElement>(null);
  const [fields, setFields] = useState<FieldMapping[]>([]);
  const [selectedField, setSelectedField] = useState<FieldMapping | null>(null);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [canvasSize, setCanvasSize] = useState({ width: 800, height: 600 });

  // Load existing field mappings from template
  useEffect(() => {
    if (template?.fieldMappings) {
      try {
        const parsed = JSON.parse(template.fieldMappings);
        if (Array.isArray(parsed)) {
          setFields(parsed);
        }
      } catch {
        // If parsing fails, start with empty fields
      }
    }
  }, [template]);

  // Calculate canvas size based on aspect ratio (A4: 800x600 default)
  useEffect(() => {
    if (canvasRef.current) {
      const rect = canvasRef.current.getBoundingClientRect();
      setCanvasSize({ width: rect.width, height: rect.height });
    }
  }, []);

  const addField = (type: TemplateFieldType) => {
    const newField: FieldMapping = {
      id: `field_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      type,
      label: FIELD_LABELS[type],
      x: 100,
      y: 100,
      width: type === 'qrCode' || type === 'institutionLogo' || type === 'watermark' ? 100 : 200,
      height: type === 'qrCode' || type === 'institutionLogo' || type === 'watermark' ? 100 : 30,
      font: 'Helvetica',
      fontSize: 14,
      fontColor: '#000000',
      alignment: 'center',
      bold: false,
      italic: false,
      rotation: 0,
      charSpacing: 0,
      lineSpacing: 1,
    };
    setFields([...fields, newField]);
    setSelectedField(newField);
  };

  const updateField = (updates: Partial<FieldMapping>) => {
    if (!selectedField) return;
    const updated = { ...selectedField, ...updates };
    setFields(fields.map((f) => (f.id === selectedField.id ? updated : f)));
    setSelectedField(updated);
  };

  const deleteField = (id: string) => {
    setFields(fields.filter((f) => f.id !== id));
    if (selectedField?.id === id) {
      setSelectedField(null);
    }
  };

  const handleSave = () => {
    if (fields.length === 0) {
      enqueueSnackbar('Add at least one field to the template', { variant: 'warning' });
      return;
    }
    onSave(JSON.stringify(fields));
  };

  const handlePreview = () => {
    setPreviewOpen(true);
  };

  const handleCanvasClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (selectedField) {
      const rect = e.currentTarget.getBoundingClientRect();
      const x = ((e.clientX - rect.left) / rect.width) * 100;
      const y = ((e.clientY - rect.top) / rect.height) * 100;
      updateField({ x, y });
    }
  };

  const handleFieldDrag = (id: string, e: React.MouseEvent) => {
    const field = fields.find((f) => f.id === id);
    if (!field) return;

    const startX = e.clientX;
    const startY = e.clientY;
    const startRect = { x: field.x, y: field.y };

    const handleMouseMove = (moveEvent: MouseEvent) => {
      if (canvasRef.current) {
        const rect = canvasRef.current.getBoundingClientRect();
        const newX = ((startRect.x * rect.width + (moveEvent.clientX - startX)) / rect.width) * 100;
        const newY = ((startRect.y * rect.height + (moveEvent.clientY - startY)) / rect.height) * 100;
        const boundedX = Math.max(0, Math.min(100, newX));
        const boundedY = Math.max(0, Math.min(100, newY));
        setFields(fields.map((f) => (f.id === id ? { ...f, x: boundedX, y: boundedY } : f)));
        if (selectedField?.id === id) {
          setSelectedField({ ...field, x: boundedX, y: boundedY });
        }
      }
    };

    const handleMouseUp = () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  };

  const renderFieldOnCanvas = (field: FieldMapping) => {
    const isImageField = field.type === 'qrCode' || field.type === 'institutionLogo' || field.type === 'watermark' || field.type === 'digitalSignature';
    const isSelected = selectedField?.id === field.id;

    return (
      <div
        key={field.id}
        onClick={(e) => {
          e.stopPropagation();
          setSelectedField(field);
        }}
        onMouseDown={(e) => {
          e.stopPropagation();
          handleFieldDrag(field.id, e);
        }}
        style={{
          position: 'absolute',
          left: `${field.x}%`,
          top: `${field.y}%`,
          width: `${field.width}%`,
          height: `${field.height}%`,
          transform: `rotate(${field.rotation}deg)`,
          border: isSelected ? '2px dashed #1976d2' : '1px dashed rgba(0,0,0,0.3)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: field.alignment === 'center' ? 'center' : field.alignment === 'left' ? 'flex-start' : 'flex-end',
          cursor: 'move',
          padding: isImageField ? 0 : '2px',
          boxSizing: 'border-box',
          backgroundColor: isSelected ? 'rgba(25, 118, 210, 0.1)' : 'rgba(0,0,0,0.03)',
        }}
      >
        {isImageField ? (
          <div style={{ width: '100%', height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '10px', color: '#666' }}>
            [{field.label}]
          </div>
        ) : (
          <div
            style={{
              fontFamily: field.font,
              fontSize: `${field.fontSize}px`,
              color: field.fontColor,
              fontWeight: field.bold ? 'bold' : 'normal',
              fontStyle: field.italic ? 'italic' : 'normal',
              textAlign: field.alignment,
              width: '100%',
              whiteSpace: 'nowrap',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              letterSpacing: `${field.charSpacing}px`,
              lineHeight: field.lineSpacing,
            }}
          >
            {field.label}
          </div>
        )}
        {isSelected && (
          <IconButton
            size="small"
            onClick={(e) => {
              e.stopPropagation();
              deleteField(field.id);
            }}
            sx={{ position: 'absolute', top: -15, right: -15, backgroundColor: 'white', border: '1px solid #ccc' }}
          >
            <DeleteIcon fontSize="small" />
          </IconButton>
        )}
      </div>
    );
  };

  return (
    <Box sx={{ display: 'flex', height: '100%', gap: 2 }}>
      {/* Canvas Area */}
      <Box sx={{ flex: 3, display: 'flex', flexDirection: 'column', gap: 2 }}>
        <Typography variant="h6">
          {template?.name || 'Template'} - Field Placement
        </Typography>
        <Paper
          ref={canvasRef}
          elevation={3}
          sx={{
            position: 'relative',
            width: '100%',
            height: 500,
            backgroundColor: '#f5f5f5',
            border: '1px solid #ddd',
            borderRadius: 1,
            overflow: 'hidden',
            cursor: selectedField ? 'crosshair' : 'default',
          }}
          onClick={handleCanvasClick}
        >
          {/* Template preview background */}
          {template?.filePath && (
            <div
              style={{
                position: 'absolute',
                top: 0,
                left: 0,
                width: '100%',
                height: '100%',
                backgroundImage: `url(${template.filePath})`,
                backgroundSize: 'cover',
                backgroundPosition: 'center',
                opacity: 0.3,
              }}
            />
          )}
          {/* Render placed fields */}
          {fields.map(renderFieldOnCanvas)}
          {/* Drop indicator */}
          {selectedField && (
            <Tooltip title="Click to place selected field at cursor position" placement="top">
              <div style={{ position: 'absolute', bottom: 10, left: 10, background: 'white', padding: '4px 8px', borderRadius: 4, fontSize: 12 }}>
                Click on canvas to reposition: {selectedField.label}
              </div>
            </Tooltip>
          )}
        </Paper>

        {/* Field Palette */}
        <Box>
          <Typography variant="subtitle2" gutterBottom>Add Fields:</Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {(Object.keys(FIELD_LABELS) as TemplateFieldType[]).map((type) => (
              <Chip
                key={type}
                label={FIELD_LABELS[type]}
                onClick={() => addField(type)}
                clickable
                icon={<AddIcon />}
                size="small"
                variant="outlined"
              />
            ))}
          </Box>
        </Box>

        <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2, mt: 'auto' }}>
          <Button onClick={onCancel} variant="outlined">
            Cancel
          </Button>
          <Button onClick={handlePreview} variant="outlined" startIcon={<PreviewIcon />}>
            Preview
          </Button>
          <Button onClick={handleSave} variant="contained" startIcon={<SaveIcon />}>
            Save Mapping
          </Button>
        </Box>
      </Box>

      {/* Properties Panel */}
      <Box sx={{ flex: 1, maxWidth: 350 }}>
        <Paper elevation={2} sx={{ p: 2, height: 500, overflowY: 'auto' }}>
          <Typography variant="h6" gutterBottom>
            {selectedField ? `Edit: ${selectedField.label}` : 'Field Properties'}
          </Typography>
          {selectedField ? (
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField
                  label="X Coordinate (%)"
                  type="number"
                  size="small"
                  value={selectedField.x}
                  onChange={(e) => updateField({ x: parseFloat(e.target.value) || 0 })}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Y Coordinate (%)"
                  type="number"
                  size="small"
                  value={selectedField.y}
                  onChange={(e) => updateField({ y: parseFloat(e.target.value) || 0 })}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Width (%)"
                  type="number"
                  size="small"
                  value={selectedField.width}
                  onChange={(e) => updateField({ width: parseFloat(e.target.value) || 0 })}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Height (%)"
                  type="number"
                  size="small"
                  value={selectedField.height}
                  onChange={(e) => updateField({ height: parseFloat(e.target.value) || 0 })}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12}>
                <FormControl size="small" fullWidth>
                  <InputLabel>Font</InputLabel>
                  <Select
                    value={selectedField.font}
                    label="Font"
                    onChange={(e) => updateField({ font: e.target.value })}
                  >
                    {FONT_OPTIONS.map((font) => (
                      <MenuItem key={font} value={font}>{font}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Font Size"
                  type="number"
                  size="small"
                  value={selectedField.fontSize}
                  onChange={(e) => updateField({ fontSize: parseInt(e.target.value) || 12 })}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Font Color"
                  type="color"
                  size="small"
                  value={selectedField.fontColor}
                  onChange={(e) => updateField({ fontColor: e.target.value })}
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                />
              </Grid>
              <Grid item xs={12}>
                <FormControl size="small" fullWidth>
                  <InputLabel>Alignment</InputLabel>
                  <Select
                    value={selectedField.alignment}
                    label="Alignment"
                    onChange={(e) => updateField({ alignment: e.target.value as any })}
                  >
                    <MenuItem value="left">Left</MenuItem>
                    <MenuItem value="center">Center</MenuItem>
                    <MenuItem value="right">Right</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={6}>
                <FormControlLabel
                  control={<Switch checked={selectedField.bold} onChange={(e) => updateField({ bold: e.target.checked })} />}
                  label="Bold"
                />
              </Grid>
              <Grid item xs={6}>
                <FormControlLabel
                  control={<Switch checked={selectedField.italic} onChange={(e) => updateField({ italic: e.target.checked })} />}
                  label="Italic"
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Rotation (degrees)"
                  type="number"
                  size="small"
                  value={selectedField.rotation}
                  onChange={(e) => updateField({ rotation: parseInt(e.target.value) || 0 })}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Character Spacing"
                  type="number"
                  size="small"
                  value={selectedField.charSpacing}
                  onChange={(e) => updateField({ charSpacing: parseFloat(e.target.value) || 0 })}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Line Spacing"
                  type="number"
                  size="small"
                  value={selectedField.lineSpacing}
                  onChange={(e) => updateField({ lineSpacing: parseFloat(e.target.value) || 1 })}
                  fullWidth
                />
              </Grid>
              <Grid item xs={12}>
                <Button
                  variant="outlined"
                  color="error"
                  startIcon={<DeleteIcon />}
                  onClick={() => deleteField(selectedField.id)}
                  fullWidth
                >
                  Remove Field
                </Button>
              </Grid>
            </Grid>
          ) : (
            <Typography color="text.secondary">
              Select a field or add a new one from the palette to configure its properties.
            </Typography>
          )}
        </Paper>
      </Box>

      {/* Preview Dialog */}
      <Dialog open={previewOpen} onClose={() => setPreviewOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Template Preview</DialogTitle>
        <DialogContent>
          <Box sx={{ position: 'relative', width: '100%', height: 400, backgroundColor: '#f5f5f5' }}>
            {template?.filePath && (
              <img src={template.filePath} alt="Template preview" style={{ width: '100%', height: '100%', objectFit: 'contain', opacity: 0.5 }} />
            )}
            {fields.map((field) => (
              <div
                key={field.id}
                style={{
                  position: 'absolute',
                  left: `${field.x}%`,
                  top: `${field.y}%`,
                  width: `${field.width}%`,
                  height: `${field.height}%`,
                  border: '1px solid #1976d2',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: `${field.fontSize}px`,
                  fontFamily: field.font,
                  color: field.fontColor,
                  fontWeight: field.bold ? 'bold' : 'normal',
                  fontStyle: field.italic ? 'italic' : 'normal',
                  textAlign: field.alignment,
                  transform: `rotate(${field.rotation}deg)`,
                }}
              >
                {field.label}
              </div>
            ))}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPreviewOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
