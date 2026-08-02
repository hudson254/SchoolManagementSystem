# Error Handling Architecture

## Overview
The School Management System implements an enterprise-grade error handling architecture that provides consistent, structured error responses across all API endpoints. The system uses a centralized exception handling middleware with a custom exception hierarchy.

## Architecture Diagram
```
Request → CorrelationIdMiddleware → LoggingEnrichmentMiddleware → ExceptionHandlingMiddleware → Controller
                                                                          ↓
                                                              Exception Classification
                                                                          ↓
                                                              Structured Error Response
                                                                          ↓
                                                              Logged with Correlation ID
```

## Exception Hierarchy

### Base Exceptions
- `SMS.Application.Exceptions.ValidationException` - Input validation failures
- `SMS.Application.Exceptions.NotFoundException` - Resource not found
- `SMS.Application.Exceptions.UnauthorizedException` - Authentication failures
- `SMS.Application.Exceptions.ForbiddenException` - Authorization failures
- `SMS.Application.Exceptions.ConflictException` - Resource conflicts
- `SMS.Application.Exceptions.BusinessRuleException` - Business rule violations

### Infrastructure Exceptions
- `SMS.Application.Exceptions.DatabaseException` - Database operation failures
- `SMS.Application.Exceptions.ExternalServiceException` - External service failures
- `SMS.Application.Exceptions.FileSystemException` - File operation failures
- `SMS.Application.Exceptions.NetworkException` - Network failures
- `SMS.Application.Exceptions.TimeoutException` - Operation timeouts
- `SMS.Application.Exceptions.BackgroundJobException` - Background job failures

## Error Response Format
```json
{
  "statusCode": 400,
  "errorCode": "VALIDATION_ERROR",
  "message": "User-friendly error message",
  "timestamp": "2026-07-28T12:00:00Z",
  "correlationId": "abc-123-def",
  "path": "/api/v1/resource",
  "errors": { "FieldName": ["Error description"] },
  "details": null
}
```

## Middleware Pipeline Order
1. CorrelationIdMiddleware - Adds correlation ID to requests
2. LoggingEnrichmentMiddleware - Scrubs sensitive data from logs
3. ExceptionHandlingMiddleware - Catches and handles all exceptions
4. SecurityHeadersMiddleware - Adds security headers
5. TenantResolutionMiddleware - Resolves tenant context
6. RateLimitingMiddleware - Rate limits requests

## Exception Flow
1. Exception thrown in controller/handler
2. ExceptionHandlingMiddleware catches it
3. Exception is classified by type
4. Appropriate HTTP status code and error code assigned
5. User-friendly message selected (no stack traces exposed)
6. Error logged with full details (server-side only)
7. Structured error response returned to client

## User-Friendly Messages
All error messages are defined in `SMS.Application.Common.ErrorMessages` class:
- Explain what happened
- Explain what the user can do next
- Avoid exposing sensitive technical details
- Support localization for future multilingual deployments
