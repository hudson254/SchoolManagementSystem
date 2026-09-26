import React from 'react';
import { Chip, ChipProps, Tooltip } from '@mui/material';
import { OmsRequestStatus, OmsRequestPriority } from '../../services/requests.service';
import { omsRequestStatusLabel, omsRequestPriorityLabel } from '../../utils/omsRequestLifecycle';

/**
 * Status colours, consistent with the existing SMS StatusBadge palette so the
 * request workspace does not introduce a second visual language.
 */
const STATUS_COLORS: Record<string, ChipProps['color']> = {
  [OmsRequestStatus.Draft]: 'default',
  [OmsRequestStatus.Submitted]: 'info',
  [OmsRequestStatus.PendingReview]: 'warning',
  [OmsRequestStatus.Assigned]: 'info',
  [OmsRequestStatus.PendingApproval]: 'warning',
  [OmsRequestStatus.Approved]: 'success',
  [OmsRequestStatus.Rejected]: 'error',
  [OmsRequestStatus.Returned]: 'warning',
  [OmsRequestStatus.InProgress]: 'info',
  [OmsRequestStatus.Completed]: 'success',
  [OmsRequestStatus.Cancelled]: 'default',
  [OmsRequestStatus.OnHold]: 'default',
  [OmsRequestStatus.Escalated]: 'error',
};

export const omsRequestStatusColor = (status: string): ChipProps['color'] =>
  STATUS_COLORS[status] ?? 'default';

export const OmsRequestStatusChip: React.FC<{ status: string }> = ({ status }) => (
  <Chip
    label={omsRequestStatusLabel(status)}
    color={omsRequestStatusColor(status)}
    size="small"
    variant="filled"
    sx={{ fontWeight: 500 }}
  />
);

const PRIORITY_COLORS: Record<number, ChipProps['color']> = {
  [OmsRequestPriority.Low]: 'default',
  [OmsRequestPriority.Normal]: 'info',
  [OmsRequestPriority.High]: 'warning',
  [OmsRequestPriority.Urgent]: 'error',
};

export const OmsRequestPriorityChip: React.FC<{
  priority: OmsRequestPriority;
  showUrgentIcon?: boolean;
}> = ({ priority, showUrgentIcon = true }) => (
  <Tooltip title={`Priority: ${omsRequestPriorityLabel(priority)}`}>
    <Chip
      label={omsRequestPriorityLabel(priority)}
      color={PRIORITY_COLORS[priority] ?? 'default'}
      size="small"
      variant="outlined"
      aria-label={`Priority ${omsRequestPriorityLabel(priority)}`}
      sx={{ fontWeight: 500 }}
    />
  </Tooltip>
);
