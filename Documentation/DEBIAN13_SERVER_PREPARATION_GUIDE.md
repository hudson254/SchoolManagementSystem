# Debian 13 Server Preparation and Production Deployment Guide

> **Version:** 1.0  
> **Updated:** 24 August 2026  
> **Project:** School Management System (SMS)  
> **Target:** Debian 13 "Trixie"  
> **Audience:** System administrators (assumes basic Linux familiarity but limited Docker/ASP.NET knowledge)

---

## Table of Contents

1. [Purpose](#1-purpose)
2. [Production Architecture](#2-production-architecture)
3. [Prerequisites](#3-prerequisites)
4. [Debian 13 Installation](#4-debian-13-installation)
5. [Initial Server Configuration](#5-initial-server-configuration)
6. [Server Security](#6-server-security)
7. [Required Software Packages](#7-required-software-packages)
8. [Docker Installation](#8-docker-installation)
9. [Docker Configuration](#9-docker-configuration)
10. [Production Directory Structure](#10-production-directory-structure)
11. [Repository Deployment](#11-repository-deployment)
12. [Production Environment Configuration](#12-production-environment-configuration)
13. [Secrets Configuration](#13-secrets-configuration)
14. [DNS Configuration](#14-dns-configuration)
15. [SSL/TLS Configuration](#15-ssltls-configuration)
16. [Firewall Configuration](#16-firewall-configuration)
17. [Docker Compose Validation](#17-docker-compose-validation)
18. [Production Build](#18-production-build)
19. [Database Initialization](#19-database-initialization)
20. [Database Migration and Seeding](#20-database-migration-and-seeding)
21. [Production Startup](#21-production-startup)
22. [API Verification](#22-api-verification)
23. [Frontend Verification](#23-frontend-verification)
24. [Authentication and Authorization Verification](#24-authentication-and-authorization-verification)
25. [Multitenancy and RLS Verification](#25-multitenancy-and-rls-verification)
26. [Monitoring and Logging](#26-monitoring-and-logging)
27. [Backup and Restore](#27-backup-and-restore)
28. [Reboot and Automatic Recovery](#28-reboot-and-automatic-recovery)
29. [Troubleshooting](#29-troubleshooting)
30. [Maintenance](#30-maintenance)
31. [Production Deployment Checklist](#31-production-deployment-checklist)
## 1. Purpose

This guide takes you step by step from a **fresh Debian 13 "Trixie" server** to a fully functioning **School Management System (SMS) production deployment** using Docker.

Follow the sections **in order**. Do not skip steps. Each step builds on the previous one.

At the end you will have:

- A secure Linux server with firewall and SSH key authentication
- Docker with all production services running
- PostgreSQL database with migrations and seed data
- HTTPS-secured web application accessible via browser
- Automated backups and monitoring
- Verified recovery after server reboot

---

## 2. Production Architecture

### 2.1 Service Overview

The production deployment runs as a Docker Compose stack with these containers:

| Service | Container Name | Purpose | Internal Port | Externally Exposed |
|---------|---------------|---------|---------------|-------------------|
| Nginx | sms-nginx | Reverse proxy, TLS termination | 80, 443 | Yes (ports 80, 443) |
| API | sms-api | ASP.NET Core backend (HTTP only) | 80 | No |
| Frontend | sms-web | React SPA served by Nginx | 80 | No |
| PostgreSQL | sms-postgres | Database | 5432 | No |
| Backup | sms-backup | Automated database backups | - | No |
| Prometheus | sms-prometheus | Metrics collection | 9090 | No |
| Grafana | sms-grafana | Monitoring dashboards | 3000 | No |
| Alertmanager | sms-alertmanager | Alert routing | 9093 | No |
| Node Exporter | sms-node-exporter | Host metrics | 9100 | No |
| PostgreSQL Exporter | sms-postgres-exporter | Database metrics | 9187 | No |
| cAdvisor | sms-cadvisor | Container metrics | 8080 | No |

### 2.2 Network Architecture

```
Internet/LAN -> Nginx (80,443) -> /api/* -> API (80)
                                   /       -> Frontend (80)
                                   /health -> API health
                                   /hub/*  -> SignalR
                                             -> PostgreSQL (5432)
```

### 2.3 Key Design Decisions

- Nginx terminates HTTPS. API runs HTTP-only behind Nginx.
- Only Nginx ports 80/443 are open on the host firewall.
- PostgreSQL is not exposed to the host. Internal Docker network only.
- Standard ports 80/443 are used in production.


---

## 3. Prerequisites

### 3.1 Hardware Requirements

- CPU: 2 cores minimum, 4 cores recommended
- RAM: 4 GB minimum, 8 GB recommended
- Storage: 20 GB minimum, 50 GB SSD recommended
- Network: 100 Mbps minimum, 1 Gbps recommended

### 3.2 Software Requirements

- Debian 13 "Trixie" (fresh installation)
- Internet access for package downloads
- Static IP address on the server LAN
- SSH access from your administration workstation

### 3.3 Information to Have Ready

Before starting, note down:
- Git repository URL for the School Management System code
- Server hostname (e.g., sms-server)
- Server static IP address (e.g., 192.168.1.100)
- LAN domain name (e.g., school.internal)
- Administrator email address

---

## 4. Debian 13 Installation

### 4.1 Download

Download the Debian 13 "Trixie" netinstall ISO from: https://www.debian.org/distrib/

### 4.2 Installation Steps

During installation:
1. Language: English
2. Hostname: Set to your server hostname (e.g., sms-server)
3. Domain name: Leave blank
4. Root password: Set a strong password (16+ chars)
5. Partitioning: Guided - use entire disk
6. Software selection: Select ONLY "SSH server"
7. GRUB boot loader: Install to master boot record

### 4.3 After Installation

Log in as root with the password you set.


---

## 5. Initial Server Configuration

### Step 5.1. Update the System

```bash
apt update && apt upgrade -y && apt autoremove -y
```

### Step 5.2. Set the Hostname

```bash
hostnamectl set-hostname sms-server
```

Verify: `hostnamectl`

### Step 5.3. Set the Timezone

```bash
timedatectl set-timezone Africa/Nairobi
```

### Step 5.4. Configure Static IP Address

Find your network interface: `ip addr show`

Edit the network config: `nano /etc/network/interfaces`

```
auto lo
iface lo inet loopback

allow-hotplug eth0
iface eth0 inet static
    address 192.168.1.100
    netmask 255.255.255.0
    gateway 192.168.1.1
    dns-nameservers 192.168.1.1 8.8.8.8
```

Replace eth0 with your interface, 192.168.1.100 with your IP, 192.168.1.1 with your gateway.

```bash
systemctl restart networking
```

Verify: `ip addr show eth0`


---

## 6. Server Security

### Step 6.1. Create the Administrative User

```bash
adduser sms_admin
usermod -aG sudo sms_admin
```

Set a strong password when prompted. Verify with: `su - sms_admin -c "sudo whoami"`

Return to root: `exit`

### Step 6.2. Configure SSH Key Authentication

On your LOCAL machine (NOT the server):

```bash
ssh-keygen -t ed25519 -a 100
ssh-copy-id sms_admin@192.168.1.100
```

### Step 6.3. Verify SSH Key Login

Open a SECOND terminal on your local machine:

```bash
ssh sms_admin@192.168.1.100
```

You should log in without a password prompt. Keep this session open.

### Step 6.4. Configure SSH Hardening

In your original root session, edit SSH config:

```bash
nano /etc/ssh/sshd_config
```

Set these values:

```
PermitRootLogin no
PasswordAuthentication no
PermitEmptyPasswords no
PubkeyAuthentication yes
AllowUsers sms_admin
```

### Step 6.5. Restart SSH and Verify

```bash
systemctl restart sshd
```

In your second terminal session, log out and log back in. If successful, close the root session.

### Step 6.6. Configure Automatic Security Updates

```bash
apt install -y unattended-upgrades
dpkg-reconfigure --priority=low unattended-upgrades
```

Select Yes when prompted.

### Step 6.7. Install Fail2ban (Optional)

```bash
apt install -y fail2ban
systemctl enable --now fail2ban
```


---

## 7. Required Software Packages

```bash
apt install -y curl wget git gnupg lsb-release ca-certificates software-properties-common ufw nano htop net-tools dnsutils openssl
```

Verify: `git --version`

---

## 8. Docker Installation

### Step 8.1. Install Prerequisites

```bash
apt install -y ca-certificates curl gnupg
```

### Step 8.2. Add Dockers GPG Key

```bash
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc
```

### Step 8.3. Add Docker APT Repository

Debian 13 "Trixie" uses the trixie repository. Do NOT use bookworm.

```bash
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian trixie stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
```

### Step 8.4. Install Docker Engine

```bash
apt update
apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

### Step 8.5. Start and Enable Docker

```bash
systemctl enable --now docker
```

### Step 8.6. Verify Docker Installation

```bash
docker --version
docker compose version
docker run hello-world
```

Expected: Docker 24+, Compose v2+, hello-world runs successfully.

### Step 8.7. Add User to Docker Group

```bash
usermod -aG docker sms_admin
su - sms_admin
docker ps
exit
```


---

## 9. Docker Configuration

Create Docker daemon configuration:

```bash
mkdir -p /etc/docker
```

Edit `/etc/docker/daemon.json`:

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "100m",
    "max-file": "5"
  },
  "storage-driver": "overlay2",
  "iptables": true
}
```

Restart Docker: `systemctl restart docker`

Verify: `systemctl status docker`

---

## 10. Production Directory Structure

Create directories:

```bash
mkdir -p /opt/sms/{app,data,backups,certs,logs}
chown -R sms_admin:sms_admin /opt/sms
```

Verify: `ls -la /opt/sms`

---

## 11. Repository Deployment

As sms_admin user:

```bash
su - sms_admin
cd /opt/sms
git clone https://github.com/your-org/SchoolManagementSystem.git app
```

Verify key files exist:

```bash
ls /opt/sms/app/docker/docker-compose.prod.yml
ls /opt/sms/app/docker/Dockerfile.api
ls /opt/sms/app/docker/nginx.conf
ls /opt/sms/app/.env.example
```


---

## 12. Production Environment Configuration

Create .env from template:

```bash
cd /opt/sms/app
cp .env.example .env
nano .env
```

Required variables (set these - production fails if missing):
- DB_PASSWORD: PostgreSQL password (generate with openssl rand -base64 24)
- JWT_SECRET: JWT signing key, 64+ chars (openssl rand -base64 64)
- FRONTEND_URL: https://sms-server.school.internal
- ADMIN_EMAIL: admin@school.internal
- ADMIN_PASSWORD: Admin password (openssl rand -base64 24)
- GRAFANA_PASSWORD: Grafana password (openssl rand -base64 24)

Optional variables with safe defaults:
- JWT_ISSUER: SMSAPI
- JWT_AUDIENCE: SMSWeb
- JWT_EXPIRY: 60 (minutes)
- BACKUP_INTERVAL: 86400 (24 hours)
- BACKUP_RETENTION_DAYS: 30
- Swagger__Enabled: false
- ENABLE_MFA: false
- ENABLE_PWA: true
- RATE_LIMIT_PERMIT: 100
- RATE_LIMIT_WINDOW: 60

Secure the .env file:

```bash
chmod 600 /opt/sms/app/.env
```

Verify: `ls -la /opt/sms/app/.env` shows `-rw-------`

---

## 13. Secrets Configuration

Generate production-strength secrets:

```bash
openssl rand -base64 64    # JWT_SECRET
openssl rand -base64 24    # DB_PASSWORD
openssl rand -base64 24    # GRAFANA_PASSWORD
openssl rand -base64 24    # ADMIN_PASSWORD
```

Never commit .env, .key files, .pfx files, or docker/certs/ to Git.

Verify no placeholder values remain:

```bash
cd /opt/sms/app
grep -E "DB_PASSWORD|JWT_SECRET|FRONTEND_URL|ADMIN_EMAIL|ADMIN_PASSWORD|GRAFANA_PASSWORD" .env | grep -v "CHANGE_ME"
```

---

## 14. DNS Configuration

Server hostname: sms-server
LAN domain: school.internal (do NOT use .local - conflicts with mDNS)
FQDN: sms-server.school.internal
App URL: https://sms-server.school.internal

Edit /etc/hosts:

```
127.0.0.1       localhost
192.168.1.100   sms-server.school.internal sms-server
```

On your router/DHCP server, add an A record:
- Name: sms-server
- IP: 192.168.1.100

Verify from a LAN client: `nslookup sms-server.school.internal`


---

## 15. SSL/TLS Configuration

Generate certificates as sms_admin:

```bash
su - sms_admin
cd /opt/sms/certs
```

Generate CA key and certificate:

```bash
openssl genrsa -out ca.key 2048
openssl req -x509 -new -nodes -key ca.key -sha256 -days 3650 -out ca.crt \
    -subj "/C=KE/ST=Nairobi/L=Nairobi/O=SMS/CN=SMS CA"
```

Generate server key:

```bash
openssl genrsa -out server.key 2048
```

Create SAN config (replace values for your server):

```bash
cat > san.cnf << EOF
[req]
distinguished_name = req_distinguished_name
req_extensions = v3_req
prompt = no

[req_distinguished_name]
CN = sms-server.school.internal

[v3_req]
keyUsage = keyEncipherment, dataEncipherment
extendedKeyUsage = serverAuth
subjectAltName = @alt_names

[alt_names]
DNS.1 = sms-server.school.internal
DNS.2 = sms-server
DNS.3 = localhost
IP.1 = 192.168.1.100
IP.2 = 127.0.0.1
EOF
```

Generate server certificate:

```bash
openssl req -new -key server.key -out server.csr -config san.cnf
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key -CAcreateserial \
    -out server.crt -days 3650 -sha256 -extensions v3_req -extfile san.cnf
rm -f server.csr san.cnf
```

Set permissions:

```bash
chmod 644 ca.crt server.crt
chmod 600 ca.key server.key
```

Copy to system SSL directories:

```bash
sudo cp server.crt /etc/ssl/certs/
sudo cp server.key /etc/ssl/private/
sudo cp ca.crt /etc/ssl/certs/
sudo chmod 644 /etc/ssl/certs/server.crt /etc/ssl/certs/ca.crt
sudo chmod 600 /etc/ssl/private/server.key
```

Verify: `openssl verify -CAfile /etc/ssl/certs/ca.crt /etc/ssl/certs/server.crt`


---

## 16. Firewall Configuration

```bash
ufw --force enable
ufw default deny incoming
ufw default allow outgoing
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp
```

Verify: `ufw status verbose`

Only ports 22 (SSH), 80 (HTTP redirect), and 443 (HTTPS) are open.

---

## 17. Docker Compose Validation

```bash
cd /opt/sms/app
```

Verify prerequisites:

```bash
ls -la .env
stat -c "%a %n" .env
ls -la docker/docker-compose.prod.yml
ls -la /etc/ssl/certs/server.crt /etc/ssl/private/server.key
docker info > /dev/null 2>&1 && echo "Docker OK"
```

Validate compose config:

```bash
docker compose -f docker/docker-compose.prod.yml config
```

Expected: Complete resolved config with no errors.

If you see required variable XYZ is not set, check .env.

---

## 18. Production Build

```bash
cd /opt/sms/app
docker compose -f docker/docker-compose.prod.yml build --pull
```

This may take 5-15 minutes.

Verify images:

```bash
docker images | grep -E "sms|nginx|postgres|prom|grafana"
```

Expected images: sms-api, sms-web, sms-backup, nginx:1.27-alpine, postgres:16-alpine, prom/prometheus:v2.54.1, grafana/grafana:11.2.0, prom/alertmanager:v0.27.0, prom/node-exporter:v1.8.2, postgres-exporter:v0.15.0, cadvisor:v0.49.1


---

## 19. Database Initialization

Start PostgreSQL first:

```bash
cd /opt/sms/app
docker compose -f docker/docker-compose.prod.yml up -d postgres
```

Wait for healthy status (10-30 seconds):

```bash
docker inspect --format="{{.State.Health.Status}}" sms-postgres
```

Expected: healthy

Verify initialization:

```bash
docker compose -f docker/docker-compose.prod.yml exec postgres \
    psql -U sms_user -d SchoolManagementSystem -c "SELECT current_database(), current_user, version();"
```

Verify RLS function exists:

```bash
docker compose -f docker/docker-compose.prod.yml exec postgres \
    psql -U sms_user -d SchoolManagementSystem -c "SELECT proname FROM pg_proc WHERE proname = chr(99)||chr(117)||chr(114)||chr(114)||chr(101)||chr(110)||chr(116)||chr(95)||chr(116)||chr(101)||chr(110)||chr(97)||chr(110)||chr(116)||chr(95)||chr(105)||chr(100);"
```

---

## 20. Database Migration and Seeding

Start the API (migrations run automatically on startup):

```bash
docker compose -f docker/docker-compose.prod.yml up -d api
```

Wait for healthy status:

```bash
docker inspect --format="{{.State.Health.Status}}" sms-api
```

Expected: healthy (after 40-60 seconds)

Verify tables exist:

```bash
docker compose -f docker/docker-compose.prod.yml exec postgres \
    psql -U sms_user -d SchoolManagementSystem -c "\dt"
```

Seed the database:

```bash
docker compose -f docker/docker-compose.prod.yml exec api \
    dotnet SMS.API.dll seed-data
```

Expected: Database seeded successfully!

Verify admin account:

```bash
docker compose -f docker/docker-compose.prod.yml exec postgres \
    psql -U sms_user -d SchoolManagementSystem -c "SELECT email FROM ""Users"" WHERE email IS NOT NULL LIMIT 1;"
```


---

## 21. Production Startup

Start all services:

```bash
cd /opt/sms/app
docker compose -f docker/docker-compose.prod.yml up -d
```

Monitor startup (1-2 minutes):

```bash
docker compose -f docker/docker-compose.prod.yml ps
```

Expected: All 11 containers show Up status. Key services show (healthy).

Check health:

```bash
docker ps --format "table {{.Names}}\t{{.Status}}"
```

View logs if needed:

```bash
docker compose -f docker/docker-compose.prod.yml logs nginx --tail=50
docker compose -f docker/docker-compose.prod.yml logs api --tail=50
```

---

## 22. API Verification

Test health endpoint (from server):

```bash
curl --silent --fail http://localhost/health
```

Expected: JSON response with status Healthy.

Test via Nginx HTTPS:

```bash
curl --silent --fail https://localhost/health -k
```

Test from LAN client:

```bash
curl --silent --fail https://sms-server.school.internal/health
```

Test login endpoint:

```bash
curl -X POST https://sms-server.school.internal/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"admin@school.internal\",\"password\":\"YourPassword\"}" -k
```

Replace admin@school.internal and YourPassword with your actual values.


---

## 23. Frontend Verification

Open a browser on any LAN client and navigate to:

```
https://sms-server.school.internal
```

Expected: Login page loads with email/password fields.

Check browser Developer Tools (F12):
- Network tab: No 404 or CORS errors
- Console: No JavaScript errors
- Address bar: HTTPS padlock (if CA cert installed on client)

---

## 24. Authentication and Authorization Verification

Login as administrator using ADMIN_EMAIL and ADMIN_PASSWORD from .env.

Expected results:
- Redirected to dashboard after login
- Administrative features visible
- JWT cookie stored with HttpOnly flag

Role hierarchy:
- Administrator: Full access
- Coordinator: User mgmt, courses, reports
- Lecturer: Units, grades, assignments
- Student: View grades, enrollments
- Receptionist: Accommodation, reports

---

## 25. Multitenancy and RLS Verification

Verify RLS infrastructure:

```bash
docker compose -f docker/docker-compose.prod.yml exec postgres \
    psql -U sms_user -d SchoolManagementSystem -c "\du"
```

Expected: sms_app_role (NOBYPASSRLS), sms_migration_role (BYPASSRLS), sms_readonly_role (NOBYPASSRLS)

Verify tenant isolation:

```bash
docker compose -f docker/docker-compose.prod.yml exec postgres \
    psql -U sms_user -d SchoolManagementSystem -c "\df app.current_tenant_id"
```

Expected: Function definition for current_tenant_id.


---

## 26. Monitoring and Logging

Access monitoring via SSH tunnel (from your workstation):

```bash
ssh -L 9090:localhost:9090 -L 3000:localhost:3000 sms_admin@192.168.1.100
```

Then access:
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (login: admin / your GRAFANA_PASSWORD)

Check Prometheus health:

```bash
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml exec prometheus \
    wget -qO- http://localhost:9090/-/healthy
```

Expected: Prometheus is Healthy.

Check Grafana health:

```bash
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml exec grafana \
    wget -qO- http://localhost:3000/api/health
```

View logs:

```bash
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml logs --tail=100 api
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml logs -f nginx
```


---

## 27. Backup and Restore

Manual backup:

```bash
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml exec backup \
    /scripts/backup.sh
```

Or direct PostgreSQL backup:

```bash
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml exec postgres \
    pg_dump -U sms_user -d SchoolManagementSystem -Fc -f /tmp/$(date +%Y%m%d).dump
docker cp sms-postgres:/tmp/*.dump /opt/sms/backups/
```

List backups:

```bash
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml exec backup \
    ls -la /backups
```

Verify backup integrity:

```bash
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml exec backup \
    pg_restore -l /backups/*.dump | head -5
```

Restore:

```bash
cd /opt/sms/app
docker compose -f docker/docker-compose.prod.yml stop api
docker compose -f docker/docker-compose.prod.yml exec -T postgres \
    pg_restore -U sms_user -d SchoolManagementSystem -Fc -c --if-exists < /backups/latest.dump
docker compose -f docker/docker-compose.prod.yml start api
```

Copy backups off-server (from your workstation):

```bash
scp sms_admin@192.168.1.100:/opt/sms/backups/*.dump /local/backup/directory/
```


---

## 28. Reboot and Automatic Recovery

Ensure Docker auto-starts:

```bash
sudo systemctl is-enabled docker
```

Expected: enabled

Reboot:

```bash
sudo reboot
```

Wait 1-2 minutes, then reconnect:

```bash
ssh sms_admin@192.168.1.100
```

Verify automatic recovery:

```bash
sudo systemctl status docker --no-pager | head -5
docker ps --format "table {{.Names}}\t{{.Status}}"
curl --silent --fail http://localhost/health
```

If all containers are not running, start them:

```bash
cd /opt/sms/app
docker compose -f docker/docker-compose.prod.yml up -d
```

Expected within 2 minutes: All containers Up, health endpoint returns Healthy.

Verify from browser: https://sms-server.school.internal loads the login page.


---

## 29. Troubleshooting

| Symptom | Diagnosis | Fix |
|---------|-----------|-----|
| Docker daemon not running | sudo systemctl status docker | sudo systemctl start docker |
| Docker socket permission denied | ls -la /var/run/docker.sock | sudo usermod -aG docker sms_admin; re-login |
| Compose file not found | pwd; ls docker/docker-compose.prod.yml | cd /opt/sms/app |
| Required var XYZ not set | grep XYZ .env | Edit .env, restart services |
| PostgreSQL not healthy | docker compose logs postgres | Check logs, verify DB_PASSWORD in .env |
| API not healthy | docker compose logs api | Check PostgreSQL is healthy, check env vars |
| Migration failed | docker compose logs api | grep -i migration | docker compose exec api dotnet SMS.API.dll migrate-database |
| CORS error in browser | grep FRONTEND_URL .env | Match .env URL to browser URL exactly |
| Nginx fails to start | docker compose logs nginx | docker compose exec nginx nginx -t |
| TLS/SSL error | ls -la /etc/ssl/certs/server.crt | Check cert exists and permissions are 644 |
| Port conflict | sudo netstat -tlnp | grep -E ":80 |:443 " | sudo fuser -k 80/tcp; sudo fuser -k 443/tcp |
| Disk space full | df -h | docker system prune -af |
| Reboot recovery fails | sudo systemctl status docker | sudo systemctl enable docker; docker compose up -d |


---

## 30. Maintenance

Daily:

```bash
curl --silent --fail https://localhost/health -k
docker ps --format "table {{.Names}}\t{{.Status}}"
df -h /
```

Weekly:

```bash
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml exec backup ls -la /backups
```

Monthly:

```bash
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml pull
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml up -d
docker system prune -af
openssl x509 -in /etc/ssl/certs/server.crt -text -noout | grep -A2 Validity
```

System updates:

```bash
sudo apt update && sudo apt upgrade -y && sudo apt autoremove -y && sudo reboot
```

Application upgrade:

```bash
cd /opt/sms/app
docker compose -f docker/docker-compose.prod.yml exec postgres \
    pg_dump -U sms_user -d SchoolManagementSystem -Fc -f /tmp/pre-upgrade.dump
docker cp sms-postgres:/tmp/pre-upgrade.dump /opt/sms/backups/
git pull
docker compose -f docker/docker-compose.prod.yml build --pull
docker compose -f docker/docker-compose.prod.yml up -d
curl --silent --fail https://localhost/health -k
```


---

## 31. Production Deployment Checklist

### System Setup
- [ ] Debian 13 installed
- [ ] Hostname correct
- [ ] Static IP configured
- [ ] DNS working (nslookup)
- [ ] System updated

### Security
- [ ] Administrative user created (sms_admin)
- [ ] SSH key authentication configured and verified before disabling passwords
- [ ] Root SSH login disabled
- [ ] Password authentication disabled
- [ ] Firewall configured (ports 22, 80, 443 only)
- [ ] Automatic security updates enabled

### Docker
- [ ] Docker Engine 24+ installed (Debian 13 trixie repo)
- [ ] Docker Compose plugin installed
- [ ] User added to docker group
- [ ] Compose config validates without errors

### Application
- [ ] Repository cloned to /opt/sms/app
- [ ] .env configured with all required variables, no CHANGE_ME values
- [ ] SSL/TLS certificates generated and in /etc/ssl/
- [ ] Docker images built successfully
- [ ] PostgreSQL healthy

### Database
- [ ] Migrations completed (auto on API startup)
- [ ] Database seeded (admin account, roles, tenant)
- [ ] RLS infrastructure verified

### Services
- [ ] All 11 containers running and healthy
- [ ] API health endpoint responds
- [ ] Frontend loads in browser
- [ ] HTTPS working

### Monitoring
- [ ] Prometheus scraping targets
- [ ] Grafana accessible via SSH tunnel

### Backup and Resilience
- [ ] Automated backup running
- [ ] Backup integrity verified
- [ ] Server reboot tested
- [ ] All services recovered after reboot

### Final Verification
- [ ] Browser test: Login page loads at https://sms-server.school.internal
- [ ] Browser test: Admin login succeeds
- [ ] Browser test: Dashboard loads with data
- [ ] Browser test: No CORS or HTTPS errors


---

## Appendix A: Service Port Mapping

| Service | Internal Port | Host Port | Externally Accessible |
|---------|--------------|-----------|----------------------|
| Nginx HTTP | 80 | 80 | Yes (redirects to HTTPS) |
| Nginx HTTPS | 443 | 443 | Yes |
| API | 80 | - | No |
| Frontend | 80 | - | No |
| PostgreSQL | 5432 | - | No |
| Prometheus | 9090 | - | No |
| Grafana | 3000 | - | No |
| Alertmanager | 9093 | - | No |
| Node Exporter | 9100 | - | No |
| PostgreSQL Exporter | 9187 | - | No |
| cAdvisor | 8080 | - | No |

## Appendix B: Required Files and Locations

- Application code: /opt/sms/app
- .env: /opt/sms/app/.env (mode 600)
- Compose file: /opt/sms/app/docker/docker-compose.prod.yml
- Nginx config: /opt/sms/app/docker/nginx.conf
- SSL cert: /etc/ssl/certs/server.crt
- SSL key: /etc/ssl/private/server.key
- CA cert: /etc/ssl/certs/ca.crt
- Backups: Docker volume backup_data
- Certs workspace: /opt/sms/certs

## Appendix C: Useful Docker Commands

```bash
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml ps
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml logs [service]
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml logs -f [service]
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml restart [service]
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml down
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml up -d
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml exec [service] [command]
docker compose -f /opt/sms/app/docker/docker-compose.prod.yml build [service]
docker stats
```

---

*End of Debian 13 Server Preparation and Production Deployment Guide*

**Version:** 1.0 | **Last updated:** 24 August 2026

