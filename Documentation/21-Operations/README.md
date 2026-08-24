# Operations Guide

## Table of Contents
- [Operational Overview](#operational-overview)
- [Daily Operations](#daily-operations)
- [Monitoring](#monitoring)
- [Alerting](#alerting)
- [Incident Response](#incident-response)
- [Capacity Planning](#capacity-planning)
- [Related Documentation](#related-documentation)

---

## Operational Overview

This guide provides the operational procedures for running the School Management System in production.

---

## Daily Operations

### Morning Checklist
- [ ] Verify API health: `curl http://localhost:8080/health` (via Nginx) or `curl http://localhost:5000/health` (direct dev access only)
- [ ] Check database connectivity
- [ ] Review overnight backup status
- [ ] Check disk space: `df -h`
- [ ] Review error logs from last 24 hours
- [ ] Check container status: `docker compose ps`

### Weekly Tasks
- Review performance metrics in Grafana
- Analyze slow queries
- Review user activity reports
- Check for expired SSL certificates

### Monthly Tasks
- Apply security patches
- Review and archive old audit logs
- Test backup restoration
- Review capacity metrics
- Update maintenance plans

---

## Monitoring

### Health Endpoints
- `/health` - Overall system health (single endpoint; no separate readiness/liveness endpoints)

### Key Metrics
| Metric | Description | Alert Threshold |
|--------|-------------|-----------------|
| API Response Time | Average response time | > 500ms |
| Error Rate | Percentage of 5xx errors | > 1% |
| Database Connections | Active connections | > 80% of pool |
| Disk Usage | Storage utilization | > 85% |
| Memory Usage | RAM utilization | > 80% |
| CPU Usage | CPU utilization | > 75% |

---

## Alerting

### Alert Channels
- Email notifications
- Grafana dashboard alerts
- Prometheus Alertmanager
- System notifications

### Critical Alerts
| Alert | Action |
|-------|--------|
| API Down | Restart container, check logs |
| Database Down | Check PostgreSQL container |
| Disk Space Critical | Clean up old files, increase storage |
| High Error Rate | Investigate API logs |
| Certificate Expiring | Renew SSL certificate |

---

## Incident Response

### Incident Levels
| Level | Description | Response Time |
|-------|-------------|---------------|
| P1 - Critical | System down, data loss | 15 minutes |
| P2 - High | Major feature unavailable | 1 hour |
| P3 - Medium | Minor feature issue | 4 hours |
| P4 - Low | Cosmetic issue | 24 hours |

### Incident Response Steps
1. **Detect**: Identify the issue via monitoring or user report
2. **Assess**: Determine severity and impact
3. **Respond**: Apply fix or workaround
4. **Resolve**: Implement permanent solution
5. **Review**: Document incident and improve

---

## Capacity Planning

### Metrics to Monitor
- Average concurrent users
- API request volume trends
- Database size growth
- Storage utilization trends
- Memory and CPU usage patterns

### Scaling Triggers
| Resource | Trigger | Action |
|----------|---------|--------|
| CPU | > 75% for 1 hour | Add API instances |
| Memory | > 80% for 30 min | Increase RAM |
| Disk | > 85% utilization | Clean up or expand |
| DB Connections | > 80% pool | Increase pool size |
| Response Time | > 500ms average | Optimize or scale |

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Maintenance Guide](../15-Maintenance/README.md) | Routine maintenance |
| [Deployment Guide](../04-Deployment/README.md) | Deployment procedures |
| [Troubleshooting Guide](../16-Troubleshooting/README.md) | Issue resolution |
| [Security Guide](../12-Security/README.md) | Security operations |
