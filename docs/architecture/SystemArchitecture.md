
```markdown
# System Architecture

## Overview

The School Management System follows **Clean Architecture** principles with clear separation of concerns. The architecture is designed for scalability, maintainability, and testability.

## Architecture Layers

### 1. Domain Layer (SMS.Domain)

The core layer containing business entities, value objects, and domain interfaces.

**Components:**
- **Entities**: Core business objects (Student, Lecturer, Course, Unit, etc.)
- **Value Objects**: Immutable objects (AuditInfo, Address, etc.)
- **Enums**: Domain enumerations (RoleType, GradeStatus, etc.)
- **Interfaces**: Repository and service interfaces
- **Common**: BaseEntity, IBaseEntity

**Key Entities:**