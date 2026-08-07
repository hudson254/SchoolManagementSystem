# Error Handling Architecture

**System:** School Management System (SMS)  
**Version:** 1.0  
**Status:** Implemented

---

## 1. Overview

This document describes the enterprise error handling architecture for the School Management System. The architecture follows a **layered approach** with clear separation between:

1. **User-Facing Layer** — Safe, user-friendly error messages (never technical details)
2. **API Envelope Layer** — Standardized `{success, code, message}` response contract
3. **Private Diagnostic Layer** — Full technical context (stack traces, user context, request data) stored server-side only
4. **Logging Pipeline** — Centralized, structured, correlated, and enriched logging
5. **Error Repository** — Persistent, searchable error history for administrators

---

## 2. Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENT (React SPA)                           │
│                                                                     │
│  ┌─────────────────┐  ┌──────────────────┐  ┌───────────────────┐  │
│  │  ErrorBoundary  │  │  useApi hook     │  │  api.ts client    │  │
│  │  (friendly UI)  │  │  (retry/offline) │  │  (normalize)      │  │
│  └────────┬────────┘  └────────┬─────────┘  └─────────┬─────────┘  │
│           │                    │                      │            │
└───────────┼────────────────────┼──────────────────────┼────────────┘
            │                    │                      │
            ▼                    ▼                      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        API GATEWAY (ASP.NET Core)                    │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │  Middleware Pipeline                                          │  │
│  │  ┌─────────────────┐  ┌──────────────────┐  ┌──────────────┐  │  │
│  │  │ CorrelationId   │→│ LoggingEnrichment │→│ Exception     │  │  │
│  │  │ Middleware      │  │ Middleware        │  │ Handling     │  │  │
│  │  └─────────────────┘  └──────────────────┘  │ Middleware   │  │  │
│  │                                             └──────┬───────┘  │  │
│  └────────────────────────────────────────────────────┼──────────┘  │
│                                                       │             │
│  ┌────────────────────────────────────────────────────▼──────────┐  │
│  │  ErrorResponse (standardized envelope)                        │  │
│  │  { success, code, message, severity, category, correlationId }│  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │  ErrorLoggingService (centralized pipeline)                   │  │
│  │  • Captures ErrorLogContext (user, role, tenant, session)     │  │
│  │  • Masks sensitive data                                       │  │
│  │  • Structured JSON output                                     │  │
│  └──────────────────────────┬────────────────────────────────────┘  │
│                             │                                       │
│  ┌──────────────────────────▼────────────────────────────────────┐  │
│  │  ErrorRepository (searchable, admin-only)                     │  │
│  │  • Persist ErrorRecord                                        │  │
│  │  • Search/filter/paginate                                     │  │
│  │  • Export CSV                                                 │  │
│  └───────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 3. Component Responsibilities

### 3.1 Frontend Components

| Component | Responsibility |
|-----------|---------------|
| `ErrorBoundary` | Catches React render errors; shows friendly fallback; **never exposes stack traces** |
| `utils/errors.ts` | Normalizes errors to user-friendly messages; detects offline/network/timeout |
| `services/api.ts` | Axios interceptor; normalizes all API errors; handles 401 refresh |
| `hooks/useApi.ts` | Retry-with-backoff for transient errors; offline detection; loading state |

### 3.2 Backend Middleware

| Middleware | Responsibility |
|------------|---------------|
| `CorrelationIdMiddleware` | Generates/propagates correlation ID for distributed tracing |
| `LoggingEnrichmentMiddleware` | Enriches logs with user/role/tenant/session/device/browser/OS context; scrubs sensitive data |
| `ExceptionHandlingMiddleware` | Central exception catch; classifies severity/category; builds standardized envelope |

### 3.3 Backend Services

| Service | Responsibility |
|---------|---------------|
| `ErrorLoggingService` | Centralized logging pipeline; masks sensitive data; structured output |
| `ErrorLogContext` | Captures complete private diagnostic context |
| `ErrorRepository` | Persistent searchable error store; admin-only access |
| `ErrorAdminController` | Admin API for search/filter/export/update error records |

---

## 4. Error Classification Taxonomy

### 4.1 Severity Levels (`ErrorSeverity`)

| Level | Value | Description | Log Level |
|-------|-------|-------------|-----------|
| Information | 0 | Informational, no action required | Information |
| Low | 1 | Minor issue, no user impact | Information |
| Medium | 2 | Partial impact, requires attention | Warning |
| High | 3 | Significant impact, requires prompt attention | Error |
| Critical | 4 | System outage or data loss, immediate attention | Critical |

### 4.2 Categories (`ErrorCategory`)

| Category | Description |
|----------|-------------|
| Validation | Input validation failure |
| Authentication | Authentication failure (invalid credentials, expired token) |
| Authorization | Authorization failure (insufficient permissions) |
| BusinessRule | Business rule violation |
| Database | Database operation failure |
| Infrastructure | Infrastructure failure (file system, caching) |
| Network | Network failure |
| Timeout | Operation timeout |
| Configuration | Configuration error |
| ExternalService | External service failure |
| Unknown | Unclassified or unexpected error |

---

## 5. Standardized API Envelope

### 5.1 Error Response

```json
{
  "success": false,
  "code": "VALIDATION_ERROR",
  "message": "Please correct the highlighted fields and try again.",
  "statusCode": 400,
  "timestamp": "2025-01-01T12:00:00.000Z",
  "correlationId": "abc-123",
  "path": "/api/v1/students",
  "errors": { "Email": ["Email is required."] },
  "severity": "Low",
  "category": "Validation"
}
```

### 5.2 Success Response

```json
{
  "success": true,
  "data": { ... }
}
```

### 5.3 Security Rules

- **NEVER** include stack traces, SQL, file paths, or internal implementation details in production responses.
- `details` field is only populated in Development environment.
- Correlation ID is always included for traceability.

---

## 6. Private Diagnostic Context

The `ErrorLogContext` captures (server-side only):

| Group | Fields |
|-------|--------|
| Request | RequestId, CorrelationId, SessionId, Route, Endpoint, HttpMethod |
| User | UserId, Username, UserRole, TenantId |
| Client | IpAddress, UserAgent, Device, Browser, OperatingSystem |
| Exception | ExceptionType, ExceptionMessage, InnerException, FullStackTrace, SourceFile, LineNumber, Namespace, Assembly, Method |
| Performance | RequestDurationMs, DatabaseDurationMs, ApiDurationMs, MemoryUsageBytes, ThreadId |
| Database | SqlCommand (redacted), DatabaseProvider, TransactionId, RetryCount, ConnectionStatus |

**Sensitive data is always masked** (passwords, tokens, secrets, connection strings, request bodies).

---

## 7. Data Flow

1. **Client request** → `api.ts` sends request with correlation ID header.
2. **CorrelationIdMiddleware** → ensures correlation ID exists.
3. **LoggingEnrichmentMiddleware** → enriches log scope with user/request context.
4. **Controller/Service** → executes business logic.
5. **On exception** → `ExceptionHandlingMiddleware` catches, classifies, builds envelope.
6. **ErrorLoggingService** → captures full diagnostic context, masks sensitive data, logs structured JSON.
7. **ErrorRepository** → persists error record for admin search.
8. **Client receives** → standardized envelope; `api.ts` normalizes; `ErrorBoundary`/UI shows friendly message.

---

## 8. Security Considerations

- Error repository access restricted to `AdministratorAccess` policy.
- All admin error API access is audited.
- Sensitive data masked at the logging boundary.
- Stack traces never leave the server in production.
- Correlation IDs enable cross-reference between user-facing errors and private logs.
