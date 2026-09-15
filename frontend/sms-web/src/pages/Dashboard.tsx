import React, { useState } from 'react';
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
  Alert,
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
  MenuBook,
  CheckCircle,
  ReportProblem,
  Schedule,
  Apartment,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { dashboardService } from '../services/dashboard.service';
import type { DashboardUnit, LecturerCourse, StudentCourse } from '../services/dashboard.service';
import { courseOfferingService, CourseOffering, CourseOfferingStatus, ConfirmationStatus } from '../services/course-offering.service';
import { confirmationService, PendingEnrollment } from '../services/confirmation.service';
import { AssignmentConfirm } from '../components/AssignmentConfirm';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { useAuth } from '../hooks/useAuth';
import { hasAnyRole, SYSTEM_ADMINISTRATOR, ADMINISTRATOR, COORDINATOR } from '../utils/roles';

export const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [selectedPending, setSelectedPending] = useState<PendingEnrollment | null>(null);
  const [confirmType, setConfirmType] = useState<'enrollment' | 'teaching'>('enrollment');

  const isStudent = user?.roles?.includes('Student') || user?.roles?.includes('student');
  const isLecturer = user?.roles?.includes('Lecturer') || user?.roles?.includes('lecturer');
  const isAdminOrModerator = hasAnyRole(user?.roles, SYSTEM_ADMINISTRATOR, ADMINISTRATOR, COORDINATOR);

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

  // Fetch course offerings for admins/moderators
  const { data: offeringsData, isLoading: offeringsLoading } = useQuery({
    queryKey: ['dashboard-course-offerings'],
    queryFn: () => courseOfferingService.getCourseOfferings({ includeInactive: true }),
    enabled: !!isAdminOrModerator,
  });

  // Fetch student's pending enrollments
  const { data: pendingEnrollments, isLoading: pendingEnrollmentsLoading } = useQuery({
    queryKey: ['pending-enrollments', user?.id],
    queryFn: () => confirmationService.getPendingEnrollments(user?.id || ''),
    enabled: !!isStudent && !!user?.id,
  });

  // Fetch student's active/history enrollments
  const { data: studentEnrollments, isLoading: studentEnrollmentsLoading } = useQuery({
    queryKey: ['student-course-enrollments', user?.id],
    queryFn: () => confirmationService.getPendingEnrollments(user?.id || ''),
    enabled: !!isStudent && !!user?.id,
  });

  // Lecturer: real teaching dashboard (courses, units, accommodation) resolved
  // server-side from persisted relationships.
  const { data: lecturerMe, isLoading: lecturerMeLoading } = useQuery({
    queryKey: ['dashboard-lecturer-me'],
    queryFn: dashboardService.getMyLecturerDashboard,
    enabled: !!isLecturer,
    retry: false,
  });

  // Student: real enrolled courses (with units) and accommodation assignment.
  const { data: studentMe, isLoading: studentMeLoading } = useQuery({
    queryKey: ['dashboard-student-me'],
    queryFn: dashboardService.getMyStudentDashboard,
    enabled: !!isStudent,
    retry: false,
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

  const offerings = offeringsData?.items || [];
  const activeOfferings = offerings.filter(o => o.status === CourseOfferingStatus.Active);
  const upcomingOfferings = offerings.filter(o => o.status === CourseOfferingStatus.Scheduled);
  const completedOfferings = offerings.filter(o => o.status === CourseOfferingStatus.Completed);

  const handleConfirmClick = (pending: PendingEnrollment, type: 'enrollment' | 'teaching') => {
    setSelectedPending(pending);
    setConfirmType(type);
    setConfirmOpen(true);
  };

  const renderOfferingCard = (offering: CourseOffering) => (
    <Card
      key={offering.id}
      sx={{
        mb: 1.5,
        cursor: 'pointer',
        '&:hover': { boxShadow: 3, transform: 'translateY(-2px)', transition: 'all 0.2s ease' },
      }}
      onClick={() => navigate(`/course-offerings/${offering.id}`)}
    >
      <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="subtitle2" fontWeight={600}>
            {offering.courseName || offering.offeringCode}
          </Typography>
          <Chip
            label={offering.status}
            size="small"
            color={
              offering.status === CourseOfferingStatus.Active ? 'success' :
              offering.status === CourseOfferingStatus.Completed ? 'default' :
              offering.status === CourseOfferingStatus.Scheduled ? 'info' : 'warning'
            }
          />
        </Box>
        <Typography variant="caption" color="textSecondary" display="block" sx={{ mt: 0.5 }}>
          {offering.academicYearName} • Semester {offering.semesterName} • {offering.offeringCode}
        </Typography>
        <Box sx={{ display: 'flex', gap: 0.5, mt: 1, flexWrap: 'wrap' }}>
          <Chip label={`${offering.totalUnits} units`} size="small" variant="outlined" />
          <Chip label={`${offering.totalEnrollments} students`} size="small" variant="outlined" />
          <Chip label={`${offering.totalLecturers} lecturers`} size="small" variant="outlined" />
        </Box>
      </CardContent>
    </Card>
  );

  // Unit chip linking to the unit's Study Materials page. Only template units
  // (real UnitId) expose materials; offering-only units have no UnitId yet.
  const renderUnitChip = (unit: DashboardUnit) => (
    <Chip
      key={unit.courseOfferingUnitId || unit.unitId}
      label={unit.code ? `${unit.code} — ${unit.name}` : unit.name}
      size="small"
      variant="outlined"
      icon={<MenuBook sx={{ fontSize: 16 }} />}
      onClick={
        unit.unitId
          ? () =>
              navigate(
                `/units/${unit.unitId}/materials?name=${encodeURIComponent(unit.name)}&code=${encodeURIComponent(unit.code)}`
              )
          : undefined
      }
      sx={unit.unitId ? { cursor: 'pointer' } : undefined}
    />
  );

  // Lecturer course card from the persisted teaching assignments.
  const renderLecturerCourseCard = (course: LecturerCourse) => (
    <Card key={course.courseOfferingId} sx={{ mb: 1.5 }}>
      <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="subtitle2" fontWeight={600}>
            {course.courseName || course.offeringCode}
            {course.courseCode ? ` (${course.courseCode})` : ''}
          </Typography>
          <Box sx={{ display: 'flex', gap: 0.5 }}>
            {course.isPrimary && <Chip label="Primary" size="small" color="primary" variant="outlined" />}
            <Chip label={course.status} size="small" color={course.status === 'Active' ? 'success' : 'default'} />
          </Box>
        </Box>
        <Typography variant="caption" color="textSecondary" display="block" sx={{ mt: 0.5 }}>
          {course.academicYearName} â€¢ Semester {course.semesterName} â€¢ {course.offeringCode}
          {course.intake ? ` â€¢ ${course.intake}` : ''}
        </Typography>
        {course.units.length > 0 && (
          <Box sx={{ display: 'flex', gap: 0.5, mt: 1, flexWrap: 'wrap' }}>
            {course.units.map(renderUnitChip)}
          </Box>
        )}
      </CardContent>
    </Card>
  );

  // Student enrolled course card from the persisted enrollments.
  const renderStudentCourseCard = (course: StudentCourse) => (
    <Card key={course.courseOfferingId} sx={{ mb: 1.5 }}>
      <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="subtitle2" fontWeight={600}>
            {course.courseName || course.offeringCode}
            {course.courseCode ? ` (${course.courseCode})` : ''}
          </Typography>
          <Chip
            label={course.confirmationStatus || course.status}
            size="small"
            color={
              course.confirmationStatus === 'Confirmed' ? 'success' :
              course.confirmationStatus === 'Pending' ? 'warning' : 'default'
            }
          />
        </Box>
        <Typography variant="caption" color="textSecondary" display="block" sx={{ mt: 0.5 }}>
          {course.academicYearName} â€¢ Semester {course.semesterName} â€¢ {course.offeringCode}
          {course.attemptNumber > 1 ? ` â€¢ Attempt ${course.attemptNumber}` : ''}
        </Typography>
        {course.units.length > 0 && (
          <Box sx={{ display: 'flex', gap: 0.5, mt: 1, flexWrap: 'wrap' }}>
            {course.units.map(renderUnitChip)}
          </Box>
        )}
      </CardContent>
    </Card>
  );

  // Accommodation card (lecturer or student) from the accommodation module.
  const myAccommodation = isLecturer ? lecturerMe?.accommodation : studentMe?.accommodation;
  const renderAccommodationCard = () => {
    if (!myAccommodation) {
      return (
        <Paper sx={{ p: 3, borderRadius: 2, mb: 3 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <Avatar sx={{ bgcolor: 'secondary.main' }}>
              <Apartment />
            </Avatar>
            <Typography variant="h6" fontWeight={600}>
              My Accommodation
            </Typography>
          </Box>
          <Typography variant="body2" color="textSecondary">
            {isStudent
              ? 'No accommodation has been assigned to you yet. Contact the accommodation office.'
              : 'No accommodation has been assigned to you.'}
          </Typography>
        </Paper>
      );
    }
    return (
      <Paper sx={{ p: 3, borderRadius: 2, mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
          <Avatar sx={{ bgcolor: 'secondary.main' }}>
            <Apartment />
          </Avatar>
          <Typography variant="h6" fontWeight={600}>
            My Accommodation
          </Typography>
        </Box>
        <List dense sx={{ px: 0 }}>
          <ListItem sx={{ px: 0 }}>
            <ListItemText primary="House" secondary={myAccommodation.houseNumber || '—'} />
          </ListItem>
          <ListItem sx={{ px: 0 }}>
            <ListItemText primary="Lane" secondary={myAccommodation.laneName || '—'} />
          </ListItem>
          <ListItem sx={{ px: 0 }}>
            <ListItemText primary="Semester" secondary={myAccommodation.semesterName || '—'} />
          </ListItem>
          <ListItem sx={{ px: 0 }}>
            <ListItemText primary="Status" secondary={myAccommodation.status || '—'} />
          </ListItem>
        </List>
      </Paper>
    );
  };

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

      {/* Pending Confirmations Banner - shown to students/lecturers */}
      {isStudent && pendingEnrollments && pendingEnrollments.length > 0 && (
        <Alert severity="warning" sx={{ mt: 3 }} action={
          <Button color="inherit" size="small" onClick={() => handleConfirmClick(pendingEnrollments[0], 'enrollment')}>
            Review
          </Button>
        }>
          You have {pendingEnrollments.length} pending enrollment confirmation{pendingEnrollments.length > 1 ? 's' : ''}. Please confirm your enrollment.
        </Alert>
      )}

      {isLecturer && pendingEnrollments && pendingEnrollments.length > 0 && (
        <Alert severity="warning" sx={{ mt: 3 }} action={
          <Button color="inherit" size="small" onClick={() => handleConfirmClick(pendingEnrollments[0], 'teaching')}>
            Review
          </Button>
        }>
          You have {pendingEnrollments.length} pending teaching assignment confirmation{pendingEnrollments.length > 1 ? 's' : ''}.
        </Alert>
      )}

      <Grid container spacing={3} sx={{ mt: 2 }}>
        <Grid item xs={12} md={8}>
          {/* Admin/Moderator: Course Offerings overview */}
          {isAdminOrModerator && (
            <Paper sx={{ p: 3, borderRadius: 2, mb: 3 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6" fontWeight={600}>
                  Course Offerings Overview
                </Typography>
                <Button variant="outlined" size="small" onClick={() => navigate('/course-offerings')}>
                  View All
                </Button>
              </Box>
              {offeringsLoading ? (
                <LinearProgress />
              ) : (
                <Grid container spacing={2}>
                  <Grid item xs={12} sm={4}>
                    <Typography variant="subtitle2" fontWeight={600} color="success.main" sx={{ mb: 1 }}>
                      Active ({activeOfferings.length})
                    </Typography>
                    {activeOfferings.slice(0, 3).map(renderOfferingCard)}
                    {activeOfferings.length === 0 && (
                      <Typography variant="body2" color="textSecondary">No active offerings</Typography>
                    )}
                  </Grid>
                  <Grid item xs={12} sm={4}>
                    <Typography variant="subtitle2" fontWeight={600} color="info.main" sx={{ mb: 1 }}>
                      Upcoming ({upcomingOfferings.length})
                    </Typography>
                    {upcomingOfferings.slice(0, 3).map(renderOfferingCard)}
                    {upcomingOfferings.length === 0 && (
                      <Typography variant="body2" color="textSecondary">No upcoming offerings</Typography>
                    )}
                  </Grid>
                  <Grid item xs={12} sm={4}>
                    <Typography variant="subtitle2" fontWeight={600} color="textSecondary" sx={{ mb: 1 }}>
                      Completed ({completedOfferings.length})
                    </Typography>
                    {completedOfferings.slice(0, 3).map(renderOfferingCard)}
                    {completedOfferings.length === 0 && (
                      <Typography variant="body2" color="textSecondary">No completed offerings</Typography>
                    )}
                  </Grid>
                </Grid>
              )}
            </Paper>
          )}

          {/* Lecturer: My Courses & Units (persisted teaching assignments) */}
          {isLecturer && (
            <Paper sx={{ p: 3, borderRadius: 2, mb: 3 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6" fontWeight={600}>
                  My Courses &amp; Units
                </Typography>
                <Button variant="outlined" size="small" onClick={() => navigate('/assignments')}>
                  My Assignments
                </Button>
              </Box>
              {lecturerMeLoading ? (
                <LinearProgress />
              ) : (lecturerMe?.courses || []).length === 0 ? (
                <Box>
                  <Typography variant="body2" color="textSecondary">
                    You are not assigned to teach any course offerings yet. Once an administrator
                    assigns you to a course offering, it will appear here.
                  </Typography>
                  {lecturerMe && lecturerMe.unitAllocations.length > 0 && (
                    <Box sx={{ mt: 2 }}>
                      <Typography variant="subtitle2" fontWeight={600} gutterBottom>
                        Unit Allocations
                      </Typography>
                      <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                        {lecturerMe.unitAllocations.map(renderUnitChip)}
                      </Box>
                    </Box>
                  )}
                </Box>
              ) : (
                <Box>
                  {lecturerMe!.courses.map(renderLecturerCourseCard)}
                </Box>
              )}
            </Paper>
          )}

          {/* Student: My Course (persisted enrollments) */}
          {isStudent && (
            <Paper sx={{ p: 3, borderRadius: 2, mb: 3 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6" fontWeight={600}>
                  My Course
                </Typography>
                <Button variant="outlined" size="small" onClick={() => navigate('/enrollment-status')}>
                  Enrollment Status
                </Button>
              </Box>
              {studentMeLoading ? (
                <LinearProgress />
              ) : (studentMe?.enrollments || []).length === 0 ? (
                <Typography variant="body2" color="textSecondary">
                  You have no active course enrollments. Browse available courses to enroll.
                </Typography>
              ) : (
                <Box>
                  {studentMe!.enrollments.map(renderStudentCourseCard)}
                </Box>
              )}
            </Paper>
          )}

          {/* Student: My Course Offerings */}
          {isStudent && (
            <Paper sx={{ p: 3, borderRadius: 2, mb: 3 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6" fontWeight={600}>
                  My Course Offerings
                </Typography>
                <Button variant="outlined" size="small" onClick={() => navigate('/course-offerings')}>
                  Browse Courses
                </Button>
              </Box>
              {studentEnrollmentsLoading ? (
                <LinearProgress />
              ) : (
                <List>
                  {(studentEnrollments || []).map((enrollment: any) => (
                    <ListItem key={enrollment.id} divider sx={{ px: 0 }}>
                      <ListItemAvatar>
                        <Avatar sx={{ bgcolor: '#576426' }}>
                          <MenuBook />
                        </Avatar>
                      </ListItemAvatar>
                      <ListItemText
                        primary={enrollment.courseName || enrollment.offeringCode || 'Course Offering'}
                        secondary={
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 0.5, flexWrap: 'wrap' }}>
                            <Typography variant="caption" color="textSecondary">
                              {enrollment.academicYearName} • Semester {enrollment.semesterName}
                            </Typography>
                            <Chip
                              label={enrollment.confirmationStatus || 'Pending'}
                              size="small"
                              color={
                                enrollment.confirmationStatus === ConfirmationStatus.Confirmed ? 'success' :
                                enrollment.confirmationStatus === ConfirmationStatus.Pending ? 'warning' : 'default'
                              }
                            />
                          </Box>
                        }
                      />
                      {enrollment.confirmationStatus === ConfirmationStatus.Pending && (
                        <Button
                          size="small"
                          variant="contained"
                          onClick={() => handleConfirmClick(enrollment, 'enrollment')}
                        >
                          Confirm
                        </Button>
                      )}
                    </ListItem>
                  ))}
                  {(studentEnrollments || []).length === 0 && (
                    <Typography variant="body2" color="textSecondary">
                      You have no active course enrollments.
                    </Typography>
                  )}
                </List>
              )}
            </Paper>
          )}

          {/* Recent Activity - visible only to SystemAdministrator, Administrator, Coordinator */}
          {user?.roles?.some(r => ['SystemAdministrator', 'Administrator', 'Coordinator'].includes(r)) && (
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
          )}
        </Grid>

        <Grid item xs={12} md={4}>
          {/* Admin: Pending confirmations summary */}
          {isAdminOrModerator && (
            <Paper sx={{ p: 3, borderRadius: 2, mb: 3 }}>
              <Typography variant="h6" fontWeight={600} sx={{ mb: 2 }}>
                Pending Confirmations
              </Typography>
              <List dense>
                <ListItem sx={{ px: 0 }}>
                  <ListItemAvatar>
                    <Avatar sx={{ bgcolor: 'warning.main' }}>
                      <CheckCircle />
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary="Enrollment Confirmations"
                    secondary={`${offerings.reduce((sum, o) => sum + (o.totalEnrollments || 0), 0)} pending`}
                  />
                </ListItem>
                <ListItem sx={{ px: 0 }}>
                  <ListItemAvatar>
                    <Avatar sx={{ bgcolor: 'info.main' }}>
                      <Schedule />
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary="Teaching Confirmations"
                    secondary="Review pending assignments"
                  />
                </ListItem>
                <ListItem sx={{ px: 0 }}>
                  <ListItemAvatar>
                    <Avatar sx={{ bgcolor: 'error.main' }}>
                      <ReportProblem />
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary="Issue Reports"
                    secondary="Users reporting assignment issues"
                  />
                </ListItem>
              </List>
              <Button fullWidth variant="outlined" size="small" sx={{ mt: 1 }} onClick={() => navigate('/course-offerings')}>
                Manage Offerings
              </Button>
            </Paper>
          )}

          {/* My Accommodation (lecturer/student) */}
          {(isLecturer || isStudent) && renderAccommodationCard()}

          {/* Upcoming Events */}
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
              <Button variant="outlined" size="small" fullWidth onClick={() => navigate('/calendar')}>
                View Full Calendar
              </Button>
            </Box>
          </Paper>
        </Grid>
      </Grid>

      {/* Assignment Confirmation Dialog */}
      {selectedPending && (
        <AssignmentConfirm
          open={confirmOpen}
          onClose={() => setConfirmOpen(false)}
          pending={selectedPending}
          type={confirmType}
        />
      )}
    </Box>
  );
};
