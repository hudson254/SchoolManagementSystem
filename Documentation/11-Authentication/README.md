# Authentication Documentation

## Table of Contents
- [Authentication Overview](#authentication-overview)
- [JWT Token System](#jwt-token-system)
- [Login Flow](#login-flow)
- [Registration Flow](#registration-flow)
- [Password Management](#password-management)
- [Token Revocation](#token-revocation)
- [Session Management](#session-management)
- [Account Lockout](#account-lockout)
- [Authorization Policies](#authorization-policies)
- [Security Considerations](#security-considerations)
- [Related Documentation](#related-documentation)

---

## Authentication Overview

The School Management System uses **JWT (JSON Web Token)** authentication with **ASP.NET Core Identity** for user management. It implements a secure, stateless authentication mechanism with support for token revocation and role-based authorization.

### Key Components
| Component | Description |
|-----------|-------------|
| ASP.NET Core Identity | User management, password hashing, account lockout |
| JWT Bearer Authentication | Token-based authentication |
| JwtService | Token generation and validation |
| Token Revocation Service | In-memory or Redis-backed token deny-list |
| Authorization Policies | Role-based access control |

---

## JWT Token System

### Token Structure
```
Header: { "alg": "HS256", "typ": "JWT" }
Payload: {
  "sub": "user-id",
  "name": "username",
  "role": "Administrator",
  "iss": "SMSAPI",
  "aud": "SMSWeb",
  "exp": 1234567890,
  "iat": 1234567890
}
Signature: HMAC-SHA256(header.payload, secret)
```

### Token Configuration
| Setting | Default | Description |
|---------|---------|-------------|
| Issuer | SMSAPI | Token issuer identifier |
| Audience | SMSWeb | Intended audience |
| Access Token Expiry | 15 minutes | Access token lifetime |
| Refresh Token Expiry | 7 days | Refresh token lifetime |
| Algorithm | HS256 | HMAC-SHA256 signing algorithm |
| Clock Skew | 0 seconds | Token validation tolerance |

### Token Storage
- Access token stored in **httpOnly cookie** named `access_token`
- Authorization header also supported: `Authorization: Bearer <token>`
- httpOnly cookie prevents XSS attacks from accessing the token

---

## Login Flow

### Standard Login
```
User → Login Page → POST /api/v1/auth/login
→ Verify Credentials (Identity)
→ Generate JWT Token
→ Set Token in httpOnly Cookie
→ Return User Profile
```

### Login Request
```json
POST /api/v1/auth/login
{
  "username": "student1",
  "password": "Password123!"
}
```

### Login Response
```json
{
  "userId": "123e4567-e89b-42d3-a456-426614174000",
  "username": "student1",
  "email": "student1@example.com",
  "roles": ["Student"]
}
```

### Login Failure Handling
- Invalid credentials: HTTP 401 Unauthorized
- Account locked: HTTP 403 with lockout message
- Account disabled: HTTP 403
- Too many attempts: Account locked for 15 minutes after 5 failures

---

## Registration Flow

### Student Registration
```
Student → POST /api/v1/auth/register
→ Validate Input (FluentValidation)
→ Check Username Availability
→ Generate Username (if not provided)
→ Create Identity User
→ Create Student Profile
→ Set Status = Pending
→ Send Approval Notification
```

### Lecturer Registration
```
Lecturer → POST /api/v1/auth/register
→ Validate Input
→ Create Identity User
→ Create Lecturer Profile
→ Set Status = Pending
→ Send Approval Notification
```

### Registration Validation
- Username must be unique
- Email must be unique
- Password must meet complexity requirements
- Name parsing with title extraction (Dr., Prof., etc.)

---

## Password Management

### Password Requirements
| Requirement | Value |
|-------------|-------|
| Minimum Length | 12 characters |
| Uppercase | At least 1 |
| Lowercase | At least 1 |
| Digit | At least 1 |
| Special Character | At least 1 |
| Unique Characters | At least 4 |

### Password Reset Flow (Admin-Mediated)
```
User → Requests Password Reset from Administrator
→ Administrator Creates Password Reset Request
→ User Receives Temporary Password
→ User Logs In with Temporary Password
→ User Changes Password on Next Login
```

### Password Reset Endpoints
- `POST /api/v1/password-reset/requests` - Create password reset request
- `GET /api/v1/password-reset/pending` - View pending requests (Admin)
- `POST /api/v1/password-reset/fulfill` - Fulfill password reset request
- `POST /api/v1/password-reset/reject` - Reject password reset request

### Changing Password
```
POST /api/v1/auth/change-password
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!",
  "confirmPassword": "NewPassword123!"
}
```

---

## Token Revocation

### In-Memory Revocation (Development)
- Uses `IMemoryCache` to store revoked tokens
- Tokens added to deny-list on logout
- Cleared on application restart
- Suitable for single-instance development/testing

### Redis Revocation (Production)
- Uses Redis `SET` with TTL
- Survives application restarts
- Works across multiple instances
- Configured via `RedisTokenRevocation:ConnectionString`

### Logout Flow
```
User → POST /api/v1/auth/logout
→ Extract Token
→ Add Token to Deny-List
→ Clear Auth Cookie
```

---

## Session Management

### Session Configuration
| Setting | Value |
|---------|-------|
| Idle Timeout | 30 minutes |
| Cookie HttpOnly | Yes |
| Cookie IsEssential | Yes |

### Token Refresh
- Access tokens expire after 15 minutes
- Refresh tokens expire after 7 days
- `POST /api/v1/auth/refresh-token` endpoint for token refresh
- Old refresh tokens are revoked on refresh

---

## Account Lockout

### Lockout Policy
| Setting | Value |
|---------|-------|
| Max Failed Attempts | 5 |
| Lockout Duration | 15 minutes |
| Automatic Unlock | Yes (after timeout) |

### Admin Unlock
Administrators can manually unlock accounts:
1. Navigate to **Users** > **User List**
2. Find the locked user
3. Click **Unlock**

---

## Authorization Policies

### Policy Definitions
| Policy | Roles |
|--------|-------|
| `AdministratorAccess` | Administrator |
| `ModeratorAccess` | Administrator, Coordinator |
| `LecturerAccess` | Administrator, Coordinator, Lecturer |
| `StudentAccess` | Administrator, Coordinator, Lecturer, Student |
| `ReceptionistAccess` | Administrator, Coordinator, Receptionist |

### Claim Types
- `sub`: Subject (user ID)
- `name`: Username
- `role`: User role(s)
- `iss`: Issuer
- `aud`: Audience
- `exp`: Expiration time
- `iat`: Issued at time

---

## Security Considerations

### JWT Security Best Practices
- Use strong, random secret keys (32+ characters)
- Store secret in environment variable, not in code
- Use HTTPS in production
- Set short expiration times (15-30 minutes)
- Implement token revocation for logout
- Use httpOnly cookies to prevent XSS token theft
- Validate issuer, audience, and lifetime
- Enforce specific signing algorithm (HS256)

### Common Attacks Mitigated
| Attack | Mitigation |
|--------|------------|
| XSS token theft | httpOnly cookies |
| CSRF | Double-submit cookie pattern |
| Token replay | Short expiration + revocation |
| Algorithm confusion | Explicit algorithm validation |
| Brute force | Account lockout + rate limiting |
| Session hijacking | httpOnly cookies + HTTPS |

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Security Guide](../12-Security/README.md) | Comprehensive security documentation |
| [System Administration](../06-System-Administration/README.md) | User management procedures |
| [Developer Guide](../18-Developer-Guide/README.md) | Authentication implementation details |
| [API Documentation](../17-API/README.md) | Auth endpoints reference |
| [Troubleshooting Guide](../16-Troubleshooting/README.md) | Login and auth issues |
