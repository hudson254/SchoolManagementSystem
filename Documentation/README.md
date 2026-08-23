# School Management System (SMS) Documentation

## Overview

Welcome to the School Management System (SMS) documentation. This is the **single source of operational truth** for the system. All content has been verified against the actual source code and configuration.

## Documentation Structure

| Section | Description |
|---------|-------------|
| [Audit Report](00-Documentation-Audit-Report.md) | Documentation audit findings and change report |
| [Verification Report](99-Verification-Report.md) | Production readiness verification |
| [01-System-Overview](01-System-Overview/README.md) | System purpose, architecture, technology stack, and high-level concepts |
| [02-Architecture](02-Architecture/README.md) | Detailed architecture documentation including CQRS, patterns, and data flow |
| [03-Installation](03-Installation/README.md) | Installation requirements, setup procedures, and initial configuration |
| [04-Deployment](04-Deployment/README.md) | Deployment overview; authoritative production guide is the Debian 13 guide |
| [05-Configuration](05-Configuration/README.md) | All configuration options, environment variables, and settings |
| [06-System-Administration](06-System-Administration/README.md) | System administration tasks, monitoring, and maintenance |
| [07-Administrator-Guide](07-Administrator-Guide/README.md) | Complete guide for system administrators |
| [08-Coordinator-Guide](08-Coordinator-Guide/README.md) | Guide for coordinators/moderators |
| [09-Lecturer-Guide](09-Lecturer-Guide/README.md) | Guide for lecturers |
| [10-Student-Guide](10-Student-Guide/README.md) | Guide for students |
| [11-Authentication](11-Authentication/README.md) | Authentication mechanisms, JWT, and security |
| [12-Security](12-Security/README.md) | Security architecture, policies, and best practices |
| [13-Database](13-Database/README.md) | Database schema, migrations, and administration |
| [14-Backup-and-Recovery](14-Backup-and-Recovery/README.md) | Backup and disaster recovery procedures |
| [15-Maintenance](15-Maintenance/README.md) | Routine maintenance tasks and schedules |
| [16-Troubleshooting](16-Troubleshooting/README.md) | Common issues and their solutions |
| [17-API](17-API/README.md) | API documentation for all endpoints |
| [18-Developer-Guide](18-Developer-Guide/README.md) | Developer setup, coding standards, and contribution guide |
| [19-Testing](19-Testing/README.md) | Testing strategy, test types, and running tests |
| [20-Release-Management](20-Release-Management/README.md) | Release process, versioning, and changelog |
| [21-Operations](21-Operations/README.md) | Operational procedures and runbooks |
| [22-Reference](22-Reference/README.md) | Reference materials, glossaries, and quick references |
| [23-Changelog](23-Changelog/README.md) | Version history and change log |

## Quick Navigation

### For System Administrators
- [Installation Guide](03-Installation/README.md)
- [Deployment Guide](04-Deployment/README.md)
- [System Administration](06-System-Administration/README.md)
- [Backup and Recovery](14-Backup-and-Recovery/README.md)
- [Security Guide](12-Security/README.md)

### For Developers
- [Developer Guide](18-Developer-Guide/README.md)
- [API Documentation](17-API/README.md)
- [Architecture Overview](02-Architecture/README.md)
- [Database Guide](13-Database/README.md)
- [Testing Guide](19-Testing/README.md)

### For End Users
- [Administrator Guide](07-Administrator-Guide/README.md)
- [Coordinator Guide](08-Coordinator-Guide/README.md)
- [Lecturer Guide](09-Lecturer-Guide/README.md)
- [Student Guide](10-Student-Guide/README.md)

## Technology Stack (Verified from Source Code)

| Layer | Technology | Version | Source |
|-------|-----------|---------|--------|
| **Backend** | .NET (C#) | 9.0 | `global.json`, `Dockerfile.api` |
| **Frontend** | React | 19 | `frontend/sms-web/package.json` |
| **Frontend Build** | Vite | 8.1.5 | `frontend/sms-web/package.json` |
| **UI Library** | Material UI (MUI) | 5.16+ | `frontend/sms-web/package.json` |
| **State Mgmt** | TanStack Query | 5.40+ | `frontend/sms-web/package.json` |
| **Routing** | React Router DOM | 7.18+ | `frontend/sms-web/package.json` |
| **Database** | PostgreSQL | 16 (Alpine) | `docker/docker-compose*.yml` |
| **ORM** | Entity Framework Core (Npgsql) | 9.0 | `.csproj` files |
| **Reverse Proxy** | Nginx | Alpine | `docker/nginx.conf` |
| **Auth** | ASP.NET Core Identity + JWT (HS256) | - | `src/SMS.Api/Program.cs` |
| **API Versioning** | Asp.Versioning | 8.1 | NuGet packages |
| **Logging** | Serilog | Latest | `Program.cs`, `appsettings.json` |
| **Metrics** | Prometheus | 2.54.1 | `docker/docker-compose.prod.yml` |
| **Dashboards** | Grafana | 11.2.0 | `docker/docker-compose.prod.yml` |
| **Alerting** | Alertmanager | 0.27.0 | `docker/docker-compose.prod.yml` |
| **Host Metrics** | Node Exporter | 1.8.2 | `docker/docker-compose.prod.yml` |
| **DB Metrics** | PostgreSQL Exporter | 0.15.0 | `docker/docker-compose.prod.yml` |
| **Container Metrics** | cAdvisor | 0.49.1 | `docker/docker-compose.prod.yml` |
| **PDF Generation** | QuestPDF | Latest | `.csproj` |
| **Excel Export** | EPPlus | Latest | `.csproj` |
| **Real-time** | SignalR | - | `src/SMS.Api/Program.cs` |
| **Certificates** | SMS.Certificates | - | `src/SMS.Certificates` |
| **Reports** | SMS.Reporting | - | `src/SMS.Reporting` |

**Note:** Redis is configured via `RedisTokenRevocation__ConnectionString` environment variable for production token revocation, but is optional. In-memory token revocation is used when Redis is not configured.

## System Requirements (Verified)

- **OS**: Windows Server 2019+, Ubuntu 22.04+, Debian 12+
- **Runtime**: .NET 9.0 SDK/Runtime
- **Database**: PostgreSQL 16 (Alpine Docker image)
- **Memory**: 4 GB RAM minimum (8 GB recommended)
- **Storage**: 20 GB minimum (50 GB+ recommended for production)
- **Docker**: Docker Engine 24+ and Docker Compose v2+
- **Network**: LAN deployment (internal network). Internet access required only for package downloads and optional monitoring notifications.

## Production Status

**This system is PRODUCTION READY.** All 423+ tests pass, security controls are verified, Docker production stack is validated, and the deployment guide is complete.

See [Verification Report](99-Verification-Report.md) for full details.

## Documentation Audit

This documentation set has been fully audited against the source code. See [Audit Report](00-Documentation-Audit-Report.md) for all corrections made.

---

*Documentation audited and verified against source code. Last updated: 23 August 2026.*
