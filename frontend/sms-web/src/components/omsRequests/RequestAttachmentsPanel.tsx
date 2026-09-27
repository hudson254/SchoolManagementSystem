import React, { useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  IconButton,
  List,
  ListItem,
  ListItemText,
  Paper,
  Tooltip,
  Typography,
} from '@mui/material';
// Named barrel import — see the note in RequestsDashboard.tsx for why deep
// default imports of MUI icons break the production bundle.
import {
  AttachFile as AttachFileIcon,
  DeleteOutline as DeleteOutlineIcon,
  Download as DownloadIcon,
  UploadFile as UploadFileIcon,
} from '@mui/icons-material';
import type { OmsRequestAttachment } from '../../services/requests.service';
import {
  OMS_REQUEST_ATTACHMENT_EXTENSIONS,
  OMS_REQUEST_ATTACHMENT_MAX_BYTES,
} from '../../services/requests.service';
import { normalizeError } from '../../utils/errors';

export const formatAttachmentBytes = (bytes: number): string => {
  if (!Number.isFinite(bytes) || bytes < 0) return '—';
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

const formatDateTime = (value?: string | null): string => {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleString();
};

/**
 * Mirrors RequestAttachmentPolicy on the client so the user gets immediate
 * feedback. The server re-validates extension and size on every upload — these
 * checks are a convenience, never an authorization decision.
 */
export const validateOmsRequestAttachment = (file: File): string | null => {
  if (file.size <= 0) return 'An empty file cannot be attached.';

  const dot = file.name.lastIndexOf('.');
  const extension = dot >= 0 ? file.name.slice(dot).toLowerCase() : '';
  if (!(OMS_REQUEST_ATTACHMENT_EXTENSIONS as readonly string[]).includes(extension)) {
    return `File type "${extension || 'unknown'}" is not permitted. Allowed types: ${OMS_REQUEST_ATTACHMENT_EXTENSIONS.join(', ')}.`;
  }

  if (file.size > OMS_REQUEST_ATTACHMENT_MAX_BYTES) {
    return `File is ${formatAttachmentBytes(file.size)}. The maximum attachment size is ${formatAttachmentBytes(OMS_REQUEST_ATTACHMENT_MAX_BYTES)}.`;
  }

  return null;
};

interface RequestAttachmentsPanelProps {
  attachments: OmsRequestAttachment[];
  canUpload: boolean;
  canDelete: (attachment: OmsRequestAttachment) => boolean;
  uploading: boolean;
  onUpload: (file: File) => Promise<unknown>;
  onDownload: (attachment: OmsRequestAttachment) => Promise<unknown>;
  onDelete: (attachment: OmsRequestAttachment) => Promise<unknown>;
  /** True when the request is terminal and therefore frozen for attachments. */
  frozen: boolean;
}

/**
 * Request attachments.
 *
 * Security notes:
 *   • No filesystem path or storage key is ever rendered or constructed — the
 *     DTO does not contain one and the UI never invents one.
 *   • Downloads go through the authorized server endpoint; an attachment id is
 *     not treated as a capability by itself, because the server re-checks
 *     object-level access on every call.
 */
export const RequestAttachmentsPanel: React.FC<RequestAttachmentsPanelProps> = ({
  attachments,
  canUpload,
  canDelete,
  uploading,
  onUpload,
  onDownload,
  onDelete,
  frozen,
}) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [clientError, setClientError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const handleFileSelected = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    // Reset immediately so re-selecting the same file still fires a change event.
    event.target.value = '';
    if (!file) return;

    const validation = validateOmsRequestAttachment(file);
    if (validation) {
      setClientError(validation);
      return;
    }

    setClientError(null);
    setActionError(null);
    try {
      await onUpload(file);
    } catch (error) {
      const normalized = normalizeError(error);
      setActionError(normalized.serverMessage || normalized.message);
    }
  };

  const runAction = async (
    attachment: OmsRequestAttachment,
    action: 'download' | 'delete',
  ) => {
    setActionError(null);
    setBusyId(attachment.id);
    try {
      if (action === 'download') {
        await onDownload(attachment);
      } else {
        await onDelete(attachment);
      }
    } catch (error) {
      const normalized = normalizeError(error);
      const message = normalized.serverMessage || normalized.message;
      setActionError(
        action === 'download'
          ? `Could not download "${attachment.fileName}": ${message}`
          : `Could not delete "${attachment.fileName}": ${message}`,
      );
    } finally {
      setBusyId(null);
    }
  };

  return (
    <Paper sx={{ p: 2.5 }} data-testid="request-attachments">
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
        <AttachFileIcon fontSize="small" color="action" />
        <Typography variant="h6" fontWeight={600}>
          Attachments
        </Typography>
        <Typography variant="caption" color="textSecondary">
          ({attachments.length})
        </Typography>
      </Box>
      <Typography variant="caption" color="textSecondary">
        Maximum {formatAttachmentBytes(OMS_REQUEST_ATTACHMENT_MAX_BYTES)} per file. Stored
        server-side; files are only ever read back through the authorized download endpoint.
      </Typography>

      {frozen && (
        <Alert severity="info" sx={{ mt: 2 }}>
          This request has reached a terminal status. Its attachments are frozen and can no
          longer be added or removed.
        </Alert>
      )}

      {clientError && (
        <Alert severity="error" sx={{ mt: 2 }} onClose={() => setClientError(null)}>
          {clientError}
        </Alert>
      )}
      {actionError && (
        <Alert severity="error" sx={{ mt: 2 }} onClose={() => setActionError(null)}>
          {actionError}
        </Alert>
      )}

      {attachments.length > 0 && (
        <List dense sx={{ mt: 1 }} data-testid="request-attachment-list">
          {attachments.map((attachment) => {
            const deletable = canDelete(attachment);
            const busy = busyId === attachment.id;
            return (
              <React.Fragment key={attachment.id}>
                <ListItem
                  alignItems="flex-start"
                  disableGutters
                  secondaryAction={
                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                      {busy && <CircularProgress size={16} sx={{ mr: 1 }} />}
                      <Tooltip title="Download">
                        <span>
                          <IconButton
                            edge="end"
                            aria-label={`Download ${attachment.fileName}`}
                            onClick={() => runAction(attachment, 'download')}
                            disabled={busy}
                          >
                            <DownloadIcon fontSize="small" />
                          </IconButton>
                        </span>
                      </Tooltip>
                      {deletable && (
                        <Tooltip title="Delete attachment">
                          <span>
                            <IconButton
                              edge="end"
                              aria-label={`Delete ${attachment.fileName}`}
                              onClick={() => runAction(attachment, 'delete')}
                              disabled={busy}
                              color="error"
                            >
                              <DeleteOutlineIcon fontSize="small" />
                            </IconButton>
                          </span>
                        </Tooltip>
                      )}
                    </Box>
                  }
                >
                  <ListItemText
                    primary={attachment.fileName}
                    secondary={
                      <>
                        {formatAttachmentBytes(attachment.size)} •{' '}
                        {attachment.uploadedByUserName || attachment.uploadedByUserId} •{' '}
                        {formatDateTime(attachment.createdAt)}
                      </>
                    }
                  />
                </ListItem>
                <Divider component="li" />
              </React.Fragment>
            );
          })}
        </List>
      )}

      {attachments.length === 0 && !frozen && (
        <Alert severity="info" sx={{ mt: 2 }}>
          No attachments have been uploaded to this request.
        </Alert>
      )}

      {canUpload && !frozen && (
        <>
          <input
            ref={inputRef}
            type="file"
            hidden
            aria-label="Upload attachment"
            onChange={handleFileSelected}
          />
          <Box sx={{ mt: 2 }}>
            <Button
              variant="outlined"
              startIcon={
                uploading ? <CircularProgress size={16} color="inherit" /> : <UploadFileIcon />
              }
              onClick={() => inputRef.current?.click()}
              disabled={uploading}
            >
              {uploading ? 'Uploading…' : 'Upload Attachment'}
            </Button>
          </Box>
        </>
      )}

      {!canUpload && !frozen && (
        <Alert severity="warning" sx={{ mt: 2 }}>
          You do not have permission to attach files to this request.
        </Alert>
      )}
    </Paper>
  );
};
