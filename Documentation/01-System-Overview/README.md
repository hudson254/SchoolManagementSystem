# System Overview

## Table of Contents
- [Purpose](#purpose)
- [System Modules](#system-modules)
- [User Roles and Permissions](#user-roles-and-permissions)
- [Architecture Overview](#architecture-overview)
- [Technology Stack](#technology-stack)
- [Data Flow](#data-flow)
- [Integration Points](#integration-points)
- [Related Documentation](#related-documentation)

---

## Purpose

The School Management System (SMS) is a comprehensive, production-ready web-based platform designed to manage all aspects of educational institution operations. It provides role-based access for students, lecturers, coordinators, administrators, and receptionists, enabling efficient management of academic, administrative, and operational tasks.

---

## System Modules

### 1. Authentication and Authorization
- JWT-based authentication with access and refresh tokens
- ASP.NET Core Identity for user management
- Role-based access control (RBAC)
- Token revocation (in-memory for development, Redis for production)
- Password policy enforcement and password reset workflows

### 2. User Management
- User registration with automatic username generation
- Role assignment (Administrator, Coordinator, Lecturer, Student, Receptionist)
- Profile management
- Account lockout and unlock
- Login history tracking

### 3. Student Management
- Student registration and profile management
- Course enrollment and academic records
- Grade tracking and transcripts
- Unit registration and academic progress

### 4. Lecturer Management
- Lecturer registration and profile management
- Teaching unit assignments
- Grade entry and assessment management
- Attendance tracking

### 5. Course and Unit Management
- Course creation and management
- Unit creation and management
- Course-to-unit mapping
- Academic year and semester management
- Programme and department management

### 6. Course Offerings
- Course offering creation per academic period
- Unit assignment to course offerings
- Student enrollment in course offerings
- Lecturer assignment to course offerings
- Enrollment and teaching assignment confirmation workflow
- Assignment issue reporting

### 7. Enrollments
- Self-service student enrollment
- Returning student course re-enrollment
- Administrative bulk enrollment
- Enrollment status management (active, dropped, completed)
- Enrollment confirmation workflow

### 8. Grades and Assessments
- Grade recording and management
- Assessment configuration (types, templates, scales)
- Weight configuration for assessments
- Manual mark entry
- Grade change history tracking
- Unit result computation
- Grade export (PDF, Excel, CSV)

### 9. Assessment Engine
- Configurable assessment types (Continuous Assessment, Exam, Practical, etc.)
- Assessment templates and grading scales
- Grade band configuration
- Moderation workflow
- Student exemptions
- Result publication controls

### 10. Accommodation Management
- Lane management
- House management (create, update, delete)
- Room assignment and transfers
- Occupant management (students and lecturers)
- Maintenance tracking
- Occupancy reports and statistics

### 11. Certificate Management
- Certificate template creation and management
- Certificate generation with QR codes
- Digital signatures
- Certificate verification via token
- Bulk certificate generation
- Certificate audit logging
- Background automatic certificate generation

### 12. Reporting
- PDF generation (QuestPDF)
- Excel export (EPPlus)
- Report authentication with QR codes and watermarks
- Report verification service
- Report revocation and restoration
- Available reports: Student Enrollment, Grades, Attendance, Accommodation, Audit Logs

### 13. Notifications
- In-app notifications via SignalR real-time hub
- SMS notifications via Twilio (configurable)
- Registration notifications
- Notification management and read tracking

### 14. File Management
- File upload and storage
- Upload categorization
- Supported file types: PDF, DOC, DOCX, PPT, PPTX, XLS, XLSX, images, ZIP
- Configurable file size limits

### 15. Audit Logging
- Comprehensive audit trail
- Entity change tracking
- User action logging
- Session tracking
- Audit log search and filtering
- Log export

### 16. Error Management
- Centralized error logging service
- Error severity classification
- Error categorization
- Searchable error repository
- Error admin interface
- Exception handling middleware
- Correlation ID tracking

### 17. Timetable Management
- Timetable creation and management
- Unit scheduling
- Room allocation
- Calendar integration

### 18. Approvals
- Registration approval workflow
- Bulk approval capabilities
- Rejection with reason
- Pending approvals dashboard

### 19. Multi-Tenancy
- Tenant isolation with Row Level Security
- Tenant resolution middleware
- Tenant-aware database queries
- Multi-tenant data segregation

### 20. Security
- CSRF protection (double-submit cookie pattern)
- Security headers middleware (HSTS, XSS, Content-Type, Frame options)
- Rate limiting middleware
- Input validation (FluentValidation)
- File upload security (content-type enforcement)
- CORS configuration
- Forwarded headers processing

---

## User Roles and Permissions

| Role | Description | Access Level |
|------|-------------|--------------|
| **Administrator** | Full system access to all features | All modules, system configuration, user management, reports |
| **Coordinator** | Academic coordination and oversight | Course management, approvals, student/lecturer management, reports |
| **Lecturer** | Teaching, grading, and assessment | Unit management, grades, attendance, assignments |
| **Student** | Learning and academic activities | Enrollment, assignments, grades, timetable, certificates |
| **Receptionist** | Limited administrative support | Onboarding, accommodation management, basic user management |

### Authorization Policies

| Policy | Roles |
|--------|-------|
| `AdministratorAccess` | Administrator |
| `ModeratorAccess` | Administrator, Coordinator |
| `LecturerAccess` | Administrator, Coordinator, Lecturer |
| `StudentAccess` | Administrator, Coordinator, Lecturer, Student |
| `ReceptionistAccess` | Administrator, Coordinator, Receptionist |

---

## Architecture Overview

The system follows **Clean Architecture** principles with the Command Query Responsibility Segregation (CQRS) pattern using MediatR.

```
┌─────────────────────────────────────────────────────────┐
│                     SMS.API (REST)                       │
│  Controllers, Middleware, Health Checks, SignalR Hub     │
├─────────────────────────────────────────────────────────┤
│                 SMS.Application (CQRS)                    │
│  Commands, Queries, Handlers, DTOs, Validators, Maps    │
├─────────────────────────────────────────────────────────┤
│                    SMS.Domain                             │
│  Entities, Enums, Interfaces, Common, Value Objects      │
├─────────────────────────────────────────────────────────┤
│  SMS.Infrastructure  │  SMS.Persistence  │  SMS.Identity │
│  Services, Storage,  │  EF Core,         │  JWT, User    │
│  File, Notifications │  Repositories,    │  Management   │
│                      │  DbContext        │               │
├─────────────────────────────────────────────────────────┤
│  SMS.Certificates  │  SMS.Reporting  │  SMS.Notifications│
│  Certificate Gen,  │  PDF, Excel,    │  SignalR, SMS     │
│  Verification       │  Report Auth    │                   │
└─────────────────────────────────────────────────────────┘
```

### Key Architecture Patterns
- **Clean Architecture**: Separation of concerns with domain-centric design
- **CQRS**: Command and Query separation via MediatR
- **Repository Pattern**: Data access abstraction
- **Unit of Work**: Transaction management
- **Dependency Injection**: Built-in ASP.NET Core DI container
- **FluentValidation**: Input validation
- **AutoMapper**: Object mapping

---

## Technology Stack

### Backend
| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 9.0 | Application framework |
| C# | 12 | Programming language |
| ASP.NET Core | 9.0 | Web API framework |
| Entity Framework Core | 9.0 | ORM and data access |
| MediatR | Latest | CQRS implementation |
| FluentValidation | Latest | Input validation |
| AutoMapper | Latest | Object mapping |
| Serilog | Latest | Structured logging |
| Hangfire | Latest | Background job processing |
| SignalR | Latest | Real-time notifications |
| QuestPDF | Latest | PDF generation |
| EPPlus | Latest | Excel export |
| Swashbuckle | Latest | API documentation (Swagger) |

### Frontend
| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19 | UI framework |
| TypeScript | Latest | Type-safe JavaScript |
| Material UI | 5.16+ | Component library |
| TanStack Query | 5.40+ | Server state management |
| React Router | 7.18+ | Client-side routing |
| React Hook Form | 7.52+ | Form management |
| Vite | 8.1.5 | Build tool and dev server |

### Database
| Technology | Version | Purpose |
|------------|---------|---------|
| PostgreSQL | 16 | Primary database |
| Row Level Security | - | Multi-tenant isolation (not yet RLS-enabled in all tables)

### Infrastructure
| Technology | Version | Purpose |
|------------|---------|---------|
| Docker | 24+ | Containerization |
| Docker Compose | v2+ | Container orchestration |
| Nginx | Latest | Reverse proxy |
| Redis | Latest | Caching and token revocation (production) |
| Prometheus | Latest | Metrics collection |
| Grafana | Latest | Monitoring dashboards |
| Alertmanager | Latest | Alert management |

---

## Data Flow

### Request Flow
```
User Browser/Nginx → API Gateway → Middleware Pipeline → Controller → MediatR Handler → Repository → Database
```

### Middleware Pipeline (Order)
1. CorrelationIdMiddleware
2. LoggingEnrichmentMiddleware
3. ExceptionHandlingMiddleware
4. SecurityHeadersMiddleware
5. CsrfProtectionMiddleware
6. TenantResolutionMiddleware
7. RateLimitingMiddleware
8. MetricsMiddleware
9. Authentication
10. Authorization

### Authentication Flow
```
Login → JWT Token Generation → Token Stored in httpOnly Cookie → Subsequent Requests Authenticated via Cookie/Header
```

### Notification Flow
```
Event → Notification Service → SignalR Hub → Connected Clients
     → SMS Service (if configured) → Twilio API
```

### Report Generation Flow
```
Request → Report Handler → Data Query → PDF/Excel Generation → Watermark → QR Code → Hash → Signed Download
```

---

## Integration Points

### External Services
- **Twilio**: SMS notifications (optional, requires configuration)
- **SMTP**: Email (currently disabled)
- **Redis**: Caching and token revocation (production only)

### Internal Services
- **SignalR Hub**: Real-time notifications at `/hub`
- **Health Endpoint**: System health checks at `/health`
- **Metrics Endpoint**: Prometheus metrics at `/metrics`
- **Uploads**: Static file serving at `/uploads`

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Architecture](../02-Architecture/README.md) | Detailed architecture documentation |
| [Installation Guide](../03-Installation/README.md) | Installation and setup procedures |
| [Deployment Guide](../04-Deployment/README.md) | Deployment configurations |
| [Configuration Guide](../05-Configuration/README.md) | All configuration options |
| [System Administration](../06-System-Administration/README.md) | System administration tasks |
| [Authentication](../11-Authentication/README.md) | Authentication and authorization |
| [Security](../12-Security/README.md) | Security architecture and best practices |
| [Database](../13-Database/README.md) | Database schema and administration |
| [API Documentation](../17-API/README.md) | API endpoints reference |
| [Developer Guide](../18-Developer-Guide/README.md) | Development setup and standards |
