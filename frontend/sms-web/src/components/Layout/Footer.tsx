import React from 'react';
import { Box, Typography, Container, Link } from '@mui/material';

export const Footer: React.FC = () => {
  const currentYear = new Date().getFullYear();

  return (
    <Box
      component="footer"
      sx={{
        py: 3,
        px: 2,
        mt: 'auto',
        backgroundColor: (theme) =>
          theme.palette.mode === 'light'
            ? theme.palette.grey[100]
            : theme.palette.grey[900],
        borderTop: '1px solid',
        borderColor: 'divider',
      }}
    >
      <Container maxWidth="lg">
        <Box
          sx={{
            display: 'flex',
            flexDirection: { xs: 'column', sm: 'row' },
            justifyContent: 'space-between',
            alignItems: 'center',
            gap: 2,
          }}
        >
          <Typography variant="body2" color="textSecondary" align="center">
            © {currentYear} School Management System. All rights reserved.
          </Typography>
          <Box sx={{ display: 'flex', gap: 3 }}>
            <Link href="#" variant="body2" color="textSecondary">
              Privacy Policy
            </Link>
            <Link href="#" variant="body2" color="textSecondary">
              Terms of Service
            </Link>
            <Link href="#" variant="body2" color="textSecondary">
              Support
            </Link>
          </Box>
        </Box>
      </Container>
    </Box>
  );
};