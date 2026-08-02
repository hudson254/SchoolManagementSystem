# Deployment Guide

## Overview
This guide covers the deployment of the School Management System to production environments.

## Prerequisites
- .NET 9.0 SDK
- PostgreSQL 16
- Node.js 20+ (for frontend)
- Docker (optional, for containerized deployment)

## Environment Configuration

### Required Environment Variables
| Variable | Description | Required |
|----------|-------------|----------|
| `ASPNETCORE_ENVIRONMENT` | Environment name (Production) | Yes |
| `JWT_SECRET` | JWT signing key (64+ chars) | Yes |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | Yes |
| `SMTP__Host` | SMTP server hostname | Yes |
| `SMTP__Username` | SMTP username | Yes |
| `SMTP__Password` | SMTP password | Yes |
| `SMTP__From` | From email address | Yes |
| `Frontend__Url` | Frontend application URL | Yes |
| `Tenant__DefaultTenantId` | Default tenant ID | Yes |

### Connection String Format
```
Host=db.example.com;Database=SMS_Prod;Username=sms_user;Password=***;Minimum Pool Size=5;Maximum Pool Size=100;Connection Lifetime=300;
```

## Deployment Steps

### 1. Database Migration
```bash
dotnet ef database update --project src/SMS.Persistence --startup-project src/SMS.API
```

### 2. Build Application
```bash
dotnet publish src/SMS.API -c Release -o ./publish
```

### 3. Configure Application
- Set environment variables
- Configure Kestrel endpoints in appsettings.Production.json
- Set up SSL certificate

### 4. Run Application
```bash
ASPNETCORE_ENVIRONMENT=Production dotnet ./publish/SMS.API.dll
```

### 5. Docker Deployment
```bash
docker-compose -f docker/docker-compose.prod.yml up -d
```

## Health Check Endpoint
```
GET /health
```

## Monitoring
- Application logs: `/var/log/sms/`
- Health check endpoint: `/health`
- Audit logs: Database `AuditLogs` table

## Backup
- Database backups configured in appsettings
- Backup interval: 24 hours
- Retention: 30 days
- Backup path: `/var/backups/sms`

## Rollback Procedure
1. Stop the application
2. Restore previous database backup
3. Deploy previous version
4. Verify health check
5. Resume traffic
