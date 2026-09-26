import React from 'react';
import { Box, Paper, Typography, Divider, Stack, Alert, Skeleton, Tooltip } from '@mui/material';
import HistoryIcon from '@mui/icons-material/History';
import type { OmsRequestStatusHistoryEntry } from '../../services/requests.service';
import { omsRequestStatusLabel, omsRequestActionLabel } from '../../utils/omsRequestLifecycle';
import { OmsRequestStatusChip } from './RequestChips';

const formatDateTime = (value?: string | null): string => {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleString();
};

const formatActor = (entry: OmsRequestStatusHistoryEntry): string =>
  entry.performedByUsername || entry.performedByUserId || 'System';

/**
 * Append-only status history, rendered newest-first.
 *
 * The rows come straight from the server (`RequestStatusHistoryDto`); the
 * component never derives or interpolates a transition that is not recorded.
 */
export const RequestStatusHistoryPanel: React.FC<{
  entries: OmsRequestStatusHistoryEntry[];
  loading?: boolean;
}> = ({ entries, loading = false }) => {
  if (loading) {
    return (
      <Paper sx={{ p: 2.5 }} data-testid="request-history-loading">
        <Skeleton variant="text" width="40%" />
        <Skeleton variant="text" />
        <Skeleton variant="text" width="70%" />
      </Paper>
    );
  }

  if (entries.length === 0) {
    return (
      <Paper sx={{ p: 2.5 }} data-testid="request-history-empty">
        <Typography variant="h6" fontWeight={600} gutterBottom>
          Status History
        </Typography>
        <Alert severity="info">
          No status changes have been recorded for this request yet.
        </Alert>
      </Paper>
    );
  }

  // The server returns history in ascending order; show the newest first.
  const ordered = [...entries].sort(
    (a, b) => new Date(b.performedAtUtc).getTime() - new Date(a.performedAtUtc).getTime(),
  );

  return (
    <Paper sx={{ p: 2.5 }} data-testid="request-history">
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
        <HistoryIcon fontSize="small" color="action" />
        <Typography variant="h6" fontWeight={600}>
          Status History
        </Typography>
      </Box>
      <Typography variant="caption" color="textSecondary">
        Append-only audit trail maintained by the server.
      </Typography>
      <Divider sx={{ my: 2 }} />
      <Stack spacing={0} divider={<Divider flexItem />}>
        {ordered.map((entry) => (
          <Box key={entry.id} sx={{ py: 1.5 }}>
            <Box
              sx={{
                display: 'flex',
                flexWrap: 'wrap',
                alignItems: 'center',
                gap: 1,
              }}
            >
              <OmsRequestStatusChip status={entry.toStatus} />
              <Typography variant="body2" fontWeight={500}>
                {omsRequestActionLabel(entry.action)}
              </Typography>
              {entry.fromStatus && (
                <Tooltip title={`From ${omsRequestStatusLabel(entry.fromStatus)}`}>
                  <Typography variant="caption" color="textSecondary">
                    from {omsRequestStatusLabel(entry.fromStatus)}
                  </Typography>
                </Tooltip>
              )}
            </Box>
            <Typography variant="caption" color="textSecondary" display="block" sx={{ mt: 0.5 }}>
              {formatActor(entry)} • {formatDateTime(entry.performedAtUtc)}
            </Typography>
            {entry.reason && (
              <Typography
                variant="body2"
                sx={{
                  mt: 1,
                  p: 1,
                  bgcolor: 'action.hover',
                  borderRadius: 1,
                  whiteSpace: 'pre-wrap',
                }}
              >
                {entry.reason}
              </Typography>
            )}
          </Box>
        ))}
      </Stack>
    </Paper>
  );
};
