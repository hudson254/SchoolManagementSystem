
```markdown
# Administrator Guide

## Introduction

This guide is for System Administrators who manage the School Management System. It covers user management, system configuration, backup, recovery, and maintenance.

## Table of Contents

1. [Getting Started](#getting-started)
2. [User Management](#user-management)
3. [Role Management](#role-management)
4. [System Configuration](#system-configuration)
5. [Multi-Tenancy Management](#multi-tenancy-management)
6. [Backup and Recovery](#backup-and-recovery)
7. [Monitoring](#monitoring)
8. [Security](#security)
9. [Maintenance](#maintenance)
10. [Troubleshooting](#troubleshooting)

## Getting Started

### Accessing the System

1. Navigate to `https://localhost`
2. Login with administrator credentials
3. You will be directed to the Dashboard

### Dashboard Overview

The admin dashboard displays:
- System statistics (users, students, lecturers)
- Recent activities
- Upcoming events
- System health status

## User Management

### Viewing Users

1. Navigate to **Users** in the sidebar
2. Use the search bar to find specific users
3. Apply filters by role or status
4. Click **View** to see user details

### Creating Users

1. Click **Add User** on the Users page
2. Fill in user details:
   - First Name (required)
   - Last Name (required)
   - Email (required, unique)
   - Phone Number (required)
   - Role (select from available roles)
3. Click **Create**

**Note**: New users will receive a welcome email with login instructions.

### Editing Users

1. Find the user in the list
2. Click **Edit** (pencil icon)
3. Update user information
4. Click **Save**

### Managing User Roles

1. Find the user in the list
2. Click **More** (three dots icon)
3. Select **Manage Roles**
4. Toggle roles on/off
5. Click **Save**

### Activating/Deactivating Users

1. Find the user in the list
2. Click **More** (three dots icon)
3. Select **Activate** or **Deactivate**
4. Confirm the action

**Deactivation Effects**:
- User cannot log in
- User's data remains intact
- Can be reactivated later

### Resetting User Passwords (Admin-Mediated — RISK-21)

> **LAN deployment note:** Email/SMTP has been fully removed from the system.
> Password recovery on the isolated LAN is **admin-mediated** — there is no
> self-service email reset link. A user who cannot sign in submits a request
> (via `POST /api/v1/auth/forgot-password`), and an **Administrator** fulfills
> it from the admin panel.

**Step 1 — User submits a request:**
1. On the login screen, the user clicks **"Forgot password?"**
2. They enter the email address registered on their account.
3. The system creates a **pending password reset request** (the response is
   identical whether or not the email exists, to prevent account enumeration).

**Step 2 — Administrator reviews pending requests:**
1. Navigate to **Password Resets** in the admin section (**Administrator** role required).
2. The list shows pending requests with the requester's email, optional note,
   and request date.
3. API equivalent: `GET /api/v1/admin/password-resets/pending`.

**Step 3 — Administrator fulfills or rejects:**
- **Fulfill** — Assigns the user a new temporary password (the user can change
  it on their next sign-in).
  - API equivalent: `POST /api/v1/admin/password-resets/{requestId}/fulfill`.
- **Reject** — Marks the request as rejected (e.g. unverified identity).
  - API equivalent: `POST /api/v1/admin/password-resets/{requestId}/reject`.

All four admin password-reset endpoints require the **Administrator** role;
anonymous and non-admin access returns `401 Unauthorized`.

## Role Management

### Available Roles

| Role | Description | Access Level |
|------|-------------|--------------|
| System Administrator | Full system control | Complete |
| Moderator | Academic management | High |
| Lecturer | Teaching and grading | Medium |
| Student | Learning and submission | Low |
| Receptionist | Onboarding and accommodation | Medium |

### Role Permissions

**System Administrator**:
- Full access to all modules
- User management
- System configuration
- Backup and restore
- View audit logs

**Moderator**:
- Course and programme management
- Unit management
- Lecturer verification
- Timetable management
- Student enrollment

**Lecturer**:
- Unit allocation
- Upload lecture notes
- Create assignments
- Grade submissions
- View enrolled students

**Student**:
- Browse courses
- Enroll in units
- Submit assignments
- View grades and transcript
- View timetable

**Receptionist**:
- Student onboarding
- Lecturer verification assistance
- Accommodation management
- Room assignments

## System Configuration

### General Settings

1. Navigate to **Settings** in the sidebar
2. Configure:
   - Application Name
   - Default Language
   - Timezone
   - Maintenance Mode toggle

### Security Settings

1. Navigate to Settings > Security
2. Configure:
   - Two-Factor Authentication (MFA)
   - Session Timeout
   - Max Login Attempts
   - Password Policy

### Notification Settings

1. Navigate to Settings > Notifications
2. Configure:
   - Email Notifications
   - SMS Notifications
   - Push Notifications

## Multi-Tenancy Management

### Viewing Tenants

1. Navigate to **Tenants** in the admin section
2. View list of all tenants
3. See tenant status and subscription details

### Creating Tenants

1. Click **Add Tenant**
2. Enter:
   - Tenant Name
   - Organization Name
   - Subdomain
   - Contact Information
3. Set limits:
   - Max Students
   - Max Lecturers
   - Storage Quota
4. Click **Create**

### Managing Tenants

- **Activate/Deactivate**: Control tenant access
- **Edit**: Update tenant information
- **View Details**: See tenant usage statistics
- **Delete**: Remove tenant (use with caution)

## Backup and Recovery

### Automated Backups

The system performs automated daily backups:
- **Time**: 2:00 AM daily
- **Location**: `/var/backups/sms/`
- **Retention**: 30 days
- **Format**: Compressed SQL files

### Creating Manual Backups

```bash
# From the server
./scripts/backup.sh

# Output
Backup created: /var/backups/sms/sms_backup_20240101_020000.sql.gz
