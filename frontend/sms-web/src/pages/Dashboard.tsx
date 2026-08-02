import React from 'react';
import {
  Grid,
  Paper,
  Typography,
  Box,
  Card,
  CardContent,
  Button,
  List,
  ListItem,
  ListItemText,
  ListItemAvatar,
  Avatar,
  Chip,
  LinearProgress,
} from '@mui/material';
import {
  People,
  School,
  Book,
  Assignment,
  TrendingUp,
  TrendingDown,
  PersonAdd,
  Event,
  Dashboard as DashboardIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { dashboardService } from '../services/dashboard.service';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';

export const Dashboard: React.FC = () => {
  const { data: statistics, isLoading: statsLoading } = useQuery({
    queryKey: ['dashboardStatistics'],
    queryFn: dashboardService.getStatistics,
  });

  const { data: activities, isLoading: activitiesLoading } = useQuery({
    queryKey: ['recentActivities'],
    queryFn: () => dashboardService.getRecentActivities(10),
  });

  const { data: upcomingEvents, isLoading: eventsLoading } = useQuery({
    queryKey: ['upcomingEvents'],
    queryFn: () => dashboardService.getUpcomingEvents(30),
  });

  if (statsLoading) {
    return <LoadingSpinner />;
  }

  const stats = [
    {
      title: 'Total Students',
      value: statistics?.totalStudents || 0,
      icon: <People sx={{ fontSize: 32 }} />,
      color: '#576426',
      change: '+12%',
      trend: 'up',
    },
    {
      title: 'Total Lecturers',
      value: statistics?.totalLecturers || 0,
      icon: <School sx={{ fontSize: 32 }} />,
      color: '#1976d2',
      change: '+5%',
      trend: 'up',
    },
    {
      title: 'Active Courses',
      value: statistics?.activeCourses || 0,
      icon: <Book sx={{ fontSize: 32 }} />,
      color: '#ed6c02',
      change: '+8%',
      trend: 'up',
    },
    {
      title: 'Pending Assignments',
      value: statistics?.pendingAssignments || 0,
      icon: <Assignment sx={{ fontSize: 32 }} />,
      color: '#d32f2f',
      change: '-3%',
      trend: 'down',
    },
  ];

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 4 }}>
        <Typography variant="h4" fontWeight={600}>
          Dashboard
        </Typography>
        <Typography variant="body2" color="textSecondary">
          Last updated: {new Date().toLocaleString()}
        </Typography>
      </Box>

      <Grid container spacing={3}>
        {stats.map((stat, index) => (
          <Grid item xs={12} sm={6} md={3} key={index}>
            <Card
              sx={{
                height: '100%',
                position: 'relative',
                overflow: 'visible',
                '&:hover': {
                  boxShadow: 6,
                  transform: 'translateY(-4px)',
                  transition: 'all 0.3s ease',
                },
              }}
            >
              <CardContent>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <Box>
                    <Typography variant="caption" color="textSecondary" fontWeight={500}>
                      {stat.title}
                    </Typography>
                    <Typography variant="h4" fontWeight={700} sx={{ mt: 1 }}>
                      {stat.value}
                    </Typography>
                  </Box>
                  <Avatar
                    sx={{
                      bgcolor: stat.color,
                      width: 56,
                      height: 56,
                    }}
                  >
                    {stat.icon}
                  </Avatar>
                </Box>
                <Box sx={{ display: 'flex', alignItems: 'center', mt: 2 }}>
                  {stat.trend === 'up' ? (
                    <TrendingUp sx={{ color: 'success.main', fontSize: 16, mr: 0.5 }} />
                  ) : (
                    <TrendingDown sx={{ color: 'error.main', fontSize: 16, mr: 0.5 }} />
                  )}
                  <Typography
                    variant="caption"
                    color={stat.trend === 'up' ? 'success.main' : 'error.main'}
                  >
                    {stat.change}
                  </Typography>
                  <Typography variant="caption" color="textSecondary" sx={{ ml: 1 }}>
                    vs last month
                  </Typography>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Grid container spacing={3} sx={{ mt: 2 }}>
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3, borderRadius: 2 }}>
            <Typography variant="h6" fontWeight={600} sx={{ mb: 2 }}>
              Recent Activity
            </Typography>
            {activitiesLoading ? (
              <LinearProgress />
            ) : (
              <List>
                {activities?.map((activity: any, index: number) => (
                  <ListItem key={index} divider={index < activities.length - 1}>
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: activity.color || '#576426' }}>
                        {activity.icon === 'student' && <PersonAdd />}
                        {activity.icon === 'event' && <Event />}
                        {activity.icon === 'assignment' && <Assignment />}
                        {!activity.icon && <DashboardIcon />}
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText
                      primary={activity.message}
                      secondary={
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mt: 0.5 }}>
                          <Typography variant="caption" color="textSecondary">
                            {activity.user}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            {new Date(activity.timestamp).toLocaleString()}
                          </Typography>
                          {activity.status && (
                            <Chip
                              label={activity.status}
                              size="small"
                              color={activity.status === 'Completed' ? 'success' : 'warning'}
                            />
                          )}
                        </Box>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            )}
          </Paper>
        </Grid>

        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3, borderRadius: 2 }}>
            <Typography variant="h6" fontWeight={600} sx={{ mb: 2 }}>
              Upcoming Events
            </Typography>
            {eventsLoading ? (
              <LinearProgress />
            ) : (
              <List>
                {upcomingEvents?.map((event: any, index: number) => (
                  <ListItem key={index} divider={index < upcomingEvents.length - 1}>
                    <Box sx={{ width: '100%' }}>
                      <Typography variant="body2" fontWeight={500}>
                        {event.title}
                      </Typography>
                      <Typography variant="caption" color="textSecondary">
                        {new Date(event.date).toLocaleDateString()} • {event.time}
                      </Typography>
                      {event.location && (
                        <Typography variant="caption" color="textSecondary" display="block">
                          📍 {event.location}
                        </Typography>
                      )}
                    </Box>
                  </ListItem>
                ))}
              </List>
            )}
            <Box sx={{ mt: 2, textAlign: 'center' }}>
              <Button variant="outlined" size="small" fullWidth>
                View Full Calendar
              </Button>
            </Box>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};