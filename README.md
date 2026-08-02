# School Management System

## Overview

A comprehensive, production-ready School Management System built with ASP.NET Core 9 and React 19. The system features multi-tenancy, role-based access control, and enterprise-grade architecture.

## Features

- **Multi-Tenancy**: Complete tenant isolation with Row Level Security
- **Authentication & Authorization**: JWT-based authentication with RBAC
- **Student Portal**: Enrollment, course browsing, assignments, grades, timetable
- **Lecturer Portal**: Unit management, notes, assignments, grading
- **Moderator Portal**: Course management, verification, scheduling
- **Receptionist Portal**: Onboarding, accommodation management
- **Administrator Portal**: Full system control, user management, reporting
- **Accommodation Management**: Complete room allocation and tracking
- **Reporting**: PDF, Excel, CSV exports
- **Docker**: Complete containerization

## Technology Stack

### Backend
- ASP.NET Core 9
- C# 12
- Entity Framework Core
- MediatR (CQRS)
- FluentValidation
- Serilog
- Hangfire

### Frontend
- React 19
- TypeScript
- Material UI
- TanStack Query
- React Router
- React Hook Form

### Database
- PostgreSQL 16
- Row Level Security

### Infrastructure
- Docker
- Nginx
- Prometheus
- Grafana

## Quick Start

### Prerequisites
- Docker 24+
- Docker Compose 2.20+

### Installation

```bash
git clone https://github.com/your-org/school-management-system.git
cd school-management-system

cp .env.example .env
# Edit .env with your configuration

docker-compose -f docker/docker-compose.yml up -d

# Run migrations
docker exec sms-api dotnet ef database update

# Seed data
docker exec sms-api dotnet run -- seed-data
