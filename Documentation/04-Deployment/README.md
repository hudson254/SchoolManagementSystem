# Deployment Guide

## Table of Contents
- [Deployment Environments](#deployment-environments)
- [Development Deployment](#development-deployment)
- [Production Deployment](#production-deployment)
- [Docker Deployment](#docker-deployment)
- [Container Management](#container-management)
- [Reverse Proxy Configuration](#reverse-proxy-configuration)
- [Environment Variables](#environment-variables)
- [SSL Considerations](#ssl-considerations)
- [Database Migrations](#database-migrations)
- [Upgrade Procedures](#upgrade-procedures)
- [Rollback Procedures](#rollback-procedures)
- [Monitoring Setup](#monitoring-setup)
- [Related Documentation](#related-documentation)

---

## Deployment Environments

The system supports multiple deployment environments:

| Environment | Configuration File | Purpose |
|-------------|-------------------|---------|
| Development | `appsettings.Development.json` | Local development |
| Testing | `appsettings.Testing.json` | Automated testing |
| Staging | `appsettings.Staging.json` | Pre-production validation |
| Production | `appsettings.Production.json` | Live production |

---

## Development Deployment

### Prerequisites
- Docker 24+ and Docker Compose v2+
- .NET SDK 9.0
- Node.js 20+

### Quick Start (Docker)
```bash
# Start development environment
docker compose -f docker/docker-compose.dev.yml up -d

# Run migrations
docker exec sms-api dotnet run -- migrate-database

# Seed data
docker exec sms-api dotnet run -- seed-data
```

### Quick Start (Manual)
```bash
# Terminal 1: Start the API
cd src/SMS.API
dotnet run

# Terminal 2: Start the frontend
cd frontend/sms-web
npm run dev
```

---

## Production Deployment

### Production Checklist

Before deploying to production, verify:

- [ ] Strong `JWT_SECRET` configured (min 32 characters, cryptographically random)
- [ ] Strong database password configured
- [ ] SSL/TLS certificates installed and configured
- [ ] CORS origins properly configured
- [ ] Swagger disabled (`Swagger__Enabled=false`)
- [ ] Redis configured for token revocation
- [ ] Database backup strategy in place
- [ ] Logging configured for production (JSON format)
- [ ] Monitoring and alerting configured
- [ ] Rate limiting configured
- [ ] File upload limits configured
- [ ] Security headers configured
- [ ] Health checks verified
- [ ] Load testing completed

### Production Architecture

```
Internet → Nginx (HTTPS) → API (HTTP) → PostgreSQL
                  → Frontend (Static Files)
                  → Prometheus/Grafana (Monitoring)
                  → Backup Service
```

### Production Docker Deployment

#### 1. Prepare Environment
```bash
# Clone repository
git clone https://github.com/your-org/school-management-system.git
cd school-management-system

# Configure production environment
cp .env.example .env

# Edit .env for production
# Set strong passwords and secrets
```

#### 2. Deploy with Production Compose
```bash
# Start production stack
docker compose -f docker/docker-compose.prod.yml up -d

# Check all services are running
docker compose ps

# Apply migrations
docker exec sms-api dotnet run -- migrate-database

# Seed initial data
docker exec sms-api dotnet run -- seed-data
```

#### 3. Configure Nginx
The production Nginx configuration (`docker/nginx.conf`) includes:
- SSL/TLS termination
- HTTP to HTTPS redirect
- Reverse proxy to API
- Static file serving for frontend
- Security headers
- Gzip compression
- Request size limits

```nginx
# Example Nginx configuration
server {
    listen 443 ssl;
    server_name sms.example.com;
    
    ssl_certificate /etc/ssl/certs/sms.crt;
    ssl_certificate_key /etc/ssl/private/sms.key;
    
    location / {
        proxy_pass http://frontend:80;
    }
    
    location /api/ {
        proxy_pass http://api:80;
    }
}
```

---

## Container Management

### Starting Services
```bash
# Start all services
docker compose up -d

# Start specific service
docker compose up -d api

# Start with production override
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Stopping Services
```bash
# Stop all services
docker compose down

# Stop and remove volumes
docker compose down -v

# Stop specific service
docker compose stop api
```

### Viewing Logs
```bash
# View all logs
docker compose logs

# View specific service logs
docker compose logs api
docker compose logs postgres

# Follow logs
docker compose logs -f

# Last N lines
docker compose logs --tail=100 api
```

### Service Status
```bash
# Check all services
docker compose ps

# Check specific service
docker compose ps api

# Check resource usage
docker stats
```

### Restarting Services
```bash
# Restart all services
docker compose restart

# Restart specific service
docker compose restart api

# Rebuild and restart
docker compose up -d --build api
```

---

## Reverse Proxy Configuration

### Nginx Configuration

The system includes an Nginx reverse proxy that:
- Terminates SSL/TLS
- Routes HTTP traffic to the frontend
- Routes /api/* requests to the backend
- Implements security headers
- Enables gzip compression
- Sets client request size limits

### Custom Domain Configuration

1. Update DNS records to point to your server
2. Configure SSL certificates
3. Update Nginx configuration with your domain
4. Restart Nginx: `docker compose restart nginx`

### Load Balancing (Multiple API Instances)

```nginx
upstream api_backend {
    server api:80;
    server api2:80;
    server api3:80;
}

server {
    location /api/ {
        proxy_pass http://api_backend;
    }
}
```

---

## Environment Variables

### Required Variables
| Variable | Description | Example |
|----------|-------------|---------|
| `DB_PASSWORD` | PostgreSQL password | `S3cur3P@ssw0rd!` |
| `JWT_SECRET` | JWT signing key (32+ chars) | `your-super-secret-key-that-is-at-least-32-chars` |
| `GRAFANA_PASSWORD` | Grafana admin password | `Gr4f@n@P@ss!` |

### Optional Variables
| Variable | Default | Description |
|----------|---------|-------------|
| `JWT_ISSUER` | SMSAPI | Token issuer |
| `JWT_AUDIENCE` | SMSWeb | Token audience |
| `JWT_EXPIRY` | 60 | Token expiry minutes |
| `NGINX_HTTP_PORT` | 8080 | Nginx HTTP port |
| `NGINX_HTTPS_PORT` | 8443 | Nginx HTTPS port |
| `BACKUP_INTERVAL` | 86400 | Backup interval seconds |
| `BACKUP_RETENTION_DAYS` | 30 | Days to retain backups |

---

## SSL Considerations

### Development SSL
Use self-signed certificates for development only:
```bash
./scripts/gen-dev-cert.ps1
```

### Production SSL
Options for production SSL certificates:
1. **Let's Encrypt** (free, automated)
2. **Commercial CA** (DigiCert, Comodo, etc.)
3. **Cloudflare** (if using Cloudflare proxy)

### SSL Best Practices
- Use certificates from a trusted CA
- Enable automatic renewal (Let's Encrypt)
- Use strong cipher suites
- Enable HSTS in production
- Redirect HTTP to HTTPS
- Use 2048-bit or higher RSA keys

---

## Database Migrations

### Applying Migrations
```bash
# Via Docker
docker exec sms-api dotnet run -- migrate-database

# Via .NET CLI
cd src/SMS.API
dotnet run -- migrate-database
```

### Creating Migrations
```bash
# Add new migration
cd src/SMS.Persistence
dotnet ef migrations add MigrationName
```

### Rolling Back Migrations
```bash
# Remove last migration
cd src/SMS.Persistence
dotnet ef migrations remove

# Rollback to specific migration
dotnet ef database update PreviousMigrationName
```

---

## Upgrade Procedures

### Standard Upgrade

1. **Backup the database**
   ```bash
   docker compose exec postgres pg_dump -U sms_user -d SchoolManagementSystem -F c -f /backups/pre-upgrade.dump
   ```

2. **Pull latest code**
   ```bash
   git fetch
   git checkout tags/v1.1.0  # or specific version
   ```

3. **Rebuild services**
   ```bash
   docker compose build
   ```

4. **Apply migrations**
   ```bash
   docker exec sms-api dotnet run -- migrate-database
   ```

5. **Restart services**
   ```bash
   docker compose up -d
   ```

6. **Verify deployment**
   ```bash
   curl http://localhost:5000/health
   ```

### Zero-Downtime Upgrade
For zero-downtime deployments, consider:
- Blue-green deployment strategy
- Rolling updates with multiple API instances
- Database migration compatibility (backward-compatible changes)

---

## Rollback Procedures

### Standard Rollback

1. **Stop current services**
   ```bash
   docker compose down
   ```

2. **Revert code**
   ```bash
   git checkout tags/v1.0.0  # previous version
   ```

3. **Restore database** (if migration was applied)
   ```bash
   docker compose up -d postgres
   docker compose exec -T postgres pg_restore -U sms_user -d SchoolManagementSystem < pre-upgrade.dump
   ```

4. **Rebuild and restart**
   ```bash
   docker compose build
   docker compose up -d
   ```

5. **Verify rollback**
   ```bash
   curl http://localhost:5000/health
   ```

---

## Monitoring Setup

### Prometheus
Prometheus is configured to scrape metrics from the API:
- Metrics available at `/metrics` endpoint
- Pre-configured alerting rules in `docker/prometheus-alerts.yml`
- Data persisted in Prometheus volume

### Grafana
Grafana provides visualization dashboards:
- Pre-configured dashboards in `docker/grafana-dashboards/`
- Prometheus data source pre-configured
- Accessible at port 3001 (mapped from 3000)
- Admin credentials from `GRAFANA_PASSWORD`

### Alertmanager
Alertmanager handles alert routing:
- Configuration in `docker/alertmanager.yml`
- Configurable notification channels (email, Slack, etc.)

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Installation Guide](../03-Installation/README.md) | Initial installation |
| [Configuration Guide](../05-Configuration/README.md) | All configuration options |
| [Operations Guide](../21-Operations/README.md) | Operational procedures |
| [Maintenance Guide](../15-Maintenance/README.md) | Routine maintenance |
| [Troubleshooting Guide](../16-Troubleshooting/README.md) | Deployment troubleshooting |
| [Backup and Recovery](../14-Backup-and-Recovery/README.md) | Backup procedures |
