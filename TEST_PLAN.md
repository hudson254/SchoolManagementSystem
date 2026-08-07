# Error Handling Test Plan

**System:** School Management System (SMS)  
**Version:** 1.0  
**Status:** Implemented

---

## 1. Overview

This document defines the automated test strategy for the enterprise error handling framework. The goal is to verify that:

1. Users **never** encounter unhandled errors.
2. Users **always** receive helpful, non-technical messages.
3. Full technical details are **always** logged privately.
4. Sensitive data is **never** exposed.
5. Users can **recover** whenever possible.

---

## 2. Test Layers

### 2.1 Backend Unit/Integration Tests

| Test File | Coverage |
|-----------|----------|
| `tests/SMS.ApiTests/Middleware/ExceptionHandlingMiddlewareTests.cs` | Envelope shape, severity/category classification, no stack trace in production, dev-only details |
| `tests/SMS.ApiTests/Logging/ErrorLoggingServiceTests.cs` | Sensitive data masking, diagnostic context capture, severity→log-level mapping |

### 2.2 Frontend Vitest Tests

| Test File | Coverage |
|-----------|----------|
| `frontend/sms-web/src/utils/errors.test.ts` | Error normalization, offline/network/timeout detection, field error extraction, friendly message mapping |
| `frontend/sms-web/src/components/Common/ErrorBoundary.test.tsx` | Friendly fallback, **no stack trace exposure**, recovery via Try Again |

---

## 3. Test Cases

### 3.1 ExceptionHandlingMiddlewareTests

| # | Test | Expected |
|---|------|----------|
| 1 | Production response does not expose stack trace | `details` field absent; no sensitive data in body |
| 2 | Production response honors standardized envelope | `success=false`, `code`, `message`, `statusCode`, `severity`, `category`, `correlationId` |
| 3 | ValidationException classification | 400, `VALIDATION_ERROR`, Low, Validation |
| 4 | DatabaseException classification | 500, `DB_ERROR`, High, Database |
| 5 | UnauthorizedException classification | 401, `UNAUTHORIZED`, Medium, Authentication |
| 6 | ForbiddenException classification | 403, `FORBIDDEN`, Medium, Authorization |
| 7 | NotFoundException classification | 404, `NOT_FOUND`, Low, Validation |
| 8 | Development environment includes details | `details` field present |

### 3.2 ErrorLoggingServiceTests

| # | Test | Expected |
|---|------|----------|
| 1 | Sensitive data masked in exception messages | No exception; masking succeeds |
| 2 | Diagnostic context captured | Completes without error |
| 3 | Sensitive extra context masked | No exception; masking succeeds |
| 4 | User/request info extracted from HttpContext | Completes without error |

### 3.3 errors.test.ts (Frontend)

| # | Test | Expected |
|---|------|----------|
| 1 | Validation error normalized | Friendly message, field errors preserved |
| 2 | Known error codes mapped | Friendly message |
| 3 | Unknown errors fall back | Generic message, no raw stack trace |
| 4 | 401 → session expired | `SESSION_EXPIRED` code |
| 5 | 403 → access denied | `ACCESS_DENIED` code |
| 6 | 404 → not found | `NOT_FOUND` code |
| 7 | Timeout detected | `isTimeout=true` |
| 8 | Network error detected | `isNetworkError=true` |
| 9 | Offline detected | `NETWORK_OFFLINE` code |
| 10 | Field errors extracted | Correct mapping |
| 11 | No field errors → undefined | `undefined` |
| 12 | isOffline returns navigator state | Correct boolean |

### 3.4 ErrorBoundary.test.tsx (Frontend)

| # | Test | Expected |
|---|------|----------|
| 1 | Renders children when no error | Normal content visible |
| 2 | Shows friendly fallback on error | "Something went wrong", "Try Again" |
| 3 | **Never exposes raw error/stack trace** | No raw message, no `at SMS.API`, no `password=`, no `<pre>` |
| 4 | Recovers after Try Again | Recovered content visible |

---

## 4. Execution Commands

### 4.1 Backend Tests

```
dotnet test tests/SMS.ApiTests/SMS.ApiTests.csproj
```

### 4.2 Frontend Tests

```
cd frontend/sms-web
npm run test
```

---

## 5. Acceptance Criteria

- [ ] All backend tests pass.
- [ ] All frontend Vitest tests pass.
- [ ] No stack traces exposed in production responses.
- [ ] No stack traces exposed in the frontend ErrorBoundary.
- [ ] Sensitive data masked in all log output.
- [ ] Standardized envelope honored in all error responses.
- [ ] Severity/category classification correct for all exception types.
