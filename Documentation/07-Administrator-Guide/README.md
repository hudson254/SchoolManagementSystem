# Administrator User Guide

## Table of Contents
- [Dashboard Overview](#dashboard-overview)
- [User Management](#user-management)
- [Role and Permission Management](#role-and-permission-management)
- [Course Management](#course-management)
- [Unit Management](#unit-management)
- [Academic Year Management](#academic-year-management)
- [Department Management](#department-management)
- [Report Generation](#report-generation)
- [Notification Management](#notification-management)
- [System Settings](#system-settings)
- [Audit Logs](#audit-logs)
- [Maintenance Tasks](#maintenance-tasks)
- [Search and Filtering](#search-and-filtering)
- [Import and Export](#import-and-export)
- [Related Documentation](#related-documentation)

---

## Dashboard Overview

The Administrator Dashboard provides a comprehensive overview of the system. Access it by logging in with an Administrator account.

### Dashboard Components
- **System Health**: API status, database connectivity, storage usage
- **User Statistics**: Total users, active users, locked accounts
- **Course Statistics**: Total courses, active offerings, enrollments
- **Recent Activity**: Recent system actions and changes
- **Performance Metrics**: Response times, error rates, request counts

---

## User Management

### Creating a User
1. Navigate to **Users** > **Create User**
2. Fill in required fields:
   - Full name
   - Email address
   - Username (auto-generated, can be customized)
   - Role (Administrator, Coordinator, Lecturer, Student, Receptionist)
   - Title (e.g., Dr., Prof., etc.)
3. Set an initial password (must meet complexity requirements: 12+ characters, uppercase, lowercase, digit, special character)
4. Click **Save**

### Editing a User
1. Navigate to **Users** > **User List**
2. Search for the user using name, email, or username
3. Click the **Edit** button
4. Modify the required fields
5. Click **Save**

### Disabling/Enabling a User
1. Navigate to **Users** > **User List**
2. Find the user and click **Edit**
3. Toggle the **Active** status
4. Click **Save**

### Resetting a User Password
1. Navigate to **Users** > **User List**
2. Find the user and click **Reset Password**
3. A temporary password will be generated
4. Provide the temporary password to the user
5. The user will be prompted to change their password on next login

### Unlocking a User Account
1. Navigate to **Users** > **User List**
2. Find the locked user (locked accounts are indicated)
3. Click **Unlock**
4. The user can now attempt to log in again

---

## Role and Permission Management

### Available Roles
| Role | Description |
|------|-------------|
| **Administrator** | Full system access to all features |
| **Coordinator** | Academic coordination, approvals, course management |
| **Lecturer** | Teaching, grading, attendance, assignments |
| **Student** | Enrollment, learning, grades, certificates |
| **Receptionist** | Limited administrative functions |

### Assigning Roles
1. Navigate to **Users** > **User List**
2. Find the user and click **Edit**
3. Select/deselect roles in the Roles section
4. Click **Save**

> **⚠️ WARNING**: Be careful when removing roles - users may lose access to essential functions.

---

## Course Management

### Creating a Course
1. Navigate to **Courses** > **Create Course**
2. Enter the following:
   - Course code (unique identifier)
   - Course name
   - Description
   - Programme
   - Duration
   - Credit hours
3. Click **Save**

### Editing a Course
1. Navigate to **Courses** > **Course List**
2. Search for the course
3. Click **Edit**
4. Modify the required fields
5. Click **Save**

### Deleting a Course
1. Navigate to **Courses** > **Course List**
2. Find the course and click **Delete**
3. Confirm the deletion

> **⚠️ WARNING**: Deleting a course may affect existing enrollments and academic records.

### Course Offerings
1. Navigate to **Course Offerings**
2. Create a course offering for a specific academic period
3. Assign units to the offering
4. Assign lecturers to teach specific units
5. Enroll students in the offering

---

## Unit Management

### Creating a Unit
1. Navigate to **Units** > **Create Unit**
2. Enter:
   - Unit code (unique identifier)
   - Unit name
   - Credit hours
   - Department
3. Click **Save**

### Assigning Units to Courses
1. Navigate to **Courses** > **Course Units**
2. Select the course
3. Add units to the course curriculum
4. Configure unit order and requirements

---

## Academic Year Management

### Creating an Academic Year
1. Navigate to **Academic** > **Academic Years**
2. Click **Create Academic Year**
3. Set:
   - Year name (e.g., 2024-2025)
   - Start date
   - End date
   - Status (Active/Inactive)
4. Click **Save**

### Managing Semesters
1. Navigate to **Academic** > **Academic Years**
2. Select an academic year
3. Click **Add Semester**
4. Configure:
   - Semester name (e.g., Semester 1)
   - Start date
   - End date
   - Enrollment period dates
5. Click **Save**

---

## Department Management

### Creating a Department
1. Navigate to **Departments** > **Create Department**
2. Enter:
   - Department name
   - Department code
   - Head of Department
3. Click **Save**

### Assigning Units to Departments
1. Navigate to **Units** > **Unit List**
2. Edit a unit
3. Select the department from the dropdown
4. Click **Save**

---

## Report Generation

### Available Reports
| Report Type | Description | Format |
|-------------|-------------|--------|
| Student Enrollment | Students enrolled in courses | PDF, Excel |
| Grade Report | Grades by unit, student, course | PDF, Excel |
| Attendance Report | Student attendance records | PDF, Excel |
| Accommodation Report | Occupancy and maintenance | PDF, Excel |
| Audit Log Report | System activity | PDF, Excel |
| Transcript | Student academic transcript | PDF |

### Generating a Report
1. Navigate to **Reports**
2. Select the report type from the list
3. Configure filters:
   - Date range
   - Department
   - Course
   - Student
   - Unit
4. Click **Generate**
5. Preview the report if available
6. Click **Download** to save as PDF or Excel

### Report Authentication
Generated reports include security features:
- QR code for verification
- SHA-256 hash signature
- Visible watermark
- Verification token

### Verifying a Report
1. Navigate to **Reports** > **Verify Report**
2. Upload the report file or enter the verification token
3. The system will verify the report's authenticity
4. Results show: Valid, Revoked, or Tampered

---

## Notification Management

### Creating a Notification
1. Navigate to **Notifications** > **Create**
2. Configure:
   - Recipients: All users, by role, specific users
   - Title
   - Message content
   - Priority (Low, Normal, High, Urgent)
3. Click **Send**

### Viewing Notification History
1. Navigate to **Notifications** > **History**
2. View all sent notifications
3. Filter by date, recipient, status

---

## System Settings

### Configuring System Parameters
The following settings can be configured through the admin interface or directly in configuration files:

- **Password Policy**: Minimum length, complexity requirements
- **Rate Limiting**: API request limits
- **File Upload**: Maximum file size, allowed extensions
- **JWT Settings**: Token expiration times
- **Backup Schedule**: Frequency and retention

---

## Audit Logs

### Viewing Audit Logs
1. Navigate to **System** > **Audit Logs**
2. Filter logs by:
   - Date range
   - User
   - Action type
   - Entity affected
3. View detailed log entries
4. Export logs for analysis

### Understanding Log Entries
Each audit log entry contains:
- Timestamp
- User who performed the action
- Action type (Create, Read, Update, Delete)
- Entity affected (e.g., Student, Course, Grade)
- Entity ID
- Changes made (before/after values)
- IP address

---

## Maintenance Tasks

### Database Maintenance
- Run PostgreSQL VACUUM to reclaim storage
- Update database statistics with ANALYZE
- Rebuild indexes for query performance
- Archive old data

### Storage Management
- Monitor disk usage
- Archive old files
- Clean up temporary uploads
- Review log file sizes

### System Health Checks
- Verify API health endpoint returns healthy
- Check database connectivity
- Monitor Redis cache (if configured)
- Verify backup completion

---

## Search and Filtering

### Global Search
The system provides search functionality across:
- Users (by name, email, username)
- Courses (by code, name)
- Units (by code, name)
- Students (by name, ID number)
- Enrollments

### Advanced Filtering
Most list pages support advanced filters:
- Date ranges
- Status filters
- Department filters
- Role filters
- Keyword search

---

## Import and Export

### Importing Data
Supported import operations:
- Bulk user creation via CSV
- Course import
- Unit import

### Exporting Data
Data can be exported from most list views:
- PDF reports
- Excel spreadsheets
- CSV files
- JSON (API responses)

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [System Administration](../06-System-Administration/README.md) | System administration tasks |
| [Coordinator Guide](../08-Coordinator-Guide/README.md) | Coordinator functions |
| [Security Guide](../12-Security/README.md) | Security best practices |
| [Report Generation](../06-System-Administration/README.md#report-generation) | Detailed report procedures |
| [Backup and Recovery](../14-Backup-and-Recovery/README.md) | Backup procedures |
