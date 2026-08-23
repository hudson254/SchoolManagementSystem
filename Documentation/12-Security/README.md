# Security Guide

## Table of Contents
- [Security Overview](#security-overview)
- [Authentication Security](#authentication-security)
- [Authorization](#authorization)
- [Role Permissions](#role-permissions)
- [Password Policies](#password-policies)
- [Input Validation](#input-validation)
- [File Upload Security](#file-upload-security)
- [CSRF Protection](#csrf-protection)
- [Security Headers](#security-headers)
- [Rate Limiting](#rate-limiting)
- [Audit Logging](#audit-logging)
- [Error Handling](#error-handling)
- [Security Best Practices](#security-best-practices)
- [Incident Response](#incident-response)
- [Related Documentation](#related-documentation)

---

## Security Overview

The School Management System implements defense-in-depth security with multiple layers of protection.

### Security Layers
1. **Transport Security**: HTTPS via Nginx reverse proxy
2. **Authentication**: JWT with httpOnly cookies
3. **Authorization**: Role-based access control
4. **Input Validation**: FluentValidation
5. **CSRF Protection**: Double-submit cookie pattern
6. **Security Headers**: Middleware-enforced HTTP headers
7. **Rate Limiting**: Prevent abuse and brute force
8. **Audit Logging**: Track all security-relevant events
9. **Error Handling**: Secure error responses with correlation IDs
10. **Multi-Tenancy**: Row-level security for data isolation

---

## Authentication Security

### JWT Security
- Tokens signed with HMAC-SHA256 (HS256)
- Secret key stored in environment variable (not code)
- Short expiration times (15 minutes access, 7 days refresh)
- httpOnly cookies prevent XSS theft
- Token revocation on logout
- Algorithm validation prevents confusion attacks
- Clock skew set to zero

### Session Security
- Session timeout: 30 minutes idle
- httpOnly session cookies
- Session data stored server-side

### Login Security
- Account lockout after 5 failed attempts
- 15-minute lockout duration
- Rate limiting on login endpoint

---

## Authorization

### Policy-Based Authorization (from Program.cs)
```csharp
options.AddPolicy("AdministratorAccess", policy =>
    policy.RequireRole("Administrator"));
options.AddPolicy("ModeratorAccess", policy =>
    policy.RequireRole("Administrator", "Coordinator"));
options.AddPolicy("LecturerAccess", policy =>
    policy.RequireRole("Administrator", "Coordinator", "Lecturer"));
options.AddPolicy("StudentAccess", policy =>
    policy.RequireRole("Administrator", "Coordinator", "Lecturer", "Student"));
options.AddPolicy("ReceptionistAccess", policy =>
    policy.RequireRole("Administrator", "Coordinator", "Receptionist"));
```

### Registration Status Authorization
- Pending users have limited access
- Approved users have full role-based access
- Registration status checked by custom authorization handler

---

## Role Permissions

| Feature | Admin | Coordinator | Lecturer | Student | Receptionist |
|---------|-------|-------------|----------|---------|--------------|
| System Configuration | ✅ | ❌ | ❌ | ❌ | ❌ |
| User Management | ✅ | ✅ | ❌ | ❌ | ❌ |
| Course Management | ✅ | ✅ | ❌ | ❌ | ❌ |
| Unit Management | ✅ | ✅ | ✅ | ❌ | ❌ |
| Enrollments | ✅ | ✅ | ❌ | Self | ❌ |
| Grades | ✅ | ✅ | ✅ | View | ❌ |
| Accommodation | ✅ | ✅ | View | View | ✅ |
| Reports | ✅ | ✅ | Unit | Personal | ✅ |
| Audit Logs | ✅ | Limited | ❌ | ❌ | ❌ |

---

## Password Policies

### Requirements
| Policy | Value |
|--------|-------|
| Minimum Length | 12 characters |
| Requires Digit | Yes |
| Requires Lowercase | Yes |
| Requires Uppercase | Yes |
| Requires Non-Alphanumeric | Yes |
| Required Unique Characters | 4 |

### Password Strength Service
The `PasswordPolicyService` provides additional validation:
- Password strength calculation
- Common password blacklist
- Entropy-based strength checking

---

## Input Validation

### FluentValidation
All input is validated using FluentValidation:
- Request validation rules
- Business rule validation
- Custom validators for complex rules
- Validation pipeline behavior in MediatR

### Suppression of Implicit Required
ASP.NET Core's implicit `[Required]` for non-nullable types is suppressed to allow FluentValidation to control validation.

---

## File Upload Security

### Restrictions
- Allowed extensions: `.pdf`, `.doc`, `.docx`, `.ppt`, `.pptx`, `.xls`, `.xlsx`, `.jpg`, `.jpeg`, `.png`, `.zip`
- Maximum file size: 10 MB (configurable)
- Content-Type validation via `X-Content-Type-Options: nosniff`
- Upload directory outside web root

### Serving Uploaded Files
- Static files served at `/uploads` path
- `nosniff` header prevents MIME-type sniffing
- Separate volume for uploaded files in Docker

---

## CSRF Protection

### Double-Submit Cookie Pattern
The `CsrfProtectionMiddleware` implements:
- Random CSRF token generated per session
- Token set in both cookie and request header
- Server validates both tokens match
- Protects state-changing requests (POST, PUT, DELETE, PATCH)
- Skips validation for safe methods (GET, HEAD, OPTIONS)

### Configuration
- Middleware runs before authentication
- Applies to all API endpoints
- Token rotated on login/logout

---

## Security Headers

The `SecurityHeadersMiddleware` adds the following headers:

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Content-Type-Options` | `nosniff` | Prevent MIME sniffing |
| `X-Frame-Options` | `DENY` | Prevent clickjacking |
| `X-XSS-Protection` | `1; mode=block` | XSS filter |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Referrer control |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | HSTS (production) |

---

## Rate Limiting

### Configuration
```json
{
  "RateLimiting": {
    "PermitLimit": 20,
    "WindowMinutes": 1,
    "BanDurationMinutes": 15
  }
}
```

### Behavior
- Maximum 20 requests per minute per client
- Exceeding limit results in 429 Too Many Requests
- Automatic ban for 15 minutes if limit exceeded
- Applied after authentication

---

## Audit Logging

### What is Logged
- User login/logout events
- Data creation, updates, and deletions
- Configuration changes
- Role assignments
- Password changes
- Failed authentication attempts

### Audit Log Details
Each entry contains:
- Timestamp
- User ID and username
- Action type
- Entity type and ID
- Before/after values
- IP address
- Correlation ID

---

## Error Handling

### Exception Handling Middleware
- Catches all unhandled exceptions
- Returns consistent JSON error responses
- Logs errors with correlation IDs
- Does not expose stack traces to clients

### Error Response Format
```json
{
  "title": "An error occurred",
  "status": 500,
  "detail": "An unexpected error occurred",
  "correlationId": "abc-123-def-456",
  "errors": null
}
```

### Error Categories
- Database errors
- External service errors
- File system errors
- Network errors
- Timeout errors
- Background job errors

---

## Security Best Practices

### For Administrators
1. Use strong, unique passwords
2. Enable HTTPS in production
3. Keep JWT secret confidential
4. Regularly review audit logs
5. Monitor failed login attempts
6. Keep system updated
7. Regular security audits
8. Principle of least privilege for role assignments

### For Developers
1. Validate all input using FluentValidation
2. Use parameterized queries (EF Core)
3. Do not log sensitive data
4. Implement proper error handling
5. Use dependency injection
6. Follow CORS best practices
7. Keep dependencies updated
8. Use secure coding practices

### For Users
1. Use strong passwords
2. Do not share credentials
3. Log out from shared computers
4. Report suspicious activity
5. Keep contact information updated

---

## Incident Response

### Response Plan
1. **Identify**: Detect and confirm security incident
2. **Contain**: Limit damage and prevent spread
3. **Eradicate**: Remove threat
4. **Recover**: Restore normal operations
5. **Post-Incident**: Analyze and improve

### Key Contacts
- System Administrator
- IT Security Team
- Database Administrator

### Incident Types
| Incident | Response |
|----------|----------|
| Suspicious login activity | Lock affected accounts, review logs |
| Data breach | Contain, notify affected users, investigate |
| DDoS attack | Enable rate limiting, contact hosting provider |
| Malware detection | Isolate affected systems, scan, clean |
| Unauthorized access attempt | Block IP, review logs, strengthen security |

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Authentication](../11-Authentication/README.md) | Authentication mechanisms |
| [System Administration](../06-System-Administration/README.md) | Security administration |
| [Database Guide](../13-Database/README.md) | Database security |
| [API Documentation](../17-API/README.md) | API security requirements |
| [Developer Guide](../18-Developer-Guide/README.md) | Secure coding practices |
