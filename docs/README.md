# School Management System - Documentation

Welcome to the School Management System documentation. This comprehensive guide covers everything you need to install, configure, use, and maintain the system.

## Documentation Structure

### For System Administrators
- [Installation Guide](deployment/InstallationGuide.md) - Step-by-step installation instructions
- [Deployment Guide](deployment/DeploymentGuide.md) - Production deployment and configuration
- [Maintenance Guide](deployment/MaintenanceGuide.md) - Ongoing system maintenance
- [Backup and Recovery Guide](deployment/BackupGuide.md) - Backup strategies and disaster recovery
- [Security Guide](SecurityGuide.md) - Security best practices and configuration
- [Troubleshooting Guide](TroubleshootingGuide.md) - Common issues and solutions

### For System Architects
- [System Architecture](architecture/SystemArchitecture.md) - Overall system architecture
- [Database Design](architecture/DatabaseDesign.md) - Database schema and relationships
- [API Documentation](api/README.md) - REST API reference
- [Bill of Materials](BillOfMaterials.md) - Hardware and software requirements

### For Users
- [Student Guide](user-guides/StudentGuide.md) - How students use the system
- [Lecturer Guide](user-guides/LecturerGuide.md) - How lecturers use the system
- [Moderator Guide](user-guides/ModeratorGuide.md) - How moderators manage the system
- [Receptionist Guide](user-guides/ReceptionistGuide.md) - How receptionists use the system
- [Administrator Guide](user-guides/AdministratorGuide.md) - How administrators manage the system

## Quick Links

- [System Overview](#system-overview)
- [Technology Stack](#technology-stack)
- [Quick Start](#quick-start)
- [Default Credentials](#default-credentials)

## System Overview

The School Management System is a comprehensive, multi-tenant platform designed for educational institutions. It provides:

- **Student Management**: Enrollment, academic records, grades, transcripts
- **Lecturer Management**: Unit allocation, lecture notes, assignments, grading
- **Course Management**: Course creation, unit management, programme administration
- **Timetable Management**: Class scheduling, room allocation, conflict resolution
- **Accommodation Management**: Room assignment, occupancy tracking, transfers
- **Reporting**: Comprehensive reports with PDF, Excel, CSV export

## Technology Stack

### Backend
- ASP.NET Core 9
- C# 12
- Entity Framework Core
- MediatR (CQRS)
- PostgreSQL 16
- Hangfire (Background Jobs)
- Serilog (Logging)

### Frontend
- React 19
- TypeScript
- Material UI
- TanStack Query
- React Router

### Infrastructure
- Docker
- Nginx
- Prometheus
- Grafana

## Quick Start

### Prerequisites
- Docker 24+
- Docker Compose 2.20+
- .NET 9 SDK (for development)

### Installation

```bash
# Clone the repository
git clone https://github.com/your-org/school-management-system.git
cd school-management-system

# Copy environment file
cp .env.example .env
# Edit .env with your configuration

# Deploy with Docker
docker-compose -f docker/docker-compose.yml up -d

# Run migrations
docker exec sms-api dotnet ef database update

# Seed data
docker exec sms-api dotnet run -- seed-data