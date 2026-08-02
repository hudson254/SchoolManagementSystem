import React from 'react';
import { Box, Typography, Button, Paper, Container } from '@mui/material';
import { Home as HomeIcon, Error as ErrorIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';

export const NotFound: React.FC = () => {
  const navigate = useNavigate();

  return (
    <Container component="main" maxWidth="sm">
      <Box
        sx={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Paper
          elevation={3}
          sx={{
            p: 5,
            textAlign: 'center',
            borderRadius: 2,
            width: '100%',
          }}
        >
          <ErrorIcon
            sx={{
              fontSize: 80,
              color: 'primary.main',
              mb: 2,
            }}
          />
          <Typography variant="h1" fontWeight={700} color="primary">
            404
          </Typography>
          <Typography variant="h4" fontWeight={600} gutterBottom>
            Page Not Found
          </Typography>
          <Typography variant="body1" color="textSecondary" sx={{ mb: 4 }}>
            The page you are looking for does not exist or has been moved.
          </Typography>
          <Button
            variant="contained"
            startIcon={<HomeIcon />}
            onClick={() => navigate('/dashboard')}
            size="large"
          >
            Back to Dashboard
          </Button>
        </Paper>
      </Box>
    </Container>
  );
};