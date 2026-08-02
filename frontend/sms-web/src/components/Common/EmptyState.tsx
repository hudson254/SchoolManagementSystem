import React from 'react';
import { Box, Typography, Button, SvgIconProps } from '@mui/material';
import { Inbox as InboxIcon } from '@mui/icons-material';

interface EmptyStateProps {
  title?: string;
  description?: string;
  icon?: React.ReactElement<SvgIconProps>;
  actionText?: string;
  onAction?: () => void;
  actionIcon?: React.ReactElement;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  title = 'No data available',
  description = 'There is no data to display at this time.',
  icon = <InboxIcon sx={{ fontSize: 64, color: 'text.disabled' }} />,
  actionText,
  onAction,
  actionIcon,
}) => {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        py: 8,
        px: 3,
        textAlign: 'center',
      }}
    >
      <Box sx={{ mb: 2, color: 'text.disabled' }}>{icon}</Box>
      <Typography variant="h6" color="textSecondary" gutterBottom>
        {title}
      </Typography>
      <Typography variant="body2" color="textSecondary" sx={{ maxWidth: 400, mb: 3 }}>
        {description}
      </Typography>
      {actionText && onAction && (
        <Button
          variant="contained"
          startIcon={actionIcon}
          onClick={onAction}
        >
          {actionText}
        </Button>
      )}
    </Box>
  );
};