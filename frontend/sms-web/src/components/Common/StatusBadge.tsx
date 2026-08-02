import React from 'react';
import { Chip, ChipProps } from '@mui/material';

interface StatusBadgeProps extends Omit<ChipProps, 'color'> {
  status: string;
  colorMap?: Record<string, ChipProps['color']>;
}

const defaultColorMap: Record<string, ChipProps['color']> = {
  Active: 'success',
  Inactive: 'default',
  Pending: 'warning',
  Completed: 'success',
  Failed: 'error',
  Published: 'info',
  Draft: 'default',
  Open: 'success',
  Closed: 'warning',
  Archived: 'default',
  Verified: 'success',
  Unverified: 'warning',
  Available: 'success',
  Occupied: 'warning',
  Maintenance: 'error',
  Reserved: 'info',
  Enrolled: 'success',
  Dropped: 'error',
  InProgress: 'info',
  Graduated: 'success',
  Suspended: 'warning',
  Withdrawn: 'error',
  Probation: 'warning',
  Present: 'success',
  Absent: 'error',
  Late: 'warning',
  Excused: 'info',
  Submitted: 'info',
  Graded: 'success',
  'Not Graded': 'default',
};

export const StatusBadge: React.FC<StatusBadgeProps> = ({
  status,
  colorMap = defaultColorMap,
  ...chipProps
}) => {
  const color = colorMap[status] || 'default';

  return (
    <Chip
      label={status}
      color={color}
      size="small"
      {...chipProps}
      sx={{
        fontWeight: 500,
        ...chipProps.sx,
      }}
    />
  );
};