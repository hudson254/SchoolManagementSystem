import React, { useRef, useState } from 'react';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  Grid,
  IconButton,
  Input,
  LinearProgress,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Paper,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import {
  Description as DescriptionIcon,
  Delete as DeleteIcon,
  Download as DownloadIcon,
  Upload as UploadIcon,
  School as SchoolIcon,
} from '@mui/icons-material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  studyMaterialService,
  StudyMaterial,
  STUDY_MATERIAL_EXTENSIONS,
  STUDY_MATERIAL_MAX_SIZE_MB,
  formatFileSize,
  saveBlob,
} from '../../services/studyMaterial.service';
import { useAuth } from '../../hooks/useAuth';
import { hasAnyRole, LECTURER } from '../../utils/roles';
import { normalizeError } from '../../utils/errors';

interface StudyMaterialsSectionProps {
  unitId: string;
  unitName?: string;
  unitCode?: string;
}

/**
 * Study Materials section for a unit. LecturerAccess roles get the upload
 * control; students see the published list and can download. All
 * authorization is enforced server-side — the UI only mirrors it.
 */
export const StudyMaterialsSection: React.FC<StudyMaterialsSectionProps> = ({
  unitId,
  unitName,
  unitCode,
}) => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const canUpload = hasAnyRole(user?.roles, LECTURER);

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [progress, setProgress] = useState<number | null>(null);
  const [downloadError, setDownloadError] = useState<string | null>(null);

  const { data: materials, isLoading, isError, error } = useQuery({
    queryKey: ['study-materials', unitId],
    queryFn: () => studyMaterialService.getUnitMaterials(unitId),
    enabled: !!unitId,
    retry: false,
  });

  const uploadMutation = useMutation({
    mutationFn: () => {
      if (!selectedFile) throw new Error('Select a file first');
      return studyMaterialService.uploadMaterial(
        unitId,
        selectedFile,
        title.trim(),
        description.trim() || undefined,
        setProgress
      );
    },
    onSuccess: () => {
      setTitle('');
      setDescription('');
      setSelectedFile(null);
      setFileError(null);
      setProgress(null);
      if (fileInputRef.current) fileInputRef.current.value = '';
      queryClient.invalidateQueries({ queryKey: ['study-materials', unitId] });
    },
    onError: () => {
      setProgress(null);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (materialId: string) =>
      studyMaterialService.deleteMaterial(materialId, unitId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['study-materials', unitId] });
    },
  });

  const downloadMutation = useMutation({
    mutationFn: async (material: StudyMaterial) => {
      const blob = await studyMaterialService.downloadMaterial(material.id);
      saveBlob(blob, material.originalFileName || `${material.title}${material.extension}`);
    },
    onError: () => setDownloadError('Download failed. Please try again.'),
  });

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] || null;
    setFileError(null);
    if (!file) {
      setSelectedFile(null);
      return;
    }
    const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
    if (!STUDY_MATERIAL_EXTENSIONS.includes(extension)) {
      setSelectedFile(null);
      setFileError(
        `Unsupported file type "${extension || 'unknown'}". Allowed: ${STUDY_MATERIAL_EXTENSIONS.join(', ')}`
      );
      return;
    }
    if (file.size > STUDY_MATERIAL_MAX_SIZE_MB * 1024 * 1024) {
      setSelectedFile(null);
      setFileError(`File exceeds the ${STUDY_MATERIAL_MAX_SIZE_MB} MB limit.`);
      return;
    }
    setSelectedFile(file);
  };

  const handleUpload = () => {
    if (!selectedFile) return;
    uploadMutation.mutate();
  };

  const materialsList = Array.isArray(materials) ? materials : [];

  return (
    <Paper sx={{ p: 3, borderRadius: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
        <Typography variant="h6" fontWeight={600}>
          Study Materials
        </Typography>
        {(unitName || unitCode) && (
          <Chip
            size="small"
            variant="outlined"
            label={unitCode ? `${unitCode}${unitName ? ` — ${unitName}` : ''}` : unitName}
          />
        )}
      </Box>
      <Typography variant="caption" color="textSecondary" display="block" sx={{ mb: 2 }}>
        Lecture notes, handouts, presentations and reference documents for this unit.
      </Typography>
      <Divider sx={{ mb: 2 }} />

      {canUpload && (
        <Box sx={{ mb: 3 }}>
          <Typography variant="subtitle2" fontWeight={600} gutterBottom>
            Upload material
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                size="small"
                label="Title"
                required
                value={title}
                onChange={(e) => setTitle(e.target.value)}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                size="small"
                label="Description (optional)"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </Grid>
            <Grid item xs={12}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                <Button
                  variant="outlined"
                  component="label"
                  startIcon={<UploadIcon />}
                  disabled={uploadMutation.isPending}
                >
                  Select File
                  <Input
                    type="file"
                    inputRef={fileInputRef}
                    onChange={handleFileSelect}
                    sx={{ display: 'none' }}
                    inputProps={{ accept: STUDY_MATERIAL_EXTENSIONS.join(',') }}
                  />
                </Button>
                {selectedFile && (
                  <Typography variant="body2" color="textSecondary">
                    {selectedFile.name} ({formatFileSize(selectedFile.size)})
                  </Typography>
                )}
                <Button
                  variant="contained"
                  onClick={handleUpload}
                  disabled={
                    !selectedFile ||
                    !title.trim() ||
                    uploadMutation.isPending ||
                    deleteMutation.isPending
                  }
                  startIcon={uploadMutation.isPending ? <CircularProgress size={18} /> : <UploadIcon />}
                >
                  {uploadMutation.isPending ? 'Uploading…' : 'Upload'}
                </Button>
              </Box>
            </Grid>
          </Grid>
          {fileError && (
            <Alert severity="error" sx={{ mt: 1 }}>
              {fileError}
            </Alert>
          )}
          {uploadMutation.isError && (
            <Alert severity="error" sx={{ mt: 1 }}>
              {normalizeError(uploadMutation.error).message ||
                'Upload failed. Please check the file and try again.'}
            </Alert>
          )}
          {uploadMutation.isSuccess && (
            <Alert severity="success" sx={{ mt: 1 }}>
              Material uploaded successfully.
            </Alert>
          )}
          {progress !== null && uploadMutation.isPending && (
            <Box sx={{ mt: 1 }}>
              <LinearProgress variant="determinate" value={progress} />
              <Typography variant="caption" color="textSecondary">
                Uploading… {progress}%
              </Typography>
            </Box>
          )}
        </Box>
      )}

      {downloadError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setDownloadError(null)}>
          {downloadError}
        </Alert>
      )}

      {isLoading ? (
        <LinearProgress />
      ) : isError ? (
        <Alert severity="error">
          {normalizeError(error).message || 'You do not have access to materials for this unit.'}
        </Alert>
      ) : materialsList.length === 0 ? (
        <Box sx={{ py: 4, textAlign: 'center' }}>
          <SchoolIcon sx={{ fontSize: 40, color: 'text.secondary', mb: 1 }} />
          <Typography variant="body2" color="textSecondary">
            No study materials have been uploaded for this unit yet.
          </Typography>
        </Box>
      ) : (
        <List>
          {materialsList.map((material) => (
            <ListItem
              key={material.id}
              divider
              secondaryAction={
                <Box sx={{ display: 'flex', gap: 0.5 }}>
                  <Tooltip title="Download">
                    <IconButton
                      size="small"
                      onClick={() => downloadMutation.mutate(material)}
                      disabled={downloadMutation.isPending}
                    >
                      <DownloadIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  {canUpload && (
                    <Tooltip title="Delete">
                      <span>
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => deleteMutation.mutate(material.id)}
                          disabled={deleteMutation.isPending}
                        >
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>
                  )}
                </Box>
              }
            >
              <ListItemAvatar>
                <Avatar sx={{ bgcolor: 'primary.main' }}>
                  <DescriptionIcon />
                </Avatar>
              </ListItemAvatar>
              <ListItemText
                primary={material.title}
                secondary={
                  <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mt: 0.5 }}>
                    <Typography variant="caption" color="textSecondary">
                      {material.originalFileName} • {material.extension.toUpperCase()} •{' '}
                      {formatFileSize(material.fileSizeBytes)}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      Uploaded {new Date(material.uploadedAt).toLocaleDateString()}
                      {material.lecturerName ? ` by ${material.lecturerName}` : ''}
                    </Typography>
                    {material.description && (
                      <Typography variant="caption" color="textSecondary" display="block">
                        {material.description}
                      </Typography>
                    )}
                  </Box>
                }
              />
            </ListItem>
          ))}
        </List>
      )}
    </Paper>
  );
};

export default StudyMaterialsSection;
