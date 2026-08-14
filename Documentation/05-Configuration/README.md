# Configuration Guide

## Table of Contents
- [Configuration Overview](#configuration-overview)
- [Configuration Files](#configuration-files)
- [Environment Variables](#environment-variables)
- [Appsettings Sections](#appsettings-sections)
- [Docker Configuration](#docker-configuration)
- [Nginx Configuration](#nginx-configuration)
- [Prometheus & Grafana Configuration](#prometheus--grafana-configuration)
- [Security Configuration](#security-configuration)
- [Database Configuration](#database-configuration)
- [Related Documentation](#related-documentation)

---

## Configuration Overview

The School Management System uses a hierarchical configuration system that loads settings from multiple sources in order of priority:

1. Environment variables (highest priority)
2. Command-line arguments
3. Environment-specific appsettings (e.g., `appsettings.Production.json`)
4. Base `appsettings.json`
5. User secrets (development only)

---

## Configuration Files

### Application Configuration Files

| File | Environment | Purpose |
|------|-------------|---------|
| `src/SMS.API/appsettings.json` | All | Base configuration |
| `src/SMS.API/appsettings.Development.json` | Development | Development overrides |
| `src/SMS.API/appsettings.Testing.json` | Testing | Testing environment |
| `src/SMS.API/appsettings.Staging.json` | Staging | Pre-production setup |
| `src/SMS.API/appsettings.Production.json` | Production | Production settings |

### Environment Configuration

| File | Purpose |
|------|---------|
| `.env` | Local environment variables |
| `.env.example` | Example environment file template |
| `docker/docker-compose.yml` | Docker service configuration |
| `docker/docker-compose.override.yml` | Docker overrides |
| `docker/docker-compose.dev.yml` | Development Docker config |
| `docker/docker-compose.prod.yml` | Production Docker config |

---

## Environment Variables

### Database Configuration
| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `DB_PASSWORD` | ✅ Yes | - | PostgreSQL database password |
| `ConnectionStrings__DefaultConnection` | No | appsettings value | Full connection string override |

### JWT Configuration
| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `JWT_SECRET` | ✅ Yes | - | JWT signing key (min 32 characters) |
| `JWT__Issuer` | No | SMSAPI | Token issuer |
| `JWT__Audience` | No | SMSWeb | Token audience |
| `JWT__ExpiryMinutes` | No | 60 | Access token expiry in minutes |

### Redis Configuration
| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `RedisTokenRevocation__ConnectionString` | No | - | Redis connection string for production token revocation |

### Nginx Configuration
| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `NGINX_HTTP_PORT` | No | 8080 | Nginx HTTP listener port |
| `NGINX_HTTPS_PORT` | No | 8443 | Nginx HTTPS listener port |

### Grafana Configuration
| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `GRAFANA_PASSWORD` | ✅ Yes | - | Grafana admin password |

### Backup Configuration
| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `BACKUP_INTERVAL` | No | 86400 | Backup interval in seconds |
| `BACKUP_RETENTION_DAYS` | No | 30 | Days to retain backups |

---

## Appsettings Sections

### ConnectionStrings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=SchoolManagementSystem;Username=sms_user;Password=SecurePassword123!;Minimum Pool Size=1;Maximum Pool Size=10;",
    "HangfireConnection": ""
  }
}
```

### JwtSettings
```json
{
  "JwtSettings": {
    "Secret": "",
    "Issuer": "SMSAPI",
    "Audience": "SMSWeb",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### SMTP (Currently Disabled)
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

### FileStorage
```json
{
  "FileStorage": {
    "Path": "uploads",
    "MaxFileSizeMB": 10,
    "AllowedExtensions": [".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".zip"]
  }
}
```

### Tenant Configuration
```json
{
  "Tenant": {
    "DefaultTenantId": "11111111-1111-1111-1111-111111111111"
  }
}
```

### Backup Configuration
```json
{
  "Backup": {
    "IntervalHours": 24,
    "RetentionDays": 30,
    "Path": "/var/backups/sms"
  }
}
```

### Rate Limiting
```json
{
  "RateLimiting": {
    "PermitLimit": 20,
    "WindowMinutes": 1,
    "BanDurationMinutes": 15
  }
}
```

### Report Verification
```json
{
  "ReportVerification": {
    "BaseUrl": "https://localhost:5001",
    "VerificationEndpoint": "/api/v1/verify/report",
    "DefaultWatermarkText": "Official System Generated Report",
    "WatermarkEnabled": true,
    "QrCodeSize": 150,
    "QrCodePlacement": "Footer",
    "TokenLength": 64,
    "HashAlgorithm": "SHA-256",
    "ExpirationHours": 0,
    "EnabledReportTypes": ["StudentReport", "AttendanceReport", "GradeReport", "FinanceReport", "Transcript", "ExaminationReport", "StaffReport", "AuditReport", "SystemReport", "AdministrativeReport", "CustomReport"]
  }
}
```

### Title Configuration
```json
{
  "TitleConfiguration": {
    "Titles": [
      { "Code": "Dr", "DisplayText": "Dr.", "Language": "en", "Category": "Academic", "SortOrder": 1, "IsActive": true },
      { "Code": "Prof", "DisplayText": "Prof.", "Language": "en", "Category": "Academic", "SortOrder": 2, "IsActive": true }
    ]
  }
}
```

### Logging (Serilog)
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/sms-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### Identity/Password Policy
```json
{
  "Identity": {
    "Password": {
      "RequiredLength": 12,
      "RequireDigit": true,
      "RequireLowercase": true,
      "RequireUppercase": true,
      "RequireNonAlphanumeric": true,
      "RequiredUniqueChars": 4
    },
    "Lockout": {
      "MaxFailedAccessAttempts": 5,
      "DefaultLockoutTimeSpan": "00:15:00"
    }
  }
}
```

### Swagger
```json
{
  "Swagger__Enabled": false
}
```
> **⚠️ SECURITY**: Swagger should be `false` in production. Enable only in development/staging.

---

## Docker Configuration

### Docker Compose Services

The system runs as multiple Docker containers:

| Service | Image | Container Name | Purpose |
|---------|-------|----------------|---------|
| postgres | postgres:16-alpine | sms-postgres | Database |
| api | Custom build | sms-api | Backend API |
| frontend | Custom build | sms-web | React frontend |
| nginx | nginx:alpine | sms-nginx | Reverse proxy |
| backup | Custom build | sms-backup | Automated backups |
| prometheus | prom/prometheus | sms-prometheus | Metrics collection |
| grafana | grafana/grafana | sms-grafana | Monitoring dashboards |

### Docker Compose Configuration Files

| File | Purpose |
|------|---------|
| `docker/docker-compose.yml` | Main service definitions |
| `docker/docker-compose.override.yml` | Development overrides |
| `docker/docker-compose.dev.yml` | Development-specific configuration |
| `docker/docker-compose.prod.yml` | Production-specific configuration |

### Dockerfile Locations

| File | Builds |
|------|--------|
| `docker/Dockerfile.api` | API backend container |
| `docker/Dockerfile.frontend` | Frontend container |
| `docker/Dockerfile.backup` | Backup service container |

### Docker Volumes

| Volume | Purpose |
|--------|---------|
| `postgres_data` | Persistent database storage |
| `api_logs` | API log files |
| `api_data` | API application data |
| `api_uploads` | User uploaded files |
| `backup_data` | Backup storage |
| `prometheus_data` | Prometheus metrics storage |
| `grafana_data` | Grafana data and dashboards |

---

## Nginx Configuration

### Main Configuration

The Nginx configuration is located at `docker/nginx.conf`. Key settings include:

- **SSL/TLS termination**: HTTPS handling
- **Reverse proxy**: Routing /api/* to the backend
- **Static files**: Serving the frontend build
- **Security headers**: HSTS, XSS protection, content type options
- **Gzip compression**: Response compression
- **Client max body size**: File upload limits
- **Rate limiting**: Connection rate limiting

### Frontend Nginx Configuration

The frontend Nginx configuration at `docker/nginx-frontend.conf` handles:
- Static file serving for built React app
- SPA routing (fallback to index.html)
- Cache headers for static assets
- Gzip compression

---

## Prometheus & Grafana Configuration

### Prometheus Configuration

| File | Purpose |
|------|---------|
| `docker/prometheus.yml` | Main Prometheus config |
| `docker/prometheus-alerts.yml` | Alerting rules |

### Grafana Configuration

| File | Purpose |
|------|---------|
| `docker/grafana-datasources/datasource.yml` | Prometheus data source |
| `docker/grafana-dashboards/dashboard-provider.yml` | Dashboard provisioning |
| `docker/grafana-dashboards/sms-infrastructure.json` | Infrastructure dashboard |

---

## Security Configuration

### CORS
Configured in `Program.cs`:
```csharp
options.AddPolicy("AllowFrontend", policy =>
{
    policy.WithOrigins(builder.Configuration.GetValue<string>("Frontend:Url") ?? "http://localhost:5173")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
});
```

### Rate Limiting
Configured in `appsettings.json`:
```json
{
  "RateLimiting": {
    "PermitLimit": 20,
    "WindowMinutes": 1,
    "BanDurationMinutes": 15
  }
}
```

### CSRF Protection
The CSRF middleware uses a double-submit cookie pattern. Configuration is handled in `CsrfProtectionMiddleware.cs`.

### Security Headers
Security headers are applied by `SecurityHeadersMiddleware.cs`:
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Referrer-Policy: strict-origin-when-cross-origin
- Strict-Transport-Security (HSTS) in production

---

## Database Configuration

### Connection String Options

| Parameter | Description |
|-----------|-------------|
| `Host` | Database server hostname |
| `Port` | Database server port (default: 5432) |
| `Database` | Database name |
| `Username` | Database user |
| `Password` | Database password |
| `Minimum Pool Size` | Minimum connection pool size |
| `Maximum Pool Size` | Maximum connection pool size |

### EF Core Options
```json
{
  "EnableRetryOnFailure": 3,
  "CommandTimeout": 60
}
```

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Installation Guide](../03-Installation/README.md) | Initial setup and configuration |
| [Deployment Guide](../04-Deployment/README.md) | Environment configuration |
| [Security Guide](../12-Security/README.md) | Security configuration details |
| [System Administration](../06-System-Administration/README.md) | Administrative tasks |
| [Operations Guide](../21-Operations/README.md) | Operational configuration |
