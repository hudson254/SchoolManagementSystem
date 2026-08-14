# Changelog

## Table of Contents
- [Version History](#version-history)
- [Current Version](#current-version)
- [Release Notes](#release-notes)
- [Related Documentation](#related-documentation)

---

## Version History

### v1.0.0 (Initial Release)

The initial release of the School Management System.

---

## Current Version

**Version:** 1.0.0

---

## Release Notes

### [1.0.0] - Initial Release

#### Added
- Complete multi-tenant architecture with Row Level Security
- JWT-based authentication with role-based access control
- User registration and approval workflow
- Student portal with course enrollment and academic records
- Lecturer portal with unit management, grading, and attendance
- Coordinator portal with approvals and academic coordination
- Administrator portal with full system management
- Accommodation management (lanes, houses, rooms, assignments)
- Certificate generation with QR code verification
- Report generation with PDF/Excel export
- Real-time notifications via SignalR
- SMS notifications via Twilio (configurable)
- File upload and management
- Audit logging system
- Error management and logging with correlation IDs
- Security features (CSRF, security headers, rate limiting)
- Docker containerization for all services
- Prometheus/Grafana monitoring stack
- Automated backup service
- Comprehensive API with versioning
- Swagger API documentation
- Health check endpoints
- Password reset workflow
- Course offering management
- Assessment engine with grading scales and moderation

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Release Management](../20-Release-Management/README.md) | Release process |
| [Deployment Guide](../04-Deployment/README.md) | Deployment procedures |
| [System Overview](../01-System-Overview/README.md) | System overview |
