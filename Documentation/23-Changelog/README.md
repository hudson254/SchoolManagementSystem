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

#### Changed (Documentation Audit - 24 August 2026 - Second Pass)
- Removed references to nonexistent `/health/ready` and `/health/live` endpoints (Operations, Maintenance guides)
- Fixed middleware pipeline order to match actual `Program.cs` implementation
- Updated Docker commands: `docker exec` → `docker compose exec` in DEBIAN13 backup/restore commands
- Added missing environment variables to Configuration guide: `DB_NAME`, `DB_USER`, `ADMIN_*`, `ENABLE_*`, `Swagger__Enabled`, `SSL_PASSWORD`, `API_URL`, `GRAFANA_URL`, `RATE_LIMIT_*`
- Fixed rate limiting documentation: production uses `PermitLimit: 100` (not 20)
- Clarified port availability: port 5000 is dev-only; production uses Nginx port 8080/8443
- Verified API controller list: 31 controllers match source code exactly
- Verified frontend dependencies: React 19, Vite 8.1.5, React Router 7.18.2, TanStack Query 5.40+
- Added note about Alertmanager SMTP being disabled by default for LAN-only deployment

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Release Management](../20-Release-Management/README.md) | Release process |
| [Deployment Guide](../04-Deployment/README.md) | Deployment procedures |
| [System Overview](../01-System-Overview/README.md) | System overview |
