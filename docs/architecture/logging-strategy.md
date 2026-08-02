# Logging Strategy

## Overview
The School Management System uses Serilog for structured logging with correlation IDs, sensitive data scrubbing, and environment-specific configuration.

## Logging Pipeline
```
Application → Serilog Logger → Enrichers → Sinks
                                    ↓
                            CorrelationId
                            MachineName
                            ThreadId
                            Environment
```

## Log Levels by Environment

| Environment | Default Level | Microsoft Level | EF Core Level |
|-------------|---------------|-----------------|---------------|
| Development | Debug | Information | Information |
| Test | Warning | Warning | Warning |
| Staging | Warning | Warning | Warning |
| Production | Warning | Warning | Warning |

## Structured Log Properties
Every log entry includes:
- `CorrelationId` - Request tracing identifier
- `RequestMethod` - HTTP method
- `RequestPath` - API endpoint path
- `UserAgent` - Client user agent
- `RemoteIp` - Client IP address
- `Duration` - Request processing time

## Sensitive Data Scrubbing
The following data is automatically redacted from logs:
- Authorization headers
- API keys
- Cookies
- Passwords in query strings
- Tokens in query strings
- Secrets in query strings

## Log Sinks

### Development
- Console (colored output)
- File (rolling daily)

### Production
- Console (JSON format)
- File (rolling daily, 90-day retention)
- Application Insights (optional)

## What to Log
- Exceptions with full stack traces (server-side only)
- Authentication events (login, logout, failures)
- Authorization failures
- User activities
- Database failures
- External API failures
- File uploads
- Report generation
- Background jobs
- Configuration issues

## What NOT to Log
- Passwords
- Tokens
- Encryption keys
- Personally identifiable information (PII)
- Credit card numbers
- Social security numbers

## Audit Logging
Audit events are logged separately through the `IAuditService` interface and persisted to the database. These records are immutable and capture all sensitive user actions.
