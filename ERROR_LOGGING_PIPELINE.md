# Error Logging Pipeline

**System:** School Management System (SMS)  
**Version:** 1.0  
**Status:** Implemented

---

## 1. Overview

This document describes the centralized error logging pipeline for the School Management System. All error logging flows through this pipeline to ensure consistent, structured, correlated, and enriched logging while protecting sensitive data.

---

## 2. Pipeline Flow

```
Error Occurs
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│  ExceptionHandlingMiddleware                                 │
│  • Catches exception                                        │
│  • Classifies severity & category                            │
│  • Builds standardized envelope                              │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  ErrorLoggingService (IErrorLoggingService)                  │
│  • Captures ErrorLogContext (full diagnostic context)        │
│  • Masks sensitive data                                     │
│  • Maps severity → log level                                │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Serilog Structured Logging                                 │
│  • Structured JSON output (logs/sms-.json)                  │
│  • Rolling text output (logs/sms-.txt)                      │
│  • Console output (development)                             │
│  • Enriched with Application & Environment properties       │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  ErrorRepository (persist for admin search)                  │
│  • Persists ErrorRecord                                     │
│  • Searchable by user/tenant/category/severity/route        │
│  • Accessible via ErrorAdminController (admin-only)         │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Components

### 3.1 ErrorLogContext

Captures the complete private diagnostic context for an error:

| Group | Fields |
|-------|--------|
| Request | RequestId, CorrelationId, SessionId, Route, Endpoint, HttpMethod |
| User | UserId, Username, UserRole, TenantId |
| Client | IpAddress, UserAgent, Device, Browser, OperatingSystem |
| Exception | ExceptionType, ExceptionMessage, InnerException, FullStackTrace, SourceFile, LineNumber, Namespace, Assembly, Method |
| Performance | RequestDurationMs, DatabaseDurationMs, ApiDurationMs, MemoryUsageBytes, ThreadId |
| Database | SqlCommand, DatabaseProvider, TransactionId, RetryCount, ConnectionStatus |
| Request Data | QueryParameters, RouteParameters, RequestBody, FormData |

### 3.2 ErrorLoggingService

The centralized logging service provides:

- `LogExceptionAsync(HttpContext, Exception, category, severity)` — captures full context from HTTP context.
- `LogExceptionAsync(ErrorLogContext)` — logs a pre-built context.
- `LogAsync(message, level, extraContext)` — general structured logging.

### 3.3 Sensitive Data Masking

The pipeline **always masks** the following:

| Category | Masked Fields |
|----------|---------------|
| Credentials | password, token, secret, apiKey, api_key, access_token, refresh_token |
| Headers | authorization, cookie, set-cookie, x-csrf-token, x-xsrf-token |
| Connection | connectionString |
| Financial | cardNumber, cvv, pin |
| Database | SqlCommand (always redacted) |
| Request Body | Always redacted |

---

## 4. Log Enrichment

The `LoggingEnrichmentMiddleware` enriches all log scopes with:

- CorrelationId
- RequestMethod, RequestPath, RequestQueryString
- UserAgent, RemoteIp
- UserId, Username, UserRole
- TenantId, SessionId
- Device, Browser, OperatingSystem (parsed from User-Agent)

---

## 5. Log Levels

Severity is mapped to log level:

| Severity | Log Level |
|----------|-----------|
| Critical | Critical |
| High | Error |
| Medium | Warning |
| Low | Information |
| Information | Information |

---

## 6. Output Sinks

| Sink | Path | Format |
|------|------|--------|
| Console | - | Text template |
| Rolling File | `logs/sms-.json` | Structured JSON |
| Rolling File | `logs/sms-.txt` | Text template |

Enrichment:
- `Application` = "SchoolManagementSystem"
- `Environment` = current ASP.NET Core environment

---

## 7. Error Repository

The `ErrorRepository` persists `ErrorRecord` for administrative investigation:

- **Storage:** In-memory (single-instance). Replace with database-backed for horizontal scaling.
- **Search filters:** Date range, userId, tenantId, module, category, severity, route, correlationId, sessionId, keyword, exceptionType.
- **Pagination:** page/pageSize with total count.
- **Sort:** timestamp, severity, category, username.
- **Export:** CSV export endpoint.
- **Resolution:** Update status, assignee, notes.

### 7.1 Admin API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/admin/errors` | GET | Search with filters & pagination |
| `/api/v1/admin/errors/{id}` | GET | Get single error record |
| `/api/v1/admin/errors/recent` | GET | Recent errors feed |
| `/api/v1/admin/errors/{id}` | PATCH | Update resolution status |
| `/api/v1/admin/errors/export` | GET | Export CSV |

All endpoints require `AdministratorAccess` policy.

---

## 8. Security

- **Sensitive data is masked at the logging boundary** — never written to logs.
- **Full stack traces are stored server-side only** — never in client responses.
- **Error repository access is admin-only** and audited.
- **Correlation IDs** enable cross-referencing user-facing errors with private logs.

---

## 9. Configuration

Serilog is configured in `Program.cs`:

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SchoolManagementSystem")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(...)
    .WriteTo.File(path: "logs/sms-.json", formatter: new JsonFormatter())
    .WriteTo.File(path: "logs/sms-.txt", ...)
    .CreateLogger();
