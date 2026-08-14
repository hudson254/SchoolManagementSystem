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
- **Development**: `http://localhost:5000/api/v1`
- **Docker**: `http://localhost:5000/api/v1`
- **Production**: `https://your-domain.com/api/v1`

### API Controllers
| Controller | Base Path | Description |
|------------|-----------|-------------|
| AuthController | `/api/v1/auth` | Authentication |
| StudentController | `/api/v1/students` | Student management |
| CourseController | `/api/v1/courses` | Course management |
| UnitController | `/api/v1/units` | Unit management |
| EnrollmentController | `/api/v1/enrollments` | Enrollment management |
| GradeController | `/api/v1/grades` | Grade management |
| LecturerController | `/api/v1/lecturers` | Lecturer management |
| TimetableController | `/api/v1/timetables` | Timetable management |
| AccommodationController | `/api/v1/accommodation` | Accommodation management |
| ReportController | `/api/v1/reports` | Report generation |
| NotificationController | `/api/v1/notifications` | Notification management |
| AuditController | `/api/v1/audit` | Audit log management |
| ApprovalController | `/api/v1/approvals` | Approval workflows |
| CourseOfferingController | `/api/v1/course-offerings` | Course offering management |
| AssessmentController | `/api/v1/assessments` | Assessment management |
| CertificateController | `/api/v1/certificates` | Certificate management |
| PasswordResetController | `/api/v1/password-reset` | Password reset workflow |
| ReportVerificationController | `/api/v1/report-verification` | Report verification |
| ErrorAdminController | `/api/v1/errors` | Error log administration |
| ConfirmationController | `/api/v1/confirmations` | Enrollment/teaching confirmations |

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
