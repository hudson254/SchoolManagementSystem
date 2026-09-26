import React, { useState } from 'react';
import {
  Alert,
  Avatar,
  Box,
  Button,
  CircularProgress,
  Divider,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import ChatBubbleOutlineIcon from '@mui/icons-material/ChatBubbleOutline';
import SendIcon from '@mui/icons-material/Send';
import type { OmsRequestComment } from '../../services/requests.service';
import { normalizeError } from '../../utils/errors';

/** Backend AddRequestCommentCommand validator: Message required, max 2000. */
export const REQUEST_COMMENT_MAX_LENGTH = 2000;

const formatDateTime = (value?: string | null): string => {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleString();
};

interface RequestCommentsPanelProps {
  comments: OmsRequestComment[];
  /** False for roles the Oms.CommentOnRequest policy rejects. */
  canComment: boolean;
  submitting: boolean;
  onAddComment: (message: string) => Promise<unknown>;
}

/**
 * Request discussion thread.
 *
 * Reads come from the detail payload and writes go exclusively to
 * POST /oms/requests/{id}/comments. There is intentionally no second comment
 * store: the server remains the single source of truth.
 */
export const RequestCommentsPanel: React.FC<RequestCommentsPanelProps> = ({
  comments,
  canComment,
  submitting,
  onAddComment,
}) => {
  const [message, setMessage] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setApiError(null);

    const trimmed = message.trim();
    if (trimmed.length === 0) {
      setValidationError('Comment message is required.');
      return;
    }
    if (trimmed.length > REQUEST_COMMENT_MAX_LENGTH) {
      setValidationError(
        `Comment must be ${REQUEST_COMMENT_MAX_LENGTH} characters or fewer.`,
      );
      return;
    }

    setValidationError(null);
    try {
      await onAddComment(trimmed);
      // Only clear the box once the server has accepted the comment.
      setMessage('');
    } catch (error) {
      const normalized = normalizeError(error);
      setApiError(normalized.serverMessage || normalized.message);
    }
  };

  return (
    <Paper sx={{ p: 2.5 }} data-testid="request-comments">
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        <ChatBubbleOutlineIcon fontSize="small" color="action" />
        <Typography variant="h6" fontWeight={600}>
          Comments
        </Typography>
        <Typography variant="caption" color="textSecondary">
          ({comments.length})
        </Typography>
      </Box>

      {comments.length === 0 ? (
        <Alert severity="info" sx={{ mb: 2 }}>
          No comments yet. Comments added here notify the requester through the
          existing in-app notification pipeline.
        </Alert>
      ) : (
        <Stack spacing={1.5} divider={<Divider flexItem />} sx={{ mb: 2 }}>
          {comments.map((comment) => (
            <Box key={comment.id} sx={{ display: 'flex', gap: 1.5 }}>
              <Avatar sx={{ width: 32, height: 32, bgcolor: '#576426', fontSize: '0.8rem' }}>
                {(comment.authorUserId || '?').charAt(0).toUpperCase()}
              </Avatar>
              <Box sx={{ minWidth: 0, flex: 1 }}>
                <Typography variant="body2" fontWeight={600}>
                  {comment.authorUserId}
                </Typography>
                <Typography variant="caption" color="textSecondary" display="block">
                  {formatDateTime(comment.createdAt)}
                </Typography>
                <Typography variant="body2" sx={{ mt: 0.5, whiteSpace: 'pre-wrap' }}>
                  {comment.message}
                </Typography>
              </Box>
            </Box>
          ))}
        </Stack>
      )}

      {apiError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setApiError(null)}>
          {apiError}
        </Alert>
      )}

      {canComment ? (
        <Box component="form" onSubmit={handleSubmit} noValidate>
          <TextField
            label="Add Comment"
            placeholder="Add context for the approver or requester…"
            value={message}
            onChange={(event) => setMessage(event.target.value)}
            multiline
            minRows={3}
            fullWidth
            size="small"
            error={!!validationError}
            helperText={
              validationError ||
              `${message.length}/${REQUEST_COMMENT_MAX_LENGTH} characters`
            }
            inputProps={{ 'aria-label': 'Add Comment' }}
          />
          <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 1.5 }}>
            <Button
              type="submit"
              variant="contained"
              startIcon={submitting ? <CircularProgress size={16} color="inherit" /> : <SendIcon />}
              disabled={submitting}
            >
              {submitting ? 'Posting…' : 'Post Comment'}
            </Button>
          </Box>
        </Box>
      ) : (
        <Alert severity="warning">
          You do not have permission to comment on this request.
        </Alert>
      )}
    </Paper>
  );
};
