# Environment Configuration Guide

## Overview
This guide explains how to configure the School Management System for different environments.

## Configuration Files

| File | Environment | Purpose |
|------|-------------|---------|
| `appsettings.json` | All | Base configuration (shared settings) |
| `appsettings.Development.json` | Development | Local development settings |
| `appsettings.Test.json` | Test | Automated test settings |
| `appsettings.Testing.json` | Testing | Testing environment settings |
| `appsettings.Staging.json` | Staging | Pre-production staging |
| `appsettings.Production.json` | Production | Production settings |

## Environment Detection
The application automatically detects the environment using the `ASPNETCORE_ENVIRONMENT` environment variable:
- `Development` - Local development
- `Test` - Automated testing
- `Testing` - Testing environment
- `Staging` - Pre-production
- `Production` - Production

## Configuration Sections

### ConnectionStrings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=...;Username=...;Password=...",
    "HangfireConnection": "Host=...;Database=...;Username=...;Password=..."
  }
}
```

### JwtSettings
```json
{
  "JwtSettings": {
    "Secret": "",  // Use JWT_SECRET environment variable
    "Issuer": "SMSAPI",
    "Audience": "SMSWeb",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### SMTP
```json
{
  "SMTP": {
    "Host": "",
    "Port": 587,
    "Username": "",
    "Password": "",
    "From": "",
    "EnableSsl": true
  }
}
```

### Feature Flags
```json
{
  "FeatureManagement": {
    "EnableSwagger": false,
    "EnableDetailedErrors": false,
    "EnableTelemetry": true,
    "EnableAuditLogging": true,
    "EnableRateLimiting": true,
    "EnableAccountLockout": true,
    "MaintenanceMode": false
  }
}
```

## Environment-Specific Settings

### Development
- Debug logging level
- Local database connection
- Swagger enabled
- Longer token expiration (120 min)
- CORS allows localhost:5173

### Test
- Warning logging level
- Test database connection
- High rate limit (1000 req/min)
- Console logging only

### Staging
- Warning logging level
- Kestrel endpoints configured
- 30-minute token expiration
- File logging with 30-day retention

### Production
- Warning logging level
- No secrets in source
- 15-minute token expiration
- File logging with 90-day retention
- HTTPS required
- Swagger disabled
- Detailed errors disabled

## Security Best Practices
1. Never commit secrets to source control
2. Use environment variables for sensitive values
3. Use User Secrets in development
4. Rotate JWT secrets regularly
5. Use different database credentials per environment
6. Enable HTTPS in production
7. Configure CORS with specific origins
