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
- Complete multi-tenant architecture
- JWT-based authentication with role-based access control (RBAC)
- User registration and approval workflow
- Student portal with course enrollment and academic records
- Lecturer portal with unit management, grading, and attendance
- Coordinator (COORDINATOR) portal with approvals and academic coordination
- Administrator portal with full system management
- Accommodation management (lanes, houses, rooms, assignments)
- Certificate generation with QR code verification
- Report generation with PDF/Excel export
- Real-time notifications via SignalR
- SMS notifications via Twilio (configurable, not yet operational)
- File upload and management
- Audit logging system
- Error management and logging with correlation IDs
- Security features (CSRF, security headers, rate limiting)
- Docker containerization for all services
- Prometheus/Grafana monitoring stack
- Automated backup service
- PWA (Progressive Web App) support
- Comprehensive API with versioning
- Swagger API documentation (development only)
- Health check endpoints
- Password reset workflow
- Course offering management
- Assessment engine with grading scales and moderation
- 331 unit tests, 63 API tests, 29 integration tests
- CI/CD pipeline with GitHub Actions
- End-to-end tests with Playwright
- Zero npm vulnerabilities after React Router v7 upgrade

#### Changed (Documentation Audit - 23 August 2026)
- All documentation updated to match actual source code
- Fixed database credentials: `sms_admin`/`sms_db` → `sms_user`/`SchoolManagementSystem`
- Fixed Docker commands: `docker-compose` → `docker compose`
- Fixed role names: `Coordinator` → `COORDINATOR` (matching authorization policies)
- Added missing environment variables: `FRONTEND_URL`, `ADMIN_*`, `SMTP_*`, etc.
- Updated React version from 18+ to 19
- Updated PostgreSQL version to 16
- Updated API controller list to match all 30+ controllers
- Fixed port documentation for production vs development
- Updated architecture diagram and middleware pipeline
- Removed Hangfire references (not actually used)
- Clarified Redis is optional, not required for production
- Updated production security posture documentation
- Corrected all credential references in troubleshooting commands

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Release Management](../20-Release-Management/README.md) | Release process |
| [Deployment Guide](../04-Deployment/README.md) | Deployment procedures |
| [System Overview](../01-System-Overview/README.md) | System overview |
