# Error Handling Standards

**System:** School Management System (SMS)  
**Version:** 1.0  
**Status:** Implemented

---

## 1. Purpose

This document defines the mandatory error handling standards for all developers working on the School Management System. These standards ensure consistency, security, and maintainability across the entire codebase.

---

## 2. Core Principles

### 2.1 User Experience
- **Never expose technical details** (stack traces, SQL, file paths, internal implementation) to end users.
- **Always provide actionable guidance** — explain what happened and what the user can do next.
- **Use plain, friendly language** — no error codes or technical jargon in user-facing messages.

### 2.2 Security
- **Mask all sensitive data** (passwords, tokens, secrets, connection strings) in logs and errors.
- **Store full diagnostic context server-side only** — never in client responses.
- **Restrict error repository access** to authorized administrators only.

### 2.3 Consistency
- **Always use the standardized envelope** `{success, code, message}`.
- **Always classify errors** with severity and category.
- **Always include correlation ID** for traceability.

---

## 3. Backend Standards

### 3.1 Exception Hierarchy

Use the appropriate exception type from `SMS.Application.Exceptions`:

| Exception | When to Use | HTTP Status |
|-----------|-------------|-------------|
| `ValidationException` | Input validation failure | 400 |
| `NotFoundException` | Resource not found | 404 |
| `UnauthorizedException` | Authentication failure | 401 |
| `ForbiddenException` | Authorization failure | 403 |
| `ConflictException` | State conflict | 409 |
| `BusinessRuleException` | Business rule violation | 400 |
| `DatabaseException` | Database operation failure | 500 |
| `ExternalServiceException` | External service failure | 502 |
| `FileSystemException` | File system failure | 500 |
| `NetworkException` | Network failure | 502 |
| `TimeoutException` | Operation timeout | 408 |
| `BackgroundJobException` | Background job failure | 500 |

### 3.2 Creating Exceptions

```
csharp
// DO — use the built-in exception hierarchy
throw new NotFoundException("The student with ID 123 was not found.");

// DO — include a clear, user-friendly message
throw new BusinessRuleException("This course has reached its maximum capacity.");

// DON'T — throw generic exceptions
throw new Exception("Something broke.");
```

### 3.3 Validation Errors

Use `ValidationException` with field-level errors:

```csharp
throw new ValidationException(new[]
{
    new ValidationError("Email", "Email is required."),
    new ValidationError("Age", "Age must be between 0 and 120.")
});
```

### 3.4 Controller Actions

```
csharp
// DO — let the exception middleware handle errors
[HttpGet("{id}")]
public async Task<ActionResult<StudentDto>> Get(int id)
{
    var student = await _mediator.Send(new GetStudentQuery(id));
    return Ok(student);
}

// DON'T — wrap everything in try/catch
```

### 3.5 Logging

- **Always log through** `IErrorLoggingService` (the centralized pipeline).
- **Never log raw sensitive data** — the pipeline masks it automatically.
- **Use structured logging** — never string concatenation.

---

## 4. Frontend Standards

### 4.1 Error Display

```
tsx
// DO — use the ErrorBoundary for render errors
<ErrorBoundary>
  <Dashboard />
</ErrorBoundary>

// DO — normalize all API errors
import { normalizeError } from '../utils/errors';

try {
  await api.getStudents();
} catch (error) {
  const normalized = normalizeError(error);
  showToast(normalized.message);
}
```

### 4.2 Never Expose

- **NEVER** render `error.message` directly from an uncaught error.
- **NEVER** render `error.stack` or `componentStack`.
- **ALWAYS** use `normalizeError()` to produce user-safe messages.

### 4.3 API Calls

```
tsx
// DO — use useApi hook with retry/offline handling
const { data, loading, error, execute } = useApi<Student[]>();

// DO — let api.ts normalize errors
const { data } = await apiClient.get('/students');
```

---

## 5. Error Classification

### 5.1 Severity

| Level | Guidance |
|-------|----------|
| Critical | System outage, data loss, security breach — immediate action |
| High | Significant user impact, requires prompt attention |
| Medium | Partial impact, requires attention |
| Low | Minor issue, no user impact |
| Information | Informational only |

### 5.2 Category

| Category | Guidance |
|----------|----------|
| Validation | Bad input from user |
| Authentication | Login/session failures |
| Authorization | Permission denied |
| BusinessRule | Business logic violation |
| Database | DB connectivity/query failures |
| Infrastructure | File system, caching, etc. |
| Network | Network connectivity failures |
| Timeout | Operation exceeded time limit |
| Configuration | Misconfiguration |
| ExternalService | Third-party service failures |
| Unknown | Unclassified |

---

## 6. API Envelope Contract

### 6.1 Error Response

```
json
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

### 6.2 Success Response

```json
{
  "success": true,
  "data": { ... }
}
```

---

## 7. Testing Standards

- **All error-handling changes** must include tests.
- **Backend tests** verify: envelope shape, severity/category classification, no stack trace in production.
- **Frontend tests** verify: error normalization, no stack trace exposure, offline/network detection.

---

## 8. Code Review Checklist

- [ ] No raw exception messages exposed to users
- [ ] No sensitive data logged (passwords, tokens, secrets)
- [ ] Correlation ID included in responses
- [ ] Error classified with severity and category
- [ ] Standardized envelope used
- [ ] Tests added/updated
- [ ] Documentation updated if behavior changed
