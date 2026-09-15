import React from 'react';
import {
  Box,
  Paper,
  Typography,
  Chip,
  Divider,
  Button,
  Grid,
  LinearProgress,
  Alert,
  List,
  ListItem,
  ListItemText,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Description as DescriptionIcon,
  Download as DownloadIcon,
  Delete as DeleteIcon,
  AttachFile as AttachFileIcon,
} from '@mui/icons-material';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  assignmentService,
  ASSIGNMENT_DOCUMENT_EXTENSIONS,
} from '../services/assignment.service';
import { saveBlob, formatFileSize } from '../services/studyMaterial.service';
import { useAuth } from '../hooks/useAuth';
import { hasAnyRole, LECTURER, COORDINATOR, STUDENT } from '../utils/roles';
import { normalizeError } from '../utils/errors';

export const AssignmentDetailPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const canManageAssignments = hasAnyRole(user?.roles, LECTURER, COORDINATOR);

  const { data: assignment, isLoading, isError, refetch } = useQuery({
    queryKey: ['assignment', id],
    queryFn: () => assignmentService.getAssignment(id!),
    enabled: !!id,
  });

  // Question documents attached to this assignment. Access is enforced
  // server-side; a 403 surfaces as an empty/denied state here.
  const documentsQuery = useQuery({
    queryKey: ['assignment-documents', id],
    queryFn: () => assignmentService.getDocuments(id!),
    enabled: !!id,
    retry: false,
  });

  const downloadMutation = useMutation({
    mutationFn: async (fileId: string) => {
      const doc = (documentsQuery.data || []).find((d) => d.id === fileId);
      const blob = await assignmentService.downloadDocument(id!, fileId);
      saveBlob(blob, doc?.originalFileName || 'assignment-document');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (fileId: string) => assignmentService.deleteDocument(id!, fileId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['assignment-documents', id] });
    },
  });

  const documents = Array.isArray(documentsQuery.data) ? documentsQuery.data : [];

  if (isLoading) {
    return (
      <Box sx={{ p: 3 }}>
        <LinearProgress />
      </Box>
    );
  }

  if (isError) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">
          Failed to load assignment details. Please try again.
          <Button size="small" onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const a = assignment;

  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 4, maxWidth: 900, mx: 'auto' }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h5" fontWeight={600}>
            Assignment Details
          </Typography>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button variant="outlined" onClick={() => navigate('/assignments')}>
              Back to Assignments
            </Button>
            {canManageAssignments && (
              <Button
                variant="contained"
                onClick={() => navigate(`/assignments/${id}/edit`)}
              >
                Edit Assignment
              </Button>
            )}
          </Box>
        </Box>
        <Divider sx={{ mb: 2 }} />

        {!a ? (
          <Alert severity="warning">Assignment not found.</Alert>
        ) : (
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <Typography variant="h6" fontWeight={700}>
                {a.title}
              </Typography>
              <Chip
                label={a.status}
                color={a.status === 'Published' ? 'info' : a.status === 'Open' ? 'success' : 'default'}
                size="small"
                sx={{ ml: 1 }}
              />
            </Grid>
            <Grid item xs={12}>
              <Typography variant="body2" color="textSecondary">
                {a.description}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Unit
              </Typography>
              <Typography variant="body1">
                {a.unitName} ({a.unitCode})
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Lecturer
              </Typography>
              <Typography variant="body1">{a.lecturerName}</Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Semester
              </Typography>
              <Typography variant="body1">{a.semesterName}</Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Due Date
              </Typography>
              <Typography variant="body1">
                {a.dueDate ? new Date(a.dueDate).toLocaleString() : '—'}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Closing Date
              </Typography>
              <Typography variant="body1">
                {a.closingDate ? new Date(a.closingDate).toLocaleString() : '—'}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Weight / Max Score
              </Typography>
              <Typography variant="body1">
                {a.weight}% / {a.maxScore}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Submissions
              </Typography>
              <Typography variant="body1">
                {a.submissionCount || 0} submitted
                {a.gradedCount !== undefined && ` • ${a.gradedCount} graded`}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Late Submission
              </Typography>
              <Typography variant="body1">
                {a.allowLateSubmission
                  ? `Allowed (${a.latePenaltyPercent}% penalty)`
                  : 'Not allowed'}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6}>
              <Typography variant="caption" color="textSecondary" display="block">
                Graded Automatically
              </Typography>
              <Typography variant="body1">{a.isGraded ? 'Yes' : 'No'}</Typography>
            </Grid>
            {a.instructions && (
              <Grid item xs={12}>
                <Typography variant="caption" color="textSecondary" display="block">
                  Instructions
                </Typography>
                <Typography variant="body2" whiteSpace="preWrap">
                  {a.instructions}
                </Typography>
              </Grid>
            )}
          </Grid>
        )}

        {/* Question Documents */}
        {a && (
          <>
            <Divider sx={{ my: 3 }} />
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="h6" fontWeight={600}>
                Question Documents
              </Typography>
              {canManageAssignments && (
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={<AttachFileIcon />}
                  onClick={() => navigate(`/assignments/${id}/edit`)}
                >
                  Add Document
                </Button>
              )}
            </Box>
            {documentsQuery.isLoading ? (
              <LinearProgress />
            ) : documentsQuery.isError ? (
              <Alert severity="warning" sx={{ mt: 1 }}>
                {normalizeError(documentsQuery.error).message ||
                  'You do not have access to the documents of this assignment.'}
              </Alert>
            ) : documents.length === 0 ? (
              <Typography variant="body2" color="textSecondary" sx={{ py: 1 }}>
                No question document has been attached to this assignment
                {canManageAssignments ? '. Use "Add Document" to upload one.' : ' yet.'}
              </Typography>
            ) : (
              <List dense>
                {documents.map((doc) => (
                  <ListItem
                    key={doc.id}
                    divider
                    secondaryAction={
                      <Box sx={{ display: 'flex', gap: 0.5 }}>
                        <Tooltip title="Download">
                          <IconButton
                            size="small"
                            onClick={() => downloadMutation.mutate(doc.id)}
                            disabled={downloadMutation.isPending}
                          >
                            <DownloadIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        {canManageAssignments && (
                          <Tooltip title="Delete">
                            <IconButton
                              size="small"
                              color="error"
                              onClick={() => deleteMutation.mutate(doc.id)}
                              disabled={deleteMutation.isPending}
                            >
                              <DeleteIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        )}
                      </Box>
                    }
                  >
                    <Grid container sx={{ pr: 8 }}>
                      <Grid item xs={12} sm={1}>
                        <DescriptionIcon color="action" />
                      </Grid>
                      <Grid item xs={12} sm={11}>
                        <ListItemText
                          primary={doc.originalFileName}
                          secondary={
                            <Typography variant="caption" color="textSecondary">
                              {doc.extension ? `${doc.extension.toUpperCase()} • ` : ''}
                              {formatFileSize(doc.fileSizeBytes)} • uploaded{' '}
                              {new Date(doc.uploadedAt).toLocaleDateString()}
                              {doc.uploadedByUsername ? ` by ${doc.uploadedByUsername}` : ''}
                            </Typography>
                          }
                        />
                      </Grid>
                    </Grid>
                  </ListItem>
                ))}
              </List>
            )}
            <Typography variant="caption" color="textSecondary" display="block" sx={{ mt: 1 }}>
              Supported types: {ASSIGNMENT_DOCUMENT_EXTENSIONS.join(', ')}
            </Typography>
          </>
        )}
      </Paper>
    </Box>
  );
};