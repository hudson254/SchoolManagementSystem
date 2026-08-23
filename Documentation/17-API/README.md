# API Documentation

## Table of Contents
- [API Overview](#api-overview)
- [Authentication](#authentication)
- [API Versioning](#api-versioning)
- [Controller Endpoints](#controller-endpoints)
- [Standard Responses](#standard-responses)
- [Error Handling](#error-handling)
- [Pagination](#pagination)
- [Rate Limiting](#rate-limiting)
- [Related Documentation](#related-documentation)

---

## API Overview

The School Management System exposes a RESTful API at `/api/v1/`. All endpoints are versioned and secured with JWT authentication.

### Base URL
- **Development (direct)**: `http://localhost:5000/api/v1`
- **Docker (via Nginx)**: `http://localhost:8080/api/v1` or `https://localhost:8443/api/v1`
- **Production (LAN)**: `https://<hostname>/api/v1`

### API Controllers (from src code)
| Controller | Route | Description |
|------------|-------|-------------|
| AuthController | `/api/v{version}/auth` | Authentication (login, register, refresh, logout) |
| AccommodationController | `/api/v{version}/accommodation` | Accommodation management |
| ApprovalController | `/api/v{version}/approvals` | Approval workflows |
| AssessmentController | `/api/v{version}/assessments` | Assessment management |
| AssignmentController | `/api/v{version}/assignments` | Assignment management |
| AuditController | `/api/v{version}/audit` | Audit log management |
| CertificateController | `/api/v{version}/certificates` | Certificate generation/management |
| CertificateTemplateController | `/api/v{version}/certificate-templates` | Certificate templates |
| ConfirmationController | `/api/v{version}/confirmations` | Enrollment/teaching confirmations |
| CourseController | `/api/v{version}/courses` | Course management |
| CourseOfferingController | `/api/v{version}/course-offerings` | Course offering management |
| CourseOfferingAssignmentController | `/api/v{version}/course-offering-assignments` | Assignment issues |
| DashboardController | `/api/v{version}/dashboard` | Dashboard data |
| EnrollmentController | `/api/v{version}/enrollments` | Enrollment management |
| ErrorAdminController | `/api/v{version}/admin/errors` | Error log administration |
| GradeController | `/api/v{version}/grades` | Grade management |
| HealthController | `/health` | System health checks |
| LecturerController | `/api/v{version}/lecturers` | Lecturer management |
| LecturerAssignmentController | `/api/v{version}/lecturer-assignments` | Lecturer assignments |
| NotificationController | `/api/v{version}/notifications` | Notification management |
| PasswordResetController | `/api/v{version}/password-reset` | Password reset workflow |
| ReportController | `/api/v{version}/reports` | Report generation |
| ReportAdminController | `/api/v{version}/admin/reports` | Report administration |
| ReportVerificationController | `/api/v{version}/verify/report` | Report verification |
| ReturningUserController | `/api/v{version}/returning-users` | Returning student enrollment |
| StudentController | `/api/v{version}/students` | Student management |
| TimetableController | `/api/v{version}/timetables` | Timetable management |
| UnitController | `/api/v{version}/units` | Unit management |
| UserController | `/api/v{version}/users` | User management |
| VerificationController | `/api/v{version}/verify` | Certificate verification |

---

## Authentication

### Login
```
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "string",
  "password": "string"
}

Response 200:
{
  "userId": "guid",
  "username": "string",
  "email": "string",
  "roles": ["string"]
}
```

### Register
```
POST /api/v1/auth/register
Content-Type: application/json

{
  "fullName": "string",
  "email": "string",
  "password": "string",
  "role": "string"
}
```

### Refresh Token
```
POST /api/v1/auth/refresh-token
Cookie: access_token=<token>
```

### Logout
```
POST /api/v1/auth/logout
Cookie: access_token=<token>
```

### Get Current User
```
GET /api/v1/auth/me
Authorization: Bearer <token>
```

### Change Password
```
POST /api/v1/auth/change-password
{
  "currentPassword": "string",
  "newPassword": "string",
  "confirmPassword": "string"
}
```

---

## API Versioning

The API uses URL path versioning with the `Asp.Versioning` library:
- Current version: `v1`
- Default version: `1.0`
- Version format: `/api/v{version}/{controller}`

---

## Key Endpoints

### Students
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/students` | List students | ModeratorAccess |
| GET | `/api/v1/students/{id}` | Get student | ModeratorAccess |
| POST | `/api/v1/students` | Create student | AdministratorAccess |
| PUT | `/api/v1/students/{id}` | Update student | ModeratorAccess |
| DELETE | `/api/v1/students/{id}` | Delete student | AdministratorAccess |

### Courses
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/courses` | List courses | All authenticated |
| GET | `/api/v1/courses/{id}` | Get course | All authenticated |
| POST | `/api/v1/courses` | Create course | ModeratorAccess |
| PUT | `/api/v1/courses/{id}` | Update course | ModeratorAccess |
| DELETE | `/api/v1/courses/{id}` | Delete course | AdministratorAccess |

### Enrollments
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/enrollments` | List enrollments | ModeratorAccess |
| POST | `/api/v1/enrollments` | Create enrollment | StudentAccess |
| PUT | `/api/v1/enrollments/{id}` | Update enrollment | ModeratorAccess |
| DELETE | `/api/v1/enrollments/{id}` | Drop enrollment | StudentAccess |
| POST | `/api/v1/enrollments/bulk` | Bulk enroll | AdministratorAccess |

### Grades
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/grades` | List grades | LecturerAccess |
| GET | `/api/v1/grades/{id}` | Get grade | LecturerAccess |
| POST | `/api/v1/grades` | Create grade | LecturerAccess |
| PUT | `/api/v1/grades/{id}` | Update grade | LecturerAccess |
| POST | `/api/v1/grades/export` | Export grades | ModeratorAccess |

### Accommodation
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/accommodation/houses` | List houses | All authenticated |
| POST | `/api/v1/accommodation/houses` | Create house | AdministratorAccess |
| PUT | `/api/v1/accommodation/houses/{id}` | Update house | AdministratorAccess |
| DELETE | `/api/v1/accommodation/houses/{id}` | Delete house | AdministratorAccess |
| GET | `/api/v1/accommodation/lanes` | List lanes | All authenticated |
| POST | `/api/v1/accommodation/lanes` | Create lane | AdministratorAccess |
| POST | `/api/v1/accommodation/assign` | Assign accommodation | AdministratorAccess |

### Reports
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/reports` | List reports | ModeratorAccess |
| GET | `/api/v1/reports/{id}` | Generate report | ModeratorAccess |
| POST | `/api/v1/reports/export` | Export report | ModeratorAccess |

### Health
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | System health check |

### Metrics
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/metrics` | Prometheus metrics |

---

## Standard Responses

### Success Response
```json
{
  "data": { ... },
  "message": "Operation completed successfully"
}
```

### Paginated Response
```json
{
  "data": [ ... ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 100,
  "totalPages": 5
}
```

---

## Error Handling

### Error Response Format
```json
{
  "title": "Error Title",
  "status": 400,
  "detail": "Detailed error message",
  "correlationId": "abc-123-def",
  "errors": {
    "fieldName": ["Error description"]
  }
}
```

### HTTP Status Codes
| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request (validation error) |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 409 | Conflict |
| 429 | Too Many Requests (rate limited) |
| 500 | Internal Server Error |

---

## Pagination

List endpoints support pagination:
```json
GET /api/v1/students?pageNumber=1&pageSize=20
```

### Query Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `pageNumber` | 1 | Page number |
| `pageSize` | 20 | Items per page (max 100) |
| `searchTerm` | - | Search keyword |
| `sortBy` | - | Sort field |
| `sortDirection` | asc | Sort direction |

---

## Rate Limiting

| Setting | Value |
|---------|-------|
| Maximum requests | 20 per minute |
| Ban duration | 15 minutes |
| Response status | 429 Too Many Requests |

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Authentication](../11-Authentication/README.md) | Auth mechanisms |
| [Security Guide](../12-Security/README.md) | API security |
| [Developer Guide](../18-Developer-Guide/README.md) | Development setup |
| [Testing Guide](../19-Testing/README.md) | API testing |
| [Troubleshooting Guide](../16-Troubleshooting/README.md) | API issues |
