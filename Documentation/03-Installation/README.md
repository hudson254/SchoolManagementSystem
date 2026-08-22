# Installation Guide

## Table of Contents
- [Requirements](#requirements)
- [Docker Installation](#docker-installation)
- [Manual Installation](#manual-installation)
- [Initial Configuration](#initial-configuration)
- [Database Setup](#database-setup)
- [Frontend Setup](#frontend-setup)
- [First Administrator Account](#first-administrator-account)
- [Verification](#verification)
- [Troubleshooting Installation](#troubleshooting-installation)
- [Related Documentation](#related-documentation)

---

## Requirements

### Hardware Requirements
| Resource | Minimum | Recommended |
|----------|---------|-------------|
| CPU | 2 cores | 4 cores |
| RAM | 4 GB | 8 GB |
| Storage | 20 GB | 50 GB+ |
| Network | 100 Mbps | 1 Gbps |

### Software Requirements
| Software | Version | Purpose |
|----------|---------|---------|
| Docker | 24+ | Containerization |
| Docker Compose | 2.20+ | Container orchestration |
| Git | Latest | Source control |
| Node.js (for frontend dev) | 20+ | Frontend build |
| .NET SDK (for backend dev) | 9.0 | Backend build |

### Supported Operating Systems
- **Windows**: Windows Server 2019+, Windows 10/11
- **Linux**: Ubuntu 22.04+, Debian 12+
- **macOS**: 13+ (development only)

---

## Docker Installation

### Prerequisites Check
```bash
# Verify Docker is installed
docker --version

# Verify Docker Compose
docker compose version
```

### Step-by-Step Installation

#### 1. Clone the Repository
```bash
git clone https://github.com/your-org/school-management-system.git
cd school-management-system
```

#### 2. Configure Environment File
```bash
# Copy the example environment file
cp .env.example .env

# Edit the .env file with your specific values
# Minimum required settings:
# DB_PASSWORD=your_secure_password
# JWT_SECRET=your_super_secret_key_min_32_chars
# GRAFANA_PASSWORD=your_grafana_password
```

**Important Environment Variables:**
| Variable | Required | Description |
|----------|----------|-------------|
| `DB_PASSWORD` | ✅ Yes | PostgreSQL password |
| `JWT_SECRET` | ✅ Yes | JWT signing key (min 32 chars) |
| `GRAFANA_PASSWORD` | ✅ Yes | Grafana admin password |
| `JWT_ISSUER` | No | Token issuer (default: SMSAPI) |
| `JWT_AUDIENCE` | No | Token audience (default: SMSWeb) |
| `JWT_EXPIRY` | No | Token expiry in minutes (default: 60) |
| `NGINX_HTTP_PORT` | No | HTTP port (default: 8080) |
| `NGINX_HTTPS_PORT` | No | HTTPS port (default: 8443) |
| `BACKUP_INTERVAL` | No | Backup interval seconds (default: 86400) |
| `BACKUP_RETENTION_DAYS` | No | Backup retention days (default: 30) |

#### 3. Generate SSL Certificates (Optional)
For HTTPS with self-signed certificates in development:
```bash
# Run the certificate generation script
./scripts/gen-dev-cert.ps1
# OR
./scripts/generate-ssl.sh
```

> **⚠️ WARNING**: Self-signed certificates will cause browser warnings. For **LAN-only** production deployments, use an **Internal Certificate Authority** (see [Debian 13 Server Preparation Guide](../04-Deployment/DEBIAN13_SERVER_PREPARATION_GUIDE.md) Section 4). For internet-facing deployments, use Let's Encrypt or a commercial CA.

#### 4. Start the Containers
```bash
# Start all services
docker compose -f docker/docker-compose.yml up -d

# View container status
docker compose ps
```

#### 5. Run Database Migrations
```bash
# Execute database migrations inside the API container
docker exec sms-api dotnet run -- migrate-database
```

#### 6. Seed Initial Data
```bash
# Seed roles and initial data
docker exec sms-api dotnet run -- seed-data
```

#### 7. Verify Installation
```bash
# Check API health
curl http://localhost:5000/health

# Expected response
{
  "status": "Healthy",
  "duration": 15.2,
  "entries": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": "Database connection successful",
      "duration": 5.1
    }
  ]
}
```

---

## Manual Installation

### Backend Setup

#### 1. Prerequisites
- .NET SDK 9.0
- PostgreSQL 16
- Redis (optional, for production)

#### 2. Configure Database
```bash
# Create PostgreSQL database
createdb -U postgres SchoolManagementSystem

# Create user
psql -U postgres -c "CREATE USER sms_user WITH PASSWORD 'your_password';"
psql -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE SchoolManagementSystem TO sms_user;"
```

#### 3. Configure Connection String
Set the `ConnectionStrings:DefaultConnection` in `src/SMS.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=SchoolManagementSystem;Username=sms_user;Password=your_password;"
  }
}
```

#### 4. Run Migrations
```bash
cd src/SMS.API
dotnet run -- migrate-database
```

#### 5. Seed Data
```bash
dotnet run -- seed-data
```

#### 6. Start the API
```bash
dotnet run
```
The API will be available at `http://localhost:5000` (or the configured port).

---

## Frontend Setup

### Development Mode
```bash
cd frontend/sms-web

# Install dependencies
npm install

# Configure environment
# Create .env file with:
# VITE_API_URL=http://localhost:5000

# Start dev server
npm run dev
```
The frontend will be available at `http://localhost:5173`.

### Production Build
```bash
cd frontend/sms-web

# Install dependencies
npm install

# Create production build
npm run build

# Serve the dist folder with your web server
# (nginx/apache) or with:
npx serve dist
```

---

## Initial Configuration

### Required Configuration After Installation

1. **JWT Secret**: Ensure `JWT_SECRET` environment variable is set to a strong, unique value (min 32 characters)
2. **Database Password**: Set a strong production database password
3. **Admin Password**: Change the default administrator password after first login
4. **CORS Origins**: Configure `Frontend:Url` in appsettings or environment
5. **File Storage**: Verify upload directory permissions
6. **Swagger**: Enable `Swagger__Enabled=true` in development only

### Default Ports
| Service | Port | Description |
|---------|------|-------------|
| API | 5000 | Backend REST API |
| Frontend | 3000 (Docker) / 5173 (dev) | Web interface |
| PostgreSQL | 5433 | Database (mapped from 5432) |
| Nginx HTTP | 8080 | Reverse proxy HTTP |
| Nginx HTTPS | 8443 | Reverse proxy HTTPS |
| Prometheus | 9090 | Metrics collection |
| Grafana | 3001 | Monitoring dashboards |

---

## Database Setup

### Docker Setup
The Docker Compose configuration handles database setup automatically. The database will be created on first startup with the `init-db.sql` script.

### Manual Setup
```sql
-- Create database
CREATE DATABASE SchoolManagementSystem;

-- Create application user
CREATE USER sms_user WITH PASSWORD 'secure_password';
GRANT ALL PRIVILEGES ON DATABASE SchoolManagementSystem TO sms_user;

-- Grant schema privileges
\c SchoolManagementSystem
GRANT ALL ON SCHEMA public TO sms_user;
```

### Running Migrations
```bash
# Via Docker
docker exec sms-api dotnet run -- migrate-database

# Via .NET CLI
cd src/SMS.API
dotnet run -- migrate-database
```

### Seeding Data
```bash
# Via Docker
docker exec sms-api dotnet run -- seed-data

# Via .NET CLI
cd src/SMS.API
dotnet run -- seed-data
```

The seed process creates:
- Default tenant
- All user roles (Administrator, Coordinator, Lecturer, Student, Receptionist)
- Default administrator account
- Title configuration data
- Certificate rules and templates

---

## First Administrator Account

The seed script creates a default administrator account:

### Default Credentials
| Field | Value |
|-------|-------|
| Username | Configured in seed settings (check DatabaseSeeder.cs) |
| Password | Generated by seed script |
| Role | Administrator |

> **⚠️ SECURITY WARNING**: Change the default administrator password immediately after first login!

### Creating Additional Administrators
1. Log in as an existing administrator
2. Navigate to **Users** > **Create User**
3. Fill in the user details
4. Assign the **Administrator** role
5. Set an initial password meeting complexity requirements
6. Click **Save**

---

## Verification

### Post-Installation Checklist

- [ ] All Docker containers are running: `docker compose ps`
- [ ] API health check returns "Healthy"
- [ ] Database migrations applied successfully
- [ ] Seed data created (roles, admin user)
- [ ] Frontend loads in browser
- [ ] Can log in with administrator account
- [ ] Can navigate to the dashboard
- [ ] Swagger UI accessible (development): `http://localhost:5000/swagger`

### Verification Commands
```bash
# Check service status
docker compose ps

# Check API health
curl http://localhost:5000/health

# Check API metrics
curl http://localhost:5000/metrics

# Check database
docker compose exec postgres pg_isready -U sms_user

# View API logs
docker compose logs --tail=50 api
```

---

## Troubleshooting Installation

### Docker Won't Start
- **Check**: `docker compose ps`
- **Fix**: Review the [Troubleshooting Guide](../16-Troubleshooting/README.md)

### Port Already in Use
- Change the port mapping in `docker-compose.yml` or `.env`
- Common conflict ports: 5433, 5000, 3000, 8080

### Database Connection Failed
- Verify PostgreSQL is healthy: `docker compose ps postgres`
- Check the connection string in `.env` and `appsettings.json`
- Verify the password matches between `.env` and `appsettings.json`

### JWT Secret Not Configured
- Set the `JWT_SECRET` environment variable
- Use a value of at least 32 characters
- Restart the API after setting

---

## Related Documentation

| Section | Description |
|---------|-------------|
| [Deployment Guide](../04-Deployment/README.md) | Production deployment |
| [Configuration Guide](../05-Configuration/README.md) | All configuration options |
| [System Administration](../06-System-Administration/README.md) | Post-installation administration |
| [Troubleshooting Guide](../16-Troubleshooting/README.md) | Common installation issues |
| [Backup and Recovery](../14-Backup-and-Recovery/README.md) | Backup procedures |
