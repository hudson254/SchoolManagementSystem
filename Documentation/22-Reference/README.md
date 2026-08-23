# Reference Guide

## Table of Contents
- [Quick References](#quick-references)
- [Useful Commands](#useful-commands)
- [Default Ports](#default-ports)
- [Common Error Messages](#common-error-messages)
- [API Status Codes](#api-status-codes)
- [Glossary](#glossary)
- [Related Documentation](#related-documentation)

---

## Quick References

### System Access
| Service | URL | Credentials |
|---------|-----|-------------|
| Application (via Nginx) | `https://sms.school.internal` or `http://sms.school.internal:8080` | User credentials |
| API (direct, dev only) | `http://localhost:5000` | JWT token |
| Swagger UI | `http://localhost:5000/swagger` | JWT (dev only, disabled in production) |
| Grafana | `http://localhost:3001` | admin / GRAFANA_PASSWORD |
| Prometheus | `http://localhost:9090` | None |

### Environment Files
| File | Location |
|------|----------|
| App Settings | `src/SMS.API/appsettings.json` |
| Dev Settings | `src/SMS.API/appsettings.Development.json` |
| Docker Compose | `docker/docker-compose.yml` |
| Environment | `.env` |

---

## Useful Commands

### Docker
```bash
# Start all services
docker compose -f docker/docker-compose.yml up -d

# Start production services
docker compose -f docker/docker-compose.prod.yml --env-file .env up -d

# Check status
docker compose ps

# View logs
docker compose logs -f api

# Execute migration
docker compose exec api dotnet SMS.API.dll migrate-database

# Execute seed
docker compose exec api dotnet SMS.API.dll seed-data

# Backup database
docker compose exec postgres pg_dump -U sms_user -d SchoolManagementSystem -F c -f /backups/backup.dump

# Restore database
docker compose exec -T postgres pg_restore -U sms_user -d SchoolManagementSystem -F c < backup.dump
```

### Database
```bash
# Connect to database
psql -h localhost -p 5433 -U sms_user -d SchoolManagementSystem

# List all tables
\dt

# Describe table
\d+ users

# Check database size
SELECT pg_size_pretty(pg_database_size('SchoolManagementSystem'));

# Show active connections
SELECT * FROM pg_stat_activity WHERE datname = 'SchoolManagementSystem';
```

### Git
```bash
# Create feature branch
git checkout -b feature/new-feature develop

# Create hotfix branch
git checkout -b hotfix/1.0.1 main

# Create release branch
git checkout -b release/1.1.0 develop

# Tag release
git tag -a v1.0.0 -m "Version 1.0.0"
git push origin v1.0.0
```

---

## Default Ports

| Service | Port | Environment | Notes |
|---------|------|-------------|-------|
| API (Docker - dev) | 5000 | Dev | Mapped to container port 80 |
| API (Direct) | 5000 | Dev | dotnet run |
| API (Production) | (internal) | Production | No host port; only accessible via Nginx |
| Frontend (Docker) | 3000 | All | Mapped to container port 80 |
| Frontend (Development) | 5173 | Dev | Vite dev server |
| PostgreSQL | 5433 (host) / 5432 (container) | Dev/Test | Not exposed in production |
| Nginx HTTP | 8080 (configurable via NGINX_HTTP_PORT) | All | Default HTTP entry point |
| Nginx HTTPS | 8443 (configurable via NGINX_HTTPS_PORT) | All | Default HTTPS entry point |
| Prometheus | 9090 | Dev | Internal only in production |
| Grafana | 3001 | Dev | Internal only in production |
| Alertmanager | 9093 | Internal | Docker network only |
| Node Exporter | 9100 | Internal | Docker network only |
| Postgres Exporter | 9187 | Internal | Docker network only |
| cAdvisor | 8080 | Internal | Docker network only |

---

## Common Error Messages

| Error | Cause | Solution |
|-------|-------|----------|
| `JWT Secret not configured` | JWT_SECRET not set | Set JWT_SECRET environment variable |
| `Invalid username or password` | Wrong credentials | Reset password or contact admin |
| `Account is locked` | 5 failed attempts | Wait 15 minutes or contact admin |
| `Database connection failed` | PostgreSQL unavailable | Check container status, verify connection string |
| `File too large` | Upload exceeds limit | Reduce file size or increase MaxFileSizeMB |
| `Rate limit exceeded` | Too many requests | Wait 1 minute before retrying |

---

## API Status Codes

| Code | Description |
|------|-------------|
| 200 OK | Request successful |
| 201 Created | Resource created |
| 204 No Content | Request successful, no content returned |
| 400 Bad Request | Validation error |
| 401 Unauthorized | Authentication required |
| 403 Forbidden | Insufficient permissions |
| 404 Not Found | Resource not found |
| 409 Conflict | Resource conflict |
| 429 Too Many Requests | Rate limited |
| 500 Internal Server Error | Server error |

---

## Glossary

| Term | Definition |
|------|------------|
| **CQRS** | Command Query Responsibility Segregation |
| **CRUD** | Create, Read, Update, Delete |
| **CSRF** | Cross-Site Request Forgery |
| **DTO** | Data Transfer Object |
| **EF Core** | Entity Framework Core |
| **HSTS** | HTTP Strict Transport Security |
| **JWT** | JSON Web Token |
| **ORM** | Object-Relational Mapping |
| **RBAC** | Role-Based Access Control |
| **RLS** | Row Level Security |
| **RTO** | Recovery Time Objective |
| **RPO** | Recovery Point Objective |
| **SemVer** | Semantic Versioning |
| **SMS** | School Management System |
| **SOLID** | Single responsibility, Open-closed, Liskov substitution, Interface segregation, Dependency inversion |
| **WAL** | Write-Ahead Logging |
| **XSS** | Cross-Site Scripting |

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [API Documentation](../17-API/README.md) | API endpoints |
| [Configuration Guide](../05-Configuration/README.md) | Configuration reference |
| [Troubleshooting Guide](../16-Troubleshooting/README.md) | Common issues |
| [Database Guide](../13-Database/README.md) | Database reference |
