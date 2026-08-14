# School Management System (SMS) Documentation

## Overview

Welcome to the School Management System (SMS) documentation. This comprehensive guide covers all aspects of the system, from installation and configuration to daily operation and maintenance.

## Documentation Structure

| Section | Description |
|---------|-------------|
| [01-System-Overview](01-System-Overview/README.md) | System purpose, architecture, technology stack, and high-level concepts |
| [02-Architecture](02-Architecture/README.md) | Detailed architecture documentation including CQRS, patterns, and data flow |
| [03-Installation](03-Installation/README.md) | Installation requirements, setup procedures, and initial configuration |
| [04-Deployment](04-Deployment/README.md) | Deployment guides for development, staging, and production environments |
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

## Technology Stack
- **Backend**: .NET 9.0 (C#)
- **Frontend**: React 18+ with TypeScript
- **Database**: PostgreSQL 15+
- **ORM**: Entity Framework Core 9.0
- **Authentication**: JWT with ASP.NET Core Identity
- **API Versioning**: Asp.Versioning
- **Logging**: Serilog with structured JSON logging
- **Containerization**: Docker and Docker Compose
- **Reverse Proxy**: Nginx
- **Monitoring**: Prometheus, Grafana, Alertmanager
- **PDF Generation**: QuestPDF | **Excel Export**: EPPlus | **Real-time**: SignalR | **Background Jobs**: Hangfire
- **Caching**: Redis (production), In-Memory (development)

## System Requirements
- **OS**: Windows Server 2019+, Ubuntu 22.04+, Debian 12+
- **Runtime**: .NET 9.0 SDK/Runtime | **Database**: PostgreSQL 15+
- **Memory**: 4GB RAM minimum (8GB recommended) | **Storage**: 20GB minimum
- **Docker**: Docker Engine 24+ and Docker Compose v2+

## Support
For issues, refer to the [Troubleshooting Guide](16-Troubleshooting/README.md) or contact the system administrator.

---

*Documentation generated for School Management System v1.0.0*
