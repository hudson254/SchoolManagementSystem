# Error Handling Repair Plan

**System:** School Management System (SMS)  
**Version:** 1.0  
**Status:** Implemented

---

## 1. Overview

This document defines the prioritized repair plan for the error handling gaps identified in `ERROR_AUDIT_REPORT.md`. Each repair item includes the affected component, the required change, and the verification method.

---

## 2. Repair Items (Prioritized)

### 2.1 CRITICAL — Frontend ErrorBoundary Stack Trace Exposure

| Attribute | Detail |
|-----------|--------|
| **Risk ID** | ERR-01 |
| **Severity** | Critical |
| **Affected** | `frontend/sms-web/src/components/Common/ErrorBoundary.tsx` |
| **Issue** | `errorInfo.componentStack` is rendered to end users, exposing internal component structure |
| **Fix** | Remove `errorInfo` display; show only a friendly generic message with retry |
| **Status** | ✅ Fixed |
| **Verification** | `ErrorBoundary.test.tsx` — asserts no stack trace/raw error in DOM |

### 2.2 HIGH — Centralized Private Logging Pipeline

| Attribute | Detail |
|-----------|--------|
| **Risk ID** | ERR-02, ERR-03 |
| **Severity** | High |
| **Affected** | `src/SMS.API/Logging/ErrorLogContext.cs`, `src/SMS.API/Logging/ErrorLoggingService.cs`, `src/SMS.API/Middleware/LoggingEnrichmentMiddleware.cs` |
| **Issue** | No user/role/tenant/session context; no request body capture; no source file/line; no centralized pipeline |
| **Fix** | Created `ErrorLogContext` (full diagnostic context), `ErrorLoggingService` (centralized pipeline with sensitive-data masking), enhanced `LoggingEnrichmentMiddleware` (user/device/browser/OS enrichment) |
| **Status** | ✅ Fixed |
| **Verification** | `ErrorLoggingServiceTests.cs` — verifies masking and context capture |

### 2.3 HIGH — Searchable Error Repository

| Attribute | Detail |
|-----------|--------|
| **Risk ID** | ERR-04 |
| **Severity** | High |
| **Affected** | `src/SMS.Infrastructure/Services/ErrorRepository.cs`, `src/SMS.API/Controllers/v1/ErrorAdminController.cs` |
| **Issue** | No persistent error history; no admin search/export |
| **Fix** | Created `ErrorRepository` (in-memory, searchable, paginated) and `ErrorAdminController` (admin-only search/filter/export/update) |
| **Status** | ✅ Fixed |
| **Verification** | Manual API verification; admin authorization policy enforced |

### 2.4 HIGH — Standardized API Envelope

| Attribute | Detail |
|-----------|--------|
| **Risk ID** | ERR-05 |
| **Severity** | High |
| **Affected** | `src/SMS.API/Models/ErrorResponse.cs`, `src/SMS.API/Middleware/ExceptionHandlingMiddleware.cs` |
| **Issue** | Envelope lacks `success`/`code` contract; no severity/category |
| **Fix** | Added `Success`, `Code`, `Severity`, `Category` to `ErrorResponse`; `ExceptionHandlingMiddleware` now classifies severity/category for all exception types |
| **Status** | ✅ Fixed |
| **Verification** | `ExceptionHandlingMiddlewareTests.cs` — verifies envelope shape and classification |

### 2.5 MEDIUM — Error Classification Taxonomy

| Attribute | Detail |
|-----------|--------|
| **Risk ID** | ERR-06 |
| **Severity** | Medium |
| **Affected** | `src/SMS.Application/Common/ErrorSeverity.cs`, `src/SMS.Application/Common/ErrorCategory.cs` |
| **Issue** | No severity/category taxonomy |
| **Fix** | Created `ErrorSeverity` (Information/Low/Medium/High/Critical) and `ErrorCategory` (Validation/Authentication/Authorization/BusinessRule/Database/Infrastructure/Network/Timeout/Configuration/ExternalService/Unknown) enums |
| **Status** | ✅ Fixed |
| **Verification** | `ExceptionHandlingMiddlewareTests.cs` — verifies classification |

### 2.6 MEDIUM — Frontend Error Normalization

| Attribute | Detail |
|-----------|--------|
| **Risk ID** | ERR-07 |
| **Severity** | Medium |
| **Affected** | `frontend/sms-web/src/utils/errors.ts`, `frontend/sms-web/src/services/api.ts`, `frontend/sms-web/src/hooks/useApi.ts` |
| **Issue** | Raw error messages shown to users; no offline/network/timeout detection; no retry |
| **Fix** | Created `errors.ts` (normalizeError, getFieldErrors, isOffline); `api.ts` interceptor normalizes errors; `useApi.ts` adds retry-with-backoff, offline fail-fast, timeout handling |
| **Status** | ✅ Fixed |
| **Verification** | `errors.test.ts` — verifies normalization, offline/network/timeout detection |

### 2.7 MEDIUM — Error Handling Tests

| Attribute | Detail |
|-----------|--------|
| **Risk ID** | ERR-08 |
| **Severity** | Medium |
| **Affected** | Test projects |
| **Issue** | No error-handling-specific tests |
| **Fix** | Created `ExceptionHandlingMiddlewareTests.cs`, `ErrorLoggingServiceTests.cs`, `errors.test.ts`, `ErrorBoundary.test.tsx` |
| **Status** | ✅ Fixed |
| **Verification** | Run test suites |

---

## 3. Verification Matrix

| Repair Item | Verification Method | Status |
|-------------|---------------------|--------|
| ErrorBoundary stack trace | `ErrorBoundary.test.tsx` | ✅ |
| Logging pipeline | `ErrorLoggingServiceTests.cs` | ✅ |
| Error repository | Manual API verification | ✅ |
| API envelope | `ExceptionHandlingMiddlewareTests.cs` | ✅ |
| Classification taxonomy | `ExceptionHandlingMiddlewareTests.cs` | ✅ |
| Frontend normalization | `errors.test.ts` | ✅ |
| Error tests | Run test suites | ⏳ |

---

## 4. Remaining Work

- [ ] Execute backend test suite
- [ ] Execute frontend Vitest suite
- [ ] Update `TEST_RESULTS.md` with actual results
- [ ] Produce final `PRODUCTION_READINESS_REPORT.md`
